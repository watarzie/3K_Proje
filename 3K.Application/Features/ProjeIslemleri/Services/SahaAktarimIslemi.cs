using System.Globalization;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.ProjeIslemleri.Services;

/// <summary>Uygulanan saha aktarımının sonucu: hedef saha projesi ve yazılan aktarım başlığı.</summary>
internal sealed record SahaAktarimUygulamaSonucu(ProjeDto Proje, int SahaAktarimId, int SatirSayisi, decimal ToplamAdet);

/// <summary>
/// Normal projeden saha projesine aktarımın ortak motoru. Elle ürün bazlı ("Eksiklerden oluştur"), elle sandık bazlı
/// ("Sandıkları sahaya aktar") ve otomatik aktarım aynı doğrulama ve yazma kurallarını kullanır. Çağıranın
/// transaction'ında çalışır; ret döndüğünde çağıran (<c>ExecuteAtomicAsync</c>) yazılanları geri alır.
/// </summary>
internal sealed class SahaAktarimIslemi(
    IUnitOfWork unitOfWork,
    IHareketService hareketService,
    ILookupCacheService lookupCache,
    ICurrentUserService currentUserService,
    ISahaTamamlamaService sahaTamamlamaService)
{
    public async Task<Result<SahaAktarimUygulamaSonucu>> UygulaAsync(
        EksiklerdenSahaProjesiOlusturCommand request,
        CancellationToken cancellationToken)
    {
        var projeRepo = unitOfWork.GetRepository<Proje>();
        var sandikTaslaklari = request.Sandiklar
            .Where(s => s.Urunler.Any(u => u.CekiSatiriId > 0 && u.Miktar > 0))
            .Select(s => new EksikSahaSandikDto
            {
                HedefSandikId = s.HedefSandikId,
                SandikNo = NormalizeSandikNo(s.SandikNo),
                SandikIsmi = string.IsNullOrWhiteSpace(s.SandikIsmi) ? null : s.SandikIsmi.Trim(),
                En = s.En,
                Boy = s.Boy,
                Yukseklik = s.Yukseklik,
                NetKg = s.NetKg,
                GrossKg = s.GrossKg,
                Urunler = s.Urunler
                    .Where(u => u.CekiSatiriId > 0 && u.Miktar > 0)
                    .Select(u => new EksikSahaUrunDto
                    {
                        CekiSatiriId = u.CekiSatiriId,
                        KaynakProjeId = u.KaynakProjeId,
                        KaynakSandikId = u.KaynakSandikId,
                        Miktar = u.Miktar,
                        Aciklama = string.IsNullOrWhiteSpace(u.Aciklama) ? null : u.Aciklama.Trim()
                    })
                    .ToList()
            })
            .ToList();

        if (sandikTaslaklari.Count == 0)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Saha projesi oluşturmak için en az bir sandığa ürün eklenmelidir.");

        var bosSandikNoVar = sandikTaslaklari.Any(s => string.IsNullOrWhiteSpace(s.SandikNo));
        if (bosSandikNoVar)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Tüm sandıkların sandık numarası olmalıdır.");

        var hedefSandikIdleri = sandikTaslaklari
            .Where(s => s.HedefSandikId.HasValue)
            .Select(s => s.HedefSandikId!.Value)
            .Distinct()
            .ToList();

        if (hedefSandikIdleri.Count > 0 && !request.HedefSahaProjeId.HasValue)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Mevcut sandığa ekleme yalnızca mevcut saha projesi seçildiğinde yapılabilir.");

        var tekrarEdenSandikNo = sandikTaslaklari
            .GroupBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1)?.Key;

        if (!string.IsNullOrWhiteSpace(tekrarEdenSandikNo))
            return Result<SahaAktarimUygulamaSonucu>.Failure($"'{tekrarEdenSandikNo}' numaralı sandık birden fazla kez kullanılamaz.");

        var gecersizOlcu = sandikTaslaklari.Any(s =>
            IsNegative(s.En) ||
            IsNegative(s.Boy) ||
            IsNegative(s.Yukseklik) ||
            IsNegative(s.NetKg) ||
            IsNegative(s.GrossKg));

        if (gecersizOlcu)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Sandık ebat ve ağırlık değerleri negatif olamaz.");

        // Ana projenin "1" numaralı sandığı hiçbir giriş yolundan sandık olarak aktarılamaz. Kural istemcinin bildirdiği
        // sandığa güvenmez: sandık bazlı üründe kaynak sandık zorunludur ve ürünün o sandıkta gerçek tahsisi olmalıdır.
        var sandikBazliUrunler = sandikTaslaklari
            .SelectMany(s => s.Urunler)
            .Where(u => IsSandikBazliAktarim(u.Aciklama))
            .ToList();

        if (sandikBazliUrunler.Count > 0)
        {
            if (sandikBazliUrunler.Any(u => !u.KaynakSandikId.HasValue))
                return Result<SahaAktarimUygulamaSonucu>.Failure("Sandık bazlı aktarımda kaynak sandık belirtilmelidir.");

            var sandikBazliKaynakSandikIds = sandikBazliUrunler.Select(u => u.KaynakSandikId!.Value).Distinct().ToList();
            var sandikBazliSatirIds = sandikBazliUrunler.Select(u => u.CekiSatiriId).Distinct().ToList();
            var kaynakSandiklar = (await unitOfWork.GetRepository<Sandik>()
                    .FindAsync(s => sandikBazliKaynakSandikIds.Contains(s.Id)))
                .ToDictionary(s => s.Id);
            var kaynakTahsisleri = (await unitOfWork.GetRepository<SandikIcerik>().FindAsync(i =>
                    i.CekiSatiriId.HasValue &&
                    sandikBazliSatirIds.Contains(i.CekiSatiriId.Value) &&
                    sandikBazliKaynakSandikIds.Contains(i.SandikId)))
                .Select(i => (i.SandikId, CekiSatiriId: i.CekiSatiriId!.Value))
                .ToHashSet();

            if (sandikBazliUrunler.Any(u =>
                    !kaynakSandiklar.ContainsKey(u.KaynakSandikId!.Value) ||
                    !kaynakTahsisleri.Contains((u.KaynakSandikId.Value, u.CekiSatiriId))))
            {
                return Result<SahaAktarimUygulamaSonucu>.Failure(
                    "Sandık bazlı aktarımdaki ürünlerden biri belirtilen kaynak sandıkta değil.",
                    409);
            }

            if (kaynakSandiklar.Values.Any(s => SahaAktarimSandikKurali.BirNumaraliSandikMi(s.SandikNo)))
            {
                return Result<SahaAktarimUygulamaSonucu>.Failure(
                    "Ana projenin 1 numaralı sandığı sahaya sandık olarak aktarılamaz.",
                    409);
            }
        }

        var talepGruplari = sandikTaslaklari
            .SelectMany(s => s.Urunler)
            .GroupBy(u => u.CekiSatiriId)
            .ToDictionary(g => g.Key, g => g.Sum(u => u.Miktar));
        var talepCekiSatiriIds = talepGruplari.Keys.ToList();

        var cekiSatiriRepo = unitOfWork.GetRepository<CekiSatiri>();
        var kaynakSatirlar = (await cekiSatiriRepo.FindAsync(cs =>
                talepCekiSatiriIds.Contains(cs.Id) &&
                !cs.KaynakCekiSatiriId.HasValue))
            .ToDictionary(cs => cs.Id);

        if (kaynakSatirlar.Count != talepGruplari.Count)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Seçilen ürünlerden bazıları bulunamadı veya eksik tamamlama satırı.");

        var cekiRepo = unitOfWork.GetRepository<Ceki>();
        var kaynakCekiIdler = kaynakSatirlar.Values.Select(s => s.CekiId).Distinct().ToList();
        var kaynakCekiler = (await cekiRepo.FindAsync(c => kaynakCekiIdler.Contains(c.Id)))
            .ToDictionary(c => c.Id);

        var kaynakProjeIdler = kaynakCekiler.Values.Select(c => c.ProjeId).Distinct().ToList();
        var kaynakProjeler = (await projeRepo.FindAsync(p => kaynakProjeIdler.Contains(p.Id)))
            .ToDictionary(p => p.Id);

        if (kaynakCekiler.Count != kaynakCekiIdler.Count || kaynakProjeler.Count != kaynakProjeIdler.Count)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Seçilen ürünlerin kaynak proje bilgisi bulunamadı.");

        if (kaynakProjeler.Values.Any(p => p.ProjeTipiId != (int)ProjeTipi.Normal))
            return Result<SahaAktarimUygulamaSonucu>.Failure("Eksiklerden saha projesi sadece normal projelerden oluşturulabilir.");

        var varsayilanKaynakProje = request.KaynakProjeId.HasValue && kaynakProjeler.TryGetValue(request.KaynakProjeId.Value, out var seciliKaynakProje)
            ? seciliKaynakProje
            : kaynakProjeler.Values.OrderBy(p => p.ProjeNo).FirstOrDefault();

        if (varsayilanKaynakProje == null)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Kaynak proje bulunamadı.", 404);

        var kaynakProjeNolari = kaynakProjeler.Values
            .OrderBy(p => p.ProjeNo)
            .Select(p => p.ProjeNo)
            .ToList();

        var mevcutTamamlamaSatirlari = (await cekiSatiriRepo.FindAsync(cs =>
                cs.KaynakCekiSatiriId.HasValue &&
                talepCekiSatiriIds.Contains(cs.KaynakCekiSatiriId.Value)))
            .ToList();

        var mevcutPlanMap = mevcutTamamlamaSatirlari
            .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(cs => cs.IstenenAdet));

        foreach (var (cekiSatiriId, talepMiktari) in talepGruplari)
        {
            var kaynakSatir = kaynakSatirlar[cekiSatiriId];
            var planlanmis = mevcutPlanMap.GetValueOrDefault(cekiSatiriId);
            var kalanPlanlanabilir = Math.Max(kaynakSatir.KalanMiktar - planlanmis, 0);

            if (talepMiktari > kalanPlanlanabilir)
                return Result<SahaAktarimUygulamaSonucu>.Failure($"'{kaynakSatir.Aciklama}' için istenen miktar kalan eksik adetten büyük. Kalan: {FormatAdet(kalanPlanlanabilir)}");
        }

        Proje? hedefSahaProje = null;
        if (request.HedefSahaProjeId.HasValue)
        {
            hedefSahaProje = await projeRepo.GetByIdAsync(request.HedefSahaProjeId.Value);
            if (hedefSahaProje == null)
                return Result<SahaAktarimUygulamaSonucu>.Failure("Hedef saha projesi bulunamadı.", 404);

            if (hedefSahaProje.ProjeTipiId != (int)ProjeTipi.Saha)
                return Result<SahaAktarimUygulamaSonucu>.Failure("Aktarım sadece saha projelerine eklenebilir.");

            if (hedefSahaProje.DurumId == (int)ProjeDurum.SevkEdildi ||
                hedefSahaProje.DurumId == (int)ProjeDurum.EksikSevkEdildi)
                return Result<SahaAktarimUygulamaSonucu>.Failure("Sevk edilmiş veya kısmi sevk edilmiş saha projesine yeni aktarım eklenemez.");

            if (hedefSahaProje.DurumId == (int)ProjeDurum.Tamamlandi)
            {
                hedefSahaProje.DurumId = (int)ProjeDurum.Hazirlaniyor;
                projeRepo.Update(hedefSahaProje);
            }
        }

        var projeNoTalebi = string.IsNullOrWhiteSpace(request.ProjeNo) ? null : request.ProjeNo.Trim();
        if (hedefSahaProje == null && !string.IsNullOrWhiteSpace(projeNoTalebi))
        {
            var projeNoKullanimda = (await projeRepo.FindAsync(p => p.ProjeNo == projeNoTalebi)).Any();
            if (projeNoKullanimda)
                return Result<SahaAktarimUygulamaSonucu>.Failure($"'{projeNoTalebi}' proje numarası zaten kullanılıyor.");
        }

        var tekKaynakMi = kaynakProjeler.Count == 1;
        var yeniProjeOlusturuldu = hedefSahaProje == null;
        var yeniProje = hedefSahaProje;
        if (yeniProje == null)
        {
            var baseSahaProjeNo = tekKaynakMi
                ? $"{varsayilanKaynakProje.ProjeNo}-SAHA"
                : $"SAHA-{TurkeyTime.Now:yyyyMMdd}";
            var sahaProjeNo = projeNoTalebi ?? await GenerateSahaProjeNoAsync(baseSahaProjeNo);
            yeniProje = new Proje
            {
                ProjeNo = sahaProjeNo,
                Musteri = string.IsNullOrWhiteSpace(request.Musteri)
                    ? (tekKaynakMi ? varsayilanKaynakProje.Musteri : "Çoklu Proje")
                    : request.Musteri.Trim(),
                DurumId = (int)ProjeDurum.Hazirlaniyor,
                ProjeTipiId = (int)ProjeTipi.Saha,
                PlanlananSevkTarihi = tekKaynakMi ? varsayilanKaynakProje.PlanlananSevkTarihi : null,
                SorumluKisi = tekKaynakMi ? varsayilanKaynakProje.SorumluKisi : string.Empty,
                Lokasyon = string.IsNullOrWhiteSpace(request.Lokasyon)
                    ? (tekKaynakMi ? varsayilanKaynakProje.Lokasyon : null)
                    : request.Lokasyon.Trim(),
                FBNo = tekKaynakMi ? varsayilanKaynakProje.FBNo : null,
                Guc = tekKaynakMi ? varsayilanKaynakProje.Guc : null,
                Gerilim = tekKaynakMi ? varsayilanKaynakProje.Gerilim : null,
                OlcuResmiNo = tekKaynakMi ? varsayilanKaynakProje.OlcuResmiNo : null,
                NakilOlcuResmiNo = tekKaynakMi ? varsayilanKaynakProje.NakilOlcuResmiNo : null,
                SonMontajResmiNo = tekKaynakMi ? varsayilanKaynakProje.SonMontajResmiNo : null,
                ProjeMuduru = tekKaynakMi ? varsayilanKaynakProje.ProjeMuduru : null
            };

            await projeRepo.AddAsync(yeniProje);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var kaynakCekiId = kaynakCekiIdler.Count == 1 ? kaynakCekiIdler[0] : (int?)null;
        Ceki? sahaCeki = null;
        if (!yeniProjeOlusturuldu)
        {
            sahaCeki = (await cekiRepo.FindAsync(c =>
                    c.ProjeId == yeniProje.Id &&
                    c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
                .OrderBy(c => c.Id)
                .FirstOrDefault();
        }

        if (sahaCeki == null)
        {
            var mevcutTamamlamaNo = (await cekiRepo.FindAsync(c =>
                    c.ProjeId == yeniProje.Id &&
                    c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
                .Select(c => c.TamamlamaNo ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            sahaCeki = new Ceki
            {
                ProjeId = yeniProje.Id,
                OrijinalDosyaYolu = string.Empty,
                YuklemeTarihi = TurkeyTime.Now,
                CekiTipiId = (int)CekiTipi.EksikTamamlama,
                KaynakCekiId = kaynakCekiId,
                TamamlamaNo = mevcutTamamlamaNo + 1,
                Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                    ? $"Kaynak projeler: {string.Join(", ", kaynakProjeNolari)}"
                    : request.Aciklama.Trim()
            };

            await cekiRepo.AddAsync(sahaCeki);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var sandikRepo = unitOfWork.GetRepository<Sandik>();
        var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
        var sahaAktarimRepo = unitOfWork.GetRepository<SahaAktarim>();
        var sahaAktarimKalemiRepo = unitOfWork.GetRepository<SahaAktarimKalemi>();
        var mevcutSahaSatirlari = (await cekiSatiriRepo.FindAsync(cs => cs.CekiId == sahaCeki.Id)).ToList();
        var siraNo = mevcutSahaSatirlari.Any() ? mevcutSahaSatirlari.Max(cs => cs.SiraNo) + 1 : 1;
        var mevcutSahaSandiklari = (await sandikRepo.FindAsync(s => s.ProjeId == yeniProje.Id)).ToList();
        // Çakışma normal biçimle aranır ("05" ile "5" aynı sandıktır); yeni sandık girilen numarayla açılır.
        var mevcutSandikNolari = mevcutSahaSandiklari
            .Select(s => KoliNoKurali.Normalize(s.SandikNo))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hedefSandikMap = mevcutSahaSandiklari
            .Where(s => hedefSandikIdleri.Contains(s.Id))
            .ToDictionary(s => s.Id);

        if (hedefSandikIdleri.Count != hedefSandikMap.Count)
            return Result<SahaAktarimUygulamaSonucu>.Failure("Seçilen hedef sandıklardan bazıları saha projesi altında bulunamadı.");

        // Taşımayla aynı kural: sevk edilmiş ya da sevkiyat düzeltmesi açık sandığa ürün eklenmez.
        if (hedefSandikMap.Values.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi || s.SevkiyatDuzeltmeAcikMi))
            return Result<SahaAktarimUygulamaSonucu>.Failure("Sevk edilmiş ya da sevkiyat düzeltmesindeki saha sandığına yeni ürün eklenemez.");

        // Yeni sandığın numarası sessizce değiştirilmez (28.10.2026; eskiden "2-2" eki açılır, ekranda ikinci "2" görünürdü):
        // hedefte ya da aynı aktarımda aynı numara (çeki biçiminde) varsa 409 döner, numara verilmemişse sıradaki numara
        // (en büyük sayısal + 1) atanır. Numarayı kaynak sandıktan türeten akışlar (sandık bazlı ve otomatik aktarım)
        // dolu numarayı buraya göndermeden önce KoliNoKurali.BosNumaraYaDaSiradaki ile çözer. Yazmadan önce denetlenir.
        foreach (var taslak in sandikTaslaklari.Where(s => !s.HedefSandikId.HasValue))
        {
            if (string.IsNullOrWhiteSpace(taslak.SandikNo))
            {
                taslak.SandikNo = KoliNoKurali.BosNumaraYaDaSiradaki(null, mevcutSandikNolari);
                continue;
            }
            if (!mevcutSandikNolari.Add(KoliNoKurali.Normalize(taslak.SandikNo)))
                return Result<SahaAktarimUygulamaSonucu>.Failure(
                    $"Hedef saha projesinde {taslak.SandikNo} numaralı sandık zaten var; o sandığı seçin ya da başka bir numara verin.", 409);
        }

        var toplamSatir = 0;
        var toplamAdet = 0m;
        var sahaAktarim = new SahaAktarim
        {
            SahaProjeId = yeniProje.Id,
            KaynakProjeId = kaynakProjeler.Count == 1 ? kaynakProjeler.Keys.First() : null,
            KullaniciId = currentUserService.UserId ?? 0,
            Aciklama = string.IsNullOrWhiteSpace(request.Aciklama)
                ? $"Kaynak projeler: {string.Join(", ", kaynakProjeNolari)}"
                : request.Aciklama.Trim()
        };

        await sahaAktarimRepo.AddAsync(sahaAktarim);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var sandikTaslak in sandikTaslaklari)
        {
            Sandik sandik;
            if (sandikTaslak.HedefSandikId.HasValue)
            {
                sandik = hedefSandikMap[sandikTaslak.HedefSandikId.Value];
                // Geri alınan aktarımın boş bıraktığı sandık içerik alınca hazırlanır. Kapatılmış sandık da yeniden
                // açılır (proje içi taşımayla aynı kural): içeriği değişti, yeniden kontrol edilmeden sevk edilemez.
                var eskiDurumId = sandik.DurumId;
                if (eskiDurumId is (int)SandikDurum.Bos or (int)SandikDurum.Kapandi)
                    sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                // Her zaman UPDATE: xmin ile, okumadan sonra başlayan bir sevkiyat aktarımla sessizce yarışamaz.
                sandikRepo.Update(sandik);
                if (eskiDurumId == (int)SandikDurum.Kapandi)
                    await SandikGeriAcildiKaydetAsync(hareketService, yeniProje.Id, sandik, currentUserService.UserId ?? 0);
            }
            else
            {
                var sandikNo = sandikTaslak.SandikNo;
                sandik = new Sandik
                {
                    ProjeId = yeniProje.Id,
                    SandikNo = sandikNo,
                    Ad = sandikTaslak.SandikIsmi,
                    En = sandikTaslak.En,
                    Boy = sandikTaslak.Boy,
                    Yukseklik = sandikTaslak.Yukseklik,
                    NetKg = sandikTaslak.NetKg,
                    GrossKg = sandikTaslak.GrossKg,
                    TipId = (int)SandikTipi.AhsapKapali,
                    DurumId = (int)SandikDurum.Hazirlaniyor,
                    DepoLokasyonId = (int)DepoLokasyon.Belirsiz
                };

                await sandikRepo.AddAsync(sandik);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            foreach (var urunTaslak in sandikTaslak.Urunler)
            {
                var kaynakSatir = kaynakSatirlar[urunTaslak.CekiSatiriId];
                var satirKaynakCeki = kaynakCekiler[kaynakSatir.CekiId];
                var satirKaynakProje = kaynakProjeler[satirKaynakCeki.ProjeId];
                var yeniSatir = new CekiSatiri
                {
                    CekiId = sahaCeki.Id,
                    KaynakCekiSatiriId = kaynakSatir.Id,
                    SiraNo = siraNo++,
                    OlcuResmiPozNo = kaynakSatir.OlcuResmiPozNo,
                    BarkodNo = kaynakSatir.BarkodNo,
                    Aciklama = kaynakSatir.Aciklama,
                    IstenenAdet = urunTaslak.Miktar,
                    BirimId = kaynakSatir.BirimId,
                    CekideGecenSandikNo = sandik.SandikNo,
                    FiiliSandikNo = sandik.SandikNo,
                    Remarks = kaynakSatir.Remarks,
                    DurumId = (int)UrunDurum.Bekliyor,
                    GridDurumuId = (int)GridDurum.Gelmedi,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                    UcKDurumuId = (int)UcKDurum.Bekliyor,
                    UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                    KaynakHedefProjeNo = satirKaynakProje.ProjeNo,
                    UcKAciklama = urunTaslak.Aciklama,
                    IsManuelEklenen = true,
                    EklemeNedeni = "Eksik saha tamamlama"
                };

                await cekiSatiriRepo.AddAsync(yeniSatir);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                var yeniIcerik = new SandikIcerik
                {
                    SandikId = sandik.Id,
                    CekiSatiriId = yeniSatir.Id,
                    TahsisMiktari = urunTaslak.Miktar,
                    KonulanAdet = 0,
                    EksikAdet = 0,
                    KaynakProjeNo = satirKaynakProje.ProjeNo,
                    Aciklama = urunTaslak.Aciklama
                };

                await icerikRepo.AddAsync(yeniIcerik);

                await sahaAktarimKalemiRepo.AddAsync(new SahaAktarimKalemi
                {
                    SahaAktarimId = sahaAktarim.Id,
                    KaynakProjeId = satirKaynakProje.Id,
                    SahaProjeId = yeniProje.Id,
                    KaynakCekiSatiriId = kaynakSatir.Id,
                    SahaCekiSatiriId = yeniSatir.Id,
                    KaynakSandikId = urunTaslak.KaynakSandikId,
                    SahaSandikId = sandik.Id,
                    Miktar = urunTaslak.Miktar,
                    AktarimTipiId = IsSandikBazliAktarim(urunTaslak.Aciklama)
                        ? (int)SahaAktarimTipi.SandikBazli
                        : (int)SahaAktarimTipi.UrunBazli,
                    DurumId = (int)SahaAktarimDurum.Planlandi,
                    SevkiyatKapsamindaMi = false,
                    DuzeltmeyeAcikMi = false,
                    Aciklama = urunTaslak.Aciklama
                });

                toplamSatir++;
                toplamAdet += urunTaslak.Miktar;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await sahaTamamlamaService.SenkronizeKaynakProjelerAsync(talepCekiSatiriIds, cancellationToken);

        await hareketService.HareketKaydetAsync(new HareketGecmisi
        {
            ProjeId = yeniProje.Id,
            ReferansTipi = "Proje",
            ReferansId = yeniProje.Id.ToString(),
            Islem = yeniProjeOlusturuldu ? "Eksiklerden Saha Projesi Oluşturuldu" : "Saha Projesine Eksik Aktarımı Eklendi",
            IslemTipiId = (int)IslemTipi.ProjeOlusturuldu,
            KullaniciId = currentUserService.UserId ?? 0,
            Aciklama = yeniProjeOlusturuldu
                ? $"{string.Join(", ", kaynakProjeNolari)} kaynak projesinden {sandikTaslaklari.Count} sandık, {toplamSatir} satır, {FormatAdet(toplamAdet)} adet için saha projesi oluşturuldu."
                : $"{string.Join(", ", kaynakProjeNolari)} kaynak projesinden {sandikTaslaklari.Count} sandık, {toplamSatir} satır, {FormatAdet(toplamAdet)} adet mevcut saha projesine eklendi."
        });

        var proje = new ProjeDto
        {
            Id = yeniProje.Id,
            ProjeNo = yeniProje.ProjeNo,
            Musteri = yeniProje.Musteri,
            DurumId = yeniProje.DurumId,
            DurumMetni = lookupCache.GetDeger<LookupProjeDurum>(yeniProje.DurumId),
            ProjeTipiId = yeniProje.ProjeTipiId,
            ProjeTipiMetni = lookupCache.GetDeger<LookupProjeTipi>(yeniProje.ProjeTipiId),
            BaslamaTarihi = yeniProje.CreatedDate,
            CalismaGunSayisi = Math.Max(0, (TurkeyTime.Now.Date - yeniProje.CreatedDate.Date).Days),
            PlanlananSevkTarihi = yeniProje.PlanlananSevkTarihi,
            SorumluKisi = yeniProje.SorumluKisi,
            SandikSayisi = sandikTaslaklari.Count,
            ToplamUrunSayisi = toplamSatir,
            Lokasyon = yeniProje.Lokasyon
        };

        return Result<SahaAktarimUygulamaSonucu>.Success(
            new SahaAktarimUygulamaSonucu(proje, sahaAktarim.Id, toplamSatir, toplamAdet));
    }

    private Task<string> GenerateSahaProjeNoAsync(string baseNo) => SahaProjeNoUretAsync(unitOfWork, baseNo);

    /// <summary>Saha proje numarası çakışırsa "-2", "-3"… ekler (yeni saha projesi önerisi de aynı kuralı kullanır).</summary>
    public static async Task<string> SahaProjeNoUretAsync(IUnitOfWork unitOfWork, string baseNo)
    {
        var projeRepo = unitOfWork.GetRepository<Proje>();
        var candidate = baseNo;
        var index = 2;

        while ((await projeRepo.FindAsync(p => p.ProjeNo == candidate)).Any())
        {
            candidate = $"{baseNo}-{index}";
            index++;
        }

        return candidate;
    }

    /// <summary>İçine ürün alan kapalı sandık yeniden hazırlanıyor durumuna geçince yazılan hareket (proje içi taşımadaki gibi).</summary>
    internal static Task SandikGeriAcildiKaydetAsync(IHareketService hareketService, int projeId, Sandik sandik, int kullaniciId)
        => hareketService.HareketKaydetAsync(new HareketGecmisi
        {
            ProjeId = projeId,
            KullaniciId = kullaniciId,
            ReferansTipi = "Sandik",
            ReferansId = sandik.Id.ToString(CultureInfo.InvariantCulture),
            ReferansMetni = $"Sandık {sandik.SandikNo}",
            Islem = "Sandık Geri Açıldı",
            EskiDeger = nameof(SandikDurum.Kapandi),
            YeniDeger = nameof(SandikDurum.Hazirlaniyor),
            Aciklama = "İçeriği değiştiği için sandık yeniden hazırlanıyor durumuna getirildi."
        });

    private static string NormalizeSandikNo(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static bool IsNegative(decimal? value)
    {
        return value.HasValue && value.Value < 0;
    }

    private static bool IsSandikBazliAktarim(string? aciklama)
    {
        return !string.IsNullOrWhiteSpace(aciklama) &&
            aciklama.StartsWith(SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAdet(decimal value)
    {
        return decimal.Truncate(value) == value
            ? decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.GetCultureInfo("tr-TR"));
    }
}
