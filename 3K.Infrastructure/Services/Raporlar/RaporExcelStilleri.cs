using ClosedXML.Excel;
using _3K.Core.Enums;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Excel raporlarının ortak başlık, bilgi satırı, tablo başlığı ve sayfa düzeni.</summary>
internal static class RaporExcelStilleri
{
    internal static void SetDecimalCell(IXLCell cell, decimal value)
    {
        cell.SetValue(value);
        cell.Style.NumberFormat.Format = decimal.Truncate(value) == value ? "0" : "0.####";
    }

    /// <summary>SEVK hücresi: metin kalın ve sevk durumunun rengiyle yazılır (PDF ile aynı).</summary>
    internal static void SetSevkCell(IXLCell cell, string metin, UrunSevkDurumu? durum)
    {
        cell.Value = metin;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.FromHtml(RaporBicimleri.SevkRengi(durum));
    }

    /// <summary>Sandığın sahaya aktarım metni; "-" dışındaki değer kalın ve morla yazılır (PDF ile aynı).</summary>
    internal static void SetSahaAktarimCell(IXLCell cell, string metin)
    {
        cell.Value = metin;
        if (metin != "-")
            SahaAktarimStili(cell);
    }

    private static void SahaAktarimStili(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.FromHtml(RaporBicimleri.SahaAktarimRengi);
    }

    /// <summary>
    /// Tablo başlığının üstündeki açıklama notu: satır boyunca birleştirilir (birleşik hücre sütun genişliğini
    /// etkilemez), sarılır ve italik gri yazılır.
    /// </summary>
    internal static void SetNotSatiri(IXLWorksheet worksheet, int row, int lastColumn, string metin)
    {
        var range = worksheet.Range(row, 1, row, lastColumn);
        range.Merge();
        range.Value = metin;
        range.Style.Font.Italic = true;
        range.Style.Font.FontColor = XLColor.FromHtml("#616161");
        range.Style.Alignment.WrapText = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        worksheet.Row(row).Height = 30;
    }

    internal static void StyleExcelInfoRows(IXLWorksheet worksheet, int lastColumn)
    {
        var infoRange = worksheet.Range(2, 1, 3, lastColumn);
        infoRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        infoRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    internal static void StyleExcelTitle(IXLWorksheet worksheet, int lastColumn)
    {
        var titleRange = worksheet.Range(1, 1, 1, lastColumn);
        titleRange.Merge();
        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        titleRange.Style.Font.FontColor = XLColor.White;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 16;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(1).Height = 28;
    }

    internal static void StyleExcelHeader(IXLWorksheet worksheet, int headerRow, int lastColumn)
    {
        var headerRange = worksheet.Range(headerRow, 1, headerRow, lastColumn);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
    }

    /// <summary>Aynı sayfada ana tablodan sonra gelen bölümün (ör. Saha İzi) başlık bandı: satır boyunca birleşik, mavi.</summary>
    internal static void StyleExcelBolumBasligi(IXLWorksheet worksheet, int row, int lastColumn)
    {
        var range = worksheet.Range(row, 1, row, lastColumn);
        range.Merge();
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 13;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(row).Height = 22;
    }

    internal static void TabloKenarlari(IXLWorksheet worksheet, int headerRow, int lastRow, int lastColumn)
    {
        if (lastRow < headerRow)
            return;

        var usedRange = worksheet.Range(headerRow, 1, lastRow, lastColumn);
        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D9E2EC");
        usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D9E2EC");
    }

    /// <summary>
    /// Excel birleşik hücrede sarılan metnin satır yüksekliğini kendisi ayarlamaz; satır sayısı sütun genişliklerinden
    /// tahmin edilir. Sütun genişlikleri kesinleştikten (<see cref="FinalizeExcelWorksheet"/>) sonra çağrılır.
    /// </summary>
    internal static void BirlesikSatirYuksekliginiAyarla(IXLWorksheet worksheet, int row, IEnumerable<(int Ilk, int Son)> araliklar)
    {
        var satirSayisi = 1;
        foreach (var (ilk, son) in araliklar)
        {
            var genislik = Math.Max(1, Enumerable.Range(ilk, son - ilk + 1).Sum(c => worksheet.Column(c).Width) - 1);
            var metin = worksheet.Cell(row, ilk).GetString();
            var gereken = metin.Split('\n').Sum(parca => Math.Max(1, (int)Math.Ceiling(parca.Length / genislik)));
            satirSayisi = Math.Max(satirSayisi, gereken);
        }

        worksheet.Row(row).Height = satirSayisi * 15;
    }

    internal static void FinalizeExcelWorksheet(IXLWorksheet worksheet, int headerRow, int lastRow, int lastColumn)
    {
        TabloKenarlari(worksheet, headerRow, lastRow, lastColumn);

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents();

        for (var col = 1; col <= lastColumn; col++)
        {
            if (worksheet.Column(col).Width > 48)
                worksheet.Column(col).Width = 48;
        }
    }
}
