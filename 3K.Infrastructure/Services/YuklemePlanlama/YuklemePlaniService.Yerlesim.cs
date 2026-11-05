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
/// konfigürasyonlar ve zorunlu özel yük için lowbed / ağır yük dorseleri. Yasal sınırlar yalnız onaylı kural setinden
/// gelir; taslak set kontrolde kullanılmaz.
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

        var yukTurleri = await context.LookupYukTurleri.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Deger, cancellationToken);
        var yukler = onceki.Yukler
            .Where(y => y.Dahil && YukVeriKurali.Sorunlar(y.BoyMm, y.EnMm, y.YukseklikMm, y.BrutKg).Count == 0)
            .OrderBy(y => y.SiraNo)
            .Select(y => MotorYuku(y, yukTurleri))
            .ToList();
        if (yukler.Count == 0)
            throw new IsKuraliException(HesaplanacakYukYok);

        var mevcutIdleri = mevcut?.Select(m => m.KonfigurasyonId).ToList() ?? [];
        if (mevcutIdleri.Count > 0)
            await MevcutAraclariDenetleAsync(ayarlar, cancellationToken);
        var adaylar = await AdaylarAsync(plan.TasimaAilesiId, mevcutIdleri, cancellationToken);
        if (mevcutIdleri.Any(id => adaylar.All(a => a.KonfigurasyonId != id)))
            throw new IsKuraliException(MevcutAracKullanilamaz);

        var girdi = new YerlesimGirdisi(
            yukler,
            adaylar,
            new YerlesimAyarlari(ayarlar.YerlesimBosluguMm, ayarlar.YukseklikPayiMm, ayarlar.GenislikPayiMm, ayarlar.UzunlukPayiMm,
                ayarlar.AgirlikPayiKg, ayarlar.YuklemeHareketiPayiMm),
            plan.ProjeKarisimi == YuklemeProjeKarisimi.ProjeBazindaAyri,
            lokasyonTercihi == YuklemeLokasyonTercihi.LokasyonaGoreAyri,
            mevcut?.Select(m => new MevcutArac(m.KonfigurasyonId, m.Adet)).ToList(),
            await YasalSinirlarAsync(cancellationToken),
            plan.TasimaAilesi.Ad);
        var sonuc = YerlesimMotoru.Hesapla(girdi, cancellationToken);

        plan.LokasyonTercihi = lokasyonTercihi;
        var satirlar = onceki.Yukler.OrderBy(y => y.SiraNo).Select((y, i) => Kopyala(y, i + 1)).ToList();
        var projeler = onceki.Projeler.Select(p => new YuklemePlanProjesi { ProjeId = p.ProjeId, ProjeNo = p.ProjeNo }).ToList();
        YeniRevizyonEkle(plan, kullaniciId, "Otomatik yerleşim hesaplandı.", ayarlar, projeler, satirlar, YerlesimSerilestirici.Yaz(sonuc));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await PlanGetirAsync(plan.Id, cancellationToken))!;
    }

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
    /// kullanıcının mevcut araç listesindekiler. Teknik verisi okunamayan konfigürasyon aday olmaz.
    /// </summary>
    private async Task<List<EkipmanAdayi>> AdaylarAsync(int aileId, List<int> mevcutIdleri, CancellationToken cancellationToken)
    {
        var ozelKategoriler = Enum.GetValues<EkipmanKategorisi>().Where(EkipmanKategoriKurali.OzelYukEkipmaniMi).ToList();
        var kayitlar = await context.EkipmanKonfigurasyonlari.AsNoTracking()
            .Where(k => (k.Aktif && k.Model.Tip.Aktif && (k.Model.Tip.AileId == aileId || ozelKategoriler.Contains(k.Model.Tip.Kategori))) ||
                        (k.Aktif && mevcutIdleri.Contains(k.Id)))
            .OrderBy(k => k.Kod)
            .Select(k => new
            {
                k.Id,
                k.Kod,
                k.Ad,
                k.SonRevizyonNo,
                k.Model.Tip.Kategori,
                k.Model.Tip.AileId,
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
            adaylar.Add(new EkipmanAdayi(
                k.Id, k.SonRevizyonNo, k.Kod, k.Ad, k.AileAdi, k.TipAdi,
                AileIci: k.AileId == aileId,
                OzelYukEkipmani: EkipmanKategoriKurali.OzelYukEkipmaniMi(k.Kategori),
                (GirisYonu)(int)EkipmanKategoriKurali.YuklemeYontemleri(k.Kategori),
                EkipmanKategoriKurali.DuvarliMi(k.Kategori),
                EkipmanKategoriKurali.DingilliMi(k.Kategori),
                k.Revizyon.DogrulamaDurumu == EkipmanDogrulamaDurumu.Dogrulandi,
                veri));
        }
        return adaylar;
    }

    /// <summary>Bugün geçerli onaylı Türkiye kural setinden genişlik, yükseklik ve yandan taşma sınırı; yoksa null.</summary>
    private async Task<YasalSinirlar?> YasalSinirlarAsync(CancellationToken cancellationToken)
    {
        var bugun = TurkeyTime.Now.Date;
        var set = await context.MevzuatKuralSetleri.AsNoTracking()
            .Include(s => s.Kurallar)
            .Where(s => s.YetkiAlani == MevzuatKuralSetiKurali.TurkiyeYetkiAlani && s.Durum == MevzuatKuralSetiDurumu.Onayli &&
                        s.GecerlilikBaslangici <= bugun && (s.GecerlilikBitisi == null || s.GecerlilikBitisi >= bugun))
            .OrderByDescending(s => s.SurumNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (set == null)
            return null;

        int? Parametre(MevzuatKuralTipi tip, string ad)
        {
            var kural = set.Kurallar.Where(k => k.Tip == tip).OrderBy(k => k.Sira).FirstOrDefault();
            if (kural == null)
                return null;
            try
            {
                using var belge = JsonDocument.Parse(kural.ParametrelerJson);
                return belge.RootElement.TryGetProperty(ad, out var deger) && deger.TryGetInt32(out var sayi) ? sayi : null;
            }
            catch (JsonException)
            {
                // Okunamayan parametre sınır sayılmaz; ilgili kontrol "Teyit edilmedi" olur.
                return null;
            }
        }

        return new YasalSinirlar(
            Parametre(MevzuatKuralTipi.AzamiGenislik, "genelMm"),
            Parametre(MevzuatKuralTipi.AzamiYukseklik, "genelMm"),
            Parametre(MevzuatKuralTipi.TasmaSiniri, "yandanMm"),
            set.Ad);
    }

    /// <summary>Saklanan yerleşimi ekran modeline çevirir: satır bilgileri, proje/lokasyon adları ve kullanılan ekipman geometrisi.</summary>
    private async Task<PlanYerlesimi?> YerlesimAsync(
        YuklemePlani plan, YuklemePlanRevizyonu revizyon, YerlesimSonucu? sonuc, CancellationToken cancellationToken)
    {
        if (sonuc == null)
            return null;
        var satirlar = revizyon.Yukler.ToDictionary(y => YuklemePlaniKurali.YukAnahtari(y.Kaynak, y.SandikId, y.ManuelGrupNo, y.BirimNo));
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
            HesapTarihi = revizyon.CreatedDate,
            Sonuc = (YerlesimKontrolSonucu)(int)sonuc.Sonuc,
            LokasyonaGoreAyri = plan.LokasyonTercihi == YuklemeLokasyonTercihi.LokasyonaGoreAyri,
            ProjeBazindaAyri = plan.ProjeKarisimi == YuklemeProjeKarisimi.ProjeBazindaAyri,
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
                    }).ToList(),
                    Sonuc = (YerlesimKontrolSonucu)(int)a.Sonuc,
                    TeknikVeriJson = geometriler.GetValueOrDefault((a.KonfigurasyonId, a.RevizyonNo), "{}")
                };
            }).ToList(),
            Yerlesemeyenler = sonuc.Yerlesemeyenler
                .Select(y => new PlanYerlesemeyenYuk { YukAnahtari = y.YukAnahtari, Kod = y.Kod, Neden = y.Neden }).ToList(),
            EkIhtiyaclar = sonuc.EkIhtiyaclar
                .Select(e => new PlanEkEkipmanIhtiyaci { KonfigurasyonId = e.KonfigurasyonId, EkipmanKodu = e.EkipmanKodu, EkipmanAdi = e.EkipmanAdi, Adet = e.Adet })
                .ToList(),
            Uyarilar = sonuc.Uyarilar
        };
    }
}
