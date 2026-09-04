using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed class FinansRaporService : IFinansRaporService
    {
        private readonly IFinansService _finansService;

        public FinansRaporService(IFinansService finansService)
        {
            _finansService = finansService;
        }

        public async Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _finansService.RaporVerisiAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Finans İşleri");
            var headers = new[]
            {
                "Kayıt No", "Proje No", "Müşteri", "İş Türü", "İş Adı", "Sandık No", "Adet",
                "Talep Eden", "Talep Eden Bölüm", "Birim m³", "Toplam m³", "Üretim Tarihi", "Finans Dönemi", "Durum",
                "Fiyatlandırma", "Birim Fiyat", "Para Birimi", "KDV %", "Net", "KDV", "Toplam", "PO", "Fatura"
            };
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in report.Isler)
            {
                var values = new object?[]
                {
                    item.Id, item.ProjeNo, item.Musteri, item.IsTuru.ToString(), item.IsAdi, item.SandikNo,
                    item.Adet, item.TalepEdenKisi, item.TalepEdenBolum, item.BirimM3, item.ToplamM3, item.UretimTarihi, item.FinansDonemi, item.Durum.ToString(),
                    item.FiyatlandirmaBirimi.ToString(), item.BirimFiyat, item.ParaBirimi, item.KdvOrani,
                    item.NetTutar, item.KdvTutari, item.ToplamTutar,
                    string.Join(", ", item.PoNumaralari), string.Join(", ", item.FaturaNumaralari)
                };
                WriteRow(sheet, row++, values);
            }
            FormatSheet(sheet, headers.Length, row - 1);
            return Save(workbook);
        }

        public async Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _finansService.RaporVerisiAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Giderler");
            var headers = new[]
            {
                "Kayıt No", "Tarih", "Finans Dönemi", "Kategori", "Gider Kalemi", "Firma/Kişi", "Açıklama",
                "Miktar", "Birim", "Birim Fiyat", "Para Birimi", "KDV Dahil", "KDV %", "Matrah", "KDV", "Toplam", "Proje"
            };
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in report.Giderler)
            {
                WriteRow(sheet, row++, new object?[]
                {
                    item.Id, item.Tarih, item.FinansDonemi, item.Kategori, item.GiderKalemi,
                    item.FirmaVeyaKisi, item.Aciklama, item.Miktar, item.Birim, item.BirimFiyat,
                    item.ParaBirimi, item.KdvDahil ? "Evet" : "Hayır", item.KdvOrani,
                    item.Matrah, item.KdvTutari, item.ToplamTutar, item.ProjeNo
                });
            }
            FormatSheet(sheet, headers.Length, row - 1);
            return Save(workbook);
        }

        public async Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var orders = await GetAllOrdersAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Sipariş Durumu");
            var headers = new[]
            {
                "Kayıt No", "PO No", "Proje No", "Müşteri", "İş Türleri", "Sipariş Tarihi",
                "Sipariş Adet", "Sipariş m³", "Faturalanan Adet", "Faturalanan m³",
                "Kalan Adet", "Kalan m³", "Durum", "Tutarlar", "Açıklama"
            };
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in orders)
            {
                WriteRow(sheet, row++, new object?[]
                {
                    item.KayitNo,
                    item.PoNumarasi,
                    item.ProjeNo,
                    item.Musteri,
                    string.Join(", ", item.IsTurleri),
                    item.SiparisTarihi,
                    item.SandikAdedi,
                    item.ToplamM3,
                    item.FaturalananAdet,
                    item.FaturalananM3,
                    item.KalanAdet,
                    item.KalanM3,
                    item.Durum.ToString(),
                    FormatMoneyTotals(item.Tutarlar),
                    item.Aciklama
                });
            }
            FormatSheet(sheet, headers.Length, row - 1);
            return Save(workbook);
        }

        public async Task<byte[]> AylikExcelAsync(
            int yil,
            int ay,
            IReadOnlyCollection<string>? gruplar,
            CancellationToken cancellationToken)
        {
            var satirlar = await AylikSatirlarAsync(yil, ay, gruplar, cancellationToken);
            return AylikExcelOlustur(satirlar, yil, ay);
        }

        public async Task<byte[]> AylikZipAsync(
            int yil,
            int ay,
            IReadOnlyCollection<string>? gruplar,
            CancellationToken cancellationToken)
        {
            var satirlar = await AylikSatirlarAsync(yil, ay, gruplar, cancellationToken);
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var grup in satirlar.Select(x => x.IsGrubu).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var grupSatirlari = satirlar
                        .Where(x => string.Equals(x.IsGrubu, grup, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    await ZipEntryYazAsync(
                        archive.CreateEntry($"{GuvenliDosyaAdi(grup)}.pdf"),
                        AylikPdfOlustur(grupSatirlari, yil, ay),
                        cancellationToken);
                    await ZipEntryYazAsync(
                        archive.CreateEntry($"{GuvenliDosyaAdi(grup)}.xlsx"),
                        AylikExcelOlustur(grupSatirlari, yil, ay),
                        cancellationToken);
                }
            }

            return stream.ToArray();
        }

        private static byte[] AylikExcelOlustur(
            IReadOnlyList<FinansAylikIsModel> satirlar,
            int yil,
            int ay)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Aylık Finans");
            sheet.Cell(1, 1).Value = $"3K AYLIK FİNANS RAPORU - {ay:00}.{yil}";
            sheet.Range(1, 1, 1, 20).Merge().Style.Font.SetBold().Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.FromHtml("#3584FC"));
            sheet.Cell(2, 1).Value = $"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}";
            sheet.Range(2, 1, 2, 20).Merge();
            var headers = new[]
            {
                "Grup", "Proje", "İş", "Sandık Tipi", "Boy (mm)", "En (mm)", "Yükseklik (mm)",
                "Başlangıç", "Bitiş", "Sandık", "Miktar", "Birim", "Tarife", "Para Birimi",
                "KDV %", "Net", "KDV", "Toplam", "PO", "Fatura"
            };
            for (var column = 0; column < headers.Length; column++)
                sheet.Cell(4, column + 1).Value = headers[column];
            sheet.Range(4, 1, 4, headers.Length).Style.Fill
                .SetBackgroundColor(XLColor.FromHtml("#EAF2FF")).Font.SetBold();

            for (var index = 0; index < satirlar.Count; index++)
            {
                var row = index + 5;
                var satir = satirlar[index];
                WriteRow(sheet, row,
                [
                    satir.IsGrubu, satir.ProjeNo, satir.IsAdi, satir.SandikTipi,
                    satir.Boy, satir.En, satir.Yukseklik, satir.UretimBaslangic, satir.UretimBitis,
                    satir.SandikAdedi, satir.Miktar, satir.Birim, satir.BirimFiyat, satir.ParaBirimi,
                    satir.KdvOrani, satir.NetTutar, satir.KdvTutari, satir.ToplamTutar,
                    string.Join(", ", satir.PoNumaralari), string.Join(", ", satir.FaturaNumaralari)
                ]);
            }

            var toplamSatiri = satirlar.Count + 6;
            foreach (var toplam in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key))
            {
                sheet.Cell(toplamSatiri, 14).Value = $"{toplam.Key} TOPLAM";
                sheet.Cell(toplamSatiri, 14).Style.Font.SetBold();
                sheet.Cell(toplamSatiri, 16).Value = toplam.Sum(x => x.NetTutar);
                sheet.Cell(toplamSatiri, 17).Value = toplam.Sum(x => x.KdvTutari);
                sheet.Cell(toplamSatiri, 18).Value = toplam.Sum(x => x.ToplamTutar);
                toplamSatiri++;
            }

            sheet.Range(4, 1, Math.Max(4, satirlar.Count + 4), headers.Length).SetAutoFilter();
            sheet.SheetView.FreezeRows(4);
            sheet.Columns(8, 9).Style.DateFormat.Format = "dd.MM.yyyy";
            sheet.Columns(13, 18).Style.NumberFormat.Format = "#,##0.00";
            sheet.Columns().AdjustToContents(8, 36);
            return Save(workbook);
        }

        public async Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _finansService.RaporVerisiAsync(filtre, cancellationToken);
            return GenerateJobsPdf("Finans İşleri Raporu", report.Isler);
        }

        public async Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _finansService.RaporVerisiAsync(filtre, cancellationToken);
            return GenerateExpensePdf("Finans Gider Raporu", report.Giderler);
        }

        public async Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var orders = await GetAllOrdersAsync(filtre, cancellationToken);
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Column(column =>
                    {
                        column.Item().Text("Finans Sipariş Durum Raporu").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm} · Kayıt: {orders.Count}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.8f);
                        });
                        PdfHeader(table, "Kayıt", "PO", "Proje", "Müşteri", "İş Türü", "Tarih", "Adet", "m³", "Kalan", "Durum", "Tutarlar");
                        foreach (var item in orders)
                        {
                            PdfCell(table, item.KayitNo);
                            PdfCell(table, item.PoNumarasi);
                            PdfCell(table, item.ProjeNo);
                            PdfCell(table, item.Musteri);
                            PdfCell(table, string.Join(", ", item.IsTurleri));
                            PdfCell(table, item.SiparisTarihi.ToString("dd.MM.yyyy"));
                            PdfCell(table, Format(item.SandikAdedi));
                            PdfCell(table, Format(item.ToplamM3));
                            PdfCell(table, $"{Format(item.KalanAdet)} / {Format(item.KalanM3)} m³");
                            PdfCell(table, item.Durum.ToString());
                            PdfCell(table, FormatMoneyTotals(item.Tutarlar));
                        }
                    });
                    AddFooter(page);
                });
            });
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> AylikPdfAsync(
            int yil,
            int ay,
            IReadOnlyCollection<string>? gruplar,
            CancellationToken cancellationToken)
        {
            var satirlar = await AylikSatirlarAsync(yil, ay, gruplar, cancellationToken);
            return AylikPdfOlustur(satirlar, yil, ay);
        }

        private static byte[] AylikPdfOlustur(
            IReadOnlyList<FinansAylikIsModel> satirlar,
            int yil,
            int ay)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Column(column =>
                    {
                        column.Item().Text($"AYLIK FİNANS RAPORU - {ay:00}.{yil}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                        column.Item().Text($"Gruplar: {(satirlar.Count == 0 ? "Kayıt yok" : string.Join(", ", satirlar.Select(x => x.IsGrubu).Distinct()))}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    page.Content().Column(column =>
                    {
                        column.Spacing(6);
                        foreach (var grup in satirlar.GroupBy(x => x.IsGrubu))
                        {
                            column.Item().PaddingTop(6).Text(grup.Key).FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(); c.RelativeColumn(1.5f); c.RelativeColumn();
                                    c.RelativeColumn(1.2f); c.RelativeColumn(.6f); c.RelativeColumn(.8f);
                                    c.RelativeColumn(.9f); c.RelativeColumn(.9f); c.RelativeColumn(.9f);
                                    c.RelativeColumn(.9f); c.RelativeColumn();
                                });
                                PdfHeader(table, "Proje", "İş", "Sandık Tipi", "Ölçü (mm)", "Sandık", "Miktar", "Tarife", "Net", "KDV", "Toplam", "Durum");
                                foreach (var satir in grup)
                                {
                                    PdfCell(table, satir.ProjeNo);
                                    PdfCell(table, satir.IsAdi);
                                    PdfCell(table, satir.SandikTipi ?? "-");
                                    PdfCell(table, OlcuMetni(satir.Boy, satir.En, satir.Yukseklik));
                                    PdfCell(table, satir.SandikAdedi.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR")));
                                    PdfCell(table, $"{satir.Miktar:0.###} {satir.Birim}");
                                    PdfCell(table, $"{satir.BirimFiyat:0.00} {satir.ParaBirimi}");
                                    PdfCell(table, satir.NetTutar.ToString("0.00"));
                                    PdfCell(table, satir.KdvTutari.ToString("0.00"));
                                    PdfCell(table, $"{satir.ToplamTutar:0.00} {satir.ParaBirimi}");
                                    PdfCell(table, satir.Durum);
                                }
                            });
                        }

                        foreach (var toplam in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key))
                            column.Item().AlignRight().PaddingTop(5)
                                .Text($"{toplam.Key} TOPLAM: Net {toplam.Sum(x => x.NetTutar):N2} | KDV {toplam.Sum(x => x.KdvTutari):N2} | Genel {toplam.Sum(x => x.ToplamTutar):N2}")
                                .Bold();
                    });
                    AddFooter(page);
                });
            });
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private async Task<IReadOnlyList<FinansAylikIsModel>> AylikSatirlarAsync(
            int yil,
            int ay,
            IReadOnlyCollection<string>? gruplar,
            CancellationToken cancellationToken)
        {
            var satirlar = (await _finansService.AylikOzetAsync(yil, ay, cancellationToken))
                .Where(x => !x.IptalEdildi);
            var seciliGruplar = gruplar?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (seciliGruplar is { Count: > 0 })
                satirlar = satirlar.Where(x => AylikRaporGrubunaDahil(x, seciliGruplar));
            return satirlar.OrderBy(x => x.IsGrubu).ThenBy(x => x.ProjeNo).ThenBy(x => x.IsAdi).ToArray();
        }

        internal static bool AylikRaporGrubunaDahil(
            FinansAylikIsModel row,
            IReadOnlySet<string> selectedGroups)
        {
            if (selectedGroups.Contains(row.IsGrubu))
                return true;

            var canonicalGroup = row.IsTuru == FinansIsTuru.OzelIs
                ? row.IsGrubu is "Kira" or "Sevkiyat" ? "Sabit İşler" : "Ekstra İşler"
                : "Ana Ambalaj";
            return selectedGroups.Contains(canonicalGroup);
        }

        private static async Task ZipEntryYazAsync(
            ZipArchiveEntry entry,
            byte[] content,
            CancellationToken cancellationToken)
        {
            await using var target = entry.Open();
            await target.WriteAsync(content, cancellationToken);
        }

        private static string GuvenliDosyaAdi(string value)
        {
            var gecersizKarakterler = Path.GetInvalidFileNameChars();
            return string.Concat(value.Select(c => gecersizKarakterler.Contains(c) ? '_' : c)).Replace(' ', '_');
        }

        private static string OlcuMetni(decimal? boy, decimal? en, decimal? yukseklik) =>
            boy > 0 && en > 0 && yukseklik > 0
                ? $"{boy:0.##} × {en:0.##} × {yukseklik:0.##}"
                : "-";

        private static byte[] GenerateJobsPdf(string title, IReadOnlyList<FinansIsKaydiModel> rows)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Text(title).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f); c.RelativeColumn(1.8f); c.RelativeColumn(1.4f); c.RelativeColumn();
                            c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        });
                        PdfHeader(table, "Proje", "İş", "Tür", "Adet", "m³", "Birim", "Toplam", "Durum");
                        foreach (var item in rows)
                        {
                            PdfCell(table, item.ProjeNo);
                            PdfCell(table, item.IsAdi);
                            PdfCell(table, item.IsTuru.ToString());
                            PdfCell(table, Format(item.Adet));
                            PdfCell(table, Format(item.ToplamM3));
                            PdfCell(table, item.ParaBirimi);
                            PdfCell(table, Format(item.ToplamTutar));
                            PdfCell(table, item.Durum.ToString());
                        }
                    });
                    AddFooter(page);
                });
            });
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private static byte[] GenerateExpensePdf(string title, IReadOnlyList<FinansGiderModel> rows)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Text(title).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(); c.RelativeColumn(1.3f); c.RelativeColumn(2); c.RelativeColumn(1.2f);
                            c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        });
                        PdfHeader(table, "Tarih", "Kategori", "Açıklama", "Proje", "Birim", "Matrah", "Toplam");
                        foreach (var item in rows)
                        {
                            PdfCell(table, item.Tarih.ToString("dd.MM.yyyy"));
                            PdfCell(table, item.Kategori);
                            PdfCell(table, item.Aciklama);
                            PdfCell(table, item.ProjeNo);
                            PdfCell(table, item.ParaBirimi);
                            PdfCell(table, Format(item.Matrah));
                            PdfCell(table, Format(item.ToplamTutar));
                        }
                    });
                    AddFooter(page);
                });
            });
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private static FinansListeFiltre MonthFilter(int year, int month)
        {
            if (year is < 2000 or > 2200 || month is < 1 or > 12)
                throw new InvalidOperationException("Geçerli bir yıl ve ay seçilmelidir.");
            var start = new DateTime(year, month, 1);
            return new FinansListeFiltre(PageSize: 250, Baslangic: start, Bitis: start.AddMonths(1).AddDays(-1));
        }

        private async Task<IReadOnlyList<FinansSiparisModel>> GetAllOrdersAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            const int pageSize = 250;
            var items = new List<FinansSiparisModel>();
            var pageNumber = 1;
            while (true)
            {
                var page = await _finansService.SiparislerAsync(
                    filtre with { PageNumber = pageNumber, PageSize = pageSize },
                    cancellationToken);
                items.AddRange(page.Items);
                if (!page.HasNextPage)
                    return items;
                pageNumber++;
            }
        }

        private static string FormatMoneyTotals(IEnumerable<FinansParaToplamiModel> totals)
            => string.Join(" | ", totals.Select(x =>
                $"{x.ParaBirimi}: Net {Format(x.NetTutar)}, KDV {Format(x.KdvTutari)}, Toplam {Format(x.ToplamTutar)}"));

        private static void WriteHeaders(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (var i = 0; i < headers.Count; i++) sheet.Cell(1, i + 1).Value = headers[i];
            var range = sheet.Range(1, 1, 1, headers.Count);
            range.Style.Font.Bold = true;
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#3F51B5");
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void WriteRow(IXLWorksheet sheet, int row, IReadOnlyList<object?> values)
        {
            for (var i = 0; i < values.Count; i++)
            {
                var cell = sheet.Cell(row, i + 1);
                switch (values[i])
                {
                    case null: cell.Value = string.Empty; break;
                    case DateTime date: cell.Value = date; cell.Style.DateFormat.Format = "dd.MM.yyyy"; break;
                    case decimal number: cell.Value = number; cell.Style.NumberFormat.Format = "#,##0.00####"; break;
                    case int number: cell.Value = number; break;
                    case bool flag: cell.Value = flag; break;
                    default: cell.Value = Convert.ToString(values[i], CultureInfo.CurrentCulture) ?? string.Empty; break;
                }
            }
        }

        private static void FormatSheet(IXLWorksheet sheet, int columnCount, int lastRow)
        {
            sheet.SheetView.FreezeRows(1);
            if (lastRow >= 1) sheet.Range(1, 1, lastRow, columnCount).SetAutoFilter();
            sheet.Columns(1, columnCount).AdjustToContents(1, Math.Max(1, lastRow), 8, 45);
            sheet.Range(1, 1, Math.Max(1, lastRow), columnCount).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            sheet.Range(1, 1, Math.Max(1, lastRow), columnCount).Style.Border.BottomBorderColor = XLColor.LightGray;
        }

        private static int WriteMoneySummary(IXLWorksheet sheet, int row, string type, IReadOnlyList<FinansParaToplamiModel> values)
        {
            foreach (var item in values)
                WriteRow(sheet, row++, new object?[] { type, item.ParaBirimi, item.NetTutar, item.KdvTutari, item.ToplamTutar });
            return row;
        }

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void ConfigurePage(PageDescriptor page)
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(7));
        }

        private static void AddFooter(PageDescriptor page)
            => page.Footer().AlignCenter().Text(text =>
            {
                text.Span("3K Finans · Sayfa ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });

        private static void PdfHeader(TableDescriptor table, params string[] values)
        {
            table.Header(header =>
            {
                foreach (var value in values)
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(value).FontColor(Colors.White).Bold();
            });
        }

        private static void PdfCell(TableDescriptor table, string value)
            => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(value ?? string.Empty);

        private static void AddMoneyRows(TableDescriptor table, string type, IEnumerable<FinansParaToplamiModel> values)
        {
            foreach (var item in values)
            {
                PdfCell(table, type);
                PdfCell(table, item.ParaBirimi);
                PdfCell(table, Format(item.NetTutar));
                PdfCell(table, Format(item.KdvTutari));
                PdfCell(table, Format(item.ToplamTutar));
            }
        }

        private static string Format(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
    }
}
