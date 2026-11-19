using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Ekipman;
using _3K.YuklemeMotoru.Yerlesim;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Otomatik yerleşim (Simülasyon isterleri 2, 8–10, 16, 22–27): son revizyonun planlamaya dahil ve verisi tam yükleri
/// motora verilir, sonuç yeni revizyonda saklanır. Adaylar: planın taşıma ailesindeki sandık taşımaya uygun aktif
/// konfigürasyonlar ve zorunlu özel yük için lowbed / ağır yük dorseleri.
/// </summary>
internal sealed partial class YuklemePlaniService
{
    private const string HesaplanacakYukYok = "Hesaplanacak yük yok: planlamaya dahil ve ölçü/ağırlığı tam en az bir yük olmalıdır.";
    private const string MevcutAracBulunamadi = "Mevcut araçlardan biri kitaplıkta bulunamadı veya pasif; araç listesini güncelleyin.";
    private const string MevcutAracKullanilamaz = "Mevcut araçlardan biri sandık taşımaya uygun değil ya da teknik verisi okunamıyor; araç listesini güncelleyin.";

    public async Task<YuklemePlaniDetayi> HesaplaAsync(
        int planId, int beklenenRevizyonNo, YuklemeLokasyonTercihi lokasyonTercihi, int? kullaniciId, CancellationToken cancellationToken)
    {
        var plan = await context.YuklemePlanlari.Include(p => p.TasimaAilesi).SingleOrDefaultAsync(p => p.Id == planId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
        if (plan.SonRevizyonNo != beklenenRevizyonNo)
            throw IsKuraliException.Cakisma(EszamanliKayit);

        var onceki = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        var ayarlar = Ayarlar(onceki);
        var mevcut = plan.CalismaBicimi == YuklemeCalismaBicimi.MevcutAraclaraYerlestirme ? ayarlar.MevcutAraclar : null;
        if (mevcut is { Count: 0 })
            throw new IsKuraliException(YuklemePlaniKurali.MevcutAracGerekliMesaji);

        var sonuc = await YerlesimHesaplaAsync(plan.TasimaAilesiId, plan.TasimaAilesi.Ad, plan.ProjeKarisimi, lokasyonTercihi, ayarlar, mevcut,
            onceki.Yukler, cancellationToken);

        plan.LokasyonTercihi = lokasyonTercihi;
        var satirlar = onceki.Yukler.OrderBy(y => y.SiraNo).Select((y, i) => Kopyala(y, i + 1)).ToList();
        var projeler = onceki.Projeler.Select(p => new YuklemePlanProjesi { ProjeId = p.ProjeId, ProjeNo = p.ProjeNo }).ToList();
        YeniRevizyonEkle(plan, kullaniciId, "Otomatik yerleşim hesaplandı.", ayarlar, projeler, satirlar, YerlesimSerilestirici.Yaz(sonuc));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    public async Task<YuklemePlaniDetayi> PlanHesaplaAsync(YuklemePlaniHesaplaModel model, int? kullaniciId, CancellationToken cancellationToken)
    {
        await AileyiDenetleAsync(model.TasimaAilesiId, cancellationToken);
        await TipleriDenetleAsync(model.TasimaAilesiId, model.Ayarlar.TipIdleri, cancellationToken);
        var projeler = await ProjeleriAsync(model.ProjeIdleri, cancellationToken);
        var aileAdi = await context.EkipmanAileleri.AsNoTracking().Where(a => a.Id == model.TasimaAilesiId).Select(a => a.Ad).SingleAsync(cancellationToken);

        YuklemePlani plan;
        YuklemePlanRevizyonu? onceki = null;
        if (model.PlanId is { } planId)
        {
            plan = await context.YuklemePlanlari.SingleOrDefaultAsync(p => p.Id == planId, cancellationToken)
                   ?? throw new KayitBulunamadiException(PlanBulunamadi);
            if (plan.SonRevizyonNo != model.BeklenenRevizyonNo)
                throw IsKuraliException.Cakisma(EszamanliKayit);
            onceki = await RevizyonAsync(plan.Id, plan.SonRevizyonNo, cancellationToken);
        }
        else
        {
            var yil = TurkeyTime.Now.Year;
            var onek = YuklemePlaniKurali.PlanNoOneki(yil);
            var sonNo = await context.YuklemePlanlari.Where(p => p.PlanNo.StartsWith(onek)).MaxAsync(p => (string?)p.PlanNo, cancellationToken);
            plan = new YuklemePlani
            {
                PlanNo = YuklemePlaniKurali.SonrakiPlanNo(yil, sonNo),
                Ad = OtomatikAd(projeler),
                OlusturanKullaniciId = kullaniciId
            };
        }
        // Ad elle girilmez: otomatik adlı planın adı proje kümesiyle birlikte güncellenir (elle adlandırılmış eski plan korunur).
        if (onceki != null && plan.Ad == OtomatikAd(onceki.Projeler.Where(p => p.ProjeId != null).Select(p => new SecimOgesi(p.ProjeId!.Value, p.ProjeNo))))
            plan.Ad = OtomatikAd(projeler);

        // Yükler her hesapta kaynaktan kurulur (sevk edilmişler dahil, planlamaya dahil, varsayılan izinlerle); formdan
        // kalkan seçimler (çalışma biçimi, proje karışımı, lokasyon tercihi, mevcut araçlar) varsayılandır. Eski planda
        // elle eklenmiş yükler seçili projelerdeyse (ya da projesizse) korunur.
        var sandiklar = PlanaGelenler(await SandiklarAsync(model.ProjeIdleri, [], cancellationToken), model.ProjeIdleri);
        var satirlar = sandiklar.Select((s, i) =>
            SandikSatiri(s, i + 1, dahil: true, YukIzinKurali.Varsayilan(s.YukTuruId, s.Brut), PlanYukIzni.Yok)).ToList();
        var secili = model.ProjeIdleri.ToHashSet();
        var eskiManueller = (onceki?.Yukler ?? []).Where(y => y.Kaynak == PlanYukKaynagi.Manuel && (y.ProjeId == null || secili.Contains(y.ProjeId.Value)));
        foreach (var manuel in eskiManueller.OrderBy(y => y.SiraNo))
            satirlar.Add(Kopyala(manuel, satirlar.Count + 1));
        if (satirlar.Count > YuklemePlaniKurali.EnFazlaYuk)
            throw new IsKuraliException(YukSiniriAsildi);

        // Ekranda olmayan ayar (ağırlık merkezi sapması) var olan planda önceki revizyondan gelir; istemcinin gönderdiği
        // değer onu değiştirmez. Yükler arası boşluk ekrandadır (isteğe bağlı). Mevcut araç listesi boşaltılır.
        var oncekiAyarlar = onceki == null ? null : Ayarlar(onceki);
        var ayarlar = model.Ayarlar with
        {
            MevcutAraclar = [],
            AgirlikMerkeziSapmaYuzde = oncekiAyarlar?.AgirlikMerkeziSapmaYuzde
        };
        var sonuc = await YerlesimHesaplaAsync(model.TasimaAilesiId, aileAdi, YuklemeProjeKarisimi.Karisik, YuklemeLokasyonTercihi.Karisik,
            ayarlar, null, satirlar, cancellationToken);

        plan.TasimaAilesiId = model.TasimaAilesiId;
        plan.CalismaBicimi = YuklemeCalismaBicimi.IhtiyacaGoreArac;
        plan.ProjeKarisimi = YuklemeProjeKarisimi.Karisik;
        plan.LokasyonTercihi = YuklemeLokasyonTercihi.Karisik;
        // Yeni plan, hesap başarılı olunca eklenir: reddedilen hesap bağlamda yarım kayıt bırakmaz.
        if (plan.Id == 0)
            context.YuklemePlanlari.Add(plan);
        YeniRevizyonEkle(plan, kullaniciId, "Hesaplandı.", ayarlar, ProjeSatirlari(projeler), satirlar, YerlesimSerilestirici.Yaz(sonuc));
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException ihlal) when (ihlal.ConstraintName == PlanNoIndeksi)
        {
            // İki kişi aynı anda yeni plan açtı: plan numarası tekildir, ikincisi yeniden dener.
            throw IsKuraliException.Cakisma(AyniAndaYeniPlan);
        }
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

    private const string PlanNoIndeksi = "IX_YuklemePlanlari_PlanNo";
    private const string AyniAndaYeniPlan = "Aynı anda başka bir plan açıldı; tekrar \"Hesapla\"ya basın.";

    private static string OtomatikAd(IEnumerable<SecimOgesi> projeler) =>
        YuklemePlaniKurali.OtomatikPlanAdi(projeler.Select(p => p.Ad).Order(StringComparer.Create(Tr, ignoreCase: false)));

    /// <summary>
    /// Motor girdisini kurup otomatik yerleşimi hesaplar: planlamaya dahil ve ölçü/ağırlığı tam yükler, ailenin ve özel yük
    /// ekipmanının adayları (tip seçimiyle kademe) ve paylar. Hesaplanacak yük yoksa 400.
    /// </summary>
    private async Task<YerlesimSonucu> YerlesimHesaplaAsync(
        int aileId, string aileAdi, YuklemeProjeKarisimi karisim, YuklemeLokasyonTercihi lokasyonTercihi, YuklemePlanAyarlari ayarlar,
        IReadOnlyList<PlanMevcutArac>? mevcut, IEnumerable<YuklemePlanYuku> satirlar, CancellationToken cancellationToken)
    {
        var yukTurleri = await context.LookupYukTurleri.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Deger, cancellationToken);
        var yukler = satirlar
            .Where(y => y.Dahil && YukVeriKurali.Sorunlar(y.BoyMm, y.EnMm, y.YukseklikMm, y.BrutKg).Count == 0)
            .OrderBy(y => y.SiraNo)
            .Select(y => MotorYuku(y, yukTurleri))
            .ToList();
        if (yukler.Count == 0)
            throw new IsKuraliException(HesaplanacakYukYok);

        var mevcutIdleri = mevcut?.Select(m => m.KonfigurasyonId).ToList() ?? [];
        if (mevcutIdleri.Count > 0)
            await MevcutAraclariDenetleAsync(ayarlar, cancellationToken);
        var adaylar = await AdaylarAsync(aileId, ayarlar.TipIdleri, mevcutIdleri, cancellationToken);
        if (mevcutIdleri.Any(id => adaylar.All(a => a.KonfigurasyonId != id)))
            throw new IsKuraliException(MevcutAracKullanilamaz);

        var girdi = new YerlesimGirdisi(
            yukler,
            adaylar,
            MotorAyarlari(ayarlar),
            karisim == YuklemeProjeKarisimi.ProjeBazindaAyri,
            lokasyonTercihi == YuklemeLokasyonTercihi.LokasyonaGoreAyri,
            mevcut?.Select(m => new MevcutArac(m.KonfigurasyonId, m.Adet)).ToList(),
            aileAdi);
        return YerlesimMotoru.Hesapla(girdi, cancellationToken);
    }

    /// <summary>
    /// Motor ayarları: güvenlik payları kaldırıldığı için 0 verilir (pay uygulanmaz; boş pay gibi kontrolü teyitsiz
    /// bırakmaz, ağırlık, iç yükseklik ve kapıdan geçiş uygun/uygunsuz değerlendirilir).
    /// </summary>
    private static YerlesimAyarlari MotorAyarlari(YuklemePlanAyarlari ayarlar) =>
        new(YuklemePlaniKurali.UygulananYerlesimBoslugu(ayarlar), YukseklikPayiMm: 0, GenislikPayiMm: 0, UzunlukPayiMm: 0, AgirlikPayiKg: 0,
            YuklemeHareketiPayiMm: 0, ayarlar.AgirlikMerkeziSapmaYuzde);

    /// <summary>Kaydedilen mevcut araç listesindeki konfigürasyonlar kitaplıkta var ve aktif olmalı.</summary>
    private async Task MevcutAraclariDenetleAsync(YuklemePlanAyarlari ayarlar, CancellationToken cancellationToken)
    {
        if (ayarlar.MevcutAraclar.Count == 0)
            return;
        var idler = ayarlar.MevcutAraclar.Select(m => m.KonfigurasyonId).ToList();
        var bulunan = await context.EkipmanKonfigurasyonlari.CountAsync(k => idler.Contains(k.Id) && k.Aktif, cancellationToken);
        if (bulunan != idler.Count)
            throw new IsKuraliException(MevcutAracBulunamadi);
    }

    /// <summary>Plan satırını motor yüküne çevirir; ölçüler yukarı yuvarlanır (küçük göstermemek için).</summary>
    private static YerlesimYuku MotorYuku(YuklemePlanYuku y, IReadOnlyDictionary<int, string> yukTurleri) => new(
        YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo),
        y.BirimSayisi > 1 ? $"{y.Kod} ({y.BirimNo}/{y.BirimSayisi})" : y.Kod,
        y.ProjeId,
        y.ProjeNo,
        // Belirsiz lokasyonlu yükler lokasyona göre ayrı planda ayrı bir "belirsiz" grubunda toplanır (isterler 26).
        YuklemePlaniKurali.LokasyonBelirsizMi(y.LokasyonId) ? null : y.LokasyonId,
        (int)Math.Ceiling(y.BoyMm!.Value),
        (int)Math.Ceiling(y.EnMm!.Value),
        (int)Math.Ceiling(y.YukseklikMm!.Value),
        y.BrutKg!.Value,
        y.DondurulebilirMi,
        y.UsteKonabilirMi,
        y.AltaKalabilirMi,
        y.UstYukSiniriKg,
        YukTuruKurali.OzelYukMu(y.YukTuruId),
        YukTuruKurali.OzelEkipmanZorunluMu(y.YukTuruId) ? OzelEkipmanZorunlulugu.Her : OzelEkipmanZorunlulugu.Yok,
        yukTurleri.GetValueOrDefault(y.YukTuruId));

