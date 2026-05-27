using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// İş akışı 9: PDF / Excel oluşturma
    /// 
    /// ExcelOlusturAsync:
    ///   Orijinal çeki Excel şablonunu açar → ClosedXML ile operasyon verilerini
    ///   (paketleyen baş harfleri, kontrol baş harfleri, açıklama/remarks)
    ///   ilgili hücrelere yazar → Şablon düzeni değiştirilmez → Excel döner.
    ///   "Excel neyse PDF odur."
    /// 
    /// PdfOlusturAsync:
    ///   QuestPDF ile sandık bazlı PDF raporu oluşturur.
    /// </summary>
    public class PdfService : IPdfService
    {
        private readonly AppDbContext _context;

        public PdfService(AppDbContext context)
        {
            _context = context;
        }

        private static string FormatAdet(decimal value)
        {
            if (decimal.Truncate(value) == value)
                return decimal.Truncate(value).ToString(System.Globalization.CultureInfo.InvariantCulture);

            return value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static int GetRaporSandikSortKey(string? sandikNo)
        {
            if (string.IsNullOrWhiteSpace(sandikNo))
                return int.MaxValue;

            var match = System.Text.RegularExpressions.Regex.Match(sandikNo, @"\d+");
            return match.Success && int.TryParse(match.Value, out var numericValue)
                ? numericValue
                : int.MaxValue;
        }

        private static List<string> GetRaporSandikNoList(CekiSatiri satir)
        {
            if (!string.IsNullOrWhiteSpace(satir.FiiliSandikNo))
                return new List<string> { satir.FiiliSandikNo.Trim() };

            var aktifSandiklar = satir.SandikIcerikleri
                .Where(si => si.KonulanAdet > 0 && si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
                .Select(si => si.Sandik!.SandikNo.Trim())
                .Distinct()
                .OrderBy(GetRaporSandikSortKey)
                .ThenBy(x => x)
                .ToList();

            if (aktifSandiklar.Any())
                return aktifSandiklar;

            if (!string.IsNullOrWhiteSpace(satir.CekideGecenSandikNo))
                return new List<string> { satir.CekideGecenSandikNo.Trim() };

            var sandiklar = satir.SandikIcerikleri
                .Where(si => si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
                .Select(si => si.Sandik!.SandikNo.Trim())
                .Distinct()
                .OrderBy(GetRaporSandikSortKey)
                .ThenBy(x => x)
                .ToList();

            return sandiklar;
        }

        private static string GetRaporSandikNolari(CekiSatiri satir)
        {
            return string.Join(", ", GetRaporSandikNoList(satir));
        }

        private static (int Number, string Text) GetRaporPrimarySandikSortKey(CekiSatiri satir)
        {
            var firstSandikNo = GetRaporSandikNoList(satir).FirstOrDefault() ?? "";
            return (GetRaporSandikSortKey(firstSandikNo), firstSandikNo);
        }

        private static decimal GetRaporTrafodaSevkAdet(CekiSatiri satir)
        {
            return Math.Max(satir.TrafoSevkAdet, 0);
        }

        private static bool IsRaporGridIptal(CekiSatiri satir)
        {
            return satir.GridDurumuId == (int)GridDurum.Iptal
                || satir.GridDurumuId == (int)GridDurum.IptalEdildi;
        }

        private static decimal GetRaporGerceklesenAdet(CekiSatiri satir)
        {
            if (IsRaporGridIptal(satir))
                return 0;

            var trafodaSevk = GetRaporTrafodaSevkAdet(satir);
            var gerceklesen = satir.IstenenAdet - trafodaSevk - satir.KalanMiktar;
            var maxGerceklesen = Math.Max(satir.IstenenAdet - trafodaSevk, 0);

            if (gerceklesen < 0)
                return 0;

            return gerceklesen > maxGerceklesen ? maxGerceklesen : gerceklesen;
        }

        private static int GetRaporDurumSortPriority(CekiSatiri satir)
        {
            if (GetRaporTrafodaSevkAdet(satir) > 0)
                return 0;

            if (IsRaporGridIptal(satir))
                return 1;

            if (satir.KalanMiktar > 0)
                return 2;

            return 3;
        }

        private static string GetGerceklesenRaporDurum(CekiSatiri satir)
        {
            if (GetRaporTrafodaSevkAdet(satir) > 0)
                return "Trafoda Sevk";

            if (IsRaporGridIptal(satir))
                return "İptal";

            return satir.KalanMiktar > 0 ? "Eksik Sevk" : "Tamamlandı";
        }

        private static string GetRaporAciklama(CekiSatiri satir)
        {
            var gridAciklama = satir.GridAciklama?.Trim();
            var ucKAciklama = satir.UcKAciklama?.Trim();
            var hasGrid = !string.IsNullOrWhiteSpace(gridAciklama);
            var hasUcK = !string.IsNullOrWhiteSpace(ucKAciklama);

            if (hasGrid && hasUcK)
                return $"Not 1: {gridAciklama}\nNot 2: {ucKAciklama}";
            if (hasGrid)
                return gridAciklama!;
            if (hasUcK)
                return ucKAciklama!;

            return "";
        }

        private static void SetDecimalCell(IXLCell cell, decimal value)
        {
            cell.SetValue(value);
            cell.Style.NumberFormat.Format = decimal.Truncate(value) == value ? "0" : "0.####";
        }

        private static void StyleExcelInfoRows(IXLWorksheet worksheet, int lastColumn)
        {
            var infoRange = worksheet.Range(2, 1, 3, lastColumn);
            infoRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            infoRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void StyleExcelTitle(IXLWorksheet worksheet, int lastColumn)
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

        private static void StyleExcelHeader(IXLWorksheet worksheet, int headerRow, int lastColumn)
        {
            var headerRange = worksheet.Range(headerRow, 1, headerRow, lastColumn);
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Alignment.WrapText = true;
        }

        private static void FinalizeExcelWorksheet(IXLWorksheet worksheet, int headerRow, int lastRow, int lastColumn)
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

        /// <summary>
        /// İş akışı 9: Orijinal Excel şablonunu açar, operasyon verilerini yazar, Excel olarak döner.
        /// Şablon düzeni değiştirilmez. ÇALIŞMA SAYFASI'ndaki ilgili hücrelere:
        ///   H (8) sütunu → Paketleyen baş harfleri
        ///   I (9) sütunu → Kontrol eden baş harfleri
        ///   M+ (13+) sütunu → Açıklama / Remarks
        /// </summary>
        public async Task<byte[]> ExcelOlusturAsync(int projeId)
        {
            // 1. Projeye ait son çekiyi bul
            var ceki = await _context.Cekiler
                .Where(c => c.ProjeId == projeId)
                .OrderByDescending(c => c.YuklemeTarihi)
                .FirstOrDefaultAsync();

            if (ceki == null)
                throw new KeyNotFoundException($"Proje için çeki bulunamadı: {projeId}");

            if (!File.Exists(ceki.OrijinalDosyaYolu))
                throw new FileNotFoundException($"Orijinal Excel dosyası bulunamadı: {ceki.OrijinalDosyaYolu}");

            // 2. Operasyon verilerini DB'den getir
            var satirlar = await _context.CekiSatirlari
                .Include(cs => cs.Paketleyen)
                .Include(cs => cs.KontrolEden)
                .Include(cs => cs.SandikIcerikleri)
                .Where(cs => cs.CekiId == ceki.Id)
                .OrderBy(cs => cs.SiraNo)
                .ToListAsync();

            // CekiSatiri → BarkodNo ile hızlı erişim için dictionary
            var satirBarkodMap = satirlar
                .Where(s => !string.IsNullOrWhiteSpace(s.BarkodNo))
                .GroupBy(s => s.BarkodNo)
                .ToDictionary(g => g.Key, g => g.First());

            // CekiSatiri → SıraNo+Aciklama ile alternatif eşleme
            var satirSiraMap = satirlar.ToDictionary(s => s.Id, s => s);

            // 3. Orijinal Excel şablonunu aç (kopya üzerinde çalış)
            var orijinalBytes = await File.ReadAllBytesAsync(ceki.OrijinalDosyaYolu);
            using var stream = new MemoryStream(orijinalBytes);
            using var workbook = new XLWorkbook(stream);

            // Gelişmiş isim normalizasyonu
            string NormalizeName(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return "";
                return name.Trim().ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                    .Replace("Ç", "C").Replace("Ş", "S").Replace("İ", "I").Replace("Ö", "O").Replace("Ü", "U").Replace("Ğ", "G");
            }

            IXLWorksheet? worksheet = null;
            var allSheets = workbook.Worksheets.ToList();

            foreach (var ws in allSheets)
            {
                if (NormalizeName(ws.Name) == "CIKTI SAYFASI")
                {
                    worksheet = ws;
                    break;
                }
            }

            if (worksheet == null)
            {
                foreach (var ws in allSheets)
                {
                    var nName = NormalizeName(ws.Name);
                    if (nName.Contains("CIKTI") && !nName.Contains("YDK") && !nName.Contains("YEDEK"))
                    {
                        worksheet = ws;
                        break;
                    }
                }
            }

            if (worksheet != null)
            {
                // Başlangıç satırını bul (B sütununda sayısal değer olan ilk satır)
                int baslangicSatir = 10;
                for (int r = 1; r <= 20; r++)
                {
                    var cellVal = worksheet.Cell(r, 2).GetString().Trim(); // B (2) sütunu (Sıra No)
                    if (int.TryParse(cellVal, out _))
                    {
                        baslangicSatir = r;
                        break;
                    }
                }

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? baslangicSatir;

                // 4. Her satırda eşleşen operasyon verisini bul ve yaz
                for (int r = baslangicSatir; r <= lastRow; r++)
                {
                    var barkod = worksheet.Cell(r, 4).GetString().Trim(); // D (4) sütunu = Barkod
                    var aciklama = worksheet.Cell(r, 5).GetString().Trim(); // E (5) sütunu = Tanım

                    if (string.IsNullOrWhiteSpace(barkod) && string.IsNullOrWhiteSpace(aciklama))
                        continue;

                    // Eşleşen CekiSatiri'ni bul (önce barkod, sonra sıra no ile)
                    CekiSatiri? eslesenSatir = null;
                    if (!string.IsNullOrWhiteSpace(barkod) && satirBarkodMap.TryGetValue(barkod, out var barkodEslesen))
                    {
                        eslesenSatir = barkodEslesen;
                    }

                    if (eslesenSatir == null)
                    {
                        // Sıra no ile eşle
                        var siraNoStr = worksheet.Cell(r, 2).GetString().Trim(); // B (2) sütunu
                        if (int.TryParse(siraNoStr, out int siraNo))
                        {
                            eslesenSatir = satirlar.FirstOrDefault(s => s.SiraNo == siraNo);
                        }
                    }

                    if (eslesenSatir == null) continue;

                    // I (9) sütunu → Paketleyen baş harfleri
                    if (eslesenSatir.Paketleyen != null && !string.IsNullOrWhiteSpace(eslesenSatir.Paketleyen.BasHarf))
                    {
                        worksheet.Cell(r, 9).Value = eslesenSatir.Paketleyen.BasHarf;
                    }

                    // J (10) sütunu → Kontrol eden baş harfleri
                    if (eslesenSatir.KontrolEden != null && !string.IsNullOrWhiteSpace(eslesenSatir.KontrolEden.BasHarf))
                    {
                        worksheet.Cell(r, 10).Value = eslesenSatir.KontrolEden.BasHarf;
                    }

                    // N (14) sütunu → Açıklama / Remarks
                    if (!string.IsNullOrWhiteSpace(eslesenSatir.Remarks))
                    {
                        worksheet.Cell(r, 14).Value = eslesenSatir.Remarks;
                    }
                }
            }

            // ÇEKİ LİSTESİ sayfasını da güncelle (1. sayfa — özet koli bilgileri)
            var cekiSheet = workbook.Worksheets
                .FirstOrDefault(ws => ws.Name.Contains("ÇEKİ", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.First();

            // ÇEKİ LİSTESİ sayfasında şablon düzeni korunur,
            // sadece operasyonel durum bilgisi eklenebilir (ileride genişletilebilir).

            // 5. Değiştirilmiş Excel'i byte[] olarak döndür
            using var outputStream = new MemoryStream();
            workbook.SaveAs(outputStream);
            return outputStream.ToArray();
        }

        /// <summary>
        /// İş akışı 9: QuestPDF ile PDF raporu oluşturur.
        /// Sandık bazlı tablo: Barkod, Açıklama, İstenen, Konulan, Eksik, Birim, Paketleyen, Kontrol, Remarks
        /// </summary>
        public async Task<byte[]> PdfOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadı: {projeId}");

            var sandiklar = await _context.Sandiklar
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.Paketleyen)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.KontrolEden)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.BirimLookup)
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    // Başlık — proje bilgileri
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Text($"Çeki Listesi — Proje: {proje.ProjeNo} | Müşteri: {proje.Musteri}")
                            .Bold().FontSize(12).AlignCenter();

                        headerCol.Item().PaddingTop(3).Row(row =>
                        {
                            if (!string.IsNullOrWhiteSpace(proje.FBNo))
                                row.AutoItem().PaddingRight(15).Text($"FB: {proje.FBNo}").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(proje.Guc))
                                row.AutoItem().PaddingRight(15).Text($"Güç: {proje.Guc} MVA").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(proje.Gerilim))
                                row.AutoItem().PaddingRight(15).Text($"Gerilim: {proje.Gerilim} kV").FontSize(8);
                            if (!string.IsNullOrWhiteSpace(proje.Lokasyon))
                                row.AutoItem().Text($"Lokasyon: {proje.Lokasyon}").FontSize(8);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        foreach (var sandik in sandiklar)
                        {
                            col.Item().PaddingBottom(10).Column(sandikCol =>
                            {
                                sandikCol.Item().Background(Colors.Grey.Lighten3)
                                    .Padding(5)
                                    .Text($"Sandık: {sandik.SandikNo} | Durum: {sandik.DurumId}")
                                    .Bold().FontSize(10);

                                sandikCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30);   // Sıra
                                        columns.RelativeColumn(1.5f); // Poz No
                                        columns.RelativeColumn(2);    // Barkod
                                        columns.RelativeColumn(3);    // Açıklama
                                        columns.ConstantColumn(40);   // İstenen
                                        columns.ConstantColumn(40);   // Konulan
                                        columns.ConstantColumn(40);   // Eksik
                                        columns.ConstantColumn(30);   // Birim
                                        columns.ConstantColumn(35);   // Paketleyen
                                        columns.ConstantColumn(35);   // Kontrol
                                        columns.RelativeColumn(2);    // Remarks
                                    });

                                    // Başlık satırı (TR + EN)
                                    table.Header(header =>
                                    {
                                        void BiHeader(IContainer c, string tr, string en = "")
                                        {
                                            c.Border(1).Padding(2).Column(col =>
                                            {
                                                col.Item().Text(tr).Bold();
                                                if (!string.IsNullOrEmpty(en))
                                                    col.Item().Text(en).FontSize(6).FontColor(Colors.Grey.Darken1).Italic();
                                            });
                                        }

                                        BiHeader(header.Cell(), "#");
                                        BiHeader(header.Cell(), "POZ NO", "Position No");
                                        BiHeader(header.Cell(), "BARKOD NO", "Barcode No");
                                        BiHeader(header.Cell(), "AÇIKLAMA", "Description");
                                        BiHeader(header.Cell(), "İSTENEN", "Requested");
                                        BiHeader(header.Cell(), "KONULAN", "Packed");
                                        BiHeader(header.Cell(), "EKSİK", "Missing");
                                        BiHeader(header.Cell(), "BİRİM", "Unit");
                                        BiHeader(header.Cell(), "PKT", "Packer");
                                        BiHeader(header.Cell(), "KNT", "Inspector");
                                        BiHeader(header.Cell(), "REMARKS");
                                    });

                                    int sira = 1;
                                    foreach (var icerik in sandik.SandikIcerikleri)
                                    {
                                        var cs = icerik.CekiSatiri;
                                        if (cs == null) continue;

                                        table.Cell().Border(1).Padding(2).Text(sira++.ToString());
                                        table.Cell().Border(1).Padding(2).Text(cs.OlcuResmiPozNo ?? "");
                                        table.Cell().Border(1).Padding(2).Text(cs.BarkodNo);
                                        table.Cell().Border(1).Padding(2).Text(cs.Aciklama);
                                        table.Cell().Border(1).Padding(2).Text(cs.IstenenAdet.ToString());
                                        table.Cell().Border(1).Padding(2).Text(icerik.KonulanAdet.ToString());
                                        table.Cell().Border(1).Padding(2).Text(icerik.EksikAdet.ToString());
                                        table.Cell().Border(1).Padding(2).Text(cs.BirimLookup?.Deger ?? "Adet");
                                        table.Cell().Border(1).Padding(2).Text(cs.Paketleyen?.BasHarf ?? "");
                                        table.Cell().Border(1).Padding(2).Text(cs.KontrolEden?.BasHarf ?? "");
                                        table.Cell().Border(1).Padding(2).Text(cs.Remarks ?? "");
                                    }
                                });
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }

        /// <summary>
        /// QuestPDF ile belirli bir saha sandığına ait PDF raporu oluşturur.
        /// </summary>
        public async Task<byte[]> SahaSandikPdfOlusturAsync(int sandikId)
        {
            var sandik = await _context.Sandiklar
                .Include(s => s.Proje)
                .Include(s => s.DurumLookup)
                .Include(s => s.TipLookup)
                .Include(s => s.DepoLokasyonLookup)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.Ceki)
                            .ThenInclude(c => c.Proje)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.BirimLookup)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.BirimLookup)
                .FirstOrDefaultAsync(s => s.Id == sandikId);

            if (sandik == null)
                throw new KeyNotFoundException($"Sandık bulunamadı: {sandikId}");

            var kullaniciDict = await _context.Kullanicilar.ToDictionaryAsync(k => k.Id.ToString(), k => k.AdSoyad);

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header().Column(headerCol =>
                    {
                        var projeTipiStr = sandik.Proje?.ProjeTipiId == 3 ? "Yedek" : "Saha";
                        headerCol.Item().Text($"{projeTipiStr} Sandığı İçerik Raporu")
                            .Bold().FontSize(16).AlignCenter().FontColor(Colors.Blue.Darken2);
                        
                        headerCol.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Sandık No: {sandik.SandikNo}").Bold();
                                col.Item().Text($"Ait Olduğu Proje: {sandik.Proje?.ProjeNo ?? "-"}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Sevk Durumu: {sandik.DurumLookup?.Deger ?? "-"}");
                                var sevkTarihi = sandik.Proje?.GerceklesenSevkTarihi ?? sandik.Proje?.PlanlananSevkTarihi;
                                col.Item().Text($"Sevk Tarihi: {sevkTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "-"}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Ölçüler (E/B/Y): {sandik.En ?? 0} x {sandik.Boy ?? 0} x {sandik.Yukseklik ?? 0} mm");
                                col.Item().Text($"Ağırlık (Net/Gross): {sandik.NetKg ?? 0} kg / {sandik.GrossKg ?? 0} kg");
                            });
                        });
                        
                        headerCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    // Content - Table
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f); // Proje No
                            columns.RelativeColumn(3); // Ürün Adı
                            columns.RelativeColumn(1); // Miktar
                            columns.RelativeColumn(1); // Birim
                            columns.RelativeColumn(2); // Açıklama
                            columns.RelativeColumn(1.5f); // Ekleyen
                            columns.RelativeColumn(1.5f); // Tarih
                        });

                        // Header Row (TR + EN)
                        table.Header(header =>
                        {
                            void BiHeader(IContainer c, string tr, string en = "")
                            {
                                c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Column(col =>
                                {
                                    col.Item().Text(tr).Bold();
                                    if (!string.IsNullOrEmpty(en))
                                        col.Item().Text(en).FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                                });
                            }

                            BiHeader(header.Cell(), "Proje No", "Project No");
                            BiHeader(header.Cell(), "Ürün Adı / Tanımı", "Product Name");
                            BiHeader(header.Cell(), "Miktar", "Quantity");
                            BiHeader(header.Cell(), "Birim", "Unit");
                            BiHeader(header.Cell(), "Açıklama", "Description");
                            BiHeader(header.Cell(), "Ekleyen", "Added By");
                            BiHeader(header.Cell(), "Tarih", "Date");
                        });

                        // Data Rows
                        foreach (var icerik in sandik.SandikIcerikleri)
                        {
                            string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? sandik.Proje?.ProjeNo ?? "-";
                            string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                            string miktar = FormatAdet(icerik.CekiSatiriId == null ? icerik.Miktar : icerik.KonulanAdet);
                            string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                            string aciklama = icerik.Aciklama ?? icerik.CekiSatiri?.GridAciklama ?? "-";
                            
                            string ekleyenId = icerik.CreatedBy ?? "";
                            string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;
                            
                            string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(projeNo);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(urunAdi);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(miktar);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(birim);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(aciklama);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(ekleyen);
                            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tarih);
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }

        /// <summary>
        /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
        /// Eksik raporu tarzında premium tasarım.
        /// </summary>
        public async Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .Include(p => p.ProjeTipiLookup)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.DurumLookup)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.TipLookup)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.DepoLokasyonLookup)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.CekiSatiri)
                            .ThenInclude(cs => cs.Ceki)
                                .ThenInclude(c => c.Proje)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.CekiSatiri)
                            .ThenInclude(cs => cs.BirimLookup)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.BirimLookup)
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadı: {projeId}");

            var sandiklar = proje.Sandiklar.OrderBy(s => s.SandikNo).ToList();

            if (!sandiklar.Any())
                throw new InvalidOperationException("Bu projeye ait sandık bulunamadı.");

            var kullaniciDict = await _context.Kullanicilar.ToDictionaryAsync(k => k.Id.ToString(), k => k.AdSoyad);

            QuestPDF.Settings.License = LicenseType.Community;

            // Renk paleti (eksik raporu ile uyumlu)
            var headerBg = Colors.Blue.Darken3;
            var headerText = Colors.White;
            var tableBorderColor = Colors.Grey.Lighten2;
            var altRowBg = "#F8FAFE";
            var accentColor = proje.ProjeTipiId == 3 ? "#7B1FA2" : "#1565C0"; // Yedek: mor, Saha: mavi
            var projeTipiStr = proje.ProjeTipiId == 3 ? "Yedek" : "Saha";
            var raporTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var toplamUrun = sandiklar.Sum(s => s.SandikIcerikleri.Count);

            var document = Document.Create(container =>
            {
                foreach (var sandik in sandiklar)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(20);
                        page.DefaultTextStyle(x => x.FontSize(8));

                        // ===== HEADER =====
                        page.Header().Column(headerCol =>
                        {
                            // Üst çubuk
                            headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                                    col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                                row.RelativeItem(2).AlignCenter().Column(col =>
                                {
                                    col.Item().Text($"{projeTipiStr} Sandığı İçerik Raporu").Bold().FontSize(16).FontColor(headerText);
                                    col.Item().Text($"3K Sevkiyat Yönetim Sistemi – {projeTipiStr} Yönetimi Çıktısı").FontSize(7).FontColor(Colors.Grey.Lighten3);
                                });
                                row.RelativeItem().AlignRight().Column(col =>
                                {
                                    col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                });
                            });

                            // Proje bilgisi kartları
                            headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                            {
                                // Proje Bilgisi
                                row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                                {
                                    col.Item().Text("PROJE BİLGİSİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                                    col.Item().PaddingTop(3).Text(proje.ProjeNo).Bold().FontSize(13);
                                    col.Item().Text($"Müşteri: {proje.Musteri}").FontSize(8);
                                    if (!string.IsNullOrWhiteSpace(proje.FBNo))
                                        col.Item().Text($"FB No: {proje.FBNo}").FontSize(8);
                                    var sevkTarihiStr = proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";
                                    col.Item().Text($"Sevk Tarihi: {sevkTarihiStr}").FontSize(8);
                                });

                                row.ConstantItem(10); // Spacer

                                // Sandık Bilgisi
                                row.RelativeItem().Border(1).BorderColor(accentColor).Padding(8).Column(col =>
                                {
                                    col.Item().Text("SANDIK BİLGİSİ").Bold().FontSize(7).FontColor(accentColor);
                                    col.Item().PaddingTop(3).Text($"Sandık No: {sandik.SandikNo}").Bold().FontSize(13);
                                    col.Item().Text($"Ölçüler (E/B/Y): {sandik.En ?? 0} x {sandik.Boy ?? 0} x {sandik.Yukseklik ?? 0} mm").FontSize(8);
                                    col.Item().Text($"Ağırlık (Net/Gross): {sandik.NetKg ?? 0} / {sandik.GrossKg ?? 0} kg").FontSize(8);
                                });
                            });

                            headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                        });

                        // ===== CONTENT - TABLE =====
                        page.Content().PaddingVertical(8).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);   // #
                                columns.RelativeColumn(1.5f); // Proje No
                                columns.RelativeColumn(3);    // Ürün Adı
                                columns.ConstantColumn(50);   // Miktar
                                columns.RelativeColumn(1);    // Birim
                                columns.RelativeColumn(2);    // Açıklama
                                columns.RelativeColumn(1.5f); // Ekleyen
                                columns.RelativeColumn(1.5f); // Tarih
                            });

                            // Başlık satırı (TR + EN)
                            table.Header(header =>
                            {
                                void BiHeader(IContainer c, string tr, string en = "")
                                {
                                    c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Column(col =>
                                    {
                                        col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                        if (!string.IsNullOrEmpty(en))
                                            col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                                    });
                                }

                                BiHeader(header.Cell(), "#");
                                BiHeader(header.Cell(), "PROJE NO", "Project No");
                                BiHeader(header.Cell(), "ÜRÜN ADI / TANIMI", "Product Name");
                                BiHeader(header.Cell(), "MİKTAR", "Quantity");
                                BiHeader(header.Cell(), "BİRİM", "Unit");
                                BiHeader(header.Cell(), "AÇIKLAMA", "Description");
                                BiHeader(header.Cell(), "EKLEYEN", "Added By");
                                BiHeader(header.Cell(), "TARİH", "Date");
                            });

                            // Data Rows
                            int sira = 1;
                            foreach (var icerik in sandik.SandikIcerikleri)
                            {
                                var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                                string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? proje.ProjeNo;
                                string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                                string miktar = FormatAdet(icerik.CekiSatiriId == null ? icerik.Miktar : icerik.KonulanAdet);
                                string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                                string aciklama = icerik.Aciklama ?? icerik.CekiSatiri?.GridAciklama ?? "-";

                                string ekleyenId = icerik.CreatedBy ?? "";
                                string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;

                                string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                                void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                                {
                                    var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                                    if (bold)
                                        cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                    else
                                        cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                                }

                                DataCell(table.Cell(), sira.ToString());
                                DataCell(table.Cell(), projeNo, bold: true, fontColor: accentColor);
                                DataCell(table.Cell(), urunAdi);
                                DataCell(table.Cell(), miktar, bold: true);
                                DataCell(table.Cell(), birim);
                                DataCell(table.Cell(), aciklama);
                                DataCell(table.Cell(), ekleyen);
                                DataCell(table.Cell(), tarih);

                                sira++;
                            }
                        });

                        // ===== FOOTER =====
                        page.Footer().Row(footer =>
                        {
                            footer.RelativeItem().Text($"3K Ambalaj – {projeTipiStr} Raporu | Proje: {proje.ProjeNo} | Sandık: {sandik.SandikNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                            footer.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                                x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                                x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                                x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                }
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }
        /// <summary>
        /// Normal projelerdeki Grid durumu Eksik/Gelmedi ve Kalan > 0 olan ürünlerin PDF raporu.
        /// </summary>
        public async Task<byte[]> EksikUrunlerRaporuPdfOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .Include(p => p.ProjeTipiLookup)
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadı: {projeId}");

            // Grid durumu EksikGeldi veya Gelmedi olan VE kalan > 0 olan ürünler
            var satirlar = await _context.CekiSatirlari
                .Include(cs => cs.BirimLookup)
                .Include(cs => cs.GridDurumLookup)
                .Include(cs => cs.UcKDurumLookup)
                .Include(cs => cs.DurumLookup)
                .Include(cs => cs.GeriGonderilmeSebebiLookup)
                .Include(cs => cs.SurecDurumLookup)
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .OrderBy(cs => cs.SiraNo)
                .ToListAsync();

            // Memory'de kalan > 0 filtresi (computed property)
            // Sıralama: Sandık numarasına göre sayısal (1,2,3...10,11), sonra sıra numarasına göre
            var eksikSatirlar = satirlar
                .Where(cs => cs.KalanMiktar > 0)
                .OrderBy(cs =>
                {
                    var sandikStr = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo ?? "";
                    // Sayısal kısmı çıkar (örn: "10" → 10, "SND-3" → 3)
                    var numMatch = System.Text.RegularExpressions.Regex.Match(sandikStr, @"\d+");
                    return numMatch.Success ? int.Parse(numMatch.Value) : int.MaxValue;
                })
                .ThenBy(cs => cs.SiraNo)
                .ToList();

            QuestPDF.Settings.License = LicenseType.Community;

            // Renk paleti
            var headerBg = Colors.Blue.Darken3;
            var headerText = Colors.White;
            var summaryBg = "#F0F7FF";
            var dangerColor = "#D32F2F";
            var warningColor = "#F57C00";
            var tableBorderColor = Colors.Grey.Lighten2;
            var altRowBg = "#F8FAFE";

            var raporTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var toplamEksikUrun = eksikSatirlar.Count;
            var toplamKalanAdet = eksikSatirlar.Sum(s => s.KalanMiktar);
            var toplamIstenenAdet = eksikSatirlar.Sum(s => s.IstenenAdet);
            var toplamGelenAdet = eksikSatirlar.Sum(s => s.GelenMiktar + s.StokKarsilanan + s.ProjeKarsilanan + s.TedarikciKarsilanan);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    // ===== HEADER =====
                    page.Header().Column(headerCol =>
                    {
                        // Üst çubuk
                        headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                            row.RelativeItem(2).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Eksik Ürünler Raporu").Bold().FontSize(16).FontColor(headerText);
                                col.Item().Text("3K Sevkiyat Yönetim Sistemi – Proje Eksik Ürün Çıktısı").FontSize(7).FontColor(Colors.Grey.Lighten3);
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
                        });

                        // Proje bilgisi kartları
                        headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                        {
                            // Proje Bilgisi
                            row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                            {
                                col.Item().Text("PROJE BİLGİSİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                                col.Item().PaddingTop(3).Text(proje.ProjeNo).Bold().FontSize(13);
                                col.Item().Text($"Müşteri: {proje.Musteri}").FontSize(8);
                                if (!string.IsNullOrWhiteSpace(proje.FBNo))
                                    col.Item().Text($"FB No: {proje.FBNo}").FontSize(8);
                            });

                            row.ConstantItem(10); // Spacer

                            // Eksik Özet
                            row.RelativeItem().Border(1).BorderColor(dangerColor).Padding(8).Column(col =>
                            {
                                col.Item().Text("EKSİK ÖZET").Bold().FontSize(7).FontColor(dangerColor);
                                col.Item().PaddingTop(3).Text($"{toplamEksikUrun} ürün eksik").Bold().FontSize(13).FontColor(dangerColor);
                                col.Item().Text($"Toplam Kalan: {FormatAdet(toplamKalanAdet)} adet").FontSize(8);
                                col.Item().Text($"Karşılama Oranı: %{FormatAdet(toplamIstenenAdet > 0 ? (toplamGelenAdet * 100 / toplamIstenenAdet) : 0)}").FontSize(8);
                            });
                        });

                        headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                    });

                    // ===== CONTENT - TABLE =====
                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);   // #
                            columns.ConstantColumn(28);   // Sıra
                            columns.RelativeColumn(1.25f); // Barkod
                            columns.ConstantColumn(42);   // Poz No
                            columns.RelativeColumn(2.7f); // Açıklama
                            columns.RelativeColumn(0.85f); // Sandık
                            columns.ConstantColumn(42);   // İstenen
                            columns.ConstantColumn(46);   // 3K Gelen
                            columns.ConstantColumn(46);   // Karşılanan
                            columns.ConstantColumn(46);   // Geri Gönd.
                            columns.ConstantColumn(50);   // Prj. Verildi
                            columns.ConstantColumn(42);   // Kalan
                            columns.RelativeColumn(1.35f); // 3K Durum
                            columns.RelativeColumn(1);    // Sürec
                        });

                        // Başlık satırı (TR + EN)
                        table.Header(header =>
                        {
                            void BiHeader(IContainer c, string tr, string en = "")
                            {
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Column(col =>
                                {
                                    col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                    if (!string.IsNullOrEmpty(en))
                                        col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                            }

                            BiHeader(header.Cell(), "#");
                            BiHeader(header.Cell(), "SIRA", "Seq.");
                            BiHeader(header.Cell(), "BARKOD NO", "Barcode No");
                            BiHeader(header.Cell(), "POZ NO", "Pos. No");
                            BiHeader(header.Cell(), "ÜRÜN AÇIKLAMASI", "Product Desc.");
                            BiHeader(header.Cell(), "SANDIK", "Crate");
                            BiHeader(header.Cell(), "İSTENEN", "Requested");
                            BiHeader(header.Cell(), "3K GELEN", "3K Received");
                            BiHeader(header.Cell(), "KARŞI.", "Fulfilled");
                            BiHeader(header.Cell(), "GERİ GÖN.", "Returned");
                            BiHeader(header.Cell(), "PRJ. VERİLDİ", "Given to Prj.");
                            BiHeader(header.Cell(), "KALAN", "Remaining");
                            BiHeader(header.Cell(), "3K DURUM", "3K Status");
                            BiHeader(header.Cell(), "SÜREÇ", "Process");
                        });

                        // Data satırları
                        int sira = 1;
                        foreach (var cs in eksikSatirlar)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                            var karsilanan = cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan;
                            var ucKDurum = cs.UcKDurumLookup?.Deger ?? "-";
                            var sandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo;

                            void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), cs.SiraNo.ToString());
                            DataCell(table.Cell(), cs.BarkodNo);
                            DataCell(table.Cell(), string.IsNullOrWhiteSpace(cs.OlcuResmiPozNo) ? "-" : cs.OlcuResmiPozNo);
                            DataCell(table.Cell(), cs.Aciklama);
                            DataCell(table.Cell(), sandikNo);
                            DataCell(table.Cell(), FormatAdet(cs.IstenenAdet));
                            DataCell(table.Cell(), FormatAdet(cs.GelenMiktar));
                            DataCell(table.Cell(), karsilanan > 0 ? FormatAdet(karsilanan) : "-");
                            DataCell(table.Cell(), cs.GeriGonderilenMiktar > 0 ? FormatAdet(cs.GeriGonderilenMiktar) : "-");
                            // Başka projeye verildi
                            var projVerildi = cs.ProjeGonderilen > 0 ? $"{FormatAdet(cs.ProjeGonderilen)} ({cs.KaynakHedefProjeNo ?? "?"})" : "-";
                            DataCell(table.Cell(), projVerildi, fontColor: cs.ProjeGonderilen > 0 ? "#1565C0" : null);
                            DataCell(table.Cell(), FormatAdet(cs.KalanMiktar), bold: true, fontColor: dangerColor);
                            DataCell(table.Cell(), ucKDurum, fontColor: cs.UcKKarsilamaTipiId == (int)_3K.Core.Enums.UcKDurum.Bekliyor ? dangerColor : warningColor);
                            DataCell(table.Cell(), cs.SurecDurumLookup?.Deger ?? "-");

                            sira++;
                        }
                    });

                    // ===== FOOTER =====
                    page.Footer().Row(footer =>
                    {
                        footer.RelativeItem().Text($"3K Ambalaj – Eksik Ürünler Raporu | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                        footer.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }
        /// <summary>
        /// Stok modülündeki tüm ürünlerin PDF raporunu oluşturur.
        /// </summary>
        public async Task<byte[]> GerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .Include(p => p.DurumLookup)
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadi: {projeId}");

            if (proje.DurumId != (int)ProjeDurum.SevkEdildi && proje.DurumId != (int)ProjeDurum.EksikSevkEdildi)
                throw new InvalidOperationException("Gerceklesen ceki listesi raporu sadece sevk edilmis projeler icin alinabilir.");

            var satirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Include(cs => cs.BirimLookup)
                .Include(cs => cs.SandikIcerikleri)
                    .ThenInclude(si => si.Sandik)
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .ToListAsync();

            if (!satirlar.Any())
                throw new KeyNotFoundException($"Projeye ait ceki satiri bulunamadi: {projeId}");

            // İlk sayfa için sandık bilgilerini çek
            var projeSandiklari = await _context.Sandiklar
                .AsNoTracking()
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var headerBg = Colors.Blue.Darken3;
            var headerText = Colors.White;
            var tableBorderColor = Colors.Grey.Lighten2;
            var altRowBg = "#F8FAFE";
            var raporTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var sevkTarihi = proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";

            int GetSandikSortKey(string? sandikNo)
            {
                if (string.IsNullOrWhiteSpace(sandikNo))
                    return int.MaxValue;

                var match = System.Text.RegularExpressions.Regex.Match(sandikNo, @"\d+");
                return match.Success && int.TryParse(match.Value, out var numericValue)
                    ? numericValue
                    : int.MaxValue;
            }

            // Sandıkları sayısal sıraya göre sırala
            projeSandiklari = projeSandiklari
                .OrderBy(s => GetSandikSortKey(s.SandikNo))
                .ThenBy(s => s.SandikNo)
                .ToList();

            List<string> GetSandikNoList(CekiSatiri satir)
            {
                if (!string.IsNullOrWhiteSpace(satir.FiiliSandikNo))
                    return new List<string> { satir.FiiliSandikNo.Trim() };

                var aktifSandiklar = satir.SandikIcerikleri
                    .Where(si => si.KonulanAdet > 0 && si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
                    .Select(si => si.Sandik.SandikNo.Trim())
                    .Distinct()
                    .OrderBy(GetSandikSortKey)
                    .ThenBy(x => x)
                    .ToList();

                if (aktifSandiklar.Any())
                    return aktifSandiklar;

                if (!string.IsNullOrWhiteSpace(satir.CekideGecenSandikNo))
                    return new List<string> { satir.CekideGecenSandikNo.Trim() };

                var sandiklar = satir.SandikIcerikleri
                    .Where(si => si.Sandik != null && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
                    .Select(si => si.Sandik.SandikNo.Trim())
                    .Distinct()
                    .OrderBy(GetSandikSortKey)
                    .ThenBy(x => x)
                    .ToList();

                if (sandiklar.Any())
                    return sandiklar;

                return new List<string>();
            }

            string GetSandikNolari(CekiSatiri satir)
            {
                return string.Join(", ", GetSandikNoList(satir));
            }

            (int Number, string Text) GetPrimarySandikSortKey(CekiSatiri satir)
            {
                var firstSandikNo = GetSandikNoList(satir).FirstOrDefault() ?? "";
                return (GetSandikSortKey(firstSandikNo), firstSandikNo);
            }

            decimal GetTrafodaSevkAdet(CekiSatiri satir)
            {
                return Math.Max(satir.TrafoSevkAdet, 0);
            }

            bool IsGridIptal(CekiSatiri satir)
            {
                return satir.GridDurumuId == (int)GridDurum.Iptal
                    || satir.GridDurumuId == (int)GridDurum.IptalEdildi;
            }

            decimal GetGerceklesenAdet(CekiSatiri satir)
            {
                if (IsGridIptal(satir))
                    return 0;

                var trafodaSevk = GetTrafodaSevkAdet(satir);
                var gerceklesen = satir.IstenenAdet - trafodaSevk - satir.KalanMiktar;
                var maxGerceklesen = Math.Max(satir.IstenenAdet - trafodaSevk, 0);

                if (gerceklesen < 0)
                    return 0;

                return gerceklesen > maxGerceklesen ? maxGerceklesen : gerceklesen;
            }

            int GetDurumSortPriority(CekiSatiri satir)
            {
                if (GetTrafodaSevkAdet(satir) > 0)
                    return 0;

                if (IsGridIptal(satir))
                    return 1;

                if (satir.KalanMiktar > 0)
                    return 2;

                return 3;
            }

            string GetRaporDurum(CekiSatiri satir)
            {
                if (GetTrafodaSevkAdet(satir) > 0)
                    return "Trafoda Sevk";

                if (IsGridIptal(satir))
                    return "İptal";

                return satir.KalanMiktar > 0 ? "Eksik Sevk" : "Tamamlandı";
            }

            string GetAciklama(CekiSatiri satir)
            {
                var gridAciklama = satir.GridAciklama?.Trim();
                var ucKAciklama = satir.UcKAciklama?.Trim();
                var hasGrid = !string.IsNullOrWhiteSpace(gridAciklama);
                var hasUcK = !string.IsNullOrWhiteSpace(ucKAciklama);

                if (hasGrid && hasUcK)
                    return $"Not 1: {gridAciklama}\nNot 2: {ucKAciklama}";
                if (hasGrid)
                    return gridAciklama!;
                if (hasUcK)
                    return ucKAciklama!;

                return "";
            }

            satirlar = satirlar
                .OrderBy(GetDurumSortPriority)
                .ThenBy(s => GetPrimarySandikSortKey(s).Number)
                .ThenBy(s => GetPrimarySandikSortKey(s).Text)
                .ThenBy(s => s.SiraNo)
                .ToList();

            var toplamSatir = satirlar.Count;
            var toplamAdet = satirlar.Sum(s => s.IstenenAdet);
            var toplamGerceklesen = satirlar.Sum(GetGerceklesenAdet);
            var toplamTrafodaSevk = satirlar.Sum(GetTrafodaSevkAdet);
            var toplamKalan = satirlar.Sum(s => s.KalanMiktar);

            var document = Document.Create(container =>
            {
                // ===== SAYFA 1: SANDIK ÖZET TABLOSU =====
                if (projeSandiklari.Any())
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(18);
                        page.DefaultTextStyle(x => x.FontSize(8));

                        // Header
                        page.Header().Column(headerCol =>
                        {
                            headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("3K").Bold().FontSize(24).FontColor(headerText);
                                    col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                                row.RelativeItem(2).AlignCenter().Column(col =>
                                {
                                    col.Item().Text("Koli / Sandık Bilgileri").Bold().FontSize(16).FontColor(headerText);
                                    col.Item().Text("3K Sevkiyat Yönetim Sistemi – Gerçekleşen Çeki Listesi").FontSize(7).FontColor(Colors.Grey.Lighten3);
                                });
                                row.RelativeItem().AlignRight().Column(col =>
                                {
                                    col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                    col.Item().Text($"Sevk Tarihi: {sevkTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                });
                            });

                            headerCol.Item().PaddingTop(10).Border(1).BorderColor(tableBorderColor).Padding(8).Row(row =>
                            {
                                row.RelativeItem().Text(t =>
                                {
                                    t.Span("Proje No: ").FontSize(10);
                                    t.Span(proje.ProjeNo).Bold().FontSize(11).FontColor(headerBg);
                                });
                                row.RelativeItem().Text(t =>
                                {
                                    t.Span("Müşteri: ").FontSize(10);
                                    t.Span(proje.Musteri ?? "-").Bold().FontSize(11).FontColor(headerBg);
                                });
                                row.RelativeItem().AlignRight().Text(t =>
                                {
                                    t.Span("Toplam Sandık: ").FontSize(10);
                                    t.Span($"{projeSandiklari.Count} adet").Bold().FontSize(11).FontColor(headerBg);
                                });
                            });

                            headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                        });

                        // Content - Sandık Tablosu
                        page.Content().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);    // #
                                columns.ConstantColumn(65);    // Koli No
                                columns.RelativeColumn(2.5f);  // Sandık Adı
                                columns.ConstantColumn(70);    // Net (kg)
                                columns.ConstantColumn(70);    // Brüt (kg)
                                columns.ConstantColumn(75);    // Boy (mm)
                                columns.ConstantColumn(75);    // En (mm)
                                columns.ConstantColumn(85);    // Yükseklik (mm)
                            });

                            table.Header(header =>
                            {
                                void BiHeader(IContainer c, string tr, string en = "")
                                {
                                    c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(5).Column(col =>
                                    {
                                        col.Item().Text(tr).Bold().FontSize(8).FontColor(headerText);
                                        if (!string.IsNullOrEmpty(en))
                                            col.Item().Text(en).FontSize(5.5f).FontColor(Colors.Grey.Lighten3).Italic();
                                    });
                                }

                                BiHeader(header.Cell(), "#");
                                BiHeader(header.Cell(), "KOLİ NO", "Case No");
                                BiHeader(header.Cell(), "SANDIK ADI", "Crate Name");
                                BiHeader(header.Cell(), "NET (kg)");
                                BiHeader(header.Cell(), "BRÜT (kg)", "Gross (kg)");
                                BiHeader(header.Cell(), "BOY (mm)", "Length (mm)");
                                BiHeader(header.Cell(), "EN (mm)", "Width (mm)");
                                BiHeader(header.Cell(), "YÜKSEKLİK (mm)", "Height (mm)");
                            });

                            int sandikSira = 1;
                            decimal toplamNet = 0;
                            decimal toplamBrut = 0;

                            foreach (var sandik in projeSandiklari)
                            {
                                var bg = sandikSira % 2 == 0 ? altRowBg : "#FFFFFF";
                                var netKg = sandik.NetKg ?? 0;
                                var brutKg = sandik.GrossKg ?? 0;
                                toplamNet += netKg;
                                toplamBrut += brutKg;

                                void SandikDataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                                {
                                    var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(5);
                                    if (bold)
                                        cell.Text(text).FontSize(8).FontColor(fontColor ?? Colors.Black).Bold();
                                    else
                                        cell.Text(text).FontSize(8).FontColor(fontColor ?? Colors.Black);
                                }

                                SandikDataCell(table.Cell(), sandikSira.ToString(), bold: true);
                                SandikDataCell(table.Cell(), sandik.SandikNo, bold: true, fontColor: headerBg);
                                SandikDataCell(table.Cell(), sandik.Ad ?? "-");
                                SandikDataCell(table.Cell(), FormatAdet(netKg), bold: true);
                                SandikDataCell(table.Cell(), FormatAdet(brutKg), bold: true);
                                SandikDataCell(table.Cell(), FormatAdet(sandik.Boy ?? 0));
                                SandikDataCell(table.Cell(), FormatAdet(sandik.En ?? 0));
                                SandikDataCell(table.Cell(), FormatAdet(sandik.Yukseklik ?? 0));

                                sandikSira++;
                            }

                            // Toplam satırı
                            void ToplamCell(IContainer c, string text, bool bold = true)
                            {
                                var cell = c.Background("#E3F2FD").Border(0.5f).BorderColor(tableBorderColor).Padding(5);
                                cell.Text(text).FontSize(8).FontColor(Colors.Black).Bold();
                            }

                            ToplamCell(table.Cell(), "");
                            ToplamCell(table.Cell(), "");
                            ToplamCell(table.Cell(), "TOPLAM");
                            ToplamCell(table.Cell(), FormatAdet(toplamNet));
                            ToplamCell(table.Cell(), FormatAdet(toplamBrut));
                            ToplamCell(table.Cell(), "");
                            ToplamCell(table.Cell(), "");
                            ToplamCell(table.Cell(), "");
                        });

                        // Footer
                        page.Footer().Row(footer =>
                        {
                            footer.RelativeItem().Text($"3K Ambalaj - Koli/Sandık Bilgileri | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                            footer.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                                x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                                x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                                x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                }

                // ===== SAYFA 2+: GERÇEKLEŞEN ÇEKİ LİSTESİ DETAY =====
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("3K").Bold().FontSize(24).FontColor(headerText);
                                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                            row.RelativeItem(2).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Gerçekleşen Çeki Listesi Raporu").Bold().FontSize(16).FontColor(headerText);
                                col.Item().Text("3K Sevkiyat Yönetim Sistemi").FontSize(9).FontColor(Colors.Grey.Lighten3);
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                col.Item().Text($"Sevk Tarihi: {sevkTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
                        });

                        headerCol.Item().PaddingTop(10).Border(1).BorderColor(tableBorderColor).Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Proje No: ").FontSize(10);
                                t.Span(proje.ProjeNo).Bold().FontSize(11).FontColor(headerBg);
                            });
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Müşteri: ").FontSize(10);
                                t.Span(proje.Musteri ?? "-").Bold().FontSize(11).FontColor(headerBg);
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Toplam: ").FontSize(10);
                                t.Span($"{toplamSatir} satır / {FormatAdet(toplamAdet)} adet / {FormatAdet(toplamGerceklesen)} gerçekleşen / {FormatAdet(toplamTrafodaSevk)} trafoda / {FormatAdet(toplamKalan)} kalan").Bold().FontSize(9);
                            });
                        });
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);
                            columns.ConstantColumn(46);
                            columns.ConstantColumn(66);
                            columns.ConstantColumn(42);
                            columns.RelativeColumn(2.05f);
                            columns.ConstantColumn(42);
                            columns.ConstantColumn(36);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(42);
                            columns.ConstantColumn(82);
                            columns.RelativeColumn(1.55f);
                        });

                        table.Header(header =>
                        {
                            void BiHeader(IContainer c, string tr, string en = "")
                            {
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(4).Column(col =>
                                {
                                    col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                    if (!string.IsNullOrEmpty(en))
                                        col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                            }

                            BiHeader(header.Cell(), "#");
                            BiHeader(header.Cell(), "SANDIK", "Crate");
                            BiHeader(header.Cell(), "BARKOD", "Barcode");
                            BiHeader(header.Cell(), "POZ NO", "Pos. No");
                            BiHeader(header.Cell(), "İSİM", "Name");
                            BiHeader(header.Cell(), "MİKTAR", "Quantity");
                            BiHeader(header.Cell(), "BİRİM", "Unit");
                            BiHeader(header.Cell(), "GERÇEKLEŞEN", "Actual");
                            BiHeader(header.Cell(), "TRAFODA", "At Transformer");
                            BiHeader(header.Cell(), "KALAN", "Remaining");
                            BiHeader(header.Cell(), "DURUM", "Status");
                            BiHeader(header.Cell(), "AÇIKLAMA", "Remarks");
                        });

                        var sira = 1;
                        foreach (var satir in satirlar)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                            var gerceklesenAdet = GetGerceklesenAdet(satir);
                            var trafodaSevkAdet = GetTrafodaSevkAdet(satir);
                            var kalanMiktar = satir.KalanMiktar;
                            var durumMetni = GetRaporDurum(satir);
                            var aciklama = GetAciklama(satir);

                            void DataCell(IContainer c, string text, bool bold = false, string? color = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(4);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(color ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(color ?? Colors.Black);
                            }

                            void DurumCell(IContainer c, string text)
                            {
                                var cell = c.Background(bg)
                                    .Border(0.5f)
                                    .BorderColor(tableBorderColor)
                                    .Padding(4);

                                cell.Text(text)
                                    .FontSize(7)
                                    .Bold()
                                    .FontColor(Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString(), bold: true);
                            DataCell(table.Cell(), GetSandikNolari(satir), bold: true, color: headerBg);
                            DataCell(table.Cell(), satir.BarkodNo);
                            DataCell(table.Cell(), string.IsNullOrWhiteSpace(satir.OlcuResmiPozNo) ? "-" : satir.OlcuResmiPozNo);
                            DataCell(table.Cell(), satir.Aciklama);
                            DataCell(table.Cell(), FormatAdet(satir.IstenenAdet), bold: true);
                            DataCell(table.Cell(), satir.BirimLookup?.Deger ?? "Adet");
                            DataCell(table.Cell(), FormatAdet(gerceklesenAdet), bold: true);
                            DataCell(table.Cell(), trafodaSevkAdet > 0 ? FormatAdet(trafodaSevkAdet) : "-", bold: trafodaSevkAdet > 0);
                            DataCell(table.Cell(), FormatAdet(kalanMiktar), bold: true);
                            DurumCell(table.Cell(), durumMetni);
                            DataCell(table.Cell(), aciklama);

                            sira++;
                        }
                    });

                    page.Footer().Row(footer =>
                    {
                        footer.RelativeItem().Text($"3K Ambalaj - Gerçekleşen Çeki Listesi | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
                        footer.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }

        public async Task<byte[]> EksikUrunlerRaporuExcelOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .Include(p => p.ProjeTipiLookup)
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadı: {projeId}");

            var satirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Include(cs => cs.BirimLookup)
                .Include(cs => cs.GridDurumLookup)
                .Include(cs => cs.UcKDurumLookup)
                .Include(cs => cs.DurumLookup)
                .Include(cs => cs.GeriGonderilmeSebebiLookup)
                .Include(cs => cs.SurecDurumLookup)
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .ToListAsync();

            var eksikSatirlar = satirlar
                .Where(cs => cs.KalanMiktar > 0)
                .OrderBy(cs => GetRaporSandikSortKey(cs.FiiliSandikNo ?? cs.CekideGecenSandikNo))
                .ThenBy(cs => cs.FiiliSandikNo ?? cs.CekideGecenSandikNo)
                .ThenBy(cs => cs.SiraNo)
                .ToList();

            var toplamEksikUrun = eksikSatirlar.Count;
            var toplamKalanAdet = eksikSatirlar.Sum(s => s.KalanMiktar);
            var toplamIstenenAdet = eksikSatirlar.Sum(s => s.IstenenAdet);
            var toplamGelenAdet = eksikSatirlar.Sum(s => s.GelenMiktar + s.StokKarsilanan + s.ProjeKarsilanan + s.TedarikciKarsilanan);
            var karsilamaOrani = toplamIstenenAdet > 0 ? toplamGelenAdet * 100 / toplamIstenenAdet : 0;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Eksik Raporu");
            const int lastColumn = 14;
            const int headerRow = 6;

            worksheet.Cell(1, 1).Value = "Eksik Ürünler Raporu";
            StyleExcelTitle(worksheet, lastColumn);

            worksheet.Cell(2, 1).Value = "Proje No";
            worksheet.Cell(2, 2).Value = proje.ProjeNo;
            worksheet.Cell(2, 4).Value = "Müşteri";
            worksheet.Cell(2, 5).Value = proje.Musteri ?? "-";
            worksheet.Cell(2, 8).Value = "Rapor Tarihi";
            worksheet.Cell(2, 9).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

            worksheet.Cell(3, 1).Value = "Eksik Ürün";
            SetDecimalCell(worksheet.Cell(3, 2), toplamEksikUrun);
            worksheet.Cell(3, 4).Value = "Toplam Kalan";
            SetDecimalCell(worksheet.Cell(3, 5), toplamKalanAdet);
            worksheet.Cell(3, 8).Value = "Karşılama Oranı";
            worksheet.Cell(3, 9).Value = $"%{FormatAdet(karsilamaOrani)}";
            StyleExcelInfoRows(worksheet, lastColumn);

            var headers = new[]
            {
                "#", "Sıra", "Barkod No", "Poz No", "Ürün Açıklaması", "Sandık",
                "İstenen", "3K Gelen", "Karşılanan", "Geri Gön.", "Prj. Verildi",
                "Kalan", "3K Durum", "Süreç"
            };

            for (var i = 0; i < headers.Length; i++)
                worksheet.Cell(headerRow, i + 1).Value = headers[i];

            StyleExcelHeader(worksheet, headerRow, lastColumn);

            var row = headerRow + 1;
            var sira = 1;
            foreach (var cs in eksikSatirlar)
            {
                var karsilanan = cs.StokKarsilanan + cs.ProjeKarsilanan + cs.TedarikciKarsilanan;
                var sandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo;
                var projVerildi = cs.ProjeGonderilen > 0 ? $"{FormatAdet(cs.ProjeGonderilen)} ({cs.KaynakHedefProjeNo ?? "?"})" : "-";

                worksheet.Cell(row, 1).SetValue(sira);
                worksheet.Cell(row, 2).SetValue(cs.SiraNo);
                worksheet.Cell(row, 3).Value = cs.BarkodNo;
                worksheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(cs.OlcuResmiPozNo) ? "-" : cs.OlcuResmiPozNo;
                worksheet.Cell(row, 5).Value = cs.Aciklama;
                worksheet.Cell(row, 6).Value = sandikNo;
                SetDecimalCell(worksheet.Cell(row, 7), cs.IstenenAdet);
                SetDecimalCell(worksheet.Cell(row, 8), cs.GelenMiktar);
                if (karsilanan > 0) SetDecimalCell(worksheet.Cell(row, 9), karsilanan); else worksheet.Cell(row, 9).Value = "-";
                if (cs.GeriGonderilenMiktar > 0) SetDecimalCell(worksheet.Cell(row, 10), cs.GeriGonderilenMiktar); else worksheet.Cell(row, 10).Value = "-";
                worksheet.Cell(row, 11).Value = projVerildi;
                SetDecimalCell(worksheet.Cell(row, 12), cs.KalanMiktar);
                worksheet.Cell(row, 13).Value = cs.UcKDurumLookup?.Deger ?? "-";
                worksheet.Cell(row, 14).Value = cs.SurecDurumLookup?.Deger ?? "-";

                if (sira % 2 == 0)
                    worksheet.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

                row++;
                sira++;
            }

            worksheet.Column(5).Style.Alignment.WrapText = true;
            FinalizeExcelWorksheet(worksheet, headerRow, Math.Max(row - 1, headerRow), lastColumn);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> GerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId)
        {
            var proje = await _context.Projeler
                .Include(p => p.DurumLookup)
                .FirstOrDefaultAsync(p => p.Id == projeId);

            if (proje == null)
                throw new KeyNotFoundException($"Proje bulunamadi: {projeId}");

            if (proje.DurumId != (int)ProjeDurum.SevkEdildi && proje.DurumId != (int)ProjeDurum.EksikSevkEdildi)
                throw new InvalidOperationException("Gerceklesen ceki listesi raporu sadece sevk edilmis projeler icin alinabilir.");

            var satirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Include(cs => cs.BirimLookup)
                .Include(cs => cs.SandikIcerikleri)
                    .ThenInclude(si => si.Sandik)
                .Where(cs => cs.Ceki.ProjeId == projeId)
                .ToListAsync();

            if (!satirlar.Any())
                throw new KeyNotFoundException($"Projeye ait ceki satiri bulunamadi: {projeId}");

            var projeSandiklari = await _context.Sandiklar
                .AsNoTracking()
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();

            projeSandiklari = projeSandiklari
                .OrderBy(s => GetRaporSandikSortKey(s.SandikNo))
                .ThenBy(s => s.SandikNo)
                .ToList();

            satirlar = satirlar
                .OrderBy(GetRaporDurumSortPriority)
                .ThenBy(s => GetRaporPrimarySandikSortKey(s).Number)
                .ThenBy(s => GetRaporPrimarySandikSortKey(s).Text)
                .ThenBy(s => s.SiraNo)
                .ToList();

            var toplamSatir = satirlar.Count;
            var toplamAdet = satirlar.Sum(s => s.IstenenAdet);
            var toplamGerceklesen = satirlar.Sum(GetRaporGerceklesenAdet);
            var toplamTrafodaSevk = satirlar.Sum(GetRaporTrafodaSevkAdet);
            var toplamKalan = satirlar.Sum(s => s.KalanMiktar);
            var sevkTarihi = proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";

            using var workbook = new XLWorkbook();

            if (projeSandiklari.Any())
            {
                var sandikSheet = workbook.Worksheets.Add("Sandık Bilgileri");
                const int sandikLastColumn = 8;
                const int sandikHeaderRow = 6;

                sandikSheet.Cell(1, 1).Value = "Koli / Sandık Bilgileri";
                StyleExcelTitle(sandikSheet, sandikLastColumn);
                sandikSheet.Cell(2, 1).Value = "Proje No";
                sandikSheet.Cell(2, 2).Value = proje.ProjeNo;
                sandikSheet.Cell(2, 4).Value = "Müşteri";
                sandikSheet.Cell(2, 5).Value = proje.Musteri ?? "-";
                sandikSheet.Cell(2, 7).Value = "Rapor Tarihi";
                sandikSheet.Cell(2, 8).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                sandikSheet.Cell(3, 1).Value = "Toplam Sandık";
                SetDecimalCell(sandikSheet.Cell(3, 2), projeSandiklari.Count);
                sandikSheet.Cell(3, 4).Value = "Sevk Tarihi";
                sandikSheet.Cell(3, 5).Value = sevkTarihi;
                StyleExcelInfoRows(sandikSheet, sandikLastColumn);

                var sandikHeaders = new[] { "#", "Koli No", "Sandık Adı", "Net (kg)", "Brüt (kg)", "Boy (mm)", "En (mm)", "Yükseklik (mm)" };
                for (var i = 0; i < sandikHeaders.Length; i++)
                    sandikSheet.Cell(sandikHeaderRow, i + 1).Value = sandikHeaders[i];

                StyleExcelHeader(sandikSheet, sandikHeaderRow, sandikLastColumn);

                var row = sandikHeaderRow + 1;
                var sira = 1;
                foreach (var sandik in projeSandiklari)
                {
                    sandikSheet.Cell(row, 1).SetValue(sira);
                    sandikSheet.Cell(row, 2).Value = sandik.SandikNo;
                    sandikSheet.Cell(row, 3).Value = sandik.Ad ?? "-";
                    SetDecimalCell(sandikSheet.Cell(row, 4), sandik.NetKg ?? 0);
                    SetDecimalCell(sandikSheet.Cell(row, 5), sandik.GrossKg ?? 0);
                    SetDecimalCell(sandikSheet.Cell(row, 6), sandik.Boy ?? 0);
                    SetDecimalCell(sandikSheet.Cell(row, 7), sandik.En ?? 0);
                    SetDecimalCell(sandikSheet.Cell(row, 8), sandik.Yukseklik ?? 0);

                    if (sira % 2 == 0)
                        sandikSheet.Range(row, 1, row, sandikLastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

                    row++;
                    sira++;
                }

                FinalizeExcelWorksheet(sandikSheet, sandikHeaderRow, row - 1, sandikLastColumn);
            }

            var worksheet = workbook.Worksheets.Add("Gerçekleşen Çeki");
            const int lastColumn = 12;
            const int headerRow = 6;

            worksheet.Cell(1, 1).Value = "Gerçekleşen Çeki Listesi Raporu";
            StyleExcelTitle(worksheet, lastColumn);
            worksheet.Cell(2, 1).Value = "Proje No";
            worksheet.Cell(2, 2).Value = proje.ProjeNo;
            worksheet.Cell(2, 4).Value = "Müşteri";
            worksheet.Cell(2, 5).Value = proje.Musteri ?? "-";
            worksheet.Cell(2, 8).Value = "Rapor Tarihi";
            worksheet.Cell(2, 9).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            worksheet.Cell(3, 1).Value = "Sevk Tarihi";
            worksheet.Cell(3, 2).Value = sevkTarihi;
            worksheet.Cell(3, 4).Value = "Toplam";
            worksheet.Cell(3, 5).Value = $"{toplamSatir} satır / {FormatAdet(toplamAdet)} adet / {FormatAdet(toplamGerceklesen)} gerçekleşen / {FormatAdet(toplamTrafodaSevk)} trafoda / {FormatAdet(toplamKalan)} kalan";
            StyleExcelInfoRows(worksheet, lastColumn);

            var headers = new[]
            {
                "#", "Sandık", "Barkod", "Poz No", "İsim", "Miktar", "Birim",
                "Gerçekleşen", "Trafoda", "Kalan", "Durum", "Açıklama"
            };

            for (var i = 0; i < headers.Length; i++)
                worksheet.Cell(headerRow, i + 1).Value = headers[i];

            StyleExcelHeader(worksheet, headerRow, lastColumn);

            var dataRow = headerRow + 1;
            var dataSira = 1;
            foreach (var satir in satirlar)
            {
                var gerceklesenAdet = GetRaporGerceklesenAdet(satir);
                var trafodaSevkAdet = GetRaporTrafodaSevkAdet(satir);

                worksheet.Cell(dataRow, 1).SetValue(dataSira);
                worksheet.Cell(dataRow, 2).Value = GetRaporSandikNolari(satir);
                worksheet.Cell(dataRow, 3).Value = satir.BarkodNo;
                worksheet.Cell(dataRow, 4).Value = string.IsNullOrWhiteSpace(satir.OlcuResmiPozNo) ? "-" : satir.OlcuResmiPozNo;
                worksheet.Cell(dataRow, 5).Value = satir.Aciklama;
                SetDecimalCell(worksheet.Cell(dataRow, 6), satir.IstenenAdet);
                worksheet.Cell(dataRow, 7).Value = satir.BirimLookup?.Deger ?? "Adet";
                SetDecimalCell(worksheet.Cell(dataRow, 8), gerceklesenAdet);
                if (trafodaSevkAdet > 0) SetDecimalCell(worksheet.Cell(dataRow, 9), trafodaSevkAdet); else worksheet.Cell(dataRow, 9).Value = "-";
                SetDecimalCell(worksheet.Cell(dataRow, 10), satir.KalanMiktar);
                worksheet.Cell(dataRow, 11).Value = GetGerceklesenRaporDurum(satir);
                worksheet.Cell(dataRow, 12).Value = GetRaporAciklama(satir);

                if (dataSira % 2 == 0)
                    worksheet.Range(dataRow, 1, dataRow, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFE");

                dataRow++;
                dataSira++;
            }

            worksheet.Column(5).Style.Alignment.WrapText = true;
            worksheet.Column(12).Style.Alignment.WrapText = true;
            FinalizeExcelWorksheet(worksheet, headerRow, dataRow - 1, lastColumn);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> DepoSandikRaporuPdfOlusturAsync(int? projeTipiId = null)
        {
            if (projeTipiId.HasValue && !Enum.IsDefined(typeof(ProjeTipi), projeTipiId.Value))
                throw new ArgumentException("Gecersiz proje tipi filtresi.");

            IQueryable<Sandik> sandikQuery = _context.Sandiklar
                .AsNoTracking()
                .Include(s => s.Proje)
                    .ThenInclude(p => p.ProjeTipiLookup)
                .Include(s => s.DurumLookup)
                .Include(s => s.TipLookup)
                .Include(s => s.DepoLokasyonLookup)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                .Where(s => s.DurumId != (int)SandikDurum.Sevkedildi);

            if (projeTipiId.HasValue)
                sandikQuery = sandikQuery.Where(s => s.Proje.ProjeTipiId == projeTipiId.Value);

            var sandiklar = await sandikQuery.ToListAsync();
            var depoLokasyonlari = await _context.LookupDepoLokasyonlari
                .AsNoTracking()
                .OrderBy(l => l.Anahtar)
                .ToListAsync();
            var lokasyonAdlari = depoLokasyonlari.ToDictionary(l => l.Id, l => l.Deger);

            int GetSandikSortKey(string? sandikNo)
            {
                if (string.IsNullOrWhiteSpace(sandikNo)) return int.MaxValue;
                var digits = new string(sandikNo.TakeWhile(char.IsDigit).ToArray());
                return int.TryParse(digits, out var number) ? number : int.MaxValue;
            }

            bool BelirsizLokasyonMu(LookupDepoLokasyon lokasyon)
            {
                return lokasyon.Id == (int)DepoLokasyon.Belirsiz
                    || lokasyon.Deger.Equals("Belirsiz", StringComparison.OrdinalIgnoreCase);
            }

            int EtkinDepoLokasyonId(Sandik sandik)
            {
                return SandiktaGridKapandiUrunVar(sandik)
                    ? (int)DepoLokasyon.Grid
                    : sandik.DepoLokasyonId;
            }

            string EtkinDepoLokasyonMetni(Sandik sandik)
            {
                return lokasyonAdlari.GetValueOrDefault(EtkinDepoLokasyonId(sandik), "Belirsiz");
            }

            string LokasyonRengi(LookupDepoLokasyon lokasyon)
            {
                return lokasyon.Deger.Trim().ToUpperInvariant() switch
                {
                    "3K" => "#0EA5E9",
                    "SEYMEN" => "#078A55",
                    "GRID" => "#F59E0B",
                    "BELİRSİZ" or "BELIRSIZ" => "#64748B",
                    _ => "#2563EB"
                };
            }

            bool SandiktaGridKapandiUrunVar(Sandik sandik)
            {
                return sandik.SandikIcerikleri.Any(i => i.CekiSatiri?.GridDurumuId == (int)GridDurum.GridKapandi);
            }

            var siraliSandiklar = sandiklar
                .Where(s => EtkinDepoLokasyonId(s) != (int)DepoLokasyon.Belirsiz)
                .OrderBy(EtkinDepoLokasyonMetni)
                .ThenBy(s => s.Proje.ProjeNo)
                .ThenBy(s => GetSandikSortKey(s.SandikNo))
                .ThenBy(s => s.SandikNo)
                .ToList();

            QuestPDF.Settings.License = LicenseType.Community;

            var headerBg = Colors.Blue.Darken3;
            var headerText = Colors.White;
            var tableBorderColor = Colors.Grey.Lighten2;
            var altRowBg = "#F8FAFE";
            var raporTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var raporKapsami = projeTipiId switch
            {
                (int)ProjeTipi.Normal => "Normal Projeler",
                (int)ProjeTipi.Saha => "Saha Projeleri",
                (int)ProjeTipi.Yedek => "Yedek Projeleri",
                _ => "Tum Projeler"
            };

            var projeBazliDagilim = siraliSandiklar
                .GroupBy(s => new
                {
                    s.ProjeId,
                    s.Proje.ProjeNo,
                    s.Proje.ProjeTipiId,
                    ProjeTipi = s.Proje.ProjeTipiLookup != null ? s.Proje.ProjeTipiLookup.Deger : null
                })
                .Select(g => new
                {
                    g.Key.ProjeId,
                    g.Key.ProjeNo,
                    ProjeTipiId = g.Key.ProjeTipiId,
                    ProjeTipi = g.Key.ProjeTipi ?? (g.Key.ProjeTipiId switch
                    {
                        (int)ProjeTipi.Normal => "Normal",
                        (int)ProjeTipi.Saha => "Saha",
                        (int)ProjeTipi.Yedek => "Yedek",
                        _ => "-"
                    }),
                    ToplamSandik = g.Count(),
                    LokasyonSayilari = depoLokasyonlari.ToDictionary(
                        lokasyon => lokasyon.Id,
                        lokasyon => g.Count(s => EtkinDepoLokasyonId(s) == lokasyon.Id))
                })
                .OrderBy(p => p.ProjeNo)
                .ToList();

            var raporLokasyonlari = depoLokasyonlari
                .Where(l => !BelirsizLokasyonMu(l))
                .OrderBy(l => l.Anahtar)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                            row.RelativeItem(2).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Depo Sandık Raporu").Bold().FontSize(16).FontColor(headerText);
                                col.Item().Text("3K Sevkiyat Yönetim Sistemi - Depo Yönetimi Çıktısı").FontSize(7).FontColor(Colors.Grey.Lighten3);
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                col.Item().Text($"Kapsam: {raporKapsami}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
                        });

                        /*
                            row.RelativeItem().Element(c => GenelDurumKarti(c, "SAHA PROJELERİ", sahaDurum, Colors.Green.Darken1));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(c => GenelDurumKarti(c, "YEDEK PROJELERİ", yedekDurum, Colors.Orange.Darken1));
                        });

                        headerCol.Item().PaddingTop(5).Text("Not: Sevk Edildi durumundaki sandıklar depo envanterine dahil edilmez. Proje kilidi açıldığında eski durumuna dönen sandıklar tekrar rapora girer.")
                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                        */
                        headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                    });

                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.0f);
                            columns.RelativeColumn(0.9f);
                            foreach (var _ in raporLokasyonlari)
                                columns.RelativeColumn(0.9f);
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(IContainer c, string text) =>
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Text(text).Bold().FontSize(6.5f).FontColor(headerText);

                            HeaderCell(header.Cell(), "#");
                            HeaderCell(header.Cell(), "PROJE NO");
                            HeaderCell(header.Cell(), "PROJE TİPİ");
                            HeaderCell(header.Cell(), "TOPLAM SANDIK");
                            foreach (var lokasyon in raporLokasyonlari)
                                HeaderCell(header.Cell(), lokasyon.Deger.ToUpperInvariant());
                        });

                        int sira = 1;
                        foreach (var proje in projeBazliDagilim)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";

                            void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(6.5f).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(6.5f).FontColor(fontColor ?? Colors.Black);
                            }

                            string CountText(int value) => value > 0 ? value.ToString() : "-";

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), proje.ProjeNo, bold: true);
                            DataCell(table.Cell(), proje.ProjeTipi, bold: true, fontColor: proje.ProjeTipiId switch
                            {
                                (int)ProjeTipi.Saha => "#078A55",
                                (int)ProjeTipi.Yedek => "#B45309",
                                _ => "#0EA5E9"
                            });
                            DataCell(table.Cell(), proje.ToplamSandik.ToString(), bold: true);
                            foreach (var lokasyon in raporLokasyonlari)
                            {
                                var count = proje.LokasyonSayilari.GetValueOrDefault(lokasyon.Id);
                                DataCell(table.Cell(), CountText(count), bold: count > 0, fontColor: LokasyonRengi(lokasyon));
                            }

                            sira++;
                        }

                        // ===== TOPLAM SATIRI =====
                        var genelToplamSandik = projeBazliDagilim.Sum(p => p.ToplamSandik);

                        void ToplamCell(IContainer c, string text, string? fontColor = null)
                        {
                            var cell = c.Background("#E3F2FD").Border(0.5f).BorderColor(tableBorderColor).Padding(3);
                            cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                        }

                        ToplamCell(table.Cell(), "");
                        ToplamCell(table.Cell(), "TOPLAM");
                        ToplamCell(table.Cell(), $"{projeBazliDagilim.Count} proje");
                        ToplamCell(table.Cell(), genelToplamSandik.ToString());
                        foreach (var lokasyon in raporLokasyonlari)
                        {
                            var toplam = projeBazliDagilim.Sum(p => p.LokasyonSayilari.GetValueOrDefault(lokasyon.Id));
                            ToplamCell(table.Cell(), toplam > 0 ? toplam.ToString() : "-", LokasyonRengi(lokasyon));
                        }
                    });

                    page.Footer().Row(footer =>
                    {
                        footer.RelativeItem().Text("3K Ambalaj - Depo Sandık Raporu").FontSize(7).FontColor(Colors.Grey.Medium);
                        footer.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }

        public async Task<byte[]> StokRaporuPdfOlusturAsync()
        {
            var stoklar = await _context.StokKayitlari
                .OrderBy(s => s.MalzemeKodu)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var headerBg = Colors.Blue.Darken3;
            var headerText = Colors.White;
            var tableBorderColor = Colors.Grey.Lighten2;
            var altRowBg = "#F8FAFE";
            var raporTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var toplamKayit = stoklar.Count;
            var toplamMiktar = stoklar.Sum(s => s.Miktar);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    // ===== HEADER =====
                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Background(headerBg).Padding(12).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("3K").Bold().FontSize(22).FontColor(headerText);
                                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
                            });
                            row.RelativeItem(2).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Stok Raporu").Bold().FontSize(16).FontColor(headerText);
                                col.Item().Text("3K Sevkiyat Yönetim Sistemi – Stok Yönetimi Çıktısı").FontSize(7).FontColor(Colors.Grey.Lighten3);
                            });
                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
                        });

                        // Özet kartları
                        headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                            {
                                col.Item().Text("STOK ÖZETİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                                col.Item().PaddingTop(3).Text($"{toplamKayit} ürün").Bold().FontSize(13);
                                col.Item().Text($"Toplam Miktar: {toplamMiktar}").FontSize(8);
                            });

                            row.ConstantItem(10);

                            var aktifCount = stoklar.Count(s => s.DurumId == (int)_3K.Core.Enums.StokDurum.Aktif);
                            var tukendiCount = stoklar.Count(s => s.DurumId == (int)_3K.Core.Enums.StokDurum.Tukendi);
                            var rezerveCount = stoklar.Count(s => s.DurumId == (int)_3K.Core.Enums.StokDurum.Rezerve);

                            row.RelativeItem().Border(1).BorderColor("#388E3C").Padding(8).Column(col =>
                            {
                                col.Item().Text("DURUM DAĞILIMI").Bold().FontSize(7).FontColor("#388E3C");
                                col.Item().PaddingTop(3).Text($"Aktif: {aktifCount}").FontSize(9).Bold();
                                col.Item().Text($"Tükendi: {tukendiCount}").FontSize(8);
                                col.Item().Text($"Rezerve: {rezerveCount}").FontSize(8);
                            });
                        });

                        headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                    });

                    // ===== CONTENT - TABLE =====
                    page.Content().PaddingVertical(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);   // #
                            columns.RelativeColumn(1.5f); // Malzeme Kodu
                            columns.RelativeColumn(3);    // Malzeme Adı
                            columns.ConstantColumn(50);   // Miktar
                            columns.RelativeColumn(1);    // Birim
                            columns.RelativeColumn(1.5f); // Lokasyon
                            columns.RelativeColumn(1.5f); // Kaynak Proje
                            columns.RelativeColumn(2);    // Giriş Nedeni
                            columns.RelativeColumn(1);    // Durum
                        });

                        table.Header(header =>
                        {
                            void HeaderCell(IContainer c, string text) =>
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Text(text).Bold().FontSize(7).FontColor(headerText);

                            HeaderCell(header.Cell(), "#");
                            HeaderCell(header.Cell(), "MALZEME KODU");
                            HeaderCell(header.Cell(), "MALZEME ADI");
                            HeaderCell(header.Cell(), "MİKTAR");
                            HeaderCell(header.Cell(), "BİRİM");
                            HeaderCell(header.Cell(), "LOKASYON");
                            HeaderCell(header.Cell(), "KAYNAK PROJE");
                            HeaderCell(header.Cell(), "GİRİŞ NEDENİ");
                            HeaderCell(header.Cell(), "DURUM");
                        });

                        int sira = 1;
                        foreach (var s in stoklar)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                            var durumStr = ((Core.Enums.StokDurum)s.DurumId).ToString();

                            void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), s.MalzemeKodu ?? "-");
                            DataCell(table.Cell(), s.MalzemeAdi ?? "-");
                            DataCell(table.Cell(), s.Miktar.ToString(), bold: true);
                            DataCell(table.Cell(), ((_3K.Core.Enums.Birim)s.BirimId).ToString());
                            DataCell(table.Cell(), s.Lokasyon ?? "-");
                            DataCell(table.Cell(), s.KaynakProje ?? "-");
                            DataCell(table.Cell(), s.StokGirisNedeni ?? "-");
                            DataCell(table.Cell(), durumStr, fontColor: s.DurumId == (int)Core.Enums.StokDurum.Aktif ? "#388E3C" : "#D32F2F");

                            sira++;
                        }
                    });

                    // ===== FOOTER =====
                    page.Footer().Row(footer =>
                    {
                        footer.RelativeItem().Text("3K Ambalaj – Stok Raporu").FontSize(7).FontColor(Colors.Grey.Medium);
                        footer.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            using var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            return pdfStream.ToArray();
        }
    }
}
