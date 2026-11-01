using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.Validators;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;
using ProjeSandikKapat = _3K.Application.Features.ProjeIslemleri.Commands.SandikKapatCommand;
using ProjeSandikKapatHandler = _3K.Application.Features.ProjeIslemleri.Commands.SandikKapatCommandHandler;

namespace _3K.Application.Tests;

/// <summary>
/// Sandık kapanma tarihi, geri açma sebebi ve açılış/kapanış olayları: saf kural (<see cref="SandikDurumOlayKurali"/>),
/// doğrulayıcı, tekil/toplu kapatma ve sebepli geri alma handler'ları ve sandık listesindeki eksik hesabı. Kaydetme anındaki
/// otomatik olay ve istatistik sorguları PostgreSQL ile <see cref="SandikAcilisKapanisPostgresTests"/>'te sınanır.
/// </summary>
public sealed class SandikAcilisKapanisTests
{
    [Theory]
    [InlineData((int)SandikDurum.Hazirlaniyor, (int)SandikDurum.Kapandi, SandikDurumOlayTuru.Kapandi)]
    [InlineData((int)SandikDurum.Bos, (int)SandikDurum.Kapandi, SandikDurumOlayTuru.Kapandi)]
    [InlineData((int)SandikDurum.Kapandi, (int)SandikDurum.Hazirlaniyor, SandikDurumOlayTuru.Acildi)]
    [InlineData((int)SandikDurum.Kapandi, (int)SandikDurum.Bos, SandikDurumOlayTuru.Acildi)]
    [InlineData((int)SandikDurum.Kapandi, (int)SandikDurum.Sevkedildi, null)]
    [InlineData((int)SandikDurum.Sevkedildi, (int)SandikDurum.Kapandi, null)]
    [InlineData((int)SandikDurum.Sevkedildi, (int)SandikDurum.Hazirlaniyor, null)]
    [InlineData((int)SandikDurum.Kapandi, (int)SandikDurum.Kapandi, null)]
    [InlineData((int)SandikDurum.Hazirlaniyor, (int)SandikDurum.Bos, null)]
    public void OlayTuru_YalnizKapandiyaGirisVeKapandidanHazirlaniyoraDonusOlaydir(int eski, int yeni, SandikDurumOlayTuru? beklenen) =>
        Assert.Equal(beklenen, SandikDurumOlayKurali.OlayTuru(eski, yeni));

    [Fact]
    public void OlayTuru_YeniSandikKapaliEklenirseKapanmadir() =>
        Assert.Equal(SandikDurumOlayTuru.Kapandi, SandikDurumOlayKurali.OlayTuru(null, (int)SandikDurum.Kapandi));

    [Fact]
    public void KapanmaTarihi_KapanmadaYazilirSevkteKorunurGeriAcilincaBosalir()
    {
        var simdi = new DateTime(2026, 10, 24, 14, 32, 0);
        var onceki = new DateTime(2026, 10, 1, 9, 0, 0);
        Assert.Equal(simdi, SandikDurumOlayKurali.KapanmaTarihi(null, (int)SandikDurum.Hazirlaniyor, (int)SandikDurum.Kapandi, simdi));
        Assert.Equal(onceki, SandikDurumOlayKurali.KapanmaTarihi(onceki, (int)SandikDurum.Kapandi, (int)SandikDurum.Sevkedildi, simdi));
        // Sevk kilidi açılıp sevk öncesi Kapandı'ya dönmek yeni kapanma değildir: ilk kapanma zamanı kalır.
        Assert.Equal(onceki, SandikDurumOlayKurali.KapanmaTarihi(onceki, (int)SandikDurum.Sevkedildi, (int)SandikDurum.Kapandi, simdi));
        Assert.Equal(onceki, SandikDurumOlayKurali.KapanmaTarihi(onceki, (int)SandikDurum.Kapandi, (int)SandikDurum.Kapandi, simdi));
        Assert.Null(SandikDurumOlayKurali.KapanmaTarihi(onceki, (int)SandikDurum.Kapandi, (int)SandikDurum.Hazirlaniyor, simdi));
        Assert.Null(SandikDurumOlayKurali.KapanmaTarihi(onceki, (int)SandikDurum.Sevkedildi, (int)SandikDurum.Hazirlaniyor, simdi));
    }

    [Theory]
    [InlineData(null, null, "Sandığı geri açmak için bir sebep seçin.")]
    [InlineData(0, "not", "Sandığı geri açmak için bir sebep seçin.")]
    [InlineData(1, null, null)]
    [InlineData(1, "   ", null)]
    public void GeriAcmaHatasi_SebepZorunluNotIstegeBagli(int? sebepId, string? not, string? beklenen) =>
        Assert.Equal(beklenen, SandikDurumOlayKurali.GeriAcmaHatasi(sebepId, not));

