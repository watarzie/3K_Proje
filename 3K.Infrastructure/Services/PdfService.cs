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
                                col.Item().Text($"Ölçüler (E/B/Y): {sandik.En ?? 0} x {sandik.Boy ?? 0} x {sandik.Yukseklik ?? 0} cm");
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

                        // Header Row
                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).Padding(2).Text("Proje No").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Ürün Adı / Tanımı").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Miktar").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Birim").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Açıklama").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Ekleyen").Bold();
                            header.Cell().BorderBottom(1).Padding(2).Text("Tarih").Bold();
                        });

                        // Data Rows
                        foreach (var icerik in sandik.SandikIcerikleri)
                        {
                            string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? sandik.Proje?.ProjeNo ?? "-";
                            string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                            string miktar = (icerik.CekiSatiriId == null ? icerik.Miktar : icerik.KonulanAdet).ToString();
                            string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                            string aciklama = icerik.CekiSatiri?.GridAciklama ?? "-";
                            
                            string ekleyenId = icerik.CreatedBy ?? "";
                            string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;
                            
                            string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(projeNo);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(urunAdi);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(miktar);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(birim);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(aciklama);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(ekleyen);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(2).Text(tarih);
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
                                    col.Item().Text($"Ölçüler (E/B/Y): {sandik.En ?? 0} x {sandik.Boy ?? 0} x {sandik.Yukseklik ?? 0} cm").FontSize(8);
                                    col.Item().Text($"Ağırlık (Net/Gross): {sandik.NetKg ?? 0} / {sandik.GrossKg ?? 0} kg").FontSize(8);
                                });

                                row.ConstantItem(10); // Spacer

                                // Teknik Bilgiler
                                row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                                {
                                    col.Item().Text("TEKNİK BİLGİLER").Bold().FontSize(7).FontColor(Colors.Orange.Darken1);
                                    col.Item().PaddingTop(3).Text($"Güç: {proje.Guc ?? "-"} MVA").FontSize(10).Bold();
                                    col.Item().Text($"Gerilim: {proje.Gerilim ?? "-"} kV").FontSize(8);
                                    col.Item().Text($"Lokasyon: {proje.Lokasyon ?? "-"}").FontSize(8);
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

                            // Başlık satırı
                            table.Header(header =>
                            {
                                void HeaderCell(IContainer c, string text) =>
                                    c.Background(headerBg).Padding(3).Text(text).Bold().FontSize(7).FontColor(headerText);

                                HeaderCell(header.Cell(), "#");
                                HeaderCell(header.Cell(), "PROJE NO");
                                HeaderCell(header.Cell(), "ÜRÜN ADI / TANIMI");
                                HeaderCell(header.Cell(), "MİKTAR");
                                HeaderCell(header.Cell(), "BİRİM");
                                HeaderCell(header.Cell(), "AÇIKLAMA");
                                HeaderCell(header.Cell(), "EKLEYEN");
                                HeaderCell(header.Cell(), "TARİH");
                            });

                            // Data Rows
                            int sira = 1;
                            foreach (var icerik in sandik.SandikIcerikleri)
                            {
                                var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                                string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? proje.ProjeNo;
                                string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                                string miktar = (icerik.CekiSatiriId == null ? icerik.Miktar : icerik.KonulanAdet).ToString();
                                string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                                string aciklama = icerik.CekiSatiri?.GridAciklama ?? "-";

                                string ekleyenId = icerik.CreatedBy ?? "";
                                string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;

                                string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                                void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                                {
                                    var cell = c.Background(bg).BorderBottom(0.5f).BorderColor(tableBorderColor).Padding(2);
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
                .Where(cs => cs.Ceki.ProjeId == projeId
                    && (cs.GridDurumuId == (int)_3K.Core.Enums.GridDurum.EksikGeldi
                        || cs.GridDurumuId == (int)_3K.Core.Enums.GridDurum.Gelmedi)
                )
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

                            // Teknik Bilgiler
                            row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                            {
                                col.Item().Text("TEKNİK BİLGİLER").Bold().FontSize(7).FontColor(Colors.Orange.Darken1);
                                col.Item().PaddingTop(3).Text($"Güç: {proje.Guc ?? "-"} MVA").FontSize(10).Bold();
                                col.Item().Text($"Gerilim: {proje.Gerilim ?? "-"} kV").FontSize(8);
                                col.Item().Text($"Lokasyon: {proje.Lokasyon ?? "-"}").FontSize(8);
                            });

                            row.ConstantItem(10); // Spacer

                            // Eksik Özet
                            row.RelativeItem().Border(1).BorderColor(dangerColor).Padding(8).Column(col =>
                            {
                                col.Item().Text("EKSİK ÖZET").Bold().FontSize(7).FontColor(dangerColor);
                                col.Item().PaddingTop(3).Text($"{toplamEksikUrun} ürün eksik").Bold().FontSize(13).FontColor(dangerColor);
                                col.Item().Text($"Toplam Kalan: {toplamKalanAdet} adet").FontSize(8);
                                col.Item().Text($"Karşılama Oranı: %{(toplamIstenenAdet > 0 ? (toplamGelenAdet * 100 / toplamIstenenAdet) : 0)}").FontSize(8);
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
                            columns.ConstantColumn(30);   // Sıra
                            columns.RelativeColumn(1.5f); // Barkod
                            columns.RelativeColumn(3);    // Açıklama
                            columns.RelativeColumn(1);    // Sandık
                            columns.ConstantColumn(45);   // İstenen
                            columns.ConstantColumn(50);   // 3K Gelen
                            columns.ConstantColumn(50);   // Karşılanan
                            columns.ConstantColumn(50);   // Geri Gönd.
                            columns.ConstantColumn(55);   // Prj. Verildi
                            columns.ConstantColumn(45);   // Kalan
                            columns.RelativeColumn(1.5f); // 3K Durum
                        });

                        // Başlık satırı
                        table.Header(header =>
                        {
                            void HeaderCell(IContainer c, string text) =>
                                c.Background(headerBg).Padding(3).Text(text).Bold().FontSize(7).FontColor(headerText);

                            HeaderCell(header.Cell(), "#");
                            HeaderCell(header.Cell(), "SIRA");
                            HeaderCell(header.Cell(), "BARKOD NO");
                            HeaderCell(header.Cell(), "ÜRÜN AÇIKLAMASI");
                            HeaderCell(header.Cell(), "SANDIK");
                            HeaderCell(header.Cell(), "İSTENEN");
                            HeaderCell(header.Cell(), "3K GELEN");
                            HeaderCell(header.Cell(), "KARŞI.");
                            HeaderCell(header.Cell(), "GERİ GÖN.");
                            HeaderCell(header.Cell(), "PRJ. VERİLDİ");
                            HeaderCell(header.Cell(), "KALAN");
                            HeaderCell(header.Cell(), "3K DURUM");
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
                                var cell = c.Background(bg).BorderBottom(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), cs.SiraNo.ToString());
                            DataCell(table.Cell(), cs.BarkodNo);
                            DataCell(table.Cell(), cs.Aciklama);
                            DataCell(table.Cell(), sandikNo);
                            DataCell(table.Cell(), cs.IstenenAdet.ToString());
                            DataCell(table.Cell(), cs.GelenMiktar.ToString());
                            DataCell(table.Cell(), karsilanan > 0 ? karsilanan.ToString() : "-");
                            DataCell(table.Cell(), cs.GeriGonderilenMiktar > 0 ? cs.GeriGonderilenMiktar.ToString() : "-");
                            // Başka projeye verildi
                            var projVerildi = cs.ProjeGonderilen > 0 ? $"{cs.ProjeGonderilen} ({cs.KaynakHedefProjeNo ?? "?"})" : "-";
                            DataCell(table.Cell(), projVerildi, fontColor: cs.ProjeGonderilen > 0 ? "#1565C0" : null);
                            DataCell(table.Cell(), cs.KalanMiktar.ToString(), bold: true, fontColor: dangerColor);
                            DataCell(table.Cell(), ucKDurum, fontColor: cs.UcKKarsilamaTipiId == (int)_3K.Core.Enums.UcKDurum.Bekliyor ? dangerColor : warningColor);

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
                                c.Background(headerBg).Padding(3).Text(text).Bold().FontSize(7).FontColor(headerText);

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
                                var cell = c.Background(bg).BorderBottom(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), s.MalzemeKodu ?? "-");
                            DataCell(table.Cell(), s.MalzemeAdi ?? "-");
                            DataCell(table.Cell(), s.Miktar.ToString(), bold: true);
                            DataCell(table.Cell(), s.Birim ?? "-");
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
