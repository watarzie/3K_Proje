using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Elle eklenen sandığın numarası "Özellikleri Güncelle"den değişir: çekiden gelen, sevkli, 1 numaralı ve Ambalaj'da
/// üretime alınmış sandıkta kapalıdır; çakışma 409'dur. Değişiklik numaranın kopyalandığı kayıtları (çeki satırı,
/// transfer, saha taşıma, durum olayı, düzeltme kaydı, planlı Ambalaj kaydı) birlikte günceller ve hareket geçmişine
/// "eski → yeni" yazar; ret durumunda hiçbir kayıt değişmez.
/// </summary>
public sealed class SandikNoDegisikligiTests
{
    private static readonly IReadOnlySet<string> CekiNolari = AmbalajUretimPolitikasi.CekiSandikNolari(["5", "7-8"]);

    [Theory]
    [InlineData("12", (int)SandikDurum.Hazirlaniyor, null)]
    [InlineData("5", (int)SandikDurum.Hazirlaniyor, SandikNoDegisikligiKurali.CekiSandigiMesaji)]
    [InlineData("05", (int)SandikDurum.Bos, SandikNoDegisikligiKurali.CekiSandigiMesaji)]
    [InlineData("12", (int)SandikDurum.Sevkedildi, SandikNoDegisikligiKurali.SevkliMesaji)]
    [InlineData("1", (int)SandikDurum.Hazirlaniyor, SandikNoDegisikligiKurali.BirNumaraMesaji)]
    public void KilitNedeni_YalnizElleEklenenSevkEdilmemisSandiktaBostur(string sandikNo, int durumId, string? beklenen)
    {
        var sandik = new Sandik { Id = 1, SandikNo = sandikNo, DurumId = durumId };
        Assert.Equal(beklenen, SandikNoDegisikligiKurali.KilitNedeni(sandik, CekiNolari, []));
    }

    [Theory]
    [InlineData(AmbalajUretimDurumu.Planlandi, false, false, false, null)]
    [InlineData(AmbalajUretimDurumu.Uretimde, true, false, false, SandikNoDegisikligiKurali.AmbalajMesaji)]
    [InlineData(AmbalajUretimDurumu.Planlandi, true, false, false, SandikNoDegisikligiKurali.AmbalajMesaji)]
    [InlineData(AmbalajUretimDurumu.Planlandi, false, true, false, SandikNoDegisikligiKurali.AmbalajMesaji)]
    [InlineData(AmbalajUretimDurumu.Tamamlandi, true, false, true, null)]
    public void KilitNedeni_AmbalajdaYalnizPlanliKilitsizKayitNumarayiAcikBirakir(
        AmbalajUretimDurumu durum, bool kilitli, bool bagimsiz, bool iptal, string? beklenen)
    {
        var kayit = new AmbalajUretimKaydi
        {
            KaynakKayitId = 1, UretimDurumu = durum, KaynakSenkronizasyonuKilitliMi = kilitli, BagimsizKayitMi = bagimsiz, IptalMi = iptal
        };
        Assert.Equal(beklenen, SandikNoDegisikligiKurali.KilitNedeni(new Sandik { Id = 1, SandikNo = "12" }, CekiNolari, [kayit]));
    }

    [Theory]
    [InlineData("13", null, 0)]
    [InlineData(" ", SandikNoDegisikligiKurali.BosMesaji, 400)]
    [InlineData("1", SandikNoDegisikligiKurali.BirNumaraMesaji, 409)]
    [InlineData("7–8", SandikNoDegisikligiKurali.CekiNumarasiMesaji, 409)]
    [InlineData("014", "'014' numaralı sandık bu projede zaten mevcut.", 409)]
    [InlineData("a-20", "'a-20' numaralı sandık bu projede zaten mevcut.", 409)]
    public void Denetle_YeniNumaraBosCekiNumarasiVeyaCakisanOlamaz(string yeniNo, string? mesaj, int kod)
    {
        var red = SandikNoDegisikligiKurali.Denetle(
            new Sandik { Id = 1, SandikNo = "12" }, yeniNo, CekiNolari, ["14", "A-20"], []);
        Assert.Equal(mesaj, red?.Mesaj);
        Assert.Equal(kod, red?.Kod ?? 0);
        Assert.Equal(SandikNoDegisikligiKurali.UzunlukMesaji,
            SandikNoDegisikligiKurali.Denetle(new Sandik { SandikNo = "12" }, new string('9', 101), CekiNolari, [], [])!.Mesaj);
    }

