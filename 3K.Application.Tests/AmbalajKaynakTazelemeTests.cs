using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Application.Features.AmbalajIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Sandık ölçüsü ilk üretim formundan sonra güncellenince otomatik senkronizasyon üretimdeki kilitli kaydı ezmez;
/// üretim formunu (yeniden) oluşturmak kaydı güncel sandıktan tazeler ve çam m³'ünü, parçaları (teknik resim) ve
/// rapor girdilerini yeni ölçüyle üretir. Tamamlanan kayıt korunur; kaynakta eksik ölçüde ve PO geçmişi olan
/// kalemde fiyat eşleşmesi değişiyorsa hiçbir kayıt değişmez.
/// </summary>
public sealed class AmbalajKaynakTazelemeTests
{
    private const string TazelemeIslemi = "Kaynak sandık değişiklikleri üretim formuna alındı";

    // Kaynak dış ölçü 1400×1200×1375 → iç ölçü 1308×1108×1120; ayak kuralı dış boyla değerlendirilir.
    private static readonly AmbalajM3Ozeti YeniOlcuM3 = AmbalajHesaplayici.M3OzetiHesapla(
        1308, 1108, 1120, 1, AmbalajHesaplayici.VarsayilanSarfOrani, ayakHesapBoyu: 1400);

    [Theory]
    [InlineData(AmbalajUretimDurumu.Planlandi, false, true)]
    [InlineData(AmbalajUretimDurumu.Uretimde, true, true)]
    [InlineData(AmbalajUretimDurumu.Tamamlandi, true, false)]
    public void FormdaKaynaktanTazelenir_YalnizTamamlanmamisAktifKaynakKayit(
        AmbalajUretimDurumu durum, bool kilitli, bool beklenen)
    {
        var kayit = new AmbalajUretimKaydi { KaynakKayitId = 5, UretimDurumu = durum, KaynakSenkronizasyonuKilitliMi = kilitli };

        Assert.Equal(beklenen, kayit.FormdaKaynaktanTazelenirMi());
        Assert.False(new AmbalajUretimKaydi { UretimDurumu = durum }.FormdaKaynaktanTazelenirMi());
        Assert.False(new AmbalajUretimKaydi { KaynakKayitId = 5, UretimDurumu = durum, IptalMi = true }.FormdaKaynaktanTazelenirMi());
        Assert.False(new AmbalajUretimKaydi { KaynakKayitId = 5, UretimDurumu = durum, BagimsizKayitMi = true }.FormdaKaynaktanTazelenirMi());
    }

    [Fact]
    public void KaynakFiyatEslesmesi_AdCinsVeOlcuDegisiminiAyirir_BosOlcuSifirdir()
    {
        var sandik = new Sandik { Ad = "Radyatör", TipId = (int)SandikTipi.AhsapKapali, Boy = 1400, En = 1000, Yukseklik = 1375 };
        var kayit = new AmbalajUretimKaydi
        {
            Ad = " radyatör ", SandikCinsi = AmbalajSandikCinsi.AhsapKapali, Boy = 1400, En = 1000, Yukseklik = 1375
        };

        Assert.False(AmbalajUretimPolitikasi.KaynakFiyatEslesmesiniDegistirirMi(kayit, sandik));
        sandik.En = 1200;
        Assert.True(AmbalajUretimPolitikasi.KaynakFiyatEslesmesiniDegistirirMi(kayit, sandik));
        sandik.En = 1000;
        sandik.TipId = (int)SandikTipi.Kontrplak;
        Assert.True(AmbalajUretimPolitikasi.KaynakFiyatEslesmesiniDegistirirMi(kayit, sandik));
        Assert.Equal(new AmbalajKaynakSandikDegerleri("Fan", AmbalajSandikCinsi.Katlanir, 0, 0, 0),
            AmbalajUretimPolitikasi.KaynakDegerleri(new Sandik { Ad = "Fan", TipId = (int)SandikTipi.KatlanirSandik }));
    }

