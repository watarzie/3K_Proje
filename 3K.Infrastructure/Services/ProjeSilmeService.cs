using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// Proje aggregate'ini child-first ve set-based olarak temizler. Global
    /// cascade kurallarını genişletmez; böylece tekil çeki/sandık silme
    /// akışlarının mevcut korumaları değişmez.
    /// </summary>
    public sealed class ProjeSilmeService : IProjeSilmeService
    {
        private const string ReferansButunluguMesaji =
            "İşlem, ilişkili kayıtların veri bütünlüğünü bozacağı için tamamlanamadı. İlişkili kayıtları kontrol edip tekrar deneyin.";

        private readonly AppDbContext _context;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;
        private readonly ISahaAktarimSilmeKorumaService _sahaAktarimSilmeKorumaService;
        private readonly ISseNotifier _sseNotifier;
        private readonly ILogger<ProjeSilmeService> _logger;

        public ProjeSilmeService(
            AppDbContext context,
            ISahaTamamlamaService sahaTamamlamaService,
            ISahaAktarimSilmeKorumaService sahaAktarimSilmeKorumaService,
            ISseNotifier sseNotifier,
            ILogger<ProjeSilmeService> logger)
        {
            _context = context;
            _sahaTamamlamaService = sahaTamamlamaService;
            _sahaAktarimSilmeKorumaService = sahaAktarimSilmeKorumaService;
            _sseNotifier = sseNotifier;
            _logger = logger;
        }

        public async Task<bool> SilAsync(
            int projeId,
            CancellationToken cancellationToken = default)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            try
            {
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(
                        IsolationLevel.ReadCommitted,
                        cancellationToken);

                    // FK eklemeleriyle yarışan bir silme oluşmaması ve proje durumunun
                    // işlem boyunca değişmemesi için kök satırı kilitle.
                    var proje = await _context.Projeler
                        .FromSqlInterpolated(
                            $"SELECT * FROM \"Projeler\" WHERE \"Id\" = {projeId} FOR UPDATE")
                        .SingleOrDefaultAsync(cancellationToken);

                    if (proje == null)
                        return false;

                    var cekiIds = _context.Cekiler
                        .Where(c => c.ProjeId == projeId)
                        .Select(c => c.Id);
                    var cekiSatiriIds = _context.CekiSatirlari
                        .Where(cs => cekiIds.Contains(cs.CekiId))
                        .Select(cs => cs.Id);
                    var sandikIds = _context.Sandiklar
                        .Where(s => s.ProjeId == projeId)
                        .Select(s => s.Id);
                    var sevkiyatIds = _context.Sevkiyatlar
                        .Where(s => s.ProjeId == projeId)
                        .Select(s => s.Id);
                    var revizyonTalepIds = _context.CekiRevizyonTalepleri
                        .Where(t =>
                            t.ProjeId == projeId ||
                            cekiIds.Contains(t.AnaCekiId) ||
                            (t.UygulananRevizyonCekiId.HasValue &&
                             cekiIds.Contains(t.UygulananRevizyonCekiId.Value)))
                        .Select(t => t.Id);

                    var projeCekiSatiriIds = await cekiSatiriIds
                        .ToListAsync(cancellationToken);
                    var projeSandikIds = await sandikIds
                        .ToListAsync(cancellationToken);

                    // Başka projeleri, merkezi stoğu veya fiziksel sevkiyatı etkileyen
                    // işlemler sessizce geri alınmaz. Kullanıcı mevcut domain geri-alma
                    // akışlarını tamamladıktan sonra proje silinebilir.
                    await SilmeEngelleriniDogrulaAsync(
                        proje,
                        projeCekiSatiriIds,
                        projeSandikIds,
                        cancellationToken);

                    // Bir saha projesi siliniyorsa, silinen aktarım kayıtlarının yaşayan
                    // kaynak projelerdeki tamamlanma durumuna etkisini sonradan hesapla.
                    var ledgerKaynakSatirIds = _context.SahaAktarimKalemleri
                        .AsNoTracking()
                        .Where(k => k.SahaProjeId == projeId && k.KaynakProjeId != projeId)
                        .Select(k => k.KaynakCekiSatiriId);
                    var legacyKaynakSatirIds = _context.CekiSatirlari
                        .AsNoTracking()
                        .Where(cs =>
                            cekiIds.Contains(cs.CekiId) &&
                            cs.KaynakCekiSatiriId.HasValue)
                        .Select(cs => cs.KaynakCekiSatiriId!.Value);
                    var senkronizeEdilecekKaynakSatirIds = await ledgerKaynakSatirIds
                        .Union(legacyKaynakSatirIds)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    var hariciSandikIcerigiVar = await _context.SandikIcerikleri
                        .AsNoTracking()
                        .AnyAsync(i =>
                            i.CekiSatiriId.HasValue &&
                            cekiSatiriIds.Contains(i.CekiSatiriId.Value) &&
                            !sandikIds.Contains(i.SandikId),
                            cancellationToken);
                    if (hariciSandikIcerigiVar)
                    {
                        throw new ReferentialIntegrityConflictException(
                            "Projenin çeki satırlarından bazıları başka bir projenin sandık içeriğinde kullanılıyor. Veri kaybını önlemek için ilgili sandık içeriğini kontrol edip tekrar deneyin.");
                    }

                    var silinenAltKayitSayisi = 0;

                    // Bekleyen veya tamamlanmış onayların generic referansları, revizyon
                    // artifact'i silindikten sonra yetim kalmamalıdır.
                    var silinenOnaySayisi = await _context.OnayBekleyenIslemler
                        .Where(o =>
                            o.ProjeId == projeId ||
                            (o.ReferansTipi == OnayReferansTipleri.Proje && o.ReferansId == projeId) ||
                            (o.ReferansTipi == OnayReferansTipleri.Sandik &&
                             o.ReferansId.HasValue && sandikIds.Contains(o.ReferansId.Value)) ||
                            (o.ReferansTipi == OnayReferansTipleri.CekiSatiri &&
                             o.ReferansId.HasValue && cekiSatiriIds.Contains(o.ReferansId.Value)) ||
                            (o.ReferansTipi == OnayReferansTipleri.CekiRevizyonTalebi &&
                             o.ReferansId.HasValue && revizyonTalepIds.Contains(o.ReferansId.Value)))
                        .ExecuteDeleteAsync(cancellationToken);
                    silinenAltKayitSayisi += silinenOnaySayisi;

                    // Çeki yükleme bildirimleri generic Ceki referansı taşır; proje
                    // silindikten sonra bozuk bağlantı bırakmamak için birlikte temizlenir.
                    var silinecekBildirimler = _context.Bildirimler
                        .Where(b =>
                            b.ReferansTipi == BildirimReferansTipleri.Ceki &&
                            b.ReferansId.HasValue &&
                            cekiIds.Contains(b.ReferansId.Value));
                    var bildirimAliciIds = await _context.KullaniciBildirimleri
                        .AsNoTracking()
                        .Where(kb => silinecekBildirimler.Select(b => b.Id).Contains(kb.BildirimId))
                        .Select(kb => kb.KullaniciId)
                        .Distinct()
                        .ToListAsync(cancellationToken);
                    silinenAltKayitSayisi += await silinecekBildirimler
                        .ExecuteDeleteAsync(cancellationToken);

                    // Yeni RESTRICT ilişkiler önce temizlenmelidir.
                    silinenAltKayitSayisi += await _context.CekiRevizyonTalepleri
                        .Where(t => revizyonTalepIds.Contains(t.Id))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.SahaAktarimKalemleri
                        .Where(k =>
                            k.KaynakProjeId == projeId ||
                            k.SahaProjeId == projeId ||
                            cekiSatiriIds.Contains(k.KaynakCekiSatiriId) ||
                            (k.SahaCekiSatiriId.HasValue &&
                             cekiSatiriIds.Contains(k.SahaCekiSatiriId.Value)) ||
                            (k.KaynakSandikId.HasValue && sandikIds.Contains(k.KaynakSandikId.Value)) ||
                            (k.SahaSandikId.HasValue && sandikIds.Contains(k.SahaSandikId.Value)))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.SahaAktarimlari
                        .Where(a => a.SahaProjeId == projeId || a.KaynakProjeId == projeId)
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.SandikUrunTransferleri
                        // Transfer kaydının sahibi ProjeId'dir. Başka bir projeye ait
                        // snapshot/audit kaydı varsa opsiyonel dış referansları DB'deki
                        // SET NULL kuralları koparsın; yaşayan projenin geçmişini silme.
                        .Where(t => t.ProjeId == projeId)
                        .ExecuteDeleteAsync(cancellationToken);

                    // Mevcut RESTRICT bağımlılıkları set-based temizle.
                    silinenAltKayitSayisi += await _context.StokHareketleri
                        .Where(h => h.ProjeId == projeId || cekiSatiriIds.Contains(h.CekiSatiriId))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.HareketGecmisleri
                        .Where(h => h.ProjeId == projeId)
                        .ExecuteDeleteAsync(cancellationToken);

                    // Arşiv tablosunda FK yoktur; ancak tam proje silmede yetim proje
                    // geçmişi bırakmamak mevcut "tüm alt veriler" sözleşmesinin parçasıdır.
                    silinenAltKayitSayisi += await _context.HareketGecmisleriArsiv
                        .Where(h => h.ProjeId == projeId)
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.ProjeTransferleri
                        .Where(t =>
                            t.KaynakProjeId == projeId ||
                            t.HedefProjeId == projeId ||
                            cekiSatiriIds.Contains(t.KaynakCekiSatiriId) ||
                            (t.HedefCekiSatiriId.HasValue &&
                             cekiSatiriIds.Contains(t.HedefCekiSatiriId.Value)))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.SevkiyatSandiklari
                        .Where(ss =>
                            sevkiyatIds.Contains(ss.SevkiyatId) ||
                            sandikIds.Contains(ss.SandikId))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.SandikIcerikleri
                        .Where(i => sandikIds.Contains(i.SandikId))
                        .ExecuteDeleteAsync(cancellationToken);

                    silinenAltKayitSayisi += await _context.Sandiklar
                        .Where(s => s.ProjeId == projeId)
                        .ExecuteDeleteAsync(cancellationToken);

                    // Kök silme SaveChanges üzerinden kalır: Audit/ProjectLock gibi
                    // mevcut interceptor semantiği korunur. Ceki ve Sevkiyat zincirleri
                    // kendi tanımlı cascade ilişkileriyle burada silinir.
                    _context.Projeler.Remove(proje);
                    await _context.SaveChangesAsync(cancellationToken);

                    // Legacy saha kayıtları da kaynak satır ilişkisi üzerinden
                    // hesaplandığından senkronizasyon, silinen projenin çeki zinciri
                    // veritabanından kaldırıldıktan sonra yapılmalıdır.
                    if (senkronizeEdilecekKaynakSatirIds.Count > 0)
                    {
                        await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(
                            senkronizeEdilecekKaynakSatirIds,
                            cancellationToken);
                    }

                    // Commit başladıktan sonra request iptali sonucu belirsiz bırakmamalı.
                    await transaction.CommitAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "Proje {ProjeId} bağımlılıklarıyla silindi. Silinen alt kayıt: {SilinenAltKayitSayisi}",
                        projeId,
                        silinenAltKayitSayisi);

                    try
                    {
                        if (silinenOnaySayisi > 0)
                            await _sseNotifier.BroadcastApprovalUpdateAsync();

                        if (bildirimAliciIds.Count > 0)
                        {
                            await _sseNotifier.NotifyUsersAsync(
                                bildirimAliciIds,
                                SseOlaylari.BildirimGuncellendi);
                        }
                    }
                    catch (Exception exception)
                    {
                        // Veritabanı commit'i tamamlandı; geçici SSE hatası başarılı
                        // silme işlemini istemciye başarısız göstermemelidir.
                        _logger.LogWarning(
                            exception,
                            "Proje {ProjeId} silindikten sonra sayaç yenileme sinyali gönderilemedi.",
                            projeId);
                    }

                    return true;
                });
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new ConcurrencyConflictException(
                    "Proje silme sırasında başka bir işlem tarafından değiştirildi.",
                    exception);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException postgresException &&
                      GeciciEszamanlilikHatasiMi(postgresException))
            {
                throw new ConcurrencyConflictException(
                    "Proje silme başka bir işlemle çakıştı. Lütfen işlemi tekrar deneyin.",
                    exception);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.ForeignKeyViolation
                } postgresException)
            {
                ReferansButunluguHatasiniLogla(postgresException, exception);
                throw new ReferentialIntegrityConflictException(ReferansButunluguMesaji, exception);
            }
            catch (PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                ReferansButunluguHatasiniLogla(exception, exception);
                throw new ReferentialIntegrityConflictException(ReferansButunluguMesaji, exception);
            }
            catch (PostgresException exception)
                when (GeciciEszamanlilikHatasiMi(exception))
            {
                throw new ConcurrencyConflictException(
                    "Proje silme başka bir işlemle çakıştı. Lütfen işlemi tekrar deneyin.",
                    exception);
            }
        }

        private async Task SilmeEngelleriniDogrulaAsync(
            Proje proje,
            List<int> projeCekiSatiriIds,
            List<int> projeSandikIds,
            CancellationToken cancellationToken)
        {
            var engeller = new List<string>();

            var aktifSahaAktarimSayisi = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal &&
                    (
                        k.KaynakProjeId == proje.Id ||
                        k.SahaProjeId == proje.Id ||
                        projeCekiSatiriIds.Contains(k.KaynakCekiSatiriId) ||
                        (k.SahaCekiSatiriId.HasValue &&
                         projeCekiSatiriIds.Contains(k.SahaCekiSatiriId.Value)) ||
                        (k.KaynakSandikId.HasValue &&
                         projeSandikIds.Contains(k.KaynakSandikId.Value)) ||
                        (k.SahaSandikId.HasValue &&
                         projeSandikIds.Contains(k.SahaSandikId.Value))
                    ))
                .Select(k => k.SahaAktarimId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Defter altyapısından önce oluşturulmuş saha satırlarını ve taşınmış
            // içeriklerin güncel sandıklarını da aynı ilk kontrolde yakala.
            var sahaBagliSatirIds =
                await _sahaAktarimSilmeKorumaService
                    .GetAktifAktarimBagliCekiSatiriIdsAsync(
                        projeCekiSatiriIds,
                        cancellationToken);
            var sahaBagliSandikIds =
                await _sahaAktarimSilmeKorumaService
                    .GetAktifAktarimBagliSandikIdsAsync(
                        projeSandikIds,
                        cancellationToken);

            if (aktifSahaAktarimSayisi > 0 ||
                sahaBagliSatirIds.Count > 0 ||
                sahaBagliSandikIds.Count > 0)
            {
                engeller.Add("aktif saha aktarımı");
            }

            var aktifProjeTransferSayisi = await _context.ProjeTransferleri
                .AsNoTracking()
                .Where(t =>
                    t.DurumId == (int)ProjeTransferDurum.Aktif &&
                    (
                        t.KaynakProjeId == proje.Id ||
                        t.HedefProjeId == proje.Id ||
                        projeCekiSatiriIds.Contains(t.KaynakCekiSatiriId) ||
                        (t.HedefCekiSatiriId.HasValue &&
                         projeCekiSatiriIds.Contains(t.HedefCekiSatiriId.Value))
                    ))
                .CountAsync(cancellationToken);
            if (aktifProjeTransferSayisi > 0)
            {
                engeller.Add(
                    $"{aktifProjeTransferSayisi} aktif proje/FB transferi");
            }

            var stokHareketSayisi = await _context.StokHareketleri
                .AsNoTracking()
                .CountAsync(h =>
                    h.ProjeId == proje.Id ||
                    projeCekiSatiriIds.Contains(h.CekiSatiriId),
                    cancellationToken);
            if (stokHareketSayisi > 0)
                engeller.Add($"{stokHareketSayisi} stok hareketi");

            var sevkEdilmisSandikSayisi = await _context.Sandiklar
                .AsNoTracking()
                .CountAsync(s =>
                    s.ProjeId == proje.Id &&
                    s.DurumId == (int)SandikDurum.Sevkedildi,
                    cancellationToken);

            var bagliSevkiyatSayisi = await _context.SevkiyatSandiklari
                .AsNoTracking()
                .Where(ss =>
                    ss.Sevkiyat.ProjeId == proje.Id ||
                    ss.Sandik.ProjeId == proje.Id)
                .Select(ss => ss.SevkiyatId)
                .Distinct()
                .CountAsync(cancellationToken);

            var sevkiyatEngeliVar =
                proje.DurumId is
                    (int)ProjeDurum.SevkEdildi or
                    (int)ProjeDurum.EksikSevkEdildi ||
                sevkEdilmisSandikSayisi > 0 ||
                bagliSevkiyatSayisi > 0;

            if (sevkiyatEngeliVar)
            {
                var sevkiyatDetaylari = new List<string>();
                if (bagliSevkiyatSayisi > 0)
                    sevkiyatDetaylari.Add($"{bagliSevkiyatSayisi} sevkiyat");
                if (sevkEdilmisSandikSayisi > 0)
                {
                    sevkiyatDetaylari.Add(
                        $"{sevkEdilmisSandikSayisi} sevk edilmiş sandık");
                }

                engeller.Add(
                    sevkiyatDetaylari.Count > 0
                        ? string.Join(", ", sevkiyatDetaylari)
                        : "kısmi/tam sevkiyat durumu");
            }

            if (engeller.Count == 0)
                return;

            var engelMetni = string.Join("; ", engeller);
            _logger.LogInformation(
                "Proje {ProjeId} silme işlemi geri alınması gereken operasyonlar nedeniyle engellendi: {Engeller}",
                proje.Id,
                engelMetni);

            throw new ReferentialIntegrityConflictException(
                $"Proje silinemez. Silmeden önce şu işlemleri geri almalısınız: {engelMetni}. İlgili ekranlardan geri alıp tekrar deneyin.");
        }

        private static bool GeciciEszamanlilikHatasiMi(PostgresException exception)
        {
            return exception.SqlState is
                PostgresErrorCodes.DeadlockDetected or
                PostgresErrorCodes.SerializationFailure;
        }

        private void ReferansButunluguHatasiniLogla(
            PostgresException postgresException,
            Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Proje silme referans bütünlüğü kuralına takıldı. Şema: {SchemaName}, Tablo: {TableName}, Constraint: {ConstraintName}",
                postgresException.SchemaName,
                postgresException.TableName,
                postgresException.ConstraintName);
        }
    }
}