    /// <summary>
    /// Aday konfigürasyonlar (son revizyonları): ailedeki sandık taşımaya uygun aktif tipler, özel yük ekipmanları ve
    /// kullanıcının mevcut araç listesindekiler. Teknik verisi okunamayan konfigürasyon aday olmaz. Aile tiplerinin
    /// kademesi sadelik sırasından ve plandaki tip seçiminden gelir (<see cref="YuklemePlaniKurali.AracTipiKademesi"/>).
    /// </summary>
    private async Task<List<EkipmanAdayi>> AdaylarAsync(int aileId, IReadOnlyList<int> tipIdleri, List<int> mevcutIdleri, CancellationToken cancellationToken)
    {
        // Kayıtlı seçimden yalnız bu ailenin aktif tipleri sayılır (sonradan pasif yapılan tip seçimi daraltmaz).
        var secili = tipIdleri.Count == 0
            ? []
            : (await context.EkipmanTipleri.AsNoTracking()
                .Where(t => tipIdleri.Contains(t.Id) && t.AileId == aileId && t.Aktif).Select(t => t.Id)
                .ToListAsync(cancellationToken)).ToHashSet();
        var ozelKategoriler = Enum.GetValues<EkipmanKategorisi>().Where(EkipmanKategoriKurali.OzelYukEkipmaniMi).ToList();
        var kayitlar = await context.EkipmanKonfigurasyonlari.AsNoTracking()
            .Where(k => (k.Aktif && k.Model.Tip.Aktif && (k.Model.Tip.AileId == aileId || ozelKategoriler.Contains(k.Model.Tip.Kategori))) ||
                        (k.Aktif && k.Model.Tip.Aktif && mevcutIdleri.Contains(k.Id)))
            .OrderBy(k => k.Kod)
            .Select(k => new
            {
                k.Id,
                k.Kod,
                k.Ad,
                k.SonRevizyonNo,
                k.Model.Tip.Kategori,
                k.Model.Tip.AileId,
                k.Model.TipId,
                k.Model.Tip.OtomatikSecimSirasi,
                TipAdi = k.Model.Tip.Ad,
                AileAdi = k.Model.Tip.Aile.Ad,
                Revizyon = k.Revizyonlar.Where(r => r.RevizyonNo == k.SonRevizyonNo)
                    .Select(r => new { r.TeknikVeriJson, r.DogrulamaDurumu }).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var adaylar = new List<EkipmanAdayi>();
        foreach (var k in kayitlar)
        {
            if (k.Revizyon == null || !EkipmanKategoriKurali.SandikTasimayaUygunMu(k.Kategori) ||
                EkipmanTeknikVeriSerilestirici.Oku(k.Revizyon.TeknikVeriJson) is not { } veri)
                continue;
            // Aile dışı aday (özel yük ekipmanı, mevcut araç) kademe almaz: normal yükte aile adayı olarak kullanılmaz.
            var kademe = k.AileId == aileId ? YuklemePlaniKurali.AracTipiKademesi(k.TipId, k.OtomatikSecimSirasi, secili) : (null, false);
            adaylar.Add(new EkipmanAdayi(
                k.Id, k.SonRevizyonNo, k.Kod, k.Ad, k.AileAdi, k.TipAdi,
                AileIci: k.AileId == aileId,
                OzelYukEkipmani: EkipmanKategoriKurali.OzelYukEkipmaniMi(k.Kategori),
                (GirisYonu)(int)EkipmanKategoriKurali.YuklemeYontemleri(k.Kategori),
                EkipmanKategoriKurali.DuvarliMi(k.Kategori),
                EkipmanKategoriKurali.DingilliMi(k.Kategori),
                k.Revizyon.DogrulamaDurumu == EkipmanDogrulamaDurumu.Dogrulandi,
                veri,
                kademe.Kademe,
                kademe.Yedek));
        }
        return adaylar;
    }

    /// <summary>
    /// Saklanan yerleşimi ekran modeline çevirir: satır bilgileri, proje/lokasyon adları ve kullanılan ekipman geometrisi.
    /// Ekran, saha ve raporlar buradan geçer; "Teyit edilmedi" gösterilmez (YerlesimGosterimKurali).
    /// </summary>
    private async Task<PlanYerlesimi?> YerlesimAsync(
        YuklemePlani plan, YuklemePlanRevizyonu revizyon, YerlesimSonucu? sonuc, CancellationToken cancellationToken, DateTime? hesapTarihi = null)
    {
        if (sonuc == null)
            return null;
        // Sandığı sonradan silinen satırın sandık bağı boşalır ve anahtarı kalmaz (birden çok satır aynı "s:" anahtarını
        // üretirdi); yerleşimdeki eski anahtar satırsız kalır ve koduyla değil anahtarıyla gösterilir.
        var satirlar = revizyon.Yukler
            .Where(y => y.Kaynak != PlanYukKaynagi.Sandik || y.SandikId != null)
            .ToDictionary(y => YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo));
        var konfIdleri = sonuc.Araclar.Select(a => a.KonfigurasyonId).Distinct().ToList();
        var revizyonNolari = sonuc.Araclar.Select(a => a.RevizyonNo).Distinct().ToList();
        var geometriler = (await context.EkipmanKonfigurasyonRevizyonlari.AsNoTracking()
                .Where(r => konfIdleri.Contains(r.KonfigurasyonId) && revizyonNolari.Contains(r.RevizyonNo))
                .Select(r => new { r.KonfigurasyonId, r.RevizyonNo, r.TeknikVeriJson })
                .ToListAsync(cancellationToken))
            .ToDictionary(r => (r.KonfigurasyonId, r.RevizyonNo), r => r.TeknikVeriJson);

        PlanYukKonumu Konum(YukKonumu k)
        {
            var satir = satirlar.GetValueOrDefault(k.YukAnahtari);
            return new PlanYukKonumu
            {
                YukAnahtari = k.YukAnahtari,
                Kod = satir == null ? k.YukAnahtari : satir.BirimSayisi > 1 ? $"{satir.Kod} ({satir.BirimNo}/{satir.BirimSayisi})" : satir.Kod,
                ProjeNo = satir?.ProjeNo,
                BrutKg = satir?.BrutKg ?? 0,
                XMm = k.XMm,
                YMm = k.YMm,
                ZMm = k.ZMm,
                BoyMm = k.BoyMm,
                EnMm = k.EnMm,
                YukseklikMm = k.YukseklikMm,
                Donduruldu = k.Donduruldu,
                YuzeyKodu = k.YuzeyKodu,
                IstifNo = k.IstifNo,
                Kat = k.Kat,
                YuklemeSirasi = k.YuklemeSirasi,
                YuklemeYontemi = (YerlesimGirisYonu)(int)k.YuklemeYontemi
            };
        }

        return new PlanYerlesimi
        {
            HesapTarihi = hesapTarihi ?? revizyon.CreatedDate,
            Sonuc = YerlesimGosterimKurali.Sonuc((YerlesimKontrolSonucu)(int)sonuc.Sonuc),
            LokasyonaGoreAyri = plan.LokasyonTercihi == YuklemeLokasyonTercihi.LokasyonaGoreAyri,
            ProjeBazindaAyri = plan.ProjeKarisimi == YuklemeProjeKarisimi.ProjeBazindaAyri,
            ElleDuzenlendi = sonuc.ElleDuzenlendi,
            Araclar = sonuc.Araclar.Select(a =>
            {
                var aracSatirlari = a.Konumlar.Select(k => satirlar.GetValueOrDefault(k.YukAnahtari)).Where(s => s != null).ToList();
                return new PlanAraci
                {
                    AracNo = a.AracNo,
                    KonfigurasyonId = a.KonfigurasyonId,
                    RevizyonNo = a.RevizyonNo,
                    EkipmanKodu = a.EkipmanKodu,
                    EkipmanAdi = a.EkipmanAdi,
                    AileAdi = a.AileAdi,
                    TipAdi = a.TipAdi,
                    AileDisi = a.AileDisi,
                    ZorunluOzelEkipman = a.ZorunluOzelEkipman,
                    AileDisiGerekce = a.AileDisiGerekce,
                    ProjeNolari = aracSatirlari.Select(s => s!.ProjeNo ?? "Projesiz").Distinct().Order(StringComparer.Ordinal).ToList(),
                    Lokasyonlar = aracSatirlari
                        .Select(s => YuklemePlaniKurali.LokasyonBelirsizMi(s!.LokasyonId) ? YuklemeEtiketleri.BelirsizLokasyon : s.LokasyonAdi ?? YuklemeEtiketleri.BelirsizLokasyon)
                        .Distinct().Order(StringComparer.Ordinal).ToList(),
                    Konumlar = a.Konumlar.Select(Konum).ToList(),
                    ToplamKg = a.ToplamKg,
                    KapasiteKg = a.KapasiteKg,
                    TabanDolulukYuzde = a.TabanDolulukYuzde,
                    AgirlikMerkeziXMm = a.AgirlikMerkeziXMm,
                    YukluYukseklikMm = a.YukluYukseklikMm,
                    YukluGenislikMm = a.YukluGenislikMm,
                    YukluUzunlukMm = a.YukluUzunlukMm,
                    SolTasmaMm = a.SolTasmaMm,
                    SagTasmaMm = a.SagTasmaMm,
                    OnTasmaMm = a.OnTasmaMm,
                    ArkaTasmaMm = a.ArkaTasmaMm,
                    Kontroller = a.Kontroller.Select(k => new PlanAracKontrolu
                    {
                        Tur = (YerlesimKontrolTuru)(int)k.Tur,
                        Sonuc = (YerlesimKontrolSonucu)(int)k.Sonuc,
                        Mesaj = k.Mesaj
                    }).Where(k => YerlesimGosterimKurali.Gosterilir(k.Sonuc)).ToList(),
                    Sonuc = YerlesimGosterimKurali.Sonuc((YerlesimKontrolSonucu)(int)a.Sonuc),
                    YukSorunlari = a.YukSorunlari.Select(s => new PlanYukSorunu
                    {
                        YukAnahtari = s.YukAnahtari,
                        Sonuc = (YerlesimKontrolSonucu)(int)s.Sonuc,
                        Mesaj = s.Mesaj
                    }).Where(s => YerlesimGosterimKurali.Gosterilir(s.Sonuc)).ToList(),
                    TeknikVeriJson = geometriler.GetValueOrDefault((a.KonfigurasyonId, a.RevizyonNo), "{}")
                };
            }).ToList(),
            Yerlesemeyenler = sonuc.Yerlesemeyenler
                .Select(y => new PlanYerlesemeyenYuk { YukAnahtari = y.YukAnahtari, Kod = y.Kod, Neden = y.Neden }).ToList(),
            EkIhtiyaclar = sonuc.EkIhtiyaclar
                .Select(e => new PlanEkEkipmanIhtiyaci { KonfigurasyonId = e.KonfigurasyonId, EkipmanKodu = e.EkipmanKodu, EkipmanAdi = e.EkipmanAdi, Adet = e.Adet })
                .ToList(),
            Uyarilar = sonuc.Uyarilar.Where(YerlesimGosterimKurali.UyariGosterilir).ToList()
        };
    }
}
