using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>İş kayıtları: seçim ve detay, özel iş oluşturma, iptal ve geri alma, finans tarihi ve fiyatlandırma.</summary>
internal sealed class FinansIsKaydiService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim,
    FinansFiyatBelirleyici fiyatlar,
    FinansPoOnayOkuyucu poOnay) : IFinansIsKaydiService
{
    public async Task<FinansSayfaliSonuc<FinansProjeSecenekModel>> ProjeSecenekleriAsync(
        string? arama,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 250);
        var query = context.Projeler.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var normalized = arama.Trim().ToLower();
            query = query.Where(x =>
                x.ProjeNo.ToLower().Contains(normalized) ||
                x.Musteri.ToLower().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.ProjeNo)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FinansProjeSecenekModel(x.Id, x.ProjeNo, x.Musteri))
            .ToListAsync(cancellationToken);

        return new FinansSayfaliSonuc<FinansProjeSecenekModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<FinansIsKaydiModel>> IsKayitlariSecimAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var normalizedIds = NormalizeWorkSelectionIds(ids);
        if (normalizedIds.Count == 0)
            return Array.Empty<FinansIsKaydiModel>();

        var entities = await context.IsKaydiDetayQuery()
            .Where(x => normalizedIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var byId = entities.ToDictionary(x => x.Id);
        var onaylar = await poOnay.BilgileriGetirAsync(entities, cancellationToken);

        return normalizedIds
            .Where(byId.ContainsKey)
            .Select(id => FinansModelEslemeleri.MapIsKaydi(byId[id], onaylar[id]))
            .ToArray();
    }

    internal static IReadOnlyList<int> NormalizeWorkSelectionIds(IReadOnlyCollection<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Any(x => x <= 0))
            throw IsKuraliException.Cakisma("İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir.");

        var normalized = ids.Distinct().ToArray();
        if (normalized.Length > 2000)
            throw IsKuraliException.Cakisma("Tek işlemde en fazla 2000 iş kaydı seçilebilir.");

        return normalized;
    }

    public async Task<FinansIsKaydiModel?> IsKaydiGetirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await context.IsKaydiDetayQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        return await ModelAsync(entity, cancellationToken);
    }

    /// <summary>Tek kaydın modeli, PO öncesi onay durumuyla.</summary>
    private async Task<FinansIsKaydiModel> ModelAsync(FinansIsKaydi work, CancellationToken cancellationToken)
        => FinansModelEslemeleri.MapIsKaydi(work, (await poOnay.BilgileriGetirAsync([work], cancellationToken))[work.Id]);

    public Task<FinansIsKaydiModel> IsKaydiOlusturAsync(
        FinansIsKaydiKaydetModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => IsKaydiOlusturCoreAsync(model, cancellationToken), cancellationToken);

    /// <summary>Münferit işlerin rapor grubu (aylık listede ve raporlarda bu adla gruplanır).</summary>
    private const string MunferitRaporGrubu = "Münferit İş";

    public Task<FinansIsKaydiModel> MunferitIsOlusturAsync(
        FinansMunferitIsKaydetModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            var urun = await context.Set<FinansUrun>().AsNoTracking()
                .Where(x => x.Id == model.FinansUrunId && x.Aktif)
                .Select(x => new { x.Ad, x.FiyatlandirmaBirimi })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw IsKuraliException.Cakisma("Seçilen tarife bulunamadı veya pasif.");
            if (model.ProjeId.HasValue == !string.IsNullOrWhiteSpace(model.ProjeAdi))
                throw IsKuraliException.Cakisma("Sistemden bir proje seçin veya proje adını yazın.");
            var m3 = urun.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Metrekup;
            var adetli = urun.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Adet;
            if (m3 && !FinansHacimKurallari.GecerliManuelHacim(model.AhsapM3 ?? 0, model.KontraM3 ?? 0))
                throw IsKuraliException.Cakisma("Tarife m³ ile fiyatlanıyor; ahşap veya kontra m³ girilmelidir.");
            if (adetli && model.Adet <= 0)
                throw IsKuraliException.Cakisma("Adet sıfırdan büyük olmalıdır.");
            // Miktar tarifenin birimine göre: adet tarifesinde girilen adet, m³ ve sabit tutarda tek iş (1).
            var adet = adetli ? model.Adet : 1m;
            var bilesik = m3 ? FinansHacimKurallari.Bilesik(model.AhsapM3, model.KontraM3) : 0m;
            // Serbest proje adı manuel proje numarası ve müşteri olarak yazılır (panelde bağımsız işlerde ayrı satır).
            var projeAdi = model.ProjeId.HasValue ? null : model.ProjeAdi!.Trim();
            // Fiyat, para birimi ve KDV tarifenin iş tarihindeki sürümünden gelir; elle fiyat yazılmaz.
            return await IsKaydiOlusturCoreAsync(new FinansIsKaydiKaydetModel(
                model.ProjeId, projeAdi, projeAdi, projeAdi, FinansIsTuru.OzelIs,
                urun.Ad, null, null, null, adet, m3 ? "m³" : "Adet",
                m3 ? bilesik : 0m,
                model.FinansUrunId, null, null, null,
                model.IsTarihi.Date, model.IsTarihi.Date,
                HesaplamaYontemi: urun.FiyatlandirmaBirimi switch
                {
                    FinansFiyatlandirmaBirimi.Metrekup => FinansHesaplamaYontemi.Metrekup,
                    FinansFiyatlandirmaBirimi.Adet => FinansHesaplamaYontemi.DegiskenAdet,
                    _ => FinansHesaplamaYontemi.DegiskenTutar
                },
                RaporGrubu: MunferitRaporGrubu,
                AhsapM3: m3 ? model.AhsapM3 ?? 0 : null,
                KontraM3: m3 ? model.KontraM3 ?? 0 : null), cancellationToken);
        }, cancellationToken);

    private async Task<FinansIsKaydiModel> IsKaydiOlusturCoreAsync(
        FinansIsKaydiKaydetModel model,
        CancellationToken cancellationToken)
    {
        var project = await context.ResolveProjectAsync(model.ProjeId, model.ManuelProjeNo, model.ManuelProjeAdi, model.Musteri, cancellationToken);
        var entity = new FinansIsKaydi
        {
            ProjeId = project.ProjeId,
            ProjeNo = project.ProjeNo,
            Musteri = project.Musteri,
            ManuelProjeMi = project.Manuel,
            IsTuru = model.IsTuru,
            IsAdi = model.IsAdi.Trim(),
            OzelIsTuru = model.OzelIsTuru?.Trim(),
            HesaplamaYontemi = model.HesaplamaYontemi,
            RaporGrubu = model.RaporGrubu?.Trim(),
            Aciklama = model.Aciklama?.Trim(),
            TalepEdenKisi = model.TalepEdenKisi?.Trim(),
            TalepEdenBolum = model.TalepEdenBolum?.Trim(),
            SandikNo = model.SandikNo?.Trim(),
            SandikAdi = model.SandikAdi?.Trim(),
            SandikTipi = model.SandikTipi?.Trim(),
            Boy = model.Boy,
            En = model.En,
            Yukseklik = model.Yukseklik,
            IcSandikSablonId = model.IcSandikSablonId,
            Adet = model.Adet,
            Birim = model.Birim.Trim(),
            BirimM3 = model.BirimM3,
            ToplamM3 = decimal.Round(model.Adet * model.BirimM3, 6),
            UretimTarihi = model.UretimTarihi,
            FinansDonemi = FinansTarihKurallari.AyBasi(model.FinansDonemi),
            FinansTarihi = model.FinansDonemi.Date,
            FinansTarihiManuel = true,
            FinansMiktariManuel = true,
            KayitTarihi = TurkeyTime.Now,
            KaynakTuru = "Manuel",
            KaynakAktif = true
        };
        if (model.AhsapM3.HasValue || model.KontraM3.HasValue)
        {
            // Özel işte m³ ahşap ve kontra olarak ayrı girilir; bileşik toplam fiyatlandırma miktarıdır.
            entity.AhsapM3 = model.AhsapM3 ?? 0;
            entity.KontraM3 = model.KontraM3 ?? 0;
            entity.HacimManuel = true;
            FinansHacimKurallari.ManuelHacmiUygula(entity);
        }
        await ApplyPriceSnapshotAsync(entity, model.FinansUrunId, model.ManuelBirimFiyat, model.ParaBirimi, model.KdvOrani, entity.FinansTarihi, cancellationToken);
        context.Set<FinansIsKaydi>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        if (entity.IsTuru == FinansIsTuru.OzelIs && string.IsNullOrWhiteSpace(entity.SandikNo))
        {
            entity.SandikNo = $"OZL-{entity.Id:D6}";
            await context.SaveChangesAsync(cancellationToken);
        }
        denetim.Ekle(nameof(FinansIsKaydi), entity.Id, "Oluşturma", "*", null, "Kayıt oluşturuldu");
        await context.SaveChangesAsync(cancellationToken);
        return (await IsKaydiGetirAsync(entity.Id, cancellationToken))!;
    }

    private async Task ApplyPriceSnapshotAsync(
        FinansIsKaydi entity,
        int? finansUrunId,
        decimal? manuelBirimFiyat,
        string? paraBirimi,
        decimal? kdvOrani,
        DateTime tarih,
        CancellationToken cancellationToken)
    {
        entity.FinansUrunId = finansUrunId;
        if (finansUrunId.HasValue)
        {
            var urun = await context.Set<FinansUrun>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == finansUrunId && x.Aktif, cancellationToken)
                ?? throw IsKuraliException.Cakisma("Seçilen finans ürünü bulunamadı veya pasif.");
            var tarife = await fiyatlar.GecerliTarifeAsync(urun.Id, tarih, cancellationToken)
                ?? throw IsKuraliException.Cakisma($"{urun.Ad} için {tarih:dd.MM.yyyy} tarihinde geçerli tarife yok.");

            entity.FiyatlandirmaBirimiSnapshot = urun.FiyatlandirmaBirimi;
            // Ürün seçimi fiyatlandırma birimini sabitler. Yetkili kullanıcı açıkça
            // override gönderdiyse yalnız sağlanan alanlar tarifeyi ezer; sessizce
            // yok sayılması UI ile kaydedilen snapshot'ın farklılaşmasına yol açardı.
            entity.BirimFiyatSnapshot = manuelBirimFiyat ?? tarife.BirimFiyat;
            entity.ParaBirimiSnapshot = string.IsNullOrWhiteSpace(paraBirimi)
                ? tarife.ParaBirimi
                : await FinansSorgulari.ParaBirimiSecimiAsync(context, paraBirimi, [tarife.ParaBirimi], cancellationToken);
            entity.KdvOraniSnapshot = kdvOrani ?? tarife.KdvOrani;
            entity.TarifeYiliSnapshot = tarife.Yil;
            entity.TarifeIdSnapshot = tarife.Id;
            return;
        }

        if (!manuelBirimFiyat.HasValue)
        {
            entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
                ? FinansFiyatlandirmaBirimi.Metrekup
                : FinansFiyatlandirmaBirimi.Adet;
            entity.BirimFiyatSnapshot = 0;
            entity.ParaBirimiSnapshot = await SecilenParaBirimiAsync(entity, paraBirimi, cancellationToken);
            entity.KdvOraniSnapshot = kdvOrani ?? 0;
            entity.TarifeYiliSnapshot = null;
            return;
        }

        entity.FiyatlandirmaBirimiSnapshot = entity.BirimM3 > 0
            ? FinansFiyatlandirmaBirimi.Metrekup
            : entity.IsTuru == FinansIsTuru.OzelIs && entity.HesaplamaYontemi == FinansHesaplamaYontemi.SabitAylik
                ? FinansFiyatlandirmaBirimi.SabitTutar : FinansFiyatlandirmaBirimi.Adet;
        entity.BirimFiyatSnapshot = manuelBirimFiyat.Value;
        entity.ParaBirimiSnapshot = await SecilenParaBirimiAsync(entity, paraBirimi, cancellationToken);
        entity.KdvOraniSnapshot = kdvOrani ?? 0;
        entity.TarifeYiliSnapshot = null;
    }

    public Task<bool> IsKaydiIptalAsync(int id, string aciklama, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => IsKaydiIptalCoreAsync(id, aciklama, cancellationToken), cancellationToken);

    private async Task<bool> IsKaydiIptalCoreAsync(int id, string aciklama, CancellationToken cancellationToken)
    {
        FinansGerekceKurali.Dogrula(aciklama);
        var entity = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (entity.IptalEdildi) return true;
        if (entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
            throw IsKuraliException.Cakisma("Aktif siparişi bulunan iş iptal edilemez. Önce bağlı siparişleri iptal edin.");
        entity.IptalEdildi = true;
        entity.IptalTarihi = TurkeyTime.Now;
        entity.IptalAciklamasi = aciklama.Trim();
        entity.Durum = FinansIsDurumu.IptalEdildi;
        denetim.Ekle(nameof(FinansIsKaydi), id, "İptal", nameof(entity.IptalEdildi), false, true, aciklama);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> IsKaydiGeriAlAsync(int id, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => IsKaydiGeriAlCoreAsync(id, cancellationToken), cancellationToken);

    private async Task<bool> IsKaydiGeriAlCoreAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        if (!entity.IptalEdildi) return true;
        if (!string.Equals(entity.KaynakTuru, "Manuel", StringComparison.OrdinalIgnoreCase))
            throw IsKuraliException.Cakisma("Üretim kaynaklı kayıtlar Finans ekranından aktifleştirilemez; işlemi Üretim modülünden yönetin.");
        entity.IptalEdildi = false;
        entity.IptalTarihi = null;
        entity.IptalAciklamasi = null;
        entity.KaynakAktif = true;
        entity.Durum = FinansTutarKurallari.Durum(entity);
        denetim.Ekle(nameof(FinansIsKaydi), id, "Aktifleştirme", nameof(entity.IptalEdildi), true, false);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> OzelIsAylikDegerGuncelleAsync(
        int id,
        FinansAylikDegerModel model,
        CancellationToken cancellationToken)
        => islem.AtomikAsync(() => OzelIsAylikDegerGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<bool> OzelIsAylikDegerGuncelleCoreAsync(
        int id,
        FinansAylikDegerModel model,
        CancellationToken cancellationToken)
    {
        var entity = await context.IsKaydiDetayQuery(true)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsTuru == FinansIsTuru.OzelIs, cancellationToken);
        if (entity is null) return false;
        if (entity.IptalEdildi)
            throw IsKuraliException.Cakisma("İptal edilmiş özel iş güncellenemez.");
        if (entity.SiparisKalemleri.Count != 0)
            throw IsKuraliException.Cakisma("PO geçmişi bulunan özel işin fiyat snapshot'ı güncellenemez.");
        if (!model.Miktar.HasValue && !model.NetBirimFiyat.HasValue)
            throw IsKuraliException.Cakisma("Miktar veya net birim fiyat gönderilmelidir.");
        if (model.Miktar is <= 0 || model.NetBirimFiyat is < 0)
            throw IsKuraliException.Cakisma("Miktar pozitif, net birim fiyat sıfır veya daha büyük olmalıdır.");
        if (model.Miktar.HasValue && !FinansTutarKurallari.AylikMiktarDuzenlenebilir(entity))
            throw IsKuraliException.Cakisma("Bu işin miktarı düzenlenemez.");
        if (model.NetBirimFiyat.HasValue && !FinansTutarKurallari.AylikBirimFiyatDuzenlenebilir(entity))
            throw IsKuraliException.Cakisma("Bu işin tarifesi düzenlenemez.");

        var auditBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        if (model.Miktar.HasValue)
        {
            entity.Adet = model.Miktar.Value;
            // Elle girilen ahşap/kontra hacmi miktar değişse de korunur; yalnız birim m³ yeniden türetilir.
            if (entity.HacimManuel)
                FinansHacimKurallari.ManuelHacmiUygula(entity);
            else
                entity.ToplamM3 = decimal.Round(entity.Adet * entity.BirimM3, 6, MidpointRounding.AwayFromZero);
        }
        if (model.NetBirimFiyat.HasValue)
        {
            entity.BirimFiyatSnapshot = model.NetBirimFiyat.Value;
            // Manuel net tutar varsa yeni tarife etkisiz kalırdı; net tutar tarife × miktardan hesaplanır.
            entity.ManuelNetTutar = null;
        }
        entity.FinansMiktariManuel = true;
        denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), entity, auditBefore);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<FinansIsKaydiModel?> FinansTarihiDegistirAsync(int id, FinansTarihiDegistirModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            FinansGerekceKurali.Dogrula(model.Aciklama);
            if (!FinansTarihKurallari.GecerliTarih(model.FinansTarihi)) throw IsKuraliException.Cakisma("Geçerli finans tarihi girin.");
            var work = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (work is null) return null;
            if (work.IptalEdildi) throw IsKuraliException.Cakisma("İptal edilmiş işin finans tarihi değiştirilemez.");
            denetim.Ekle(nameof(FinansIsKaydi), id, "Finans Tarihi", nameof(work.FinansTarihi), work.FinansTarihi, model.FinansTarihi.Date, model.Aciklama);
            work.FinansTarihi = model.FinansTarihi.Date;
            work.FinansDonemi = FinansTarihKurallari.AyBasi(model.FinansTarihi);
            work.FinansTarihiManuel = true;
            await context.SaveChangesAsync(cancellationToken);
            return await ModelAsync(work, cancellationToken);
        }, cancellationToken);

    public Task<FinansIsKaydiModel?> FiyatlandirAsync(int id, FinansFiyatlandirmaModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            FinansGerekceKurali.Dogrula(model.Aciklama);
            if (!Enum.IsDefined(model.FiyatlandirmaBirimi) || model.Adet <= 0 || model.BirimM3 < 0 || model.BirimFiyat < 0 || model.KdvOrani is < 0 or > 100)
                throw IsKuraliException.Cakisma("Fiyatlandırma yöntemi, miktar ve KDV değerlerini kontrol edin.");
            var work = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (work is null) return null;
            if (work.IptalEdildi || !work.KaynakAktif) throw IsKuraliException.Cakisma("Pasif iş fiyatlandırılamaz.");
            if (work.SiparisKalemleri.Count > 0)
                throw IsKuraliException.Cakisma("PO geçmişi bulunan işin fiyat snapshot'ı değiştirilemez; ek/düzeltme işi oluşturun.");
            var currency = await FinansSorgulari.ParaBirimiSecimiAsync(context, model.ParaBirimi, [work.ParaBirimiSnapshot], cancellationToken);
            var componentTotal = ValidatePriceComponents(model.Bilesenler);
            if (model.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.ManuelToplam && model.ManuelNetTutar is not > 0 && componentTotal is null)
                throw IsKuraliException.Cakisma("Manuel toplam yönteminde pozitif net tutar zorunludur.");
            if (model.FiyatlandirmaBirimi != FinansFiyatlandirmaBirimi.ManuelToplam && (model.ManuelNetTutar.HasValue || componentTotal.HasValue))
                throw IsKuraliException.Cakisma("Manuel toplam veya bileşenler için ManuelToplam yöntemini seçin.");
            if (componentTotal.HasValue && model.ManuelNetTutar.HasValue && model.ManuelNetTutar.Value != componentTotal)
                throw IsKuraliException.Cakisma("Manuel net tutar bileşen toplamına eşit olmalıdır.");
            var before = FinansDenetimKaydi.AnlikGoruntu(work);
            work.FiyatlandirmaBirimiSnapshot = model.FiyatlandirmaBirimi;
            work.Adet = model.Adet;
            if (work.HacimManuel)
            {
                // Elle girilen ahşap/kontra hacmi Hacim bölümünden yönetilir; fiyatlandırma yalnız birim m³'ü adede göre türetir.
                FinansHacimKurallari.ManuelHacmiUygula(work);
            }
            else
            {
                work.BirimM3 = model.BirimM3;
                work.ToplamM3 = decimal.Round(model.Adet * model.BirimM3, 6, MidpointRounding.AwayFromZero);
            }
            work.BirimFiyatSnapshot = model.BirimFiyat;
            // Elle fiyat tek birim fiyattır; malzemeli (ahşap/kontra ayrı) tarife fiyatı bırakılır.
            FinansFiyatBelirleyici.MalzemeliFiyatiTemizle(work);
            work.ManuelNetTutar = componentTotal ?? model.ManuelNetTutar;
            if (work.ManuelNetTutar.HasValue && work.ManuelNetTutar != FinansTutarKurallari.Para(work.ManuelNetTutar.Value))
                throw IsKuraliException.Cakisma("Net tutar en fazla iki ondalıklı olmalıdır.");
            work.ParaBirimiSnapshot = currency;
            work.KdvOraniSnapshot = model.KdvOrani;
            work.FinansMiktariManuel = true;
            work.FiyatBilesenleriJson = model.Bilesenler is null ? null : JsonSerializer.Serialize(model.Bilesenler);
            if (FinansTutarKurallari.IsNet(work) <= 0) throw IsKuraliException.Cakisma("İş net bedeli sıfırdan büyük olmalıdır.");
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), work, before);
            denetim.Ekle(nameof(FinansIsKaydi), id, "Fiyatlandırma", "Gerekçe", null, null, model.Aciklama);
            await context.SaveChangesAsync(cancellationToken);
            return await ModelAsync(work, cancellationToken);
        }, cancellationToken);

    public Task<FinansIsKaydiModel?> HacimGirAsync(int id, FinansHacimModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            if (!FinansGerekceKurali.IstegeBagliGecerli(model.Aciklama))
                throw IsKuraliException.Cakisma(FinansGerekceKurali.IstegeBagliMesaj);
            if (!FinansHacimKurallari.GecerliManuelHacim(model.AhsapM3, model.KontraM3))
                throw IsKuraliException.Cakisma("Ahşap ve kontra m³ sıfır veya pozitif olmalı; en az biri sıfırdan büyük olmalıdır.");
            var work = await context.IsKaydiDetayQuery(true).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (work is null) return null;
            if (work.IptalEdildi || !work.KaynakAktif) throw IsKuraliException.Cakisma("Pasif işin hacmi değiştirilemez.");
            if (work.SiparisKalemleri.Count > 0)
                throw IsKuraliException.Cakisma("PO'ya bağlı kaydın hacmi değiştirilemez; ek/düzeltme işi oluşturun.");
            if (!FinansHacimKurallari.ElleGirilebilir(work.SandikCinsi, work.SandikTipi, work.HacimManuel))
                throw IsKuraliException.Cakisma("Ahşap/kontra m³ yalnız kontrplak veya katlanır sandıkta girilir; ahşap sandığın m³'ü üretimden gelir.");
            var before = FinansDenetimKaydi.AnlikGoruntu(work);
            work.AhsapM3 = decimal.Round(model.AhsapM3, 6, MidpointRounding.AwayFromZero);
            work.KontraM3 = decimal.Round(model.KontraM3, 6, MidpointRounding.AwayFromZero);
            work.HacimManuel = true;
            FinansHacimKurallari.ManuelHacmiUygula(work);
            // Tarifeden fiyatlanmış (veya henüz fiyatsız) iş yeniden fiyatlanır: malzemeli tarife varsa ahşap m³ ahşap,
            // kontra m³ kontra tarifesiyle; yoksa tek tarife × bileşik m³. Elle girilmiş fiyat korunur.
            if (!work.FinansMiktariManuel && !work.ManuelNetTutar.HasValue &&
                (work.FinansUrunId.HasValue || work.BirimFiyatSnapshot <= 0 && !work.TarifeYiliSnapshot.HasValue))
                await fiyatlar.OtomatikFiyatUygulaAsync(work, cancellationToken);
            work.Durum = FinansTutarKurallari.Durum(work);
            denetim.DegisiklikleriEkle(nameof(FinansIsKaydi), work, before);
            if (!string.IsNullOrWhiteSpace(model.Aciklama))
                denetim.Ekle(nameof(FinansIsKaydi), id, "Hacim", "Gerekçe", null, null, model.Aciklama.Trim());
            await context.SaveChangesAsync(cancellationToken);
            return await ModelAsync(work, cancellationToken);
        }, cancellationToken);

    internal static decimal? ValidatePriceComponents(IReadOnlyList<FinansFiyatBileseniModel>? components)
    {
        if (components is null || components.Count == 0) return null;
        if (components.Count > 100) throw IsKuraliException.Cakisma("En fazla 100 fiyat bileşeni tanımlanabilir.");
        if (components.Any(x => x is null || string.IsNullOrWhiteSpace(x.Ad) || x.Ad.Length > 150 || !Enum.IsDefined(x.Yontem) || x.Miktar <= 0 || x.BirimFiyat < 0))
            throw IsKuraliException.Cakisma("Bileşenin adı, yöntemi, pozitif miktarı ve geçerli fiyatı zorunludur.");
        // F11: m³ satırının malzemesi ahşap veya kontra olabilir; boşsa sandığın kendi malzemesi sayılır.
        if (components.Any(x => x.Malzeme is { } malzeme && (malzeme == FinansSandikMalzemesi.Bilinmiyor || !Enum.IsDefined(malzeme))))
            throw IsKuraliException.Cakisma("Bileşen malzemesi ahşap veya kontra olmalıdır.");
        if (components.Any(x => x.Malzeme.HasValue && x.Yontem != FinansFiyatlandirmaBirimi.Metrekup))
            throw IsKuraliException.Cakisma("Malzeme yalnız m³ fiyat satırında seçilebilir.");
        return components.Sum(x => FinansTutarKurallari.Para((x.Yontem is FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam ? 1 : x.Miktar) * x.BirimFiyat));
    }

    /// <summary>
    /// Manuel fiyattaki para birimi: gönderilmediyse varsayılan EUR; gönderildiyse tanımlı ve aktif olmalıdır (F1).
    /// Mevcut kayıt güncellenirken kendi para birimi, pasifleştirilmiş olsa da korunabilir.
    /// </summary>
    private async Task<string> SecilenParaBirimiAsync(FinansIsKaydi entity, string? paraBirimi, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(paraBirimi)) return "EUR";
        string?[] mevcut = entity.Id > 0 ? [entity.ParaBirimiSnapshot] : [];
        return await FinansSorgulari.ParaBirimiSecimiAsync(context, paraBirimi, mevcut, cancellationToken);
    }
}