    [Fact]
    public void DegisiyorMu_GonderilmeyenVeAyniNumaraDegisiklikDegildir()
    {
        Assert.False(SandikNoDegisikligiKurali.DegisiyorMu("12", null));
        Assert.False(SandikNoDegisikligiKurali.DegisiyorMu("12", " 12 "));
        Assert.True(SandikNoDegisikligiKurali.DegisiyorMu("12", "12A"));
    }

    [Fact]
    public async Task ElleEklenenSandik_NumarasiVeBagliKayitlarAyniIslemdeDegisir()
    {
        var o = new Ortam();
        var sonuc = await o.GuncelleAsync("  21 ", ad: "Elektrik");

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(("21", "Elektrik"), (o.Manuel.SandikNo, o.Manuel.Ad));
        Assert.Equal(("21", "21"), (o.ManuelSatir.FiiliSandikNo, o.ManuelSatir.CekideGecenSandikNo));
        // Çekiden gelen satırın koli numarası çekiye bağlıdır; yalnız fiili sandığı izlenir.
        Assert.Equal(("21", "5"), (o.TasinanCekiSatiri.FiiliSandikNo, o.TasinanCekiSatiri.CekideGecenSandikNo));
        Assert.Equal(("5", "21"), (o.Transfer.KaynakSandikNo, o.Transfer.HedefSandikNo));
        Assert.Equal(("21", "3"), (o.SahaTasima.KaynakSahaSandikNo, o.SahaTasima.HedefSahaSandikNo));
        Assert.Equal("21", o.Olay.SandikNo);
        Assert.Equal("21", o.Duzeltme.SandikNo);
        Assert.Equal("5", o.Cekiden.SandikNo);
        Assert.Equal("12", o.BaskaProjeSatiri.FiiliSandikNo);

        var hareket = Assert.Single(o.Hareket.Rows);
        Assert.Equal((SandikNoDegistirmeIslemi.HareketIslemi, "12", "21", "Sandik", o.Manuel.Id.ToString()),
            (hareket.Islem, hareket.EskiDeger, hareket.YeniDeger, hareket.ReferansTipi, hareket.ReferansId));
        Assert.Contains("12 → 21", hareket.Aciklama);

        // Planlı Ambalaj kaydı otomatik senkronun kuralıyla yeni numaraya geçer.
        Assert.Equal("21", o.Ambalaj.SandikNo);
    }

    [Fact]
    public async Task AmbalajKaydiNumaradanAdediAlir_PlanliKayitAralikNumarasindaAdetGuncellenir()
    {
        var o = new Ortam();
        Assert.True((await o.GuncelleAsync("21-23")).IsSuccess);
        Assert.Equal(("21-23", 3), (o.Ambalaj.SandikNo, o.Ambalaj.Adet));
    }