    /// <summary>
    /// Gerçek işlem sırası: sandık → kaynak senkronu → ilk form → sandık ölçüsü güncellenir → kuyruk senkronu
    /// kilitli kaydı atlar → yeniden oluşturma çam m³'ünü, parçaları, form/rapor girdisini, geçmişi ve finansı
    /// yeni ölçüyle üretir.
    /// </summary>
    [Fact]
    public async Task OlcuGuncellenince_KuyrukKilitliKaydiEzmez_YenidenOlusturmaGuncelOlcuyleHesaplar()
    {
        var o = await Ortam.UretimdeAsync();
        var kayit = o.Kayit;
        var eskiNetM3 = kayit.HesaplananToplamM3;

        Assert.True((await o.OlcuGuncelleAsync(en: 1200)).IsSuccess);
        var kuyruk = await o.SenkronizeEtAsync();
        Assert.True(kuyruk.IsSuccess, kuyruk.Error?.Message);
        Assert.Equal(1, kuyruk.Value!.Degismeyen);
        Assert.Equal(1000, kayit.En);

        var hareketOncesi = o.Hareketler.Count;
        var sonuc = await o.FormAsync(yenidenOlustur: true);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(2, sonuc.Value!.Surum);
        Assert.Equal((1400m, 1200m, 1375m), (kayit.Boy, kayit.En, kayit.Yukseklik));
        Assert.Equal(YeniOlcuM3.HesaplananToplamM3, kayit.HesaplananToplamM3);
        Assert.Equal(YeniOlcuM3.SarfM3, kayit.SarfM3);
        Assert.Equal(YeniOlcuM3.ToplamM3, kayit.ToplamM3);
        Assert.True(kayit.HesaplananToplamM3 > eskiNetM3);
        Assert.Equal(AmbalajUretimDurumu.Uretimde, kayit.UretimDurumu);
        Assert.True(kayit.KaynakSenkronizasyonuKilitliMi);

        // Teknik resim ve rapor bu kalemden üretilir: dış/iç ölçü, parça boyları ve m³ yeni ölçüdendir.
        var kalem = Assert.Single(sonuc.Value.Form.Kalemler);
        Assert.Equal(new AmbalajOlculeri(1400, 1200, 1375), kalem.DisOlculer);
        Assert.Equal(new AmbalajOlculeri(1308, 1108, 1120), kalem.IcOlculer);
        Assert.Equal(
            AmbalajHesaplayici.Hesapla(1308, 1108, 1120, ayakHesapBoyu: 1400).Parcalar
                .Select(p => (p.Kod, p.Uzunluk, Math.Round(p.HacimM3, 6, MidpointRounding.AwayFromZero))),
            kalem.Parcalar.Select(p => (p.Kod, p.Uzunluk, p.HacimM3)));
        Assert.Equal(YeniOlcuM3.NetM3, kalem.NetM3);
        Assert.Equal(YeniOlcuM3.SarfM3, kalem.SarfM3);
        Assert.Equal(YeniOlcuM3.ToplamM3, sonuc.Value.Form.ToplamM3);
        Assert.Equal(410, kalem.BrutKg);

        Assert.Contains(o.Hareketler.Skip(hareketOncesi), h => h.Islem == TazelemeIslemi &&
            h.AlanAdi == nameof(AmbalajUretimKaydi.En) && h.EskiDeger == "1000" && h.YeniDeger == "1200");
        var finans = o.Finans.Modeller.Last();
        Assert.Equal(1200, finans.En);
        Assert.Equal(YeniOlcuM3.NetM3, finans.BirimM3 * finans.Adet);
        Assert.Equal(YeniOlcuM3.SarfM3, finans.SarfM3);
    }

    /// <summary>
    /// Tazeleme olayı kayıt geçmişinde görünür; ölçü ve m³ izni olmayan kullanıcı olayın ölçü/m³ eski-yeni
    /// değerlerini göremez.
    /// </summary>
    [Fact]
    public async Task TazelemeGecmisi_OlcuVeM3IzniOlmayandaDegerGizlenir()
    {
        var o = await Ortam.UretimdeAsync();
        o.Uow.Repo<Kullanici>().Rows.Add(new Kullanici { Id = 7, AdSoyad = "Planlama" });
        Assert.True((await o.OlcuGuncelleAsync(en: 1200)).IsSuccess);
        Assert.True((await o.FormAsync(yenidenOlustur: true)).IsSuccess);
        static IEnumerable<string> TazelemeAlanlari(IReadOnlyList<AmbalajKayitGecmisiDto> gecmis) =>
            gecmis.Single(x => x.Islem == TazelemeIslemi).Degisiklikler.Select(d => d.Alan);

        var tum = (await new GetAmbalajKayitGecmisiQueryHandler(o.Uow, o.Roller, o.User)
            .Handle(new() { KayitId = o.Kayit.Id }, default)).Value!;
        Assert.Contains("En (mm)", TazelemeAlanlari(tum));
        Assert.Contains(TazelemeAlanlari(tum), alan => alan.Contains("m³", StringComparison.Ordinal));

        var olcusuz = OrtakRoller.Haric(YetkiKodlari.Ambalaj.OlcuGoruntule, YetkiKodlari.Ambalaj.M3Goruntule);
        var kisitli = (await new GetAmbalajKayitGecmisiQueryHandler(o.Uow, olcusuz, o.User)
            .Handle(new() { KayitId = o.Kayit.Id }, default)).Value!;
        var kisitliAlanlar = kisitli.Where(x => x.Islem == TazelemeIslemi).SelectMany(x => x.Degisiklikler).Select(d => d.Alan).ToList();
        Assert.DoesNotContain("En (mm)", kisitliAlanlar);
        Assert.DoesNotContain(kisitliAlanlar, alan => alan.Contains("m³", StringComparison.Ordinal));
    }

