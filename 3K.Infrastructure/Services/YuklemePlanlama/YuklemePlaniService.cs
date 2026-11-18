using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.YuklemeMotoru.Yerlesim;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Yükleme planı (Simülasyon isterleri 3–7, 27): plan ve revizyonlar. Her kaydetme değişmez yeni revizyon yazar; yük
/// satırları ölçü/ağırlık/lokasyon/yük türünün kopyasını taşır ve kaynak değişince kendiliğinden değişmez, fark
/// gösterilir. Kaydetmede istemciden yalnız seçimler alınır; kopya önceki revizyondan korunur.
/// </summary>
internal sealed partial class YuklemePlaniService(AppDbContext context, IUnitOfWork unitOfWork) : IYuklemePlaniService
{
    private static readonly JsonSerializerOptions AyarJsonu = new(JsonSerializerDefaults.Web);
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private const string PlanBulunamadi = "Yükleme planı bulunamadı.";
    private const string EszamanliKayit = "Plan siz açtıktan sonra başka biri tarafından kaydedildi; sayfayı yenileyip tekrar deneyin.";
    private const string AileGecersiz = "Taşıma ailesi bulunamadı veya sandık taşımaya uygun tipi yok.";
    private const string ProjeBulunamadi = "Seçilen projelerden biri bulunamadı.";
    private const string YabanciSandik = "Plana ait olmayan bir sandık gönderildi; sayfayı yenileyip tekrar deneyin.";
    private const string ManuelProjeGecersiz = "Elle eklenen yükün projesi planın projelerinden biri olmalıdır.";
    private const string LokasyonBulunamadi = "Elle eklenen yükün lokasyonu bulunamadı.";
    private const string KaynakDegismedi = "Planın kaynağında değişiklik yok.";
    private static readonly string YukSiniriAsildi = $"Bir planda en fazla {YuklemePlaniKurali.EnFazlaYuk} yük olabilir.";

    /// <summary>Sandığın güncel verisi (kopyalanan alanlar ve plana gelme durumu).</summary>
    private sealed record SandikKaynagi(
        int Id, int ProjeId, string ProjeNo, string SandikNo, string? Ad, decimal? Boy, decimal? En, decimal? Yukseklik,
        decimal? Brut, int LokasyonId, string? LokasyonAdi, int YukTuruId, int DurumId)
    {
        public string Kod => YuklemePlaniKurali.SandikKodu(ProjeNo, SandikNo);
    }

    public async Task<YuklemePlaniTanimlari> TanimlarAsync(CancellationToken cancellationToken)
    {
        var aileler = await context.EkipmanAileleri.AsNoTracking()
            .OrderBy(a => a.Sira)
            .Select(a => new { a.Id, a.Ad })
            .ToListAsync(cancellationToken);
        var tipler = (await context.EkipmanTipleri.AsNoTracking()
                .Where(t => t.Aktif)
                .OrderBy(t => t.OtomatikSecimSirasi == null).ThenBy(t => t.OtomatikSecimSirasi).ThenBy(t => t.Sira)
                .Select(t => new { t.Id, t.AileId, t.Ad, t.Kategori, t.OtomatikSecimSirasi })
                .ToListAsync(cancellationToken))
            .Where(t => EkipmanKategoriKurali.SandikTasimayaUygunMu(t.Kategori))
            .ToList();
        var uygunAileler = tipler.Select(t => t.AileId).ToHashSet();
        return new YuklemePlaniTanimlari
        {
            TasimaAileleri = aileler.Where(a => uygunAileler.Contains(a.Id)).Select(a => new SecimOgesi(a.Id, a.Ad)).ToList(),
            TasimaTipleri = tipler.Select(t => new TasimaTipi(t.Id, t.AileId, t.Ad, t.OtomatikSecimSirasi != null)).ToList(),
            YukTurleri = await context.LookupYukTurleri.AsNoTracking()
                .OrderBy(t => t.Id).Select(t => new SecimOgesi(t.Id, t.Deger)).ToListAsync(cancellationToken),
            Lokasyonlar = await context.LookupDepoLokasyonlari.AsNoTracking()
                .OrderBy(l => l.Id).Select(l => new SecimOgesi(l.Id, l.Deger)).ToListAsync(cancellationToken)
        };
    }

