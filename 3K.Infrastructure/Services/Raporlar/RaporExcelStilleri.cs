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

    internal static void FinalizeExcelWorksheet(IXLWorksheet worksheet, int headerRow, int lastRow, int lastColumn)
    {
        if (lastRow >= headerRow)
        {
            var usedRange = worksheet.Range(headerRow, 1, lastRow, lastColumn);
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#D9E2EC");
            usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#D9E2EC");
        }

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents();

        for (var col = 1; col <= lastColumn; col++)
        {
            if (worksheet.Column(col).Width > 48)
                worksheet.Column(col).Width = 48;
        }
    }
}
