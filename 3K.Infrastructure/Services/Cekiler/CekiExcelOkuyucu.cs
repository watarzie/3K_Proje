using ClosedXML.Excel;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using System.Globalization;
using System.Text;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Çeki Excel dosyalarının okunması: revizyon işaretli çıktı sayfası, çeki listesindeki sandık bilgileri ve packing list İngilizce adları. Veritabanına erişmez.</summary>
internal static class CekiExcelOkuyucu
{
    private const int MaxRevizyonTarananSatirSayisi = 20_000;

    private const int MaxRevizyonIsaretliSatirSayisi = 2_000;

    internal static CiktiImportBilgisi OkuCiktiImportBilgisi(IXLWorkbook workbook)
    {
        var worksheet = BulCiktiSayfasi(workbook)
            ?? throw new CekiRevizyonValidationException(
                "Excel dosyasında 'ÇIKTI SAYFASI' bulunamadı.",
                new[]
                {
                    CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                        CekiRevizyonSorunKodlari.GecersizDosya,
                        "Excel dosyasında 'ÇIKTI SAYFASI' adlı revizyon sayfası bulunamadı.")
                });

        var baslik = OkuCiktiBaslikBilgileri(worksheet);
        var baslangicSatir = BulCiktiBaslangicSatiri(worksheet);
        var headerRow = baslangicSatir - 1;
        var pozNoKolon = BulCiktiPozNoKolonu(worksheet, headerRow);
        var checkKolon = BulCiktiCheckKolonu(worksheet, headerRow);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslangicSatir;
        var tarananSatirSayisi = Math.Max(0, lastRow - baslangicSatir + 1);
        RevizyonTarananSatirSayisiniDogrula(tarananSatirSayisi, "ÇIKTI SAYFASI");

        var satirlar = new List<CiktiSatirImportBilgisi>();
        var isaretliSatirSayisi = 0;
        for (var r = baslangicSatir; r <= lastRow; r++)
        {
            var checkKodu = ReadRevisionCheckCode(worksheet.Cell(r, checkKolon));
            if (!string.IsNullOrEmpty(checkKodu) &&
                ++isaretliSatirSayisi > MaxRevizyonIsaretliSatirSayisi)
            {
                CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                    $"Revizyon dosyasında A, U veya D ile işaretlenen satır sayısı {MaxRevizyonIsaretliSatirSayisi:N0} sınırını aşıyor. Revizyonu daha küçük dosyalara bölerek yeniden deneyin.",
                    CekiRevizyonSorunKodlari.SatirSiniriAsildi);
            }

            var barkod = worksheet.Cell(r, 3).GetString().Trim();
            var tanim = worksheet.Cell(r, 4).GetString().Trim();
            var hasData = !string.IsNullOrWhiteSpace(barkod) || !string.IsNullOrWhiteSpace(tanim);

            if (!hasData && string.IsNullOrWhiteSpace(checkKodu))
                continue;

            var siraNo = TryReadInt(worksheet.Cell(r, 1)) ?? Math.Max(1, r - headerRow);
            var miktar = ReadNullableDecimal(worksheet.Cell(r, 6)) ?? 0;