    [Theory]
    [InlineData("014", 0, "'014' numaralı sandık bu projede zaten mevcut.", 409)]
    [InlineData("5", 0, SandikNoDegisikligiKurali.CekiNumarasiMesaji, 409)]
    [InlineData("21", 1, SandikNoDegisikligiKurali.CekiSandigiMesaji, 409)]
    [InlineData("21", 2, SandikNoDegisikligiKurali.SevkliMesaji, 409)]
    [InlineData("21", 3, SandikNoDegisikligiKurali.AmbalajMesaji, 409)]
    public async Task Ret_HicbirKaydiDegistirmez(string yeniNo, int senaryo, string mesaj, int kod)
    {
        var o = new Ortam();
        switch (senaryo)
        {
            case 1: o.Manuel.SandikNo = "5"; break; // çekiden gelen sandık
            case 2: o.Manuel.DurumId = (int)SandikDurum.Sevkedildi; o.Manuel.SevkiyatDuzeltmeAcikMi = true; break;
            case 3: o.Ambalaj.UretimDurumu = AmbalajUretimDurumu.Uretimde; o.Ambalaj.KaynakSenkronizasyonuKilitliMi = true; break;
        }
        var oncekiNo = o.Manuel.SandikNo;

        var sonuc = await o.GuncelleAsync(yeniNo, ad: "Değişmemeli");

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(kod, sonuc.StatusCode);
        Assert.Contains(mesaj, sonuc.Error!.Message);
        Assert.Equal((oncekiNo, "Manuel"), (o.Manuel.SandikNo, o.Manuel.Ad));
        Assert.Equal(("12", "12"), (o.ManuelSatir.FiiliSandikNo, o.ManuelSatir.CekideGecenSandikNo));
        Assert.Equal("12", o.TasinanCekiSatiri.FiiliSandikNo);
        Assert.Equal(("5", "12"), (o.Transfer.KaynakSandikNo, o.Transfer.HedefSandikNo));
        Assert.Equal("12", o.SahaTasima.KaynakSahaSandikNo);
        Assert.Equal(("12", "12"), (o.Olay.SandikNo, o.Duzeltme.SandikNo));
        Assert.Equal("12", o.Ambalaj.SandikNo);
        Assert.Empty(o.Hareket.Rows);
        Assert.Equal(0, o.Uow.SaveCount);
    }

    [Fact]
    public async Task NumaraGonderilmezseVeyaAyniysa_YalnizOzelliklerGuncellenir()
    {
        var o = new Ortam();
        Assert.True((await o.GuncelleAsync(null, ad: "A")).IsSuccess);
        Assert.True((await o.GuncelleAsync(" 12 ", ad: "B")).IsSuccess);
        Assert.Equal(("12", "B"), (o.Manuel.SandikNo, o.Manuel.Ad));
        Assert.Empty(o.Hareket.Rows);
    }

    [Fact]
    public void KilitNedeni_SandikDetayiVeListesiAyniKurali()
    {
        var o = new Ortam();
        Assert.Null(SandikNoDegistirmeIslemi.KilitNedeni(o.Uow, o.Manuel));
        Assert.Equal(SandikNoDegisikligiKurali.CekiSandigiMesaji, SandikNoDegistirmeIslemi.KilitNedeni(o.Uow, o.Cekiden));
    }

