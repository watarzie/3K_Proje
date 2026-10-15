using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

/// <summary>
/// PO/fatura grup dağıtımı, proje/grup durum birleştirmesi, kategori ve hacim ayrımı ile belge toplamı
/// kuralları (§3.3, §3.6–§3.8, F5, F7, F11, RR Q10). Kurallar Core'da tektir; SQL karşılıkları aynı sonucu verir.
/// </summary>
public sealed class FinansPoFaturaKurallariTests
{
    [Fact]
    public void Grup_tutari_kalemlere_sirayla_ve_kapasiteyi_asmadan_dagitilir()
    {
        var paylar = FinansDagitimKurallari.Dagit(3000m,
            [new(1, 1250m), new(2, 0m), new(3, 1500m), new(4, 1625m)], "Sipariş");

        Assert.Equal([new(1, 1250m), new(3, 1500m), new(4, 250m)], paylar);
        Assert.Equal(3000m, paylar.Sum(x => x.Tutar));
    }

    [Theory]
    [InlineData(4375.01)]
    [InlineData(0)]
    [InlineData(10.005)]
    public void Grup_kalanini_asan_veya_gecersiz_tutar_reddedilir(decimal tutar)
    {
        Assert.Throws<IsKuraliException>(() => FinansDagitimKurallari.Dagit(tutar,
            [new(1, 1250m), new(2, 1500m), new(3, 1625m)], "Sipariş"));
    }

    [Fact]
    public void Grupta_ayni_kalem_iki_kez_bulunamaz()
    {
        Assert.Throws<IsKuraliException>(() => FinansDagitimKurallari.Dagit(10m, [new(1, 10m), new(1, 10m)], "Fatura"));
    }

    /// <summary>§9 "Proje durumu": yalnız Ana Ambalaj PO'lu proje kısmi siparişte; bütün kalemler PO'lu olunca açılır.</summary>
    [Fact]
    public void Proje_bir_gerekli_kalem_tam_siparise_baglanmadikca_kismi_siparistir()
    {
        var anaAmbalajPolu = new FinansKalemTutarlari(4375m, 4375m, 0m);
        var sarfPoYok = new FinansKalemTutarlari(500m, 0m, 0m);
        var ilavePoYok = new FinansKalemTutarlari(800m, 0m, 0m);

        var kismi = FinansTutarKurallari.BirlesikDurum([anaAmbalajPolu, sarfPoYok, ilavePoYok]);
        Assert.Equal(FinansKarsilanma.Kismi, kismi.Siparis);
        Assert.Equal(FinansIsDurumu.KismiSiparis, kismi.Durum);

        var tam = FinansTutarKurallari.BirlesikDurum([anaAmbalajPolu, sarfPoYok with { Siparis = 500m }, ilavePoYok with { Siparis = 800m }]);
        Assert.Equal(FinansKarsilanma.Tam, tam.Siparis);
        Assert.Equal(FinansKarsilanma.Yok, tam.Fatura);
        Assert.Equal(FinansIsDurumu.SiparisAcildi, tam.Durum);
    }

    [Fact]
    public void Proje_ancak_butun_gerekli_kalemleri_faturalaninca_tamamlanir_fiyatsiz_kalem_ayri_sayilir()
    {
        var faturali = new FinansKalemTutarlari(1000m, 1000m, 1000m);
        var kismiFatura = new FinansKalemTutarlari(1000m, 1000m, 400m);
        var fiyatsiz = new FinansKalemTutarlari(0m, 0m, 0m);

        var kismi = FinansTutarKurallari.BirlesikDurum([faturali, kismiFatura, fiyatsiz]);
        Assert.Equal(FinansIsDurumu.KismiFaturalandi, kismi.Durum);
        Assert.Equal((2, 1), (kismi.GerekliKalem, kismi.FiyatBekleyenKalem));

        var tamam = FinansTutarKurallari.BirlesikDurum([faturali, kismiFatura with { Fatura = 1000m }]);
        Assert.Equal(FinansKarsilanma.Tam, tamam.Fatura);
        Assert.Equal(FinansIsDurumu.Faturalandi, tamam.Durum);

        var bos = FinansTutarKurallari.BirlesikDurum([fiyatsiz]);
        Assert.Equal(FinansIsDurumu.SiparisBekliyor, bos.Durum);
        Assert.Equal(1, bos.FiyatBekleyenKalem);
    }