    /// <summary>Kuyruk henüz işlemediyse ilk form da sandıktaki son ölçüyle oluşur.</summary>
    [Fact]
    public async Task IlkForm_KuyrukIslemedenGuncellenenOlcuyuKullanir()
    {
        var o = new Ortam();
        Assert.True((await o.SenkronizeEtAsync()).IsSuccess);
        Assert.True((await o.OlcuGuncelleAsync(en: 1200)).IsSuccess);

        var sonuc = await o.FormAsync(yenidenOlustur: false);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1200, o.Kayit.En);
        Assert.Equal(YeniOlcuM3.HesaplananToplamM3, o.Kayit.HesaplananToplamM3);
        Assert.Equal(new AmbalajOlculeri(1400, 1200, 1375), Assert.Single(sonuc.Value!.Form.Kalemler).DisOlculer);
    }

    /// <summary>Tamamlanan üretim fiziksel gerçekleşmedir; yeniden oluşturma onu kaynaktan tazelemez.</summary>
    [Fact]
    public async Task TamamlananKayit_YenidenOlusturmadaEskiOlcusunuKorur()
    {
        var o = await Ortam.UretimdeAsync();
        var kayit = o.Kayit;
        kayit.UretimDurumu = AmbalajUretimDurumu.Tamamlandi;
        kayit.TamamlanmaTarihi = new DateTime(2026, 10, 10);
        var oncesi = AmbalajUretimYardimcilari.Snapshot(kayit);
        Assert.True((await o.OlcuGuncelleAsync(en: 1200)).IsSuccess);

        var sonuc = await o.FormAsync(yenidenOlustur: true);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(1000, kayit.En);
        Assert.Equal(oncesi[nameof(AmbalajUretimKaydi.HesaplananToplamM3)],
            AmbalajUretimYardimcilari.Snapshot(kayit)[nameof(AmbalajUretimKaydi.HesaplananToplamM3)]);
        Assert.Equal(1000, Assert.Single(sonuc.Value!.Form.Kalemler).DisOlculer.En);
        Assert.DoesNotContain(o.Hareketler, h => h.Islem == TazelemeIslemi);
    }

    /// <summary>
    /// PO geçmişi tek başına yeniden oluşturmayı engellemez; ancak kaynak ölçüsü değiştiyse (iptal edilmiş PO
    /// dahil, fiyat snapshot'ı kilitli) 409 döner ve kayıt, geçmiş, form ve finans aktarımı değişmez.
    /// </summary>
    [Fact]
    public async Task PoGecmisliKalem_KaynakOlcusuDegistiyse409_HicbirSeyDegismez()
    {
        var o = await Ortam.UretimdeAsync();
        var kayit = o.Kayit;
        var isKaydi = new FinansIsKaydi
        {
            Id = 1, KaynakTuru = FinansKaynakTurleri.AmbalajUretim, KaynakKayitId = kayit.IsAkisKimligi.ToString("D")
        };
        isKaydi.SiparisKalemleri.Add(new FinansSiparisKalemi
        {
            FinansSiparis = new FinansSiparis { PoNumarasi = "4500123456", IptalEdildi = true }
        });
        o.Uow.Repo<FinansIsKaydi>().Rows.Add(isKaydi);

        var degisiklikYok = await o.FormAsync(yenidenOlustur: true);
        Assert.True(degisiklikYok.IsSuccess, degisiklikYok.Error?.Message);
        Assert.DoesNotContain(o.Hareketler, h => h.Islem == TazelemeIslemi);

        Assert.True((await o.OlcuGuncelleAsync(en: 1200)).IsSuccess);
        var oncesi = AmbalajUretimYardimcilari.Snapshot(kayit);
        var (kaydetmeSayisi, hareketSayisi, formSayisi, finansSayisi) = o.Sayilar();

        var sonuc = await o.FormAsync(yenidenOlustur: true);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.StartsWith("3 numaralı sandığın", sonuc.Error!.Message);
        Assert.Contains("PO geçmişi", sonuc.Error.Message);
        Assert.Equal(oncesi, AmbalajUretimYardimcilari.Snapshot(kayit));
        Assert.Equal((kaydetmeSayisi, hareketSayisi, formSayisi, finansSayisi), o.Sayilar());
    }

    /// <summary>Kaynakta ölçüsü silinen sandığın formu eski ölçüyle oluşmaz; 409 döner, hiçbir şey değişmez.</summary>
    [Fact]
    public async Task KaynakOlcusuEksik_YenidenOlusturma409_HicbirSeyDegismez()
    {
        var o = await Ortam.UretimdeAsync();
        Assert.True((await o.OlcuGuncelleAsync(en: null)).IsSuccess);
        var oncesi = AmbalajUretimYardimcilari.Snapshot(o.Kayit);
        var sayilar = o.Sayilar();

        var sonuc = await o.FormAsync(yenidenOlustur: true);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal("3 numaralı sandığın ölçüleri eksik olduğu için üretim formu oluşturulamadı.", sonuc.Error!.Message);
        Assert.Equal(oncesi, AmbalajUretimYardimcilari.Snapshot(o.Kayit));
        Assert.Equal(sayilar, o.Sayilar());
    }

    private sealed class Ortam
    {
        public Ortam()
        {
            Uow.Repo<Proje>().Rows.Add(new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "Müşteri", ProjeTipiId = (int)ProjeTipi.Normal });
            Sandik = new Sandik
            {
                Id = 101, ProjeId = 10, SandikNo = "3", Ad = "Radyatör", TipId = (int)SandikTipi.AhsapKapali,
                Boy = 1400, En = 1000, Yukseklik = 1375, NetKg = 260, GrossKg = 410, CreatedDate = new DateTime(2026, 10, 1)
            };
            Uow.Repo<Sandik>().Rows.Add(Sandik);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakUser User { get; } = new();
        public OrtakRoller Roller { get; } = OrtakRoller.TumIzinler();
        public FinansKaydedici Finans { get; } = new();
        public Sandik Sandik { get; }
        public AmbalajUretimKaydi Kayit => Uow.Repo<AmbalajUretimKaydi>().Rows.Single();
        public List<AmbalajUretimHareketi> Hareketler => Uow.Repo<AmbalajUretimHareketi>().Rows;

        /// <summary>Kaynak senkronu ve ilk formla üretime alınmış, otomatik senkrona kilitlenmiş kayıt.</summary>
        public static async Task<Ortam> UretimdeAsync()
        {
            var ortam = new Ortam();
            Assert.True((await ortam.SenkronizeEtAsync()).IsSuccess);
            var ilk = await ortam.FormAsync(yenidenOlustur: false);
            Assert.True(ilk.IsSuccess, ilk.Error?.Message);
            Assert.Equal(AmbalajUretimDurumu.Uretimde, ortam.Kayit.UretimDurumu);
            Assert.True(ortam.Kayit.KaynakSenkronizasyonuKilitliMi);
            Assert.Equal(new AmbalajOlculeri(1400, 1000, 1375), Assert.Single(ilk.Value!.Form.Kalemler).DisOlculer);
            return ortam;
        }

        public Task<Result<AmbalajSenkronizasyonSonucu>> SenkronizeEtAsync() =>
            new AmbalajKaynakSenkronizasyonService(Uow, Finans).SenkronizeEtAsync(10, User, default);

        public Task<Result> OlcuGuncelleAsync(decimal? en) =>
            new SandikOzellikGuncelleCommandHandler(Uow).Handle(new SandikOzellikGuncelleCommand
            {
                SandikId = Sandik.Id, SandikIsmi = Sandik.Ad, TipId = Sandik.TipId, Boy = Sandik.Boy, En = en,
                Yukseklik = Sandik.Yukseklik, NetKg = Sandik.NetKg, GrossKg = Sandik.GrossKg
            }, default);

        public Task<Result<AmbalajFormSurumuDto>> FormAsync(bool yenidenOlustur) =>
            new AmbalajFormOlusturCommandHandler(Uow, User, Roller, Finans).Handle(new AmbalajFormOlusturCommand
            {
                ProjeId = 10, KayitIdleri = [Kayit.Id], YenidenOlustur = yenidenOlustur, IdempotencyAnahtari = Guid.NewGuid()
            }, default);

        public (int Kaydetme, int Hareket, int Form, int Finans) Sayilar() =>
            (Uow.SaveCount, Hareketler.Count, Uow.Repo<AmbalajUretimFormuSurumu>().Rows.Count, Finans.Modeller.Count);
    }

    private sealed class FinansKaydedici : IFinansUretimAktarimService
    {
        public List<FinansUretimAktarimModel> Modeller { get; } = [];

        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller,
            CancellationToken cancellationToken)
        {
            Modeller.AddRange(modeller);
            return Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
        }
    }
}