    /// <summary>
    /// PA-10 (normal): çekiden gelen sandık 5 ve elle eklenen sandık 12. Sandık 12'de elle eklenen satır ve 5'ten
    /// taşınan çeki satırı; numaranın kopyalandığı transfer, saha taşıma, durum olayı, düzeltme ve planlı Ambalaj kaydı.
    /// </summary>
    private sealed class Ortam
    {
        public Ortam()
        {
            var proje = new Proje { Id = 10, ProjeNo = "PA-10", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
            var baska = new Proje { Id = 20, ProjeNo = "PA-20", Musteri = "M", ProjeTipiId = (int)ProjeTipi.Normal };
            Uow.Repo<Proje>().Rows.AddRange([proje, baska]);
            var ceki = new Ceki { Id = 100, ProjeId = 10, Proje = proje };
            var baskaCeki = new Ceki { Id = 200, ProjeId = 20, Proje = baska };
            Uow.Repo<Ceki>().Rows.AddRange([ceki, baskaCeki]);

            Cekiden = new Sandik { Id = 1, ProjeId = 10, SandikNo = "5", TipId = (int)SandikTipi.AhsapKapali, DurumId = (int)SandikDurum.Hazirlaniyor };
            Manuel = new Sandik { Id = 2, ProjeId = 10, SandikNo = "12", Ad = "Manuel", TipId = (int)SandikTipi.AhsapKapali, DurumId = (int)SandikDurum.Hazirlaniyor };
            var digerManuel = new Sandik { Id = 3, ProjeId = 10, SandikNo = "14", DurumId = (int)SandikDurum.Bos };
            Uow.Repo<Sandik>().Rows.AddRange([Cekiden, Manuel, digerManuel]);

            ManuelSatir = new CekiSatiri { Id = 11, CekiId = 100, Ceki = ceki, IsManuelEklenen = true, CekideGecenSandikNo = "12", FiiliSandikNo = "12" };
            TasinanCekiSatiri = new CekiSatiri { Id = 12, CekiId = 100, Ceki = ceki, CekideGecenSandikNo = "5", FiiliSandikNo = "12" };
            BaskaProjeSatiri = new CekiSatiri { Id = 13, CekiId = 200, Ceki = baskaCeki, IsManuelEklenen = true, CekideGecenSandikNo = "12", FiiliSandikNo = "12" };
            Uow.Repo<CekiSatiri>().Rows.AddRange([ManuelSatir, TasinanCekiSatiri, BaskaProjeSatiri]);

            Transfer = new SandikUrunTransferi { Id = 1, ProjeId = 10, KaynakSandikId = 1, HedefSandikId = 2, KaynakSandikNo = "5", HedefSandikNo = "12" };
            SahaTasima = new SahaTasimaKalemi { Id = 1, KaynakSahaSandikId = 2, KaynakSahaSandikNo = "12", HedefSahaSandikId = 99, HedefSahaSandikNo = "3" };
            Olay = new SandikDurumOlayi { Id = 1, ProjeId = 10, SandikId = 2, SandikNo = "12" };
            Duzeltme = new SandikDuzeltmeKaydi { Id = 1, ProjeId = 10, SandikId = 2, SandikNo = "12" };
            Ambalaj = new AmbalajUretimKaydi
            {
                Id = 1, ProjeId = 10, KaynakKayitId = 2, KaynakModul = AmbalajKaynakModulu.Sandik, Tur = AmbalajSandikTuru.Ilave,
                SandikNo = "12", Adet = 1, UretimDurumu = AmbalajUretimDurumu.Planlandi
            };
            Uow.Repo<SandikUrunTransferi>().Rows.Add(Transfer);
            Uow.Repo<SahaTasimaKalemi>().Rows.Add(SahaTasima);
            Uow.Repo<SandikDurumOlayi>().Rows.Add(Olay);
            Uow.Repo<SandikDuzeltmeKaydi>().Rows.Add(Duzeltme);
            Uow.Repo<AmbalajUretimKaydi>().Rows.Add(Ambalaj);
        }

        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public Sandik Cekiden { get; }
        public Sandik Manuel { get; }
        public CekiSatiri ManuelSatir { get; }
        public CekiSatiri TasinanCekiSatiri { get; }
        public CekiSatiri BaskaProjeSatiri { get; }
        public SandikUrunTransferi Transfer { get; }
        public SahaTasimaKalemi SahaTasima { get; }
        public SandikDurumOlayi Olay { get; }
        public SandikDuzeltmeKaydi Duzeltme { get; }
        public AmbalajUretimKaydi Ambalaj { get; }

        public Task<Result> GuncelleAsync(string? sandikNo, string ad = "Manuel") =>
            new SandikOzellikGuncelleCommandHandler(
                    Uow, Hareket, new AmbalajKaynakSenkronizasyonService(Uow, new SessizFinans()), new OrtakUser())
                .Handle(new SandikOzellikGuncelleCommand
                {
                    SandikId = Manuel.Id, SandikNo = sandikNo, SandikIsmi = ad, TipId = Manuel.TipId
                }, default);
    }

    private sealed class SessizFinans : IFinansUretimAktarimService
    {
        public Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(
            IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken) =>
            Task.FromResult(new FinansSenkronizasyonSonucModel(0, modeller.Count, 0));
    }
}
