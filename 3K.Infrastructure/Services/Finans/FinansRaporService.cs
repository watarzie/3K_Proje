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
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services.Finans
{
    internal sealed partial class FinansRaporService : IFinansRaporService
    {
        private readonly IFinansRaporVerisiService _raporVerisi;
        private readonly IFinansAylikOzetService _aylikOzet;
        private readonly IFinansPanelService _panel;
        private readonly IFinansSiparisFaturaService _faturalar;

        public FinansRaporService(
            IFinansRaporVerisiService raporVerisi,
            IFinansAylikOzetService aylikOzet,
            IFinansPanelService panel,
            IFinansSiparisFaturaService faturalar)
        {
            _raporVerisi = raporVerisi;
            _aylikOzet = aylikOzet;
            _panel = panel;
            _faturalar = faturalar;
        }

        public async Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Finans İşleri");
            var headers = IsBasliklari;
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in report.Isler)
                WriteRow(sheet, row++, IsSatiri(item));
            FormatSheet(sheet, headers.Length, row - 1);
            return Save(workbook);
        }

        public async Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Giderler");
            var headers = GiderBasliklari;
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in report.Giderler)
                WriteRow(sheet, row++, GiderSatiri(item));
            FormatSheet(sheet, headers.Length, row - 1);
            return Save(workbook);
        }

        /// <summary>
        /// Sipariş durum raporu (RR Q5): satırlar seçilen sipariş/fatura karşılanmasındaki iş kalemleridir; kategori,
        /// dönem, proje ve diğer liste filtreleri sunucuda uygulanır.
        /// </summary>
        public async Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Sipariş Durumu");
            var headers = new[]
            {
                "Kayıt No", "Proje No", "Müşteri", "Kategori", "İş", "Sandık No", "Finans Tarihi", "Para Birimi",
                "İş Net", "Sipariş Net", "Kalan Sipariş", "Faturalanan Net", "Kalan Fatura",
                "Sipariş Durumu", "Fatura Durumu", "PO", "Fatura"
            };
            WriteHeaders(sheet, headers);
            var row = 2;
            foreach (var item in report.Isler)
            {
                WriteRow(sheet, row++, new object?[]
                {
                    item.Id, item.ProjeNo, item.Musteri, FinansKategoriKurali.Ad(item.Kategori), item.IsAdi, item.SandikNo,
                    item.FinansTarihi, item.ParaBirimi, item.NetTutar, item.SiparisNetTutar, item.KalanSiparisNetTutar,
                    item.FaturalananNetTutar, item.KalanFaturaNetTutar,
                    SiparisKarsilanmaMetni(FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.SiparisNetTutar)),
                    FaturaKarsilanmaMetni(FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.FaturalananNetTutar)),
                    string.Join(", ", item.PoNumaralari), string.Join(", ", item.FaturaNumaralari)
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
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            return GenerateJobsPdf("Finans İşleri Raporu", report.Isler);
        }

        public async Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            return GenerateExpensePdf("Finans Gider Raporu", report.Giderler);
        }

        public async Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
        {
            var report = await _raporVerisi.RaporVerisiAsync(filtre, cancellationToken);
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Column(column =>
                    {
                        column.Item().Text("Finans Sipariş Durum Raporu").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm} · Kalem: {report.Isler.Count}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                        });
                        PdfHeader(table, "Proje", "Kategori", "İş", "Tarih", "İş Net", "Sipariş Net", "Faturalanan", "Sipariş", "Fatura", "PO / Fatura");
                        foreach (var item in report.Isler)
                        {
                            PdfCell(table, item.ProjeNo);
                            PdfCell(table, FinansKategoriKurali.Ad(item.Kategori));
                            PdfCell(table, item.IsAdi);
                            PdfCell(table, item.FinansTarihi.ToString("dd.MM.yyyy"));
                            PdfCell(table, $"{Format(item.NetTutar)} {item.ParaBirimi}");
                            PdfCell(table, $"{Format(item.SiparisNetTutar)} {item.ParaBirimi}");
                            PdfCell(table, $"{Format(item.FaturalananNetTutar)} {item.ParaBirimi}");
                            PdfCell(table, SiparisKarsilanmaMetni(FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.SiparisNetTutar)));
                            PdfCell(table, FaturaKarsilanmaMetni(FinansTutarKurallari.KalemKarsilanma(item.NetTutar, item.FaturalananNetTutar)));
                            PdfCell(table, $"{string.Join(", ", item.PoNumaralari)} / {string.Join(", ", item.FaturaNumaralari)}");
                        }
                    });
                    AddFooter(page);
                });
            });
            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        /// <summary>Sipariş karşılanmasının rapor metni; ön yüz durum filtresindeki adlarla aynıdır.</summary>
        internal static string SiparisKarsilanmaMetni(FinansKarsilanma durum) => durum switch
        {
            FinansKarsilanma.Tam => "Tam Açıldı",
            FinansKarsilanma.Kismi => "Kısmi Açıldı",
            _ => "Açılmadı"
        };

        internal static string FaturaKarsilanmaMetni(FinansKarsilanma durum) => durum switch
        {
            FinansKarsilanma.Tam => "Faturalandı",
            FinansKarsilanma.Kismi => "Kısmi Faturalandı",
            _ => "Fatura Yok"
        };

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
            var satirlar = (await _aylikOzet.AylikOzetAsync(yil, ay, cancellationToken))
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
                            PdfCell(table, FinansEtiketleri.IsTuru(item.IsTuru));
                            PdfCell(table, Format(item.Adet));
                            PdfCell(table, Format(item.ToplamM3));
                            PdfCell(table, item.ParaBirimi);
                            PdfCell(table, Format(item.ToplamTutar));
                            PdfCell(table, FinansEtiketleri.IsDurumu(item.Durum));
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

        private static string Format(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
    }
}
