using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Entities;
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
                                    .Text($"Sandık: {sandik.SandikNo} | Durum: {sandik.Durum}")
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

                                    // Başlık satırı
                                    table.Header(header =>
                                    {
                                        header.Cell().Border(1).Padding(2).Text("#").Bold();
                                        header.Cell().Border(1).Padding(2).Text("POZ NO").Bold();
                                        header.Cell().Border(1).Padding(2).Text("BARKOD NO").Bold();
                                        header.Cell().Border(1).Padding(2).Text("AÇIKLAMA").Bold();
                                        header.Cell().Border(1).Padding(2).Text("İSTENEN").Bold();
                                        header.Cell().Border(1).Padding(2).Text("KONULAN").Bold();
                                        header.Cell().Border(1).Padding(2).Text("EKSİK").Bold();
                                        header.Cell().Border(1).Padding(2).Text("BİRİM").Bold();
                                        header.Cell().Border(1).Padding(2).Text("PKT").Bold();
                                        header.Cell().Border(1).Padding(2).Text("KNT").Bold();
                                        header.Cell().Border(1).Padding(2).Text("REMARKS").Bold();
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
                                        table.Cell().Border(1).Padding(2).Text(cs.Birim);
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
    }
}