    /// <summary>SQL karşılanma ifadesi Core kuralıyla aynı sonucu verir (CLAUDE.md: kural ve SQL eşdeğerliği sabitlenir).</summary>
    [Theory]
    [InlineData(1000, 0, FinansKarsilanma.Yok)]
    [InlineData(1000, 600, FinansKarsilanma.Kismi)]
    [InlineData(1000, 1000, FinansKarsilanma.Tam)]
    [InlineData(0, 0, FinansKarsilanma.Yok)]
    public void Sql_karsilanma_ifadesi_core_kuraliyla_aynidir(decimal net, decimal siparis, FinansKarsilanma beklenen)
    {
        var order = new FinansSiparis { Id = 1, PoNumarasi = "PO" };
        var work = new FinansIsKaydi { FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar, BirimFiyatSnapshot = net, Adet = 1 };
        if (siparis > 0)
            work.SiparisKalemleri.Add(new FinansSiparisKalemi { FinansSiparis = order, NetTutarSnapshot = siparis });

        Assert.Equal(beklenen, FinansTutarKurallari.KalemKarsilanma(FinansTutarKurallari.IsNet(work), FinansTutarKurallari.SiparisNet(work)));
        foreach (var durum in Enum.GetValues<FinansKarsilanma>())
        {
            var sql = FinansSorgulari.KarsilanmaIfadesi(FinansSorgulari.SiparisNetIfadesi, durum).Compile();
            Assert.Equal(durum == beklenen, sql(work));
        }
        Assert.Equal(net > siparis, FinansSorgulari.SiparisBekleyenIfadesi().Compile()(work));
    }

    [Fact]
    public void Kategori_uretim_turlerini_ve_duzenli_isi_ayirir_uretim_kategorisi_ozel_is_olmaz()
    {
        Assert.Equal(FinansIsKategorisi.AnaAmbalaj, FinansKategoriKurali.Kategori(FinansIsTuru.AnaAmbalaj, null));
        Assert.Equal(FinansIsKategorisi.SarfKereste, FinansKategoriKurali.Kategori(FinansIsTuru.SarfKereste, null));
        Assert.Equal(FinansIsKategorisi.DigerUretim, FinansKategoriKurali.Kategori(FinansIsTuru.Tadilat, null));
        Assert.Equal(FinansIsKategorisi.OzelIs, FinansKategoriKurali.Kategori(FinansIsTuru.OzelIs, null));
        Assert.Equal(FinansIsKategorisi.DuzenliIs, FinansKategoriKurali.Kategori(FinansIsTuru.OzelIs, 7));
        foreach (var tur in Enum.GetValues<FinansIsTuru>().Where(x => x != FinansIsTuru.OzelIs))
            Assert.True(FinansKategoriKurali.UretimKategorisi(FinansKategoriKurali.Kategori(tur, 7)));
    }

    /// <summary>§9 "Kontra sandık": iki fiyat satırı (10 m³ × X, 5 m³ × Y) kontra m³'ü 15 yapar; ahşap m³ ayrı raporlanır.</summary>
    [Fact]
    public void Kontra_m3_fiyat_satirlarindan_ahsap_m3_uretimden_ayri_hesaplanir()
    {
        var kontra = FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Kontrplak, "Kontrplak Sandık", 0m,
            [new("Kontra 18 mm", FinansFiyatlandirmaBirimi.Metrekup, 10m, 300m), new("Kontra 12 mm", FinansFiyatlandirmaBirimi.Metrekup, 5m, 250m),
             new("İşçilik", FinansFiyatlandirmaBirimi.SabitTutar, 1m, 100m)]);
        var ahsap = FinansHacimKurallari.Hacim(AmbalajSandikCinsi.AhsapKapali, null, 12.5m, null);
        var eskiKayit = FinansHacimKurallari.Hacim(null, "Kafes Sandık", 2m, null);

