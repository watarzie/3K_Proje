using ClosedXML.Excel;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class YedekCekiExcelParserTests
{
    [Fact]
    public void YeniFormat_ProjeSatirlariVeOlcuBirimleriniOkur()
    {
        var bytes = ExcelOlustur(workbook =>
        {
            workbook.Worksheets.Add("BosSayfa");
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Bileşen numarası";
            sheet.Cell(1, 2).Value = "      PA573-5010";
            sheet.Cell(1, 3).Value = "Bileşen miktarı";
            sheet.Cell(1, 4).Value = "Bileşen ölçü birimi";

            for (var index = 0; index < 27; index++)
            {
                var row = index + 2;
                sheet.Cell(row, 1).Value = index == 0
                    ? "FC3006240-07"
                    : index == 26
                        ? "FCT01210937"
                        : $"FC{index:00000000}";
                sheet.Cell(row, 2).Value = $"Yedek malzeme {index + 1}";
                sheet.Cell(row, 3).Value = index + 1;
                sheet.Cell(row, 4).Value = index switch
                {
                    24 or 25 => "M",
                    26 => "KG",
                    _ => "AD"
                };
            }

            workbook.Worksheets.Add("BosSayfa2");
        });

        var result = YedekCekiImportService.ExceliOkuVeDogrula(bytes, CancellationToken.None);

        Assert.Equal("PA573-5010", result.ProjeNo);
        Assert.Equal(27, result.Satirlar.Count);
        Assert.Equal("FC3006240-07", result.Satirlar[0].BarkodNo);
        Assert.Equal("FCT01210937", result.Satirlar[^1].BarkodNo);
        Assert.Equal(24, result.Satirlar.Count(s => s.BirimId == (int)Birim.Adet));
        Assert.Equal(2, result.Satirlar.Count(s => s.BirimId == (int)Birim.Metre));
        Assert.Single(result.Satirlar, s => s.BirimId == (int)Birim.Kg);
        Assert.All(result.Satirlar, s => Assert.Equal(string.Empty, s.UretimDepoYeri));
    }

    [Fact]
    public void EskiFormat_MevcutKolonlariVeVarsayilanAdetBiriminiKorumayaDevamEder()
    {
        var bytes = ExcelOlustur(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Kalem no.";
            sheet.Cell(1, 2).Value = "Bileşen numarası";
            sheet.Cell(1, 3).Value = "PA629-5010";
            sheet.Cell(1, 4).Value = "Bileşen miktarı(BÖB)";
            sheet.Cell(1, 5).Value = "Üretim depo yeri";
            sheet.Cell(2, 1).Value = "0001";
            sheet.Cell(2, 2).Value = "FCT01153847";
            sheet.Cell(2, 3).Value = "DİĞER KORUMA CİHAZLARI";
            sheet.Cell(2, 4).Value = 2.5m;
            sheet.Cell(2, 5).Value = "ATT1";
        });

        var result = YedekCekiImportService.ExceliOkuVeDogrula(bytes, CancellationToken.None);

        Assert.Equal("PA629-5010", result.ProjeNo);
        var satir = Assert.Single(result.Satirlar);
        Assert.Equal("FCT01153847", satir.BarkodNo);
        Assert.Equal("DİĞER KORUMA CİHAZLARI", satir.Aciklama);
        Assert.Equal(2.5m, satir.Miktar);
        Assert.Equal((int)Birim.Adet, satir.BirimId);
        Assert.Equal("ATT1", satir.UretimDepoYeri);
    }

    [Theory]
    [InlineData("AD", Birim.Adet)]
    [InlineData("PÇ", Birim.Adet)]
    [InlineData("SET", Birim.Set)]
    [InlineData("MT", Birim.Metre)]
    [InlineData("KİLOGRAM", Birim.Kg)]
    [InlineData("LİTRE", Birim.Litre)]
    [InlineData("TKM", Birim.Takim)]
    [InlineData("PKT", Birim.Paket)]
    [InlineData("TN", Birim.Ton)]
    [InlineData("M²", Birim.Metrekare)]
    [InlineData("METREKÜP", Birim.Metrekup)]
    public void YeniFormat_DesteklenenBirimleriDomainBirimineCevirir(string excelBirimi, Birim beklenen)
    {
        var bytes = YeniFormatTekSatir(excelBirimi);

        var result = YedekCekiImportService.ExceliOkuVeDogrula(bytes, CancellationToken.None);

        Assert.Equal((int)beklenen, Assert.Single(result.Satirlar).BirimId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("KUTU")]
    public void YeniFormat_BosVeyaDesteklenmeyenBirimiSatirNumarasiylaReddeder(string excelBirimi)
    {
        var bytes = YeniFormatTekSatir(excelBirimi);

        var exception = Assert.Throws<CekiImportValidationException>(() =>
            YedekCekiImportService.ExceliOkuVeDogrula(bytes, CancellationToken.None));

        Assert.Contains("Excel satırı 2", exception.Message);
        Assert.Contains("ölçü birimi", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] YeniFormatTekSatir(string birim)
    {
        return ExcelOlustur(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Bileşen numarası";
            sheet.Cell(1, 2).Value = "PA573-5010";
            sheet.Cell(1, 3).Value = "Bileşen miktarı";
            sheet.Cell(1, 4).Value = "Bileşen ölçü birimi";
            sheet.Cell(2, 1).Value = "FC3006240-07";
            sheet.Cell(2, 2).Value = "CONTA";
            sheet.Cell(2, 3).Value = 1;
            sheet.Cell(2, 4).Value = birim;
        });
    }

    private static byte[] ExcelOlustur(Action<XLWorkbook> duzenle)
    {
        using var workbook = new XLWorkbook();
        duzenle(workbook);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