    [Fact]
    public void GeriAcmaHatasi_NotSinirinaKadarKabulEdilir()
    {
        Assert.Null(SandikDurumOlayKurali.GeriAcmaHatasi(1, new string('a', SandikDurumOlayKurali.NotEnFazlaUzunluk)));
        Assert.Equal("Not en fazla 500 karakter olabilir.",
            SandikDurumOlayKurali.GeriAcmaHatasi(1, new string('a', SandikDurumOlayKurali.NotEnFazlaUzunluk + 1)));
    }

    [Theory]
    [InlineData("Örnek Müşteri", "ÖRNEK müş", true)]
    [InlineData("IŞIK Enerji", "ışık", true)]
    [InlineData("PA600-20", "pa600", true)]
    [InlineData("PA600-20", "pa601", false)]
    [InlineData(null, "pa", false)]
    public void AramaEslesir_TurkceHarfDuyarsiz(string? metin, string aranan, bool beklenen) =>
        Assert.Equal(beklenen, SandikDurumOlayKurali.AramaEslesir(metin, aranan));

    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 25)]
    [InlineData(10, 10)]
    [InlineData(500, 100)]
    public void SayfaKurali_BoyutSinirlanir(int? boyut, int beklenen) => Assert.Equal(beklenen, SayfaKurali.Boyut(boyut));

    [Theory]
    [InlineData(null, 0, 1)]
    [InlineData(0, 30, 1)]
    [InlineData(2, 30, 2)]
    [InlineData(9, 30, 2)]
    public void SayfaKurali_SayfaToplamaGoreSinirlanir(int? sayfa, int toplam, int beklenen) =>
        Assert.Equal(beklenen, SayfaKurali.Sayfa(sayfa, 25, toplam));

    [Fact]
    public void SayfaValidator_SayfaVeBoyutSiniri()
    {
        var validator = new SandikDurumOlaylariQueryValidator();
        Assert.True(validator.Validate(new SandikDurumOlaylariQuery { Sayfa = 1, Boyut = 100 }).IsValid);
        Assert.False(validator.Validate(new SandikDurumOlaylariQuery { Sayfa = 0 }).IsValid);
        Assert.False(validator.Validate(new SandikDurumOlaylariQuery { Boyut = 101 }).IsValid);
        Assert.False(validator.Validate(new SandikDurumOlaylariQuery { Baslangic = new DateTime(2026, 10, 2), Bitis = new DateTime(2026, 10, 1) }).IsValid);
    }

    [Fact]
    public void Validator_GeriAlmadaSebepIsterKapatmadaIstemez()
    {
        var validator = new SandikKapatCommandValidator();
        Assert.False(validator.Validate(new ProjeSandikKapat { SandikId = 1, Kapali = false }).IsValid);
        Assert.True(validator.Validate(new ProjeSandikKapat { SandikId = 1, Kapali = false, AcilmaSebebiId = 2 }).IsValid);
        Assert.True(validator.Validate(new ProjeSandikKapat { SandikId = 1, Kapali = true }).IsValid);
        Assert.False(validator.Validate(new ProjeSandikKapat { SandikId = 0, Kapali = true }).IsValid);
    }

    [Fact]
    public void IstatistikValidator_BaslangicBitistenSonraOlamaz()
    {
        var validator = new SandikDurumIstatistigiQueryValidator();
        Assert.False(validator.Validate(new SandikDurumIstatistigiQuery { Baslangic = new DateTime(2026, 10, 2), Bitis = new DateTime(2026, 10, 1) }).IsValid);
        Assert.True(validator.Validate(new SandikDurumIstatistigiQuery { Baslangic = new DateTime(2026, 10, 1), Bitis = new DateTime(2026, 10, 1) }).IsValid);
        Assert.True(validator.Validate(new SandikDurumIstatistigiQuery()).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(99)]
    public async Task GeriAl_SebepYoksaVeyaBulunamazsaSandikDegismez(int? sebepId)
    {
        var k = new Kurgu((int)SandikDurum.Kapandi);
        var sonuc = await k.ProjeKapat(kapali: false, sebepId);
        Assert.False(sonuc.IsSuccess);
        Assert.Equal(400, sonuc.StatusCode);
        Assert.Equal((int)SandikDurum.Kapandi, k.Sandik.DurumId);
        Assert.Empty(k.Olaylar);
        Assert.Empty(k.Hareket.Rows);
        Assert.Equal(0, k.Uow.SaveCount);
    }

    [Fact]
    public async Task GeriAl_SebepliElleOlayYazarHareketeSebebiVeNotuEkler()
    {
        var k = new Kurgu((int)SandikDurum.Kapandi);
        var sonuc = await k.ProjeKapat(kapali: false, sebepId: 1, not: "  Trafo sahada kuruldu  ");
        Assert.True(sonuc.IsSuccess);
        Assert.Equal((int)SandikDurum.Hazirlaniyor, k.Sandik.DurumId);
        var olay = Assert.Single(k.Olaylar);
        Assert.Equal((SandikDurumOlayTuru.Acildi, SandikDurumOlayKaynagi.Elle, (int?)1, "Trafo sahada kuruldu", (int?)7),
            (olay.Tur, olay.Kaynak, olay.SebepId, olay.Not, olay.KullaniciId));
        Assert.Equal(((int?)(int)SandikDurum.Kapandi, (int)SandikDurum.Hazirlaniyor, 3, "S-1"),
            (olay.EskiDurumId, olay.YeniDurumId, olay.ProjeId, olay.SandikNo));
        var hareket = Assert.Single(k.Hareket.Rows);
        Assert.Contains("Sebep: Trafo kurulum.", hareket.Aciklama);
        Assert.Contains("Not: Trafo sahada kuruldu", hareket.Aciklama);
    }

    [Fact]
    public async Task ProjeKapat_ElleKapanmaOlayiSebepsizdir()
    {
        var k = new Kurgu((int)SandikDurum.Hazirlaniyor);
        Assert.True((await k.ProjeKapat(kapali: true, sebepId: 1, not: "yok sayılır")).IsSuccess);
        var olay = Assert.Single(k.Olaylar);
        Assert.Equal((SandikDurumOlayTuru.Kapandi, SandikDurumOlayKaynagi.Elle, (int?)null, (string?)null),
            (olay.Tur, olay.Kaynak, olay.SebepId, olay.Not));
    }

    [Fact]
    public async Task ProjeKapat_DurumDegismezseOlayYazilmaz()
    {
        var k = new Kurgu((int)SandikDurum.Hazirlaniyor);
        Assert.True((await k.ProjeKapat(kapali: false, sebepId: 1)).IsSuccess);
        Assert.Empty(k.Olaylar);
        Assert.Empty(k.Hareket.Rows);
    }

    [Fact]
    public async Task TekilVeTopluKapatma_KaynaginiYazar()
    {
        var tekil = new Kurgu((int)SandikDurum.Hazirlaniyor);
        Assert.True((await new SandikKapatCommandHandler(tekil.Uow, new OrtakUser(), tekil.Hareket, new OrtakSaha())
            .Handle(new() { SandikId = 1 }, default)).IsSuccess);
        Assert.Equal(SandikDurumOlayKaynagi.Elle, Assert.Single(tekil.Olaylar).Kaynak);

        var toplu = new Kurgu((int)SandikDurum.Hazirlaniyor);
        Assert.True((await new TopluSandikKapatCommandHandler(toplu.Uow, new OrtakUser(), toplu.Hareket, new OrtakSaha())
            .Handle(new() { SandikIds = [1] }, default)).IsSuccess);
        var olay = Assert.Single(toplu.Olaylar);
        Assert.Equal((SandikDurumOlayTuru.Kapandi, SandikDurumOlayKaynagi.Toplu, (int?)(int)SandikDurum.Hazirlaniyor),
            (olay.Tur, olay.Kaynak, olay.EskiDurumId));
    }

    [Fact]
    public void IcerikMiktari_SandikDetayiylaAyniEksikHesabi()
    {
        var satir = new CekiSatiri { Id = 10, IstenenAdet = 5m };
        var icerik = new SandikIcerik { SandikId = 1, CekiSatiriId = 10, CekiSatiri = satir, TahsisMiktari = 4m, KonulanAdet = 1.5m };
        var diger = new SandikIcerik { SandikId = 2, CekiSatiriId = 10, TahsisMiktari = 1m, KonulanAdet = 1m };
        Assert.Equal(new SandikIcerikMiktari(4m, 1.5m, 2.5m), SandikTahsisHelper.HesaplaIcerikMiktari(icerik, [icerik, diger]));
        // Grid'de kapanan satır tamam sayılır.
        satir.GridDurumuId = (int)GridDurum.GridKapandi;
        Assert.Equal(new SandikIcerikMiktari(4m, 4m, 0m), SandikTahsisHelper.HesaplaIcerikMiktari(icerik, [icerik, diger]));
        // Fazla konulan eksik üretmez; çeki satırı olmayan (elle eklenen) malzeme kendi miktarıyla hesaplanır.
        var elle = new SandikIcerik { Miktar = 2m, KonulanAdet = 3m };
        Assert.Equal(0m, SandikTahsisHelper.HesaplaIcerikMiktari(elle, [elle]).Eksik);
        elle.KonulanAdet = 1m;
        Assert.Equal(1m, SandikTahsisHelper.HesaplaIcerikMiktari(elle, [elle]).Eksik);
    }

    [Fact]
    public void IcerikMiktari_KalaniSifirlayanDurumlarEksikSayilmaz()
    {
        // Ekrandaki durum: 1 adetlik satır trafoyla gitti, sandığa konmadı; kalan 0, eksik de 0 olmalı.
        var trafo = new CekiSatiri { Id = 20, IstenenAdet = 1m, TrafoSevkAdet = 1m, GridDurumuId = (int)GridDurum.TrafoSevk };
        var trafoIcerik = new SandikIcerik { SandikId = 1, CekiSatiriId = 20, CekiSatiri = trafo, TahsisMiktari = 1m };
        Assert.Equal(0m, trafo.KalanMiktar);
        Assert.Equal(new SandikIcerikMiktari(1m, 0m, 0m), SandikTahsisHelper.HesaplaIcerikMiktari(trafoIcerik, [trafoIcerik]));

        // Kısmi trafo: 5'in 2'si trafoyla, 1'i sandıkta → sandıkta 2 eksik.
        var kismi = new CekiSatiri { Id = 21, IstenenAdet = 5m, TrafoSevkAdet = 2m };
        var kismiIcerik = new SandikIcerik { SandikId = 1, CekiSatiriId = 21, CekiSatiri = kismi, TahsisMiktari = 5m, KonulanAdet = 1m };
        Assert.Equal(2m, SandikTahsisHelper.HesaplaIcerikMiktari(kismiIcerik, [kismiIcerik]).Eksik);

        // Çok sandıklı satırda trafo adedi tahsis oranında paylaşılır (3K ürün listesindeki gibi): 6+4, trafo 5 → 3 ve 2.
        var bolunmus = new CekiSatiri { Id = 22, IstenenAdet = 10m, TrafoSevkAdet = 5m };
        var a = new SandikIcerik { SandikId = 1, CekiSatiriId = 22, CekiSatiri = bolunmus, TahsisMiktari = 6m };
        var b = new SandikIcerik { SandikId = 2, CekiSatiriId = 22, CekiSatiri = bolunmus, TahsisMiktari = 4m };
        Assert.Equal(3m, SandikTahsisHelper.HesaplaIcerikMiktari(a, [a, b]).Eksik);
        Assert.Equal(2m, SandikTahsisHelper.HesaplaIcerikMiktari(b, [a, b]).Eksik);

        // Grid İptal: kalan 0, konulan değişmeden eksik 0.
        var iptal = new CekiSatiri { Id = 23, IstenenAdet = 3m, GridDurumuId = (int)GridDurum.Iptal };
        var iptalIcerik = new SandikIcerik { SandikId = 1, CekiSatiriId = 23, CekiSatiri = iptal, TahsisMiktari = 3m, KonulanAdet = 1m };
        Assert.Equal(0m, iptal.KalanMiktar);
        Assert.Equal(new SandikIcerikMiktari(3m, 1m, 0m), SandikTahsisHelper.HesaplaIcerikMiktari(iptalIcerik, [iptalIcerik]));
    }

    private sealed class Kurgu
    {
        public OrtakMemoryUow Uow { get; } = new();
        public OrtakHareket Hareket { get; } = new();
        public Sandik Sandik { get; }
        public List<SandikDurumOlayi> Olaylar => Uow.Repo<SandikDurumOlayi>().Rows;

        public Kurgu(int durumId)
        {
            Sandik = new Sandik { Id = 1, ProjeId = 3, SandikNo = "S-1", DurumId = durumId };
            Uow.Repo<Sandik>().Rows.Add(Sandik);
            Uow.Repo<LookupSandikAcilmaSebebi>().Rows.Add(new LookupSandikAcilmaSebebi { Id = 1, Anahtar = 1, Deger = "Trafo kurulum" });
        }

        public Task<Result<bool>> ProjeKapat(bool kapali, int? sebepId = null, string? not = null) =>
            new ProjeSandikKapatHandler(Uow, Hareket, new OrtakUser())
                .Handle(new ProjeSandikKapat { SandikId = 1, Kapali = kapali, AcilmaSebebiId = sebepId, Not = not }, default);
    }
}