        Assert.Equal((0m, 15m), kontra);
        Assert.Equal((12.5m, 0m), ahsap);
        Assert.Equal((2m, 0m), eskiKayit);
    }

    /// <summary>
    /// Elle girilen ahşap/kontra m³ türetilen payın yerine geçer; bileşik toplam (ahşap + kontra) fiyatlandırma
    /// miktarıdır: ToplamM3 bileşik, BirimM3 = ToplamM3 / Adet. Boş değer eski türetmeye düşer.
    /// </summary>
    [Fact]
    public void Elle_girilen_ahsap_kontra_m3_bilesik_toplam_olur_ve_turetmenin_yerine_gecer()
    {
        Assert.Equal((1.2m, 0.8m), FinansHacimKurallari.Hacim(1.2m, 0.8m, AmbalajSandikCinsi.Kontrplak, null, 99m, null));
        Assert.Equal((0m, 3m), FinansHacimKurallari.Hacim(null, 3m, AmbalajSandikCinsi.AhsapKapali, null, 12.5m, null));
        Assert.Equal((12.5m, 0m), FinansHacimKurallari.Hacim(null, null, AmbalajSandikCinsi.AhsapKapali, null, 12.5m, null));

        var kayit = new FinansIsKaydi { Adet = 3, AhsapM3 = 1.2m, KontraM3 = 0.8m };
        FinansHacimKurallari.ManuelHacmiUygula(kayit);
        Assert.Equal((2m, 0.666667m), (kayit.ToplamM3, kayit.BirimM3));
        kayit.Adet = 4;
        FinansHacimKurallari.ManuelHacmiUygula(kayit);
        Assert.Equal((2m, 0.5m), (kayit.ToplamM3, kayit.BirimM3));
        kayit.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup;
        kayit.BirimFiyatSnapshot = 100m;
        Assert.Equal(200m, FinansTutarKurallari.IsNet(kayit));
    }

    /// <summary>Elle hacim yalnız Ambalaj'ın m³ hesaplamadığı kontrplak/katlanır sandıkta; önceden elle girilmiş hacim düzeltilebilir.</summary>
    [Theory]
    [InlineData(AmbalajSandikCinsi.Kontrplak, null, false, true)]
    [InlineData(AmbalajSandikCinsi.Katlanir, null, false, true)]
    [InlineData(null, "Katlanır Sandık", false, true)]
    [InlineData(AmbalajSandikCinsi.AhsapKapali, null, false, false)]
    [InlineData(AmbalajSandikCinsi.Kafes, null, false, false)]
    [InlineData(null, null, false, false)]
    [InlineData(null, null, true, true)]
    [InlineData(AmbalajSandikCinsi.AhsapKapali, null, true, true)]
    public void Elle_hacim_yalniz_kontrplak_katlanir_sandikta_girilir(AmbalajSandikCinsi? cinsi, string? tipi, bool hacimManuel, bool beklenen) =>
        Assert.Equal(beklenen, FinansHacimKurallari.ElleGirilebilir(cinsi, tipi, hacimManuel));

    [Theory]
    [InlineData(1.2, 0.8, true)]
    [InlineData(0, 0.5, true)]
    [InlineData(0, 0, false)]
    [InlineData(-1, 2, false)]
    [InlineData(1000000, 0, false)]
    public void Elle_hacim_girisi_negatif_olamaz_en_az_biri_pozitif_olmalidir(double ahsap, double kontra, bool gecerli) =>
        Assert.Equal(gecerli, FinansHacimKurallari.GecerliManuelHacim((decimal)ahsap, (decimal)kontra));

    /// <summary>F11: m³ fiyat satırı kendi malzemesini taşır; malzemesi boş satır sandığın malzemesini alır.</summary>
    [Fact]
    public void Malzemeli_m3_satirlari_kendi_malzemesine_eklenir()
    {
        var karma = FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Katlanir, null, 0m,
            [new("Kontra", FinansFiyatlandirmaBirimi.Metrekup, 10m, 300m, FinansSandikMalzemesi.Kontra),
             new("Ahşap kızak", FinansFiyatlandirmaBirimi.Metrekup, 2m, 200m, FinansSandikMalzemesi.Ahsap),
             new("Malzemesiz", FinansFiyatlandirmaBirimi.Metrekup, 5m, 250m)]);
        var yalnizAhsapSatiri = FinansHacimKurallari.Hacim(AmbalajSandikCinsi.Kontrplak, null, 7m,
            [new("Ahşap kızak", FinansFiyatlandirmaBirimi.Metrekup, 2m, 200m, FinansSandikMalzemesi.Ahsap)]);
        var ahsapSandikKontraEki = FinansHacimKurallari.Hacim(AmbalajSandikCinsi.AhsapKapali, null, 12.5m,
            [new("Kontra kapak", FinansFiyatlandirmaBirimi.Metrekup, 1.5m, 300m, FinansSandikMalzemesi.Kontra),
             new("Ahşap (üretim m³'ünde)", FinansFiyatlandirmaBirimi.Metrekup, 12.5m, 350m)]);
        var cinsiBilinmeyen = FinansHacimKurallari.Hacim(null, null, 3m,
            [new("Kontra", FinansFiyatlandirmaBirimi.Metrekup, 4m, 300m, FinansSandikMalzemesi.Kontra)]);

        Assert.Equal((2m, 15m), karma);
        Assert.Equal((2m, 0m), yalnizAhsapSatiri);
        Assert.Equal((12.5m, 1.5m), ahsapSandikKontraEki);
        Assert.Equal((0m, 4m), cinsiBilinmeyen);
    }

    /// <summary>RR Q10/D2: belge net + KDV = brüt, kuruşa yuvarlandıktan sonra kesin eşitlik; doğrulayıcı aynı kuralı kullanır.</summary>
    [Theory]
    [InlineData(100, 20, 120, true)]
    [InlineData(100, 20, 120.01, false)]
    [InlineData(100.004, 20, 120, true)]
    public void Belge_toplami_kesin_esitlikle_dogrulanir(decimal net, decimal kdv, decimal toplam, bool gecerli)
    {
        Assert.Equal(gecerli, FinansTutarKurallari.BelgeToplamiDengeli(net, kdv, toplam));
        var model = new FinansFaturaOlusturModel(1, "F-1", new DateTime(2026, 9, 29), null,
            [new FinansFaturaKalemiOlusturModel(1, 0, 0, 100)], "EUR", net, kdv, toplam);
        Assert.Equal(gecerli, new FinansFaturaOlusturModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void Siparis_ve_fatura_tek_tek_veya_grup_kalemiyle_olusturulabilir()
    {
        var poValidator = new FinansSiparisOlusturModelValidator();
        Assert.False(poValidator.Validate(new FinansSiparisOlusturModel("PO-1", new DateTime(2026, 9, 29), null, [])).IsValid);
        Assert.True(poValidator.Validate(new FinansSiparisOlusturModel("PO-1", new DateTime(2026, 9, 29), null, [],
            Gruplar: [new FinansSiparisGrupDagitimModel([1, 2, 3], 3000m)])).IsValid);
        Assert.False(poValidator.Validate(new FinansSiparisOlusturModel("PO-1", new DateTime(2026, 9, 29), null, [],
            Gruplar: [new FinansSiparisGrupDagitimModel([], 3000m)])).IsValid);

        var invoiceValidator = new FinansFaturaOlusturModelValidator();
        Assert.True(invoiceValidator.Validate(new FinansFaturaOlusturModel(1, "F-1", new DateTime(2026, 9, 29), null, [],
            Gruplar: [new FinansFaturaGrupKalemiModel([5, 6], 2000m)])).IsValid);
        Assert.False(invoiceValidator.Validate(new FinansFaturaOlusturModel(1, "F-1", new DateTime(2026, 9, 29), null, [])).IsValid);
    }

    /// <summary>Fatura grup kalemi yalnız seçilen PO'nun satırlarına ve her satırın faturalanmamış bakiyesine dağıtılır.</summary>
    [Fact]
    public void Fatura_grup_kalemi_po_satir_bakiyelerine_dagitilir_baska_po_satiri_kabul_edilmez()
    {
        var order = new FinansSiparis { Id = 9, PoNumarasi = "PO-9" };
        var invoice = new FinansFatura { FinansSiparis = order };
        var line1 = new FinansSiparisKalemi { Id = 11, FinansSiparis = order, NetTutarSnapshot = 1000m };
        line1.FaturaKalemleri.Add(new FinansFaturaKalemi { FinansFatura = invoice, NetTutarSnapshot = 600m });
        var line2 = new FinansSiparisKalemi { Id = 12, FinansSiparis = order, NetTutarSnapshot = 3000m };
        order.Kalemler.Add(line1);
        order.Kalemler.Add(line2);

        var lines = FinansSiparisFaturaService.FaturaKalemleriniHazirla(order, [], [new FinansFaturaGrupKalemiModel([12, 11], 2000m)]);

        Assert.Equal([(11, 400m), (12, 1600m)], lines.Select(x => (x.SiparisKalemiId, x.NetTutar!.Value)));
        Assert.Throws<IsKuraliException>(() => FinansSiparisFaturaService.FaturaKalemleriniHazirla(order, [],
            [new FinansFaturaGrupKalemiModel([11, 12], 3400.01m)]));
        Assert.Throws<IsKuraliException>(() => FinansSiparisFaturaService.FaturaKalemleriniHazirla(order, [],
            [new FinansFaturaGrupKalemiModel([99], 10m)]));
    }

    [Fact]
    public void Siparis_grubu_uretimde_proje_kategori_para_birimi_duzenli_iste_is_para_birimi_ozel_iste_tek_istir()
    {
        var a1 = new FinansIsKaydi { Id = 1, ProjeId = 5, IsTuru = FinansIsTuru.AnaAmbalaj, ParaBirimiSnapshot = "EUR" };
        var a2 = new FinansIsKaydi { Id = 2, ProjeId = 5, IsTuru = FinansIsTuru.AnaAmbalaj, ParaBirimiSnapshot = "EUR" };
        var sarf = new FinansIsKaydi { Id = 3, ProjeId = 5, IsTuru = FinansIsTuru.SarfKereste, ParaBirimiSnapshot = "EUR" };
        var kira1 = new FinansIsKaydi { Id = 4, IsTuru = FinansIsTuru.OzelIs, DuzenliIsId = 8, ParaBirimiSnapshot = "TRY" };
        var kira2 = new FinansIsKaydi { Id = 5, IsTuru = FinansIsTuru.OzelIs, DuzenliIsId = 8, ParaBirimiSnapshot = "TRY" };
        var ozel = new FinansIsKaydi { Id = 6, IsTuru = FinansIsTuru.OzelIs, ParaBirimiSnapshot = "EUR" };

        var keys = new[] { a1, a2, sarf, kira1, kira2, ozel }.Select(FinansSiparisFaturaService.SiparisGrupAnahtari).ToArray();

        Assert.Equal(keys[0], keys[1]);
        Assert.NotEqual(keys[0], keys[2]);
        Assert.Equal(keys[3], keys[4]);
        Assert.Equal("O:6", keys[5]);
    }
}
