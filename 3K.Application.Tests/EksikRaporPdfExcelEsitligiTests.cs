using ClosedXML.Excel;
using _3K.Infrastructure.Services.Raporlar;
using static _3K.Infrastructure.Services.Raporlar.EksikUrunlerRaporService;

namespace _3K.Application.Tests;

/// <summary>
/// Eksik raporunun PDF'i ve Excel'i aynı veri modelinden (<see cref="EksikRaporVerisi"/>) üretilir: Excel ana tablo ve
/// Saha İzi hücreleri modelin PDF'e yazdığı metinlerle (<c>Hucreler</c>) birebir aynıdır; Saha İzi ayrı sekmede değil,
/// ana tablonun altında başlıklı bölümdür; PDF başlığındaki FB No Excel'de de vardır.
/// </summary>
public sealed class EksikRaporPdfExcelEsitligiTests
{
    private static EksikRaporVerisi Veri(bool sahaIziVar = true) => new(
        "PA600-39-40-SAHA", "SONELGAZ", "PA600-39", SahaRaporuMu: true, "31.10.2026 23:27",
        [
            new EksikRaporSatiri(1, 8, "FCT00682571", "-", "Buchholz Rölesi\n\\Buchholz Relay", "1: sevk sonrası / -",
                1, 0, 0, 0, "PA600-40 (1)", "-", "-", "-", 1, null, "Bekliyor", true, "-", ""),
            new EksikRaporSatiri(2, 12, "FC3098044", "P-4", "KELEPCE (VANT.MONT.)A2-70", "36: Ortak Sandık / -",
                40, 0, 40, 2, "PA600-40 (40)", "-", "Alınan: 3 (PA1)", "-", 0, null, "Tedarikçiden Geldi", false, "-",
                "3K: ambar teslim 22.10.2026")
        ],
        sahaIziVar
            ?
            [
                new SahaIziSatiri("PA600-39", 136, "FC403895", "Kablo Bağı 300W\n\\Cable Holder", 100, "PA600-39-40-SAHA", "36",
                    "PA600-39-40-SAHA (36)", "Sevk edildi 23.10.2026", true),
                new SahaIziSatiri("PA600-39", 172, "SMC45149", "Basınç Ventili Flanşı", 2, "PA600-51-SAHA", "3",
                    "PA600-39-40-SAHA (1) → PA600-51-SAHA (3)", "Bekliyor", false)
            ]
            : []);

    [Fact]
    public void Excel_AnaTabloVeSahaIziPdfMetinleriyleAyni_SahaIziAyniSayfadaTablonunAltinda()
    {
        var veri = Veri();
        using var kitap = new XLWorkbook(new MemoryStream(ExcelOlustur(veri)));
        var sayfa = Assert.Single(kitap.Worksheets);

        Assert.Equal(("FB No", "PA600-39"), (sayfa.Cell(2, 11).GetString(), sayfa.Cell(2, 12).GetString()));
        Assert.Equal(AnaTabloBasliklari, Enumerable.Range(1, 19).Select(c => sayfa.Cell(6, c).GetString()));
        foreach (var satir in veri.Satirlar)
            Assert.Equal(satir.Hucreler(), Enumerable.Range(1, 19).Select(c => sayfa.Cell(6 + satir.No, c).GetFormattedString()));

        // Ana tablo 8. satırda biter; bir boş satırdan sonra bölüm başlığı, özet notu ve Saha İzi tablosu gelir.
        Assert.True(sayfa.Row(9).IsEmpty());
        Assert.Equal($"{SahaIziBasligi} — PA600-39-40-SAHA", sayfa.Cell(10, 1).GetString());
        Assert.True(sayfa.Cell(10, 1).IsMerged());
        Assert.Equal($"{SahaIziAciklamasi} · {veri.SahaIziOzeti}", sayfa.Cell(11, 1).GetString());
        Assert.Equal("2 ürün · sevk edilen 1", veri.SahaIziOzeti);
        Assert.Equal(SahaIziBasliklari, SahaIziExcelSutunlari.Select(s => sayfa.Cell(12, s.Ilk).GetString()));

        for (var i = 0; i < veri.SahaIzi.Count; i++)
        {
            var satir = 13 + i;
            Assert.Equal(veri.SahaIzi[i].Hucreler(i + 1), SahaIziExcelSutunlari.Select(s => sayfa.Cell(satir, s.Ilk).GetFormattedString()));
            foreach (var (ilk, son) in SahaIziExcelSutunlari.Where(s => s.Son > s.Ilk))
                Assert.Equal(sayfa.Range(satir, ilk, satir, son).RangeAddress.ToString(),
                    sayfa.MergedRanges.Single(r => r.Contains(sayfa.Cell(satir, ilk))).RangeAddress.ToString());
        }
        Assert.Equal("PA600-39-40-SAHA (1) → PA600-51-SAHA (3)", sayfa.Cell(14, 13).GetString());
        Assert.True(sayfa.Row(15).IsEmpty());
    }

    [Fact]
    public void SahaIziSutunlari_PdfVeExcelAyniSirada_AnaTablonunAyniAnlamliSutunlarininAltinda()
    {
        Assert.Equal(SahaIziBasliklari.Length, SahaIziExcelSutunlari.Length);
        Assert.Equal(SahaIziBasliklari.Length, Veri().SahaIzi[0].Hucreler(1).Length);
        // Sütun aralıkları boşluksuz ve çakışmasız ana tablonun 19 sütununu kaplar.
        Assert.Equal(Enumerable.Range(1, 19), SahaIziExcelSutunlari.SelectMany(s => Enumerable.Range(s.Ilk, s.Son - s.Ilk + 1)));
        // Birleşik aralık ana tablonun aynı anlamlı sütununda biter (ör. Kaynak Proje 8–11, Ürün Açıklaması 4–5).
        string AnaBaslik(string izBasligi) => AnaTabloBasliklari[SahaIziExcelSutunlari[Array.IndexOf(SahaIziBasliklari, izBasligi)].Son - 1];
        Assert.Equal(("Sıra", "Barkod No", "Ürün Açıklaması", "Sandık", "Kaynak Proje", "Hedef Saha"),
            (AnaBaslik("Sıra"), AnaBaslik("Barkod No"), AnaBaslik("Ürün Açıklaması"), AnaBaslik("Sandık"),
                AnaBaslik("Kaynak Proje"), AnaBaslik("Gittiği Saha")));
    }

    [Fact]
    public void SahaIziYoksa_ExcelYalnizAnaTablo_PdfAyniModeldenUretilir()
    {
        var veri = Veri(sahaIziVar: false);
        using var kitap = new XLWorkbook(new MemoryStream(ExcelOlustur(veri)));
        var sayfa = Assert.Single(kitap.Worksheets);
        Assert.Equal(8, sayfa.LastRowUsed()!.RowNumber());

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(PdfOlustur(veri), 0, 4));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(PdfOlustur(Veri()), 0, 4));
    }
}