    public async Task<SayfaliSonuc<PlanProjeAdayi>> ProjeAdaylariAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        // Yalnız plana gelecek (sevk edilmemiş) sandığı olan projeler; arama, sayım ve sayfalama veritabanında.
        var adayIdleri = context.Sandiklar.Where(YuklemePlaniKurali.PlanaGelenSandik).Select(s => s.ProjeId);
        var sorgu = context.Projeler.AsNoTracking().Where(p => adayIdleri.Contains(p.Id));
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var desen = $"%{IlikeDeseniniKacir(arama.Trim())}%";
            sorgu = sorgu.Where(p => EF.Functions.ILike(p.ProjeNo, desen, "\\") || EF.Functions.ILike(p.Musteri, desen, "\\"));
        }

        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var sayfadakiler = await sorgu
            .OrderByDescending(p => p.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(p => new { p.Id, p.ProjeNo, p.Musteri, p.ProjeTipiId })
            .ToListAsync(cancellationToken);
        var idler = sayfadakiler.Select(p => p.Id).ToList();
        var toplamlar = await ProjeSandikSayilariAsync(context.Sandiklar.Where(s => idler.Contains(s.ProjeId)), cancellationToken);
        var gelenler = await ProjeSandikSayilariAsync(
            context.Sandiklar.Where(YuklemePlaniKurali.PlanaGelenSandik).Where(s => idler.Contains(s.ProjeId)), cancellationToken);
        var tipler = await context.LookupProjeTipleri.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Deger, cancellationToken);

        return new SayfaliSonuc<PlanProjeAdayi>
        {
            Items = sayfadakiler.Select(p => new PlanProjeAdayi
            {
                ProjeId = p.Id,
                ProjeNo = p.ProjeNo,
                Musteri = p.Musteri,
                ProjeTipiId = p.ProjeTipiId,
                ProjeTipiAdi = tipler.GetValueOrDefault(p.ProjeTipiId, string.Empty),
                PlanaGelecekSandikSayisi = gelenler.GetValueOrDefault(p.Id),
                SevkEdilmisSandikSayisi = toplamlar.GetValueOrDefault(p.Id) - gelenler.GetValueOrDefault(p.Id)
            }).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<SayfaliSonuc<YuklemePlaniOzeti>> PlanlarAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken)
    {
        var sorgu = context.YuklemePlanlari.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(arama))
        {
            var desen = $"%{IlikeDeseniniKacir(arama.Trim())}%";
            sorgu = sorgu.Where(p => EF.Functions.ILike(p.PlanNo, desen, "\\") || EF.Functions.ILike(p.Ad, desen, "\\"));
        }

        var toplam = await sorgu.CountAsync(cancellationToken);
        var sayfaBoyutu = SayfaKurali.Boyut(boyut);
        var gecerliSayfa = SayfaKurali.Sayfa(sayfa, sayfaBoyutu, toplam);
        var planlar = await sorgu
            .OrderByDescending(p => p.UpdatedDate ?? p.CreatedDate).ThenByDescending(p => p.Id)
            .Skip(SayfaKurali.Atlanan(gecerliSayfa, sayfaBoyutu)).Take(sayfaBoyutu)
            .Select(p => new { p.Id, p.PlanNo, p.Ad, p.CalismaBicimi, AileAdi = p.TasimaAilesi.Ad, p.SonRevizyonNo })
            .ToListAsync(cancellationToken);

        var idler = planlar.Select(p => p.Id).ToList();
        var revizyonlar = await context.YuklemePlanRevizyonlari.AsNoTracking()
            .Where(r => idler.Contains(r.PlanId) && r.RevizyonNo == r.Plan.SonRevizyonNo)
            .Select(r => new
            {
                r.PlanId,
                r.CreatedDate,
                r.KaydedenKullaniciId,
                Projeler = r.Projeler.OrderBy(x => x.ProjeNo).Select(x => x.ProjeNo).ToList(),
                YukSayisi = r.Yukler.Count,
                DahilSayisi = r.Yukler.Count(y => y.Dahil)
            })
            .ToDictionaryAsync(r => r.PlanId, cancellationToken);
        var kullaniciIdleri = revizyonlar.Values.Where(r => r.KaydedenKullaniciId != null).Select(r => r.KaydedenKullaniciId!.Value).Distinct().ToList();
        var kullanicilar = await context.Kullanicilar.AsNoTracking()
            .Where(k => kullaniciIdleri.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.AdSoyad, cancellationToken);

        return new SayfaliSonuc<YuklemePlaniOzeti>
        {
            Items = planlar.Select(p =>
            {
                var r = revizyonlar.GetValueOrDefault(p.Id);
                return new YuklemePlaniOzeti
                {
                    Id = p.Id,
                    PlanNo = p.PlanNo,
                    Ad = p.Ad,
                    CalismaBicimi = p.CalismaBicimi,
                    TasimaAilesiAdi = p.AileAdi,
                    Projeler = r == null ? string.Empty : string.Join(", ", r.Projeler),
                    YukSayisi = r?.YukSayisi ?? 0,
                    DahilYukSayisi = r?.DahilSayisi ?? 0,
                    RevizyonNo = p.SonRevizyonNo,
                    SonKayitTarihi = r?.CreatedDate ?? default,
                    SonKaydeden = r?.KaydedenKullaniciId is { } kid ? kullanicilar.GetValueOrDefault(kid) : null
                };
            }).ToList(),
            PageNumber = gecerliSayfa,
            PageSize = sayfaBoyutu,
            TotalCount = toplam
        };
    }

    public async Task<YuklemePlaniDetayi?> PlanGetirAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.AsNoTracking().Include(p => p.TasimaAilesi)
            .SingleOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null)
            return null;
        var revizyon = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        return await DetayAsync(plan, revizyon, cancellationToken);
    }

    public async Task<YuklemePlaniDetayi> OlusturAsync(YuklemePlaniOlusturModel model, int? kullaniciId, CancellationToken cancellationToken)
    {
        await AileyiDenetleAsync(model.TasimaAilesiId, cancellationToken);
        var projeler = await ProjeleriAsync(model.ProjeIdleri, cancellationToken);
        var sandiklar = PlanaGelenler(await SandiklarAsync(model.ProjeIdleri, [], cancellationToken), model.ProjeIdleri);
        if (sandiklar.Count > YuklemePlaniKurali.EnFazlaYuk)
            throw new IsKuraliException(YukSiniriAsildi);

        var yil = TurkeyTime.Now.Year;
        var onek = YuklemePlaniKurali.PlanNoOneki(yil);
        var sonNo = await context.YuklemePlanlari.Where(p => p.PlanNo.StartsWith(onek)).MaxAsync(p => (string?)p.PlanNo, cancellationToken);
        var revizyon = new YuklemePlanRevizyonu
        {
            RevizyonNo = 1,
            KaydedenKullaniciId = kullaniciId,
            AyarlarJson = JsonSerializer.Serialize(new YuklemePlanAyarlari(), AyarJsonu),
            Projeler = ProjeSatirlari(projeler),
            Yukler = sandiklar.Select((s, i) =>
                SandikSatiri(s, i + 1, dahil: true, YukIzinKurali.Varsayilan(s.YukTuruId, s.Brut), PlanYukIzni.Yok)).ToList()
        };
        var plan = new YuklemePlani
        {
            PlanNo = YuklemePlaniKurali.SonrakiPlanNo(yil, sonNo),
            Ad = model.Ad,
            CalismaBicimi = model.CalismaBicimi,
            TasimaAilesiId = model.TasimaAilesiId,
            ProjeKarisimi = model.ProjeKarisimi,
            YuklemeTarihi = model.YuklemeTarihi?.Date,
            Aciklama = model.Aciklama,
            OlusturanKullaniciId = kullaniciId,
            SonRevizyonNo = 1,
            Revizyonlar = [revizyon]
        };
        context.YuklemePlanlari.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    public async Task<YuklemePlaniDetayi> KaydetAsync(YuklemePlaniKaydetModel model, int? kullaniciId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.SingleOrDefaultAsync(p => p.Id == model.PlanId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        if (plan.SonRevizyonNo != model.BeklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EszamanliKayit);
        if (plan.TasimaAilesiId != model.TasimaAilesiId)
            await AileyiDenetleAsync(model.TasimaAilesiId, cancellationToken);
        await MevcutAraclariDenetleAsync(model.Ayarlar, cancellationToken);
        await TipleriDenetleAsync(model.TasimaAilesiId, model.Ayarlar.TipIdleri, cancellationToken);

        var onceki = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var projeler = await ProjeleriAsync(model.ProjeIdleri, cancellationToken);
        var secilenProjeler = model.ProjeIdleri.ToHashSet();

        // Sandık kümesi: önceki revizyonun seçili projelerdeki satırları (kopya korunur) ve plana yeni eklenen projelerin
        // plana gelen sandıkları (şimdiki kaynaktan). Var olan projeye sonradan eklenen sandık kaynak farkı olarak görünür.
        var oncekiProjeler = onceki.Projeler.Where(p => p.ProjeId != null).Select(p => p.ProjeId!.Value).ToHashSet();
        var yeniProjeler = model.ProjeIdleri.Where(id => !oncekiProjeler.Contains(id)).ToList();
        var kalanSatirlar = onceki.Yukler
            .Where(y => y.Kaynak == PlanYukKaynagi.Sandik && (y.ProjeId == null || secilenProjeler.Contains(y.ProjeId.Value)))
            .OrderBy(y => y.SiraNo)
            .ToList();
        var yeniSandiklar = yeniProjeler.Count == 0
            ? []
            : PlanaGelenler(await SandiklarAsync(yeniProjeler, [], cancellationToken), yeniProjeler);

        var secimler = model.SandikYukleri.ToDictionary(y => y.SandikId);
        var kumedekiler = kalanSatirlar.Where(y => y.SandikId != null).Select(y => y.SandikId!.Value)
            .Concat(yeniSandiklar.Select(s => s.Id)).ToHashSet();
        if (secimler.Keys.Any(id => !kumedekiler.Contains(id)))
            throw new IsKuraliException(YabanciSandik);

        var satirlar = new List<YuklemePlanYuku>();
        foreach (var y in kalanSatirlar)
        {
            var satir = Kopyala(y, satirlar.Count + 1);
            if (y.SandikId is { } id && secimler.TryGetValue(id, out var secim))
                SecimiUygula(satir, secim.Dahil, new PlanYukIzinleri(secim.DondurulebilirMi, secim.UsteKonabilirMi, secim.AltaKalabilirMi, secim.UstYukSiniriKg), secim.ElleDegisenIzinler);
            satirlar.Add(satir);
        }
        foreach (var s in yeniSandiklar)
        {
            var satir = SandikSatiri(s, satirlar.Count + 1, dahil: true, YukIzinKurali.Varsayilan(s.YukTuruId, s.Brut), PlanYukIzni.Yok);
            if (secimler.TryGetValue(s.Id, out var secim))
                SecimiUygula(satir, secim.Dahil, new PlanYukIzinleri(secim.DondurulebilirMi, secim.UsteKonabilirMi, secim.AltaKalabilirMi, secim.UstYukSiniriKg), secim.ElleDegisenIzinler);
            satirlar.Add(satir);
        }
        satirlar.AddRange(await ManuelSatirlarAsync(model.ManuelYukler, projeler, satirlar.Count, cancellationToken));
        if (satirlar.Count > YuklemePlaniKurali.EnFazlaYuk)
            throw new IsKuraliException(YukSiniriAsildi);

        plan.Ad = model.Ad;
        plan.CalismaBicimi = model.CalismaBicimi;
        plan.TasimaAilesiId = model.TasimaAilesiId;
        plan.ProjeKarisimi = model.ProjeKarisimi;
        plan.YuklemeTarihi = model.YuklemeTarihi?.Date;
        plan.Aciklama = model.Aciklama;
        YeniRevizyonEkle(plan, kullaniciId, model.RevizyonAciklamasi, model.Ayarlar, ProjeSatirlari(projeler), satirlar);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    public async Task<YuklemePlaniDetayi> KaynaktanGuncelleAsync(int planId, int beklenenRevizyonNo, int? kullaniciId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.SingleOrDefaultAsync(p => p.Id == planId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        if (plan.SonRevizyonNo != beklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EszamanliKayit);

        var onceki = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var projeIdleri = onceki.Projeler.Where(p => p.ProjeId != null).Select(p => p.ProjeId!.Value).ToList();
        var guncel = await SandiklarAsync(projeIdleri, OncekiSandikIdleri(onceki), cancellationToken);
        if (KaynakFarklari(onceki, guncel, projeIdleri, new Dictionary<int, string>()).Count == 0)
            throw new IsKuraliException(KaynakDegismedi);

        // Plana gelen her sandık: önceki satır varsa seçim ve elle değişen izinler korunur, kopya güncellenir; yoksa
        // varsayılanla eklenir. Silinen, sevk edilen ve projeden çıkan sandık düşer. Elle eklenen yükler aynen kalır.
        var oncekiSatirlar = onceki.Yukler.Where(y => y.SandikId != null).ToDictionary(y => y.SandikId!.Value);
        var satirlar = new List<YuklemePlanYuku>();
        foreach (var s in PlanaGelenler(guncel, projeIdleri))
        {
            if (oncekiSatirlar.TryGetValue(s.Id, out var y))
            {
                var mevcut = new PlanYukIzinleri(y.DondurulebilirMi, y.UsteKonabilirMi, y.AltaKalabilirMi, y.UstYukSiniriKg);
                satirlar.Add(SandikSatiri(s, satirlar.Count + 1, y.Dahil,
                    YukIzinKurali.Yenile(mevcut, y.ElleDegisenIzinler, s.YukTuruId, s.Brut), y.ElleDegisenIzinler));
            }
            else
            {
                satirlar.Add(SandikSatiri(s, satirlar.Count + 1, dahil: true, YukIzinKurali.Varsayilan(s.YukTuruId, s.Brut), PlanYukIzni.Yok));
            }
        }
        foreach (var manuel in onceki.Yukler.Where(y => y.Kaynak == PlanYukKaynagi.Manuel).OrderBy(y => y.SiraNo))
            satirlar.Add(Kopyala(manuel, satirlar.Count + 1));
        if (satirlar.Count > YuklemePlaniKurali.EnFazlaYuk)
            throw new IsKuraliException(YukSiniriAsildi);

        var projeler = await context.Projeler.AsNoTracking().Where(p => projeIdleri.Contains(p.Id))
            .Select(p => new SecimOgesi(p.Id, p.ProjeNo)).ToListAsync(cancellationToken);
        YeniRevizyonEkle(plan, kullaniciId, "Kaynaktan güncellendi.", Ayarlar(onceki), ProjeSatirlari(projeler), satirlar);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    private async Task<YuklemePlanRevizyonu> RevizyonAsync(int planId, int revizyonNo, CancellationToken cancellationToken) =>
        await context.YuklemePlanRevizyonlari.AsNoTracking()
            .Include(r => r.Projeler)
            .Include(r => r.Yukler)
            .AsSplitQuery()
            .SingleAsync(r => r.PlanId == planId && r.RevizyonNo == revizyonNo, cancellationToken);

    private async Task<YuklemePlaniDetayi> DetayAsync(YuklemePlani plan, YuklemePlanRevizyonu revizyon, CancellationToken cancellationToken)
    {
        var kaydeden = revizyon.KaydedenKullaniciId is { } kullaniciId
            ? await context.Kullanicilar.AsNoTracking().Where(k => k.Id == kullaniciId).Select(k => k.AdSoyad).SingleOrDefaultAsync(cancellationToken)
            : null;
        var yukTurleri = await context.LookupYukTurleri.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Deger, cancellationToken);
        var projeIdleri = revizyon.Projeler.Where(p => p.ProjeId != null).Select(p => p.ProjeId!.Value).ToList();
        var projeBilgileri = await context.Projeler.AsNoTracking()
            .Where(p => projeIdleri.Contains(p.Id))
            .Select(p => new { p.Id, p.Musteri, p.ProjeTipiId })
            .ToDictionaryAsync(p => p.Id, cancellationToken);
        var guncel = await SandiklarAsync(projeIdleri, OncekiSandikIdleri(revizyon), cancellationToken);
        var sevkliSayilari = guncel
            .Where(s => projeIdleri.Contains(s.ProjeId) && !YuklemePlaniKurali.PlanaGelirMi(s.DurumId))
            .GroupBy(s => s.ProjeId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Yerleşim yalnız bu revizyonla hesaplanmışsa vardır; satır durumu ondan okunur (yerleşti / yerleşemedi).
        var sonuc = YerlesimSerilestirici.Oku(revizyon.YerlesimJson);
        var aracNolari = sonuc?.Araclar.SelectMany(a => a.Konumlar.Select(k => (k.YukAnahtari, a.AracNo)))
            .ToDictionary(x => x.YukAnahtari, x => x.AracNo) ?? [];
        var nedenler = sonuc?.Yerlesemeyenler.ToDictionary(y => y.YukAnahtari, y => y.Neden) ?? [];

        var satirlar = revizyon.Yukler.OrderBy(y => y.SiraNo).Select(y =>
        {
            var sorunlar = YukVeriKurali.Sorunlar(y.BoyMm, y.EnMm, y.YukseklikMm, y.BrutKg);
            var durum = YuklemePlaniKurali.Durum(y.Dahil, sorunlar);
            var anahtar = YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo);
            int? aracNo = aracNolari.TryGetValue(anahtar, out var no) ? no : null;
            var neden = nedenler.GetValueOrDefault(anahtar);
            if (durum == PlanYukDurumu.Bekliyor && aracNo != null)
                durum = PlanYukDurumu.Yerlesti;
            else if (durum == PlanYukDurumu.Bekliyor && neden != null)
                durum = PlanYukDurumu.Yerlesemedi;
            return new PlanYukSatiri
            {
                SiraNo = y.SiraNo,
                Kaynak = y.Kaynak,
                SandikId = y.SandikId,
                ManuelGrupNo = y.ManuelGrupNo,
                BirimNo = y.BirimNo,
                BirimSayisi = y.BirimSayisi,
                Kod = y.Kod,
                Aciklama = y.Aciklama,
                ProjeId = y.ProjeId,
                ProjeNo = y.ProjeNo,
                YukTuruId = y.YukTuruId,
                YukTuruAdi = yukTurleri.GetValueOrDefault(y.YukTuruId, string.Empty),
                BoyMm = y.BoyMm,
                EnMm = y.EnMm,
                YukseklikMm = y.YukseklikMm,
                BrutKg = y.BrutKg,
                LokasyonId = y.LokasyonId,
                LokasyonAdi = y.LokasyonAdi,
                Dahil = y.Dahil,
                DondurulebilirMi = y.DondurulebilirMi,
                UsteKonabilirMi = y.UsteKonabilirMi,
                AltaKalabilirMi = y.AltaKalabilirMi,
                UstYukSiniriKg = y.UstYukSiniriKg,
                ElleDegisenIzinler = y.ElleDegisenIzinler,
                OzelTasimaNotu = y.OzelTasimaNotu,
                Durum = durum,
                VeriSorunlari = sorunlar,
                AracNo = aracNo,
                YerlesimNotu = neden
            };
        }).ToList();

        return new YuklemePlaniDetayi
        {
            Id = plan.Id,
            PlanNo = plan.PlanNo,
            Ad = plan.Ad,
            CalismaBicimi = plan.CalismaBicimi,
            TasimaAilesiId = plan.TasimaAilesiId,
            TasimaAilesiAdi = plan.TasimaAilesi.Ad,
            ProjeKarisimi = plan.ProjeKarisimi,
            LokasyonTercihi = plan.LokasyonTercihi,
            YuklemeTarihi = plan.YuklemeTarihi,
            Aciklama = plan.Aciklama,
            RevizyonNo = revizyon.RevizyonNo,
            RevizyonTarihi = revizyon.CreatedDate,
            Kaydeden = kaydeden,
            RevizyonAciklamasi = revizyon.Aciklama,
            Ayarlar = Ayarlar(revizyon),
            Projeler = revizyon.Projeler.OrderBy(p => p.ProjeNo).Select(p => new PlanProjeBilgisi
            {
                ProjeId = p.ProjeId,
                ProjeNo = p.ProjeNo,
                Musteri = p.ProjeId is { } id && projeBilgileri.TryGetValue(id, out var b) ? b.Musteri : null,
                ProjeTipiId = p.ProjeId is { } tid && projeBilgileri.TryGetValue(tid, out var t) ? t.ProjeTipiId : null,
                SevkEdilmisSandikSayisi = p.ProjeId is { } sid ? sevkliSayilari.GetValueOrDefault(sid) : 0
            }).ToList(),
            Yukler = satirlar,
            Sayaclar = new PlanYukSayaclari
            {
                Toplam = satirlar.Count,
                Secilen = satirlar.Count(s => s.Dahil),
                Hesaplanabilir = satirlar.Count(s => s.Durum is PlanYukDurumu.Bekliyor or PlanYukDurumu.Yerlesti or PlanYukDurumu.Yerlesemedi),
                VeriEksik = satirlar.Count(s => s.Durum == PlanYukDurumu.EksikVeri),
                HaricTutulan = satirlar.Count(s => s.Durum == PlanYukDurumu.HaricTutuldu),
                SevkEdilmisSandik = sevkliSayilari.Values.Sum(),
                Yerlesen = satirlar.Count(s => s.Durum == PlanYukDurumu.Yerlesti),
                Yerlesemeyen = satirlar.Count(s => s.Durum == PlanYukDurumu.Yerlesemedi)
            },
            KaynakFarklari = KaynakFarklari(revizyon, guncel, projeIdleri, yukTurleri),
            Yerlesim = await YerlesimAsync(plan, revizyon, sonuc, cancellationToken),
            FarkliLokasyonSayisi = satirlar.Where(s => s.Dahil && s.VeriSorunlari.Count == 0)
                .Select(s => YuklemePlaniKurali.LokasyonBelirsizMi(s.LokasyonId) ? null : s.LokasyonId).Distinct().Count(),
            SurenYukleme = await SurenYuklemeAsync(plan, cancellationToken)
        };
    }

    /// <summary>
    /// Plandaki sandık kopyası ile güncel sandığın farkları; yeni sandıklar (plana gelen, kopyada olmayan) sonda.
    /// </summary>
    private static List<PlanKaynakFarki> KaynakFarklari(
        YuklemePlanRevizyonu revizyon, IReadOnlyList<SandikKaynagi> guncel, IReadOnlyCollection<int> projeIdleri,
        IReadOnlyDictionary<int, string> yukTurleri)
    {
        var guncelById = guncel.ToDictionary(s => s.Id);
        var farklar = new List<PlanKaynakFarki>();
        foreach (var y in revizyon.Yukler.Where(y => y.Kaynak == PlanYukKaynagi.Sandik).OrderBy(y => y.SiraNo))
        {
            if (y.SandikId is not { } id || !guncelById.TryGetValue(id, out var s))
            {
                farklar.Add(new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.SandikSilindi, SandikId = y.SandikId, Kod = y.Kod });
                continue;
            }
            if (!projeIdleri.Contains(s.ProjeId))
            {
                farklar.Add(new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.ProjedenCikti, SandikId = id, Kod = y.Kod, Guncel = s.Kod });
                continue;
            }
            if (!YuklemePlaniKurali.PlanaGelirMi(s.DurumId))
            {
                farklar.Add(new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.SandikSevkEdildi, SandikId = id, Kod = y.Kod });
                continue;
            }
            if (s.Kod != y.Kod)
                farklar.Add(new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.KodDegisti, SandikId = id, Kod = y.Kod, Onceki = y.Kod, Guncel = s.Kod });
            if (s.Boy != y.BoyMm || s.En != y.EnMm || s.Yukseklik != y.YukseklikMm)
                farklar.Add(new PlanKaynakFarki
                {
                    Tur = PlanKaynakFarkTuru.OlcuDegisti, SandikId = id, Kod = y.Kod,
                    Onceki = OlcuMetni(y.BoyMm, y.EnMm, y.YukseklikMm), Guncel = OlcuMetni(s.Boy, s.En, s.Yukseklik)
                });
            if (s.Brut != y.BrutKg)
                farklar.Add(new PlanKaynakFarki
                {
                    Tur = PlanKaynakFarkTuru.AgirlikDegisti, SandikId = id, Kod = y.Kod, Onceki = KgMetni(y.BrutKg), Guncel = KgMetni(s.Brut)
                });
            if (s.LokasyonId != y.LokasyonId)
                farklar.Add(new PlanKaynakFarki
                {
                    Tur = PlanKaynakFarkTuru.LokasyonDegisti, SandikId = id, Kod = y.Kod, Onceki = y.LokasyonAdi, Guncel = s.LokasyonAdi
                });
            if (s.YukTuruId != y.YukTuruId)
                farklar.Add(new PlanKaynakFarki
                {
                    Tur = PlanKaynakFarkTuru.YukTuruDegisti, SandikId = id, Kod = y.Kod,
                    Onceki = yukTurleri.GetValueOrDefault(y.YukTuruId), Guncel = yukTurleri.GetValueOrDefault(s.YukTuruId)
                });
        }

        var plandakiler = OncekiSandikIdleri(revizyon).ToHashSet();
        farklar.AddRange(PlanaGelenler(guncel, projeIdleri)
            .Where(s => !plandakiler.Contains(s.Id))
            .Select(s => new PlanKaynakFarki { Tur = PlanKaynakFarkTuru.YeniSandik, SandikId = s.Id, Kod = s.Kod }));
        return farklar;
    }

    private async Task<List<YuklemePlanYuku>> ManuelSatirlarAsync(
        IReadOnlyList<PlanManuelYukKaydi> manuelYukler, IReadOnlyList<SecimOgesi> projeler, int oncekiSatirSayisi,
        CancellationToken cancellationToken)
    {
        var satirlar = new List<YuklemePlanYuku>();
        if (manuelYukler.Count == 0)
            return satirlar;

        var projeNolari = projeler.ToDictionary(p => p.Id, p => p.Ad);
        var lokasyonlar = await context.LookupDepoLokasyonlari.AsNoTracking().ToDictionaryAsync(l => l.Id, l => l.Deger, cancellationToken);
        var grupNo = 0;
        foreach (var m in manuelYukler)
        {
            if (m.ProjeId is { } projeId && !projeNolari.ContainsKey(projeId))
                throw new IsKuraliException(ManuelProjeGecersiz);
            if (m.LokasyonId is { } lokasyonId && !lokasyonlar.ContainsKey(lokasyonId))
                throw new IsKuraliException(LokasyonBulunamadi);

            grupNo++;
            var birimler = m.Birimler.ToDictionary(b => b.BirimNo);
            var varsayilan = YukIzinKurali.Varsayilan(m.YukTuruId, m.BrutKg);
            for (var birimNo = 1; birimNo <= m.Adet; birimNo++)
            {
                var birim = birimler.GetValueOrDefault(birimNo);
                satirlar.Add(new YuklemePlanYuku
                {
                    SiraNo = oncekiSatirSayisi + satirlar.Count + 1,
                    Kaynak = PlanYukKaynagi.Manuel,
                    ManuelGrupNo = grupNo,
                    BirimNo = birimNo,
                    BirimSayisi = m.Adet,
                    Kod = m.Kod,
                    Aciklama = m.Aciklama,
                    ProjeId = m.ProjeId,
                    ProjeNo = m.ProjeId is { } pid ? projeNolari[pid] : null,
                    YukTuruId = m.YukTuruId,
                    BoyMm = m.BoyMm,
                    EnMm = m.EnMm,
                    YukseklikMm = m.YukseklikMm,
                    BrutKg = m.BrutKg,
                    LokasyonId = m.LokasyonId,
                    LokasyonAdi = m.LokasyonId is { } lid ? lokasyonlar[lid] : null,
                    Dahil = birim?.Dahil ?? true,
                    DondurulebilirMi = birim?.DondurulebilirMi ?? varsayilan.DondurulebilirMi,
                    UsteKonabilirMi = birim?.UsteKonabilirMi ?? varsayilan.UsteKonabilirMi,
                    AltaKalabilirMi = birim?.AltaKalabilirMi ?? varsayilan.AltaKalabilirMi,
                    UstYukSiniriKg = birim != null ? birim.UstYukSiniriKg : varsayilan.UstYukSiniriKg,
                    ElleDegisenIzinler = birim?.ElleDegisenIzinler ?? PlanYukIzni.Yok,
                    OzelTasimaNotu = m.OzelTasimaNotu
                });
            }
        }
        return satirlar;
    }

    private void YeniRevizyonEkle(
        YuklemePlani plan, int? kullaniciId, string? aciklama, YuklemePlanAyarlari ayarlar,
        List<YuklemePlanProjesi> projeler, List<YuklemePlanYuku> yukler, string? yerlesimJson = null)
    {
        plan.SonRevizyonNo++;
        context.YuklemePlanRevizyonlari.Add(new YuklemePlanRevizyonu
        {
            PlanId = plan.Id,
            RevizyonNo = plan.SonRevizyonNo,
            KaydedenKullaniciId = kullaniciId,
            Aciklama = aciklama,
            AyarlarJson = JsonSerializer.Serialize(ayarlar, AyarJsonu),
            YerlesimJson = yerlesimJson,
            Projeler = projeler,
            Yukler = yukler
        });
    }

    /// <summary>Seçilen tipler planın ailesinde, aktif ve sandık taşımaya uygun olmalı.</summary>
    private async Task TipleriDenetleAsync(int aileId, IReadOnlyList<int> tipIdleri, CancellationToken cancellationToken)
    {
        if (tipIdleri.Count == 0)
            return;
        var kategoriler = await context.EkipmanTipleri.AsNoTracking()
            .Where(t => tipIdleri.Contains(t.Id) && t.AileId == aileId && t.Aktif)
            .Select(t => t.Kategori).ToListAsync(cancellationToken);
        if (kategoriler.Count != tipIdleri.Count || !kategoriler.All(EkipmanKategoriKurali.SandikTasimayaUygunMu))
            throw new IsKuraliException(YuklemePlaniKurali.TipAileDisiMesaji);
    }

    private async Task AileyiDenetleAsync(int aileId, CancellationToken cancellationToken)
    {
        var kategoriler = await context.EkipmanTipleri.AsNoTracking()
            .Where(t => t.AileId == aileId && t.Aktif).Select(t => t.Kategori).ToListAsync(cancellationToken);
        if (!kategoriler.Any(EkipmanKategoriKurali.SandikTasimayaUygunMu))
            throw new IsKuraliException(AileGecersiz);
    }

    private async Task<List<SecimOgesi>> ProjeleriAsync(IReadOnlyList<int> projeIdleri, CancellationToken cancellationToken)
    {
        var projeler = await context.Projeler.AsNoTracking()
            .Where(p => projeIdleri.Contains(p.Id)).Select(p => new SecimOgesi(p.Id, p.ProjeNo)).ToListAsync(cancellationToken);
        if (projeler.Count != projeIdleri.Distinct().Count())
            throw new KayitBulunamadiException(ProjeBulunamadi);
        return projeler;
    }

    private async Task<List<SandikKaynagi>> SandiklarAsync(
        IReadOnlyCollection<int> projeIdleri, IReadOnlyCollection<int> ekSandikIdleri, CancellationToken cancellationToken) =>
        await context.Sandiklar.AsNoTracking()
            .Where(s => projeIdleri.Contains(s.ProjeId) || ekSandikIdleri.Contains(s.Id))
            .Select(s => new SandikKaynagi(
                s.Id, s.ProjeId, s.Proje.ProjeNo, s.SandikNo, s.Ad, s.Boy, s.En, s.Yukseklik, s.GrossKg, s.DepoLokasyonId,
                s.DepoLokasyonLookup != null ? s.DepoLokasyonLookup.Deger : null, s.YukTuruId, s.DurumId))
            .ToListAsync(cancellationToken);

    /// <summary>Plana gelen sandıklar (projelerde, sevk edilmemiş), proje ve doğal sandık numarası sırasıyla.</summary>
    private static List<SandikKaynagi> PlanaGelenler(IEnumerable<SandikKaynagi> sandiklar, IReadOnlyCollection<int> projeIdleri) =>
        sandiklar
            .Where(s => projeIdleri.Contains(s.ProjeId) && YuklemePlaniKurali.PlanaGelirMi(s.DurumId))
            .OrderBy(s => s.ProjeNo, StringComparer.Create(Tr, ignoreCase: false))
            .ThenBy(s => s.SandikNo.Length).ThenBy(s => s.SandikNo, StringComparer.Ordinal).ThenBy(s => s.Id)
            .ToList();

    private static async Task<Dictionary<int, int>> ProjeSandikSayilariAsync(IQueryable<Sandik> sandiklar, CancellationToken cancellationToken) =>
        await sandiklar.GroupBy(s => s.ProjeId).Select(g => new { ProjeId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.ProjeId, x => x.Adet, cancellationToken);

    private static List<int> OncekiSandikIdleri(YuklemePlanRevizyonu revizyon) =>
        revizyon.Yukler.Where(y => y.SandikId != null).Select(y => y.SandikId!.Value).ToList();

    private static List<YuklemePlanProjesi> ProjeSatirlari(IEnumerable<SecimOgesi> projeler) =>
        projeler.OrderBy(p => p.Ad, StringComparer.Create(Tr, ignoreCase: false))
            .Select(p => new YuklemePlanProjesi { ProjeId = p.Id, ProjeNo = p.Ad })
            .ToList();

    private static YuklemePlanYuku SandikSatiri(SandikKaynagi s, int siraNo, bool dahil, PlanYukIzinleri izin, PlanYukIzni elleDegisen) => new()
    {
        SiraNo = siraNo,
        Kaynak = PlanYukKaynagi.Sandik,
        SandikId = s.Id,
        BirimNo = 1,
        BirimSayisi = 1,
        Kod = s.Kod,
        Aciklama = s.Ad,
        ProjeId = s.ProjeId,
        ProjeNo = s.ProjeNo,
        YukTuruId = s.YukTuruId,
        BoyMm = s.Boy,
        EnMm = s.En,
        YukseklikMm = s.Yukseklik,
        BrutKg = s.Brut,
        LokasyonId = s.LokasyonId,
        LokasyonAdi = s.LokasyonAdi,
        Dahil = dahil,
        DondurulebilirMi = izin.DondurulebilirMi,
        UsteKonabilirMi = izin.UsteKonabilirMi,
        AltaKalabilirMi = izin.AltaKalabilirMi,
        UstYukSiniriKg = izin.UstYukSiniriKg,
        ElleDegisenIzinler = elleDegisen
    };

    /// <summary>Önceki revizyonun satırını yeni revizyona aynen kopyalar (kimliksiz).</summary>
    private static YuklemePlanYuku Kopyala(YuklemePlanYuku y, int siraNo) => new()
    {
        SiraNo = siraNo,
        Kaynak = y.Kaynak,
        SandikId = y.SandikId,
        ManuelGrupNo = y.ManuelGrupNo,
        BirimNo = y.BirimNo,
        BirimSayisi = y.BirimSayisi,
        Kod = y.Kod,
        Aciklama = y.Aciklama,
        ProjeId = y.ProjeId,
        ProjeNo = y.ProjeNo,
        YukTuruId = y.YukTuruId,
        BoyMm = y.BoyMm,
        EnMm = y.EnMm,
        YukseklikMm = y.YukseklikMm,
        BrutKg = y.BrutKg,
        LokasyonId = y.LokasyonId,
        LokasyonAdi = y.LokasyonAdi,
        Dahil = y.Dahil,
        DondurulebilirMi = y.DondurulebilirMi,
        UsteKonabilirMi = y.UsteKonabilirMi,
        AltaKalabilirMi = y.AltaKalabilirMi,
        UstYukSiniriKg = y.UstYukSiniriKg,
        ElleDegisenIzinler = y.ElleDegisenIzinler,
        OzelTasimaNotu = y.OzelTasimaNotu
    };

    private static void SecimiUygula(YuklemePlanYuku satir, bool dahil, PlanYukIzinleri izin, PlanYukIzni elleDegisen)
    {
        satir.Dahil = dahil;
        satir.DondurulebilirMi = izin.DondurulebilirMi;
        satir.UsteKonabilirMi = izin.UsteKonabilirMi;
        satir.AltaKalabilirMi = izin.AltaKalabilirMi;
        satir.UstYukSiniriKg = izin.UstYukSiniriKg;
        satir.ElleDegisenIzinler = elleDegisen;
    }

    private static YuklemePlanAyarlari Ayarlar(YuklemePlanRevizyonu revizyon)
    {
        var ayarlar = JsonSerializer.Deserialize<YuklemePlanAyarlari>(revizyon.AyarlarJson, AyarJsonu) ?? new YuklemePlanAyarlari();
        // JSON'da olmayan ya da açık null gelen liste boş sayılır (MevcutAraclar Faz 2'de, TipIdleri 19.11.2026'da eklendi).
        return ayarlar with { MevcutAraclar = ayarlar.MevcutAraclar ?? [], TipIdleri = ayarlar.TipIdleri ?? [] };
    }

    private static string OlcuMetni(decimal? boy, decimal? en, decimal? yukseklik) =>
        $"{Sayi(boy)} × {Sayi(en)} × {Sayi(yukseklik)} mm";

    private static string KgMetni(decimal? kg) => $"{Sayi(kg)} kg";

    private static string Sayi(decimal? deger) => deger?.ToString("#,0.##", Tr) ?? "—";

    private static string IlikeDeseniniKacir(string metin) =>
        metin.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