            satirlar.Add(new CiktiSatirImportBilgisi
            {
                ExcelSatirNo = r,
                SiraNo = siraNo,
                OlcuResmiPozNo = pozNoKolon.HasValue ? worksheet.Cell(r, pozNoKolon.Value).GetString().Trim() : string.Empty,
                BarkodNo = barkod,
                Aciklama = tanim,
                KoliNo = NormalizeKoliNo(worksheet.Cell(r, 5).GetString().Trim()),
                IstenenAdet = miktar,
                BirimId = ParseBirimToId(worksheet.Cell(r, 7).GetString().Trim()),
                Remarks = worksheet.Cell(r, 14).GetString().Trim(),
                CheckKodu = checkKodu,
                VeriSatiriMi = hasData
            });
        }

        return new CiktiImportBilgisi
        {
            FbNo = baslik.FbNo,
            Musteri = baslik.Musteri,
            Lokasyon = baslik.Lokasyon,
            Satirlar = satirlar,
            SandikBilgileri = OkuCekiListesiSandikBilgileri(
                workbook,
                revizyonSinirlariniUygula: true)
        };
    }

    private static void RevizyonTarananSatirSayisiniDogrula(
        int tarananSatirSayisi,
        string sayfaAdi)
    {
        if (tarananSatirSayisi <= MaxRevizyonTarananSatirSayisi)
            return;

        CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
            $"Revizyon dosyasının {sayfaAdi} bölümünde taranacak veri satırı sayısı {MaxRevizyonTarananSatirSayisi:N0} sınırını aşıyor. Gereksiz boş satırları temizleyip revizyonu daha küçük dosyalara bölerek yeniden deneyin.",
            CekiRevizyonSorunKodlari.SatirSiniriAsildi);
    }

    private static IXLWorksheet? BulCiktiSayfasi(IXLWorkbook workbook)
    {
        return workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name) == "CIKTI SAYFASI")
            ?? workbook.Worksheets.FirstOrDefault(ws =>
            {
                var name = NormalizeExcelText(ws.Name);
                return name.Contains("CIKTI") && !name.Contains("YDK") && !name.Contains("YEDEK");
            });
    }

    private static CiktiBaslikBilgisi OkuCiktiBaslikBilgileri(IXLWorksheet worksheet)
    {
        string fbNo = string.Empty;
        for (int r = 1; r <= 10; r++)
        {
            for (int c = 1; c <= 8; c++)
            {
                var val = NormalizeExcelText(worksheet.Cell(r, c).GetString());
                if (val.Contains("FB NO") || val.Contains("SERIAL NO"))
                {
                    fbNo = worksheet.Cell(r, c + 1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(fbNo))
                        fbNo = worksheet.Cell(r, c + 2).GetString().Trim();
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(fbNo)) break;
        }

        return new CiktiBaslikBilgisi
        {
            FbNo = fbNo,
            Musteri = worksheet.Cell(2, 6).GetString().Trim(),
            Lokasyon = worksheet.Cell(4, 6).GetString().Trim()
        };
    }

    private static int BulCiktiBaslangicSatiri(IXLWorksheet worksheet)
    {
        for (int r = 1; r <= 20; r++)
        {
            if (TryReadInt(worksheet.Cell(r, 1)).HasValue)
                return r;
        }

        return 6;
    }

    private static int BulCiktiCheckKolonu(IXLWorksheet worksheet, int headerRow)
    {
        if (headerRow <= 0)
            return 11;

        var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 80);
        for (int c = 1; c <= lastColumn; c++)
        {
            var header = NormalizeExcelText(worksheet.Cell(headerRow, c).GetString());
            if (header.Contains("CHECK") || header.Contains("KONTROL"))
                return c;
        }

        return 11;
    }

    private static string ReadRevisionCheckCode(IXLCell cell)
    {
        var value = NormalizeExcelText(cell.GetString());
        return value switch
        {
            "A" => "A",
            "U" => "U",
            "D" => "D",
            _ => string.Empty
        };
    }

    private static int? TryReadInt(IXLCell cell)
    {
        if (cell.TryGetValue<int>(out var intValue))
            return intValue;

        var text = cell.GetString().Trim();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
            return (int)decimalValue;

        return null;
    }

    /// <summary>
    /// Reads case names and physical values from the packing list sheet.
    /// </summary>
    internal static Dictionary<string, SandikImportBilgisi> OkuCekiListesiSandikBilgileri(
        IXLWorkbook workbook,
        bool revizyonSinirlariniUygula = false)
    {
        var sonuc = new Dictionary<string, SandikImportBilgisi>();
        var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
        {
            var name = NormalizeExcelText(ws.Name);
            return name.Contains("CEKI") && name.Contains("LISTESI") && !name.Contains("CIKTI");
        }) ?? workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name).Contains("PACKING LIST"));

        if (worksheet == null)
            return sonuc;

        var baslikSatiri = BulCekiListesiBaslikSatiri(worksheet);
        if (baslikSatiri == null)
            return sonuc;

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslikSatiri.Value;
        if (revizyonSinirlariniUygula)
        {
            RevizyonTarananSatirSayisiniDogrula(
                Math.Max(0, lastRow - baslikSatiri.Value),
                "ÇEKİ LİSTESİ");
        }

        for (int r = baslikSatiri.Value + 1; r <= lastRow; r++)
        {
            var koliNoRaw = worksheet.Cell(r, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(koliNoRaw))
                continue;

            var sandikIsmi = worksheet.Cell(r, 10).GetString().Trim();
            var ambalajCinsi = worksheet.Cell(r, 3).GetString().Trim();
            var netKg = ReadNullableDecimal(worksheet.Cell(r, 4));
            var grossKg = ReadNullableDecimal(worksheet.Cell(r, 5));
            var boyMm = ReadNullableDecimal(worksheet.Cell(r, 6));
            var enMm = ReadNullableDecimal(worksheet.Cell(r, 7));
            var yukseklikMm = ReadNullableDecimal(worksheet.Cell(r, 8));

            if (string.IsNullOrWhiteSpace(sandikIsmi)
                && string.IsNullOrWhiteSpace(ambalajCinsi)
                && netKg == null
                && grossKg == null
                && boyMm == null
                && enMm == null
                && yukseklikMm == null)
            {
                continue;
            }

            foreach (var koliNo in ExpandKoliNo(koliNoRaw))
            {
                if (sonuc.ContainsKey(koliNo))
                    continue;

                sonuc[koliNo] = new SandikImportBilgisi
                {
                    SandikNo = koliNo,
                    SandikIsmi = string.IsNullOrWhiteSpace(sandikIsmi) ? null : sandikIsmi,
                    AmbalajCinsi = string.IsNullOrWhiteSpace(ambalajCinsi) ? null : ambalajCinsi,
                    En = enMm,
                    Boy = boyMm,
                    Yukseklik = yukseklikMm,
                    NetKg = netKg,
                    GrossKg = grossKg
                };
            }
        }

        foreach (var item in OkuPackingListSandikIngilizceAdlari(
                     workbook,
                     revizyonSinirlariniUygula))
        {
            if (sonuc.TryGetValue(item.Key, out var bilgi))
            {
                bilgi.SandikIsmiIngilizce = item.Value;
            }
            else
            {
                sonuc[item.Key] = new SandikImportBilgisi
                {
                    SandikNo = item.Key,
                    SandikIsmiIngilizce = item.Value
                };
            }
        }

        return sonuc;
    }

    private static Dictionary<string, string> OkuPackingListSandikIngilizceAdlari(
        IXLWorkbook workbook,
        bool revizyonSinirlariniUygula)
    {
        var sonuc = new Dictionary<string, string>();
        var worksheet = workbook.Worksheets.FirstOrDefault(ws => NormalizeExcelText(ws.Name).Contains("PACKING LIST"));
        var usedRange = worksheet?.RangeUsed();

        if (worksheet == null || usedRange == null)
            return sonuc;

        if (!TryFindPackingListColumns(worksheet, usedRange, out var headerRow, out var caseNumberColumn, out var caseDescriptionColumn))
            return sonuc;

        var lastRow = usedRange.LastRow().RowNumber();
        if (revizyonSinirlariniUygula)
        {
            RevizyonTarananSatirSayisiniDogrula(
                Math.Max(0, lastRow - headerRow),
                "PACKING LIST");
        }

        for (var row = headerRow + 1; row <= lastRow; row++)
        {
            var caseNumber = worksheet.Cell(row, caseNumberColumn).GetString().Trim();
            var caseDescription = worksheet.Cell(row, caseDescriptionColumn).GetString().Trim();

            if (string.IsNullOrWhiteSpace(caseNumber) || string.IsNullOrWhiteSpace(caseDescription))
                continue;

            var sandikNo = ExtractPackingListSandikNo(caseNumber);
            if (string.IsNullOrWhiteSpace(sandikNo))
                continue;

            sonuc[sandikNo] = caseDescription;
        }

        return sonuc;
    }

    private static bool TryFindPackingListColumns(
        IXLWorksheet worksheet,
        IXLRange usedRange,
        out int headerRow,
        out int caseNumberColumn,
        out int caseDescriptionColumn)
    {
        headerRow = 0;
        caseNumberColumn = 0;
        caseDescriptionColumn = 0;

        var firstRow = usedRange.FirstRow().RowNumber();
        var lastRow = Math.Min(usedRange.LastRow().RowNumber(), firstRow + 80);
        var firstColumn = usedRange.FirstColumn().ColumnNumber();
        var lastColumn = usedRange.LastColumn().ColumnNumber();

        for (var row = firstRow; row <= lastRow; row++)
        {
            for (var column = firstColumn; column <= lastColumn; column++)
            {
                var header = NormalizeExcelText(worksheet.Cell(row, column).GetString());
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                if (caseNumberColumn == 0 && header.Contains("CASE") && (header.Contains("NUMBER") || header.Contains("NO")))
                {
                    caseNumberColumn = column;
                    headerRow = Math.Max(headerRow, row);
                }

                if (caseDescriptionColumn == 0
                    && (header.Contains("CONTENT")
                        || header.Contains("CASE DESCRIPTION")
                        || header.Contains("CASE NAME")))
                {
                    caseDescriptionColumn = column;
                    headerRow = Math.Max(headerRow, row);
                }
            }

            if (caseNumberColumn > 0 && caseDescriptionColumn > 0)
                return true;
        }

        return false;
    }

    private static string ExtractPackingListSandikNo(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();
        var trailingDigits = new string(text.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());

        return string.IsNullOrWhiteSpace(trailingDigits)
            ? NormalizeKoliNo(text)
            : NormalizeKoliNo(trailingDigits);
    }

    private static int? BulCekiListesiBaslikSatiri(IXLWorksheet worksheet)
    {
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 100);
        var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 20);

        for (int r = 1; r <= lastRow; r++)
        {
            var rowText = new StringBuilder();
            for (int c = 1; c <= lastColumn; c++)
                rowText.Append(' ').Append(NormalizeExcelText(worksheet.Cell(r, c).GetString()));

            var text = rowText.ToString();
            if ((text.Contains("KOLI") || text.Contains("CASE"))
                && (text.Contains("AMBALAJ") || text.Contains("PACKAGE"))
                && (text.Contains("CINSI") || text.Contains("DESCRIPTION")))
            {
                return r;
            }
        }

        return null;
    }

    internal static int? BulCiktiPozNoKolonu(IXLWorksheet worksheet, int headerRow)
    {
        if (headerRow <= 0)
            return null;

        var lastColumn = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, 80);
        int? fallbackColumn = null;

        for (int c = 4; c <= lastColumn; c++)
        {
            var text = NormalizeExcelText(worksheet.Cell(headerRow, c).GetString());
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var isPozNo = (text.Contains("POZ") || text.Contains("POS")) && text.Contains("NO");
            if (!isPozNo)
                continue;

            fallbackColumn = c;
            if (!text.Contains("OLCU") && !text.Contains("DIMENSIONAL") && !text.Contains("DRW"))
                return c;
        }

        return fallbackColumn;
    }

    private static IEnumerable<string> ExpandKoliNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        var text = value.Trim()
            .Replace('\u00A0', ' ')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        var parts = text.Split(new[] { ',', ';', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var normalizedPart = part.Trim();
            var rangeParts = normalizedPart.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (rangeParts.Length == 2
                && int.TryParse(NormalizeKoliNo(rangeParts[0]), out var start)
                && int.TryParse(NormalizeKoliNo(rangeParts[1]), out var end)
                && start > 0
                && end >= start
                && end - start <= 500)
            {
                for (var no = start; no <= end; no++)
                    yield return no.ToString(CultureInfo.InvariantCulture);

                continue;
            }

            var normalized = NormalizeKoliNo(normalizedPart);
            if (!string.IsNullOrWhiteSpace(normalized))
                yield return normalized;
        }
    }

    internal static string NormalizeKoliNo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim()
            .Replace('\u00A0', ' ')
            .Replace(" ", string.Empty)
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        if (!text.Contains(',') && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue)
            && numericValue == decimal.Truncate(numericValue))
        {
            return ((int)numericValue).ToString(CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static decimal? ReadNullableDecimal(IXLCell cell)
    {
        var text = cell.GetString().Trim().Replace("\u00A0", string.Empty).Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out var trValue))
            return trValue;

        if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var invariantValue))
            return invariantValue;

        return null;
    }

    internal static string NormalizeExcelText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim()
            .Replace('\u00A0', ' ')
            .ToUpper(new CultureInfo("tr-TR"))
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    internal static string NormalizeTextKey(string? value)
    {
        return NormalizeExcelText(value).Replace(" ", string.Empty);
    }

    /// <summary>
    /// Sanitizes picture names that ClosedXML cannot read.
    /// </summary>
    internal static byte[] SanitizePictureNames(byte[] fileBytes)
    {
        var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };

        using var stream = new MemoryStream();
        stream.Write(fileBytes, 0, fileBytes.Length);
        stream.Position = 0;

        try
        {
            using var doc = SpreadsheetDocument.Open(stream, true);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return fileBytes;

            foreach (var wsPart in workbookPart.WorksheetParts)
            {
                var drawingsPart = wsPart.DrawingsPart;
                if (drawingsPart == null) continue;

                var wsDr = drawingsPart.WorksheetDrawing;
                if (wsDr == null) continue;

                bool changed = false;
                foreach (var anchor in wsDr.Descendants<TwoCellAnchor>())
                {
                    var pic = anchor.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture>().FirstOrDefault();
                    if (pic?.NonVisualPictureProperties?.NonVisualDrawingProperties != null)
                    {
                        var nvProps = pic.NonVisualPictureProperties.NonVisualDrawingProperties;
                        var name = nvProps.Name?.Value;
                        if (!string.IsNullOrEmpty(name) && name.IndexOfAny(invalidChars) >= 0)
                        {
                            foreach (var ch in invalidChars)
                                name = name.Replace(ch, '_');
                            nvProps.Name = name;
                            changed = true;
                        }
                    }
                }

                // OneCellAnchor'daki resimleri de temizle
                foreach (var anchor in wsDr.Descendants<OneCellAnchor>())
                {
                    var pic = anchor.Descendants<DocumentFormat.OpenXml.Drawing.Spreadsheet.Picture>().FirstOrDefault();
                    if (pic?.NonVisualPictureProperties?.NonVisualDrawingProperties != null)
                    {
                        var nvProps = pic.NonVisualPictureProperties.NonVisualDrawingProperties;
                        var name = nvProps.Name?.Value;
                        if (!string.IsNullOrEmpty(name) && name.IndexOfAny(invalidChars) >= 0)
                        {
                            foreach (var ch in invalidChars)
                                name = name.Replace(ch, '_');
                            nvProps.Name = name;
                            changed = true;
                        }
                    }
                }

                if (changed) wsDr.Save();
            }

            doc.Save();
            stream.Position = 0;
            return stream.ToArray();
        }
        catch
        {
            // OpenXML ile açılamazsa orijinal byte'ları dön
            return fileBytes;
        }
    }

    /// <summary>
    /// Excel'deki serbest metin birim değerini Birim Enum Id'sine çevirir.
    /// </summary>
    internal static int ParseBirimToId(string birimText)
    {
        if (string.IsNullOrWhiteSpace(birimText))
            return (int)Birim.Adet;

        var normalized = birimText.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        return normalized switch
        {
            "ADET" or "AD" or "PCS" or "PÇ" or "PC" => (int)Birim.Adet,
            "SET" or "ST" => (int)Birim.Set,
            "METRE" or "MT" or "M" => (int)Birim.Metre,
            "KG" or "KILOGRAM" => (int)Birim.Kg,
            "LT" or "LITRE" or "LİTRE" => (int)Birim.Litre,
            "TAKIM" or "TK" or "TKM" => (int)Birim.Takim,
            "PAKET" or "PKT" or "PK" => (int)Birim.Paket,
            "TON" or "TN" => (int)Birim.Ton,
            "M2" or "METREKARE" or "M²" => (int)Birim.Metrekare,
            "M3" or "METREKÜP" or "METREKUP" or "M³" => (int)Birim.Metrekup,
            _ => (int)Birim.Adet
        };
    }
}
