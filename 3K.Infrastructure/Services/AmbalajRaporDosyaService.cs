using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed class AmbalajRaporDosyaService : IAmbalajRaporDosyaService
    {
        public byte[] ExcelOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Ambalaj Üretim");
            var headers = new[]
            {
                "Kayıt ID", "İş Akış Kimliği", "Proje No", "Proje / Müşteri", "Sandık No", "Sandık Adı",
                "Tür", "Kaynak", "Sandık Cinsi", "Adet", "Boy (mm)", "En (mm)", "Yükseklik (mm)",
                "Net m³", "Sarf Oranı", "Sarf m³", "Net + Sarf m³", "Ambalaja Dahil", "Üretime Alındı",
                "Üretim Durumu", "Üretim Tarihi", "Talep Eden", "Talep Eden Bölüm", "Talimat Veren",
                "Fırın Parti No", "Açıklama", "İptal", "İptal Nedeni", "Birim m³", "Oluşturulma Tarihi"
            };

            for (var column = 0; column < headers.Length; column++)
                sheet.Cell(1, column + 1).Value = headers[column];

            var row = 2;
            foreach (var item in satirlar)
            {
                sheet.Cell(row, 1).Value = item.KayitId;
                sheet.Cell(row, 2).Value = item.IsAkisKimligi.ToString();
                sheet.Cell(row, 3).Value = item.ProjeNo;
                sheet.Cell(row, 4).Value = item.ProjeAdi;
                sheet.Cell(row, 5).Value = item.SandikNo;
                sheet.Cell(row, 6).Value = item.SandikAdi;
                sheet.Cell(row, 7).Value = TurMetni(item.Tur);
                sheet.Cell(row, 8).Value = KaynakMetni(item.KaynakModul);
                sheet.Cell(row, 9).Value = item.SandikCinsi;
                sheet.Cell(row, 10).Value = item.Adet;
                sheet.Cell(row, 11).Value = item.Boy;
                sheet.Cell(row, 12).Value = item.En;
                sheet.Cell(row, 13).Value = item.Yukseklik;
                sheet.Cell(row, 14).Value = item.NetM3;
                sheet.Cell(row, 15).Value = item.SarfOrani;
                sheet.Cell(row, 16).Value = item.SarfM3;
                sheet.Cell(row, 17).Value = item.ToplamM3;
                sheet.Cell(row, 18).Value = EvetHayir(item.AmbalajaDahil);
                sheet.Cell(row, 19).Value = EvetHayir(item.UretimeAlindi);
                sheet.Cell(row, 20).Value = DurumMetni(item.UretimDurumu);
                if (item.UretimTarihi.HasValue)
                    sheet.Cell(row, 21).Value = item.UretimTarihi.Value;
                sheet.Cell(row, 22).Value = item.TalepEdenKisi;
                sheet.Cell(row, 23).Value = item.TalepEdenBolum;
                sheet.Cell(row, 24).Value = item.TalimatVeren;
                sheet.Cell(row, 25).Value = item.FirinPartiNo;
                sheet.Cell(row, 26).Value = item.Aciklama;
                sheet.Cell(row, 27).Value = EvetHayir(item.IptalMi);
                sheet.Cell(row, 28).Value = item.IptalNedeni;
                sheet.Cell(row, 29).Value = item.BirimM3;
                sheet.Cell(row, 30).Value = item.CreatedDate;
                row++;
            }

            var header = sheet.Range(1, 1, 1, headers.Length);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header.Style.Alignment.WrapText = true;
            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, Math.Max(row - 1, 1), headers.Length).SetAutoFilter();
            sheet.Columns(11, 13).Style.NumberFormat.Format = "0.00";
            sheet.Columns(14, 14).Style.NumberFormat.Format = "0.000000";
            sheet.Column(15).Style.NumberFormat.Format = "0.00%";
            sheet.Columns(16, 17).Style.NumberFormat.Format = "0.000000";
            sheet.Column(29).Style.NumberFormat.Format = "0.000000";
            sheet.Column(30).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sheet.Column(21).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            sheet.Columns().AdjustToContents(1, Math.Min(row - 1, 500));
            sheet.Column(2).Width = 38;
            sheet.Columns(4, 9).Width = Math.Min(sheet.Column(4).Width, 35);
            sheet.Columns(22, 28).Width = 28;
            sheet.Column(26).Style.Alignment.WrapText = true;

            var summary = workbook.Worksheets.Add("Özet");
            summary.Cell("A1").Value = "AMBALAJ ÜRETİM RAPORU ÖZETİ";
            summary.Range("A1:B1").Merge().Style.Font.Bold = true;
            summary.Range("A1:B1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            summary.Range("A1:B1").Style.Font.FontColor = XLColor.White;
            var labels = new[] { "Kayıt Sayısı", "Toplam Sandık Adedi", "Net m³", "Sarf m³", "Net + Sarf m³" };
            var values = new object[] { ozet.KayitSayisi, ozet.ToplamSandikAdedi, ozet.NetM3, ozet.SarfM3, ozet.ToplamM3 };
            for (var index = 0; index < labels.Length; index++)
            {
                summary.Cell(index + 2, 1).Value = labels[index];
                summary.Cell(index + 2, 2).Value = XLCellValue.FromObject(values[index]);
            }
            summary.Range("A2:A6").Style.Font.Bold = true;
            summary.Range("B4:B6").Style.NumberFormat.Format = "0.000000";
            summary.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] PdfOlustur(IReadOnlyList<AmbalajRaporSatiri> satirlar, AmbalajRaporOzeti ozet)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A3.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(style => style.FontSize(7));
                    page.Header().Column(column =>
                    {
                        column.Item().Text("AMBALAJ ÜRETİM RAPORU").Bold().FontSize(15).FontColor(Colors.Blue.Darken2);
                        column.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"Kayıt: {ozet.KayitSayisi}   Sandık: {ozet.ToplamSandikAdedi}");
                            row.RelativeItem().AlignRight().Text(
                                $"Net: {ozet.NetM3:N6} m³   Sarf: {ozet.SarfM3:N6} m³   Toplam: {ozet.ToplamM3:N6} m³");
                        });
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.15f); // Proje
                            columns.RelativeColumn(0.9f);  // Sandık
                            columns.RelativeColumn(0.8f);  // Tür
                            columns.RelativeColumn(0.7f);  // Kaynak
                            columns.RelativeColumn(1.15f); // Cins
                            columns.RelativeColumn(0.45f); // Adet
                            columns.RelativeColumn(1.35f); // Ölçü
                            columns.RelativeColumn(0.8f);  // Net
                            columns.RelativeColumn(0.8f);  // Sarf
                            columns.RelativeColumn(0.85f); // Toplam
                            columns.RelativeColumn(0.75f); // Durum
                            columns.RelativeColumn(0.85f); // Tarih
                            columns.RelativeColumn(1.2f);  // Talep
                            columns.RelativeColumn(1.25f); // Detay
                        });

                        table.Header(header =>
                        {
                            Baslik(header.Cell(), "Proje");
                            Baslik(header.Cell(), "Sandık");
                            Baslik(header.Cell(), "Tür");
                            Baslik(header.Cell(), "Kaynak");
                            Baslik(header.Cell(), "Cins");
                            Baslik(header.Cell(), "Adet");
                            Baslik(header.Cell(), "Boy × En × Yük.");
                            Baslik(header.Cell(), "Birim / Net m³");
                            Baslik(header.Cell(), "Sarf m³");
                            Baslik(header.Cell(), "Toplam m³");
                            Baslik(header.Cell(), "Durum");
                            Baslik(header.Cell(), "Üretim T.");
                            Baslik(header.Cell(), "Talep Eden / Bölüm");
                            Baslik(header.Cell(), "Fırın / Açıklama");
                        });

                        foreach (var item in satirlar)
                        {
                            Hucre(table.Cell(), item.ProjeNo);
                            Hucre(table.Cell(), item.SandikNo);
                            Hucre(table.Cell(), TurMetni(item.Tur));
                            Hucre(table.Cell(), KaynakMetni(item.KaynakModul));
                            Hucre(table.Cell(), item.SandikCinsi);
                            Hucre(table.Cell(), item.Adet.ToString());
                            Hucre(table.Cell(), $"{item.Boy:N0} × {item.En:N0} × {item.Yukseklik:N0}");
                            Hucre(table.Cell(), $"{item.BirimM3:N6}\n{item.NetM3:N6}");
                            Hucre(table.Cell(), $"{item.SarfM3:N6}\n(%{item.SarfOrani * 100:N2})");
                            Hucre(table.Cell(), item.ToplamM3.ToString("N6"));
                            Hucre(table.Cell(), item.IptalMi ? "İptal" : DurumMetni(item.UretimDurumu));
                            Hucre(table.Cell(), item.UretimTarihi?.ToString("dd.MM.yyyy") ?? "-");
                            Hucre(table.Cell(), $"{item.TalepEdenKisi ?? "-"}\n{item.TalepEdenBolum ?? "-"}");
                            Hucre(table.Cell(),
                                $"{item.FirinPartiNo ?? "-"}\n{item.Aciklama ?? item.IptalNedeni ?? "-"}\nOluş.: {item.CreatedDate:dd.MM.yyyy}");
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Sayfa ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] UretimFormuExcelOlustur(AmbalajUretimFormuModel form)
        {
            using var workbook = new XLWorkbook();
            var gruplar = AmbalajUretimFormuGruplayici.Grupla(form.Kalemler);
            var summary = workbook.Worksheets.Add("Üretim Özeti");
            summary.Cell("A1").Value = "AMBALAJ ÜRETİM FORMU";
            summary.Range("A1:F1").Merge();
            summary.Range("A1:F1").Style.Font.Bold = true;
            summary.Range("A1:F1").Style.Font.FontSize = 16;
            summary.Range("A1:F1").Style.Font.FontColor = XLColor.White;
            summary.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            summary.Cell("A3").Value = "Proje No";
            summary.Cell("B3").Value = form.ProjeNo;
            summary.Cell("A4").Value = "Proje / Müşteri";
            summary.Cell("B4").Value = form.ProjeAdi;
            summary.Cell("A5").Value = "Net m³";
            summary.Cell("B5").Value = form.NetM3;
            summary.Cell("A6").Value = "Sarf m³";
            summary.Cell("B6").Value = form.SarfM3;
            summary.Cell("A7").Value = "Net + Sarf m³";
            summary.Cell("B7").Value = form.ToplamM3;
            summary.Range("A3:A7").Style.Font.Bold = true;
            summary.Range("B5:B7").Style.NumberFormat.Format = "0.000000";

            var headers = new[]
            {
                "Sandık No", "Sandık Adı", "Tür", "Cins", "Sandık Adedi", "İç Boy", "İç En", "İç Yükseklik",
                "Dış Boy", "Dış En", "Dış Yükseklik", "Kod", "Grup", "Parça Açıklaması", "Malzeme",
                "Kesit En", "Kesit Yükseklik", "Uzunluk", "Teorik Adet", "Kesim Adedi", "Parça m³",
                "Net Sandık m³", "Sarf Oranı", "Sarf m³", "Net + Sarf m³", "Fırın Parti", "Üretim Tarihi",
                "Formül Sürümü"
            };
            var detail = workbook.Worksheets.Add("Kesim Listesi");
            for (var column = 0; column < headers.Length; column++)
                detail.Cell(1, column + 1).Value = headers[column];

            var row = 2;
            foreach (var grup in gruplar)
            {
                var item = grup.Temsilci;
                foreach (var part in grup.Parcalar)
                {
                    detail.Cell(row, 1).Value = grup.SandikNo;
                    detail.Cell(row, 2).Value = item.SandikAdi;
                    detail.Cell(row, 3).Value = item.SandikTuru;
                    detail.Cell(row, 4).Value = item.SandikCinsi;
                    detail.Cell(row, 5).Value = grup.Adet;
                    detail.Cell(row, 6).Value = item.IcOlculer.Boy;
                    detail.Cell(row, 7).Value = item.IcOlculer.En;
                    detail.Cell(row, 8).Value = item.IcOlculer.Yukseklik;
                    detail.Cell(row, 9).Value = item.DisOlculer.Boy;
                    detail.Cell(row, 10).Value = item.DisOlculer.En;
                    detail.Cell(row, 11).Value = item.DisOlculer.Yukseklik;
                    detail.Cell(row, 12).Value = part.Kod;
                    detail.Cell(row, 13).Value = part.Grup;
                    detail.Cell(row, 14).Value = part.Aciklama;
                    detail.Cell(row, 15).Value = part.Malzeme;
                    detail.Cell(row, 16).Value = part.KesitEn;
                    detail.Cell(row, 17).Value = part.KesitYukseklik;
                    detail.Cell(row, 18).Value = part.Uzunluk;
                    detail.Cell(row, 19).Value = part.TeorikAdet;
                    detail.Cell(row, 20).Value = part.KesimAdedi;
                    detail.Cell(row, 21).Value = part.HacimM3;
                    detail.Cell(row, 22).Value = grup.NetM3;
                    detail.Cell(row, 23).Value = grup.NetM3 == 0 ? 0 : grup.SarfM3 / grup.NetM3;
                    detail.Cell(row, 24).Value = grup.SarfM3;
                    detail.Cell(row, 25).Value = grup.ToplamM3;
                    detail.Cell(row, 26).Value = item.FirinPartiNo;
                    if (grup.UretimTarihleri.Count == 1)
                        detail.Cell(row, 27).Value = grup.UretimTarihleri[0];
                    else if (grup.UretimTarihleri.Count > 1)
                        detail.Cell(row, 27).Value = string.Join(" / ", grup.UretimTarihleri.Select(tarih => tarih.ToString("dd.MM.yyyy HH:mm")));
                    detail.Cell(row, 28).Value = item.FormulVersiyonu;
                    row++;
                }
            }

            var header = detail.Range(1, 1, 1, headers.Length);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            header.Style.Alignment.WrapText = true;
            detail.SheetView.FreezeRows(1);
            detail.Range(1, 1, Math.Max(1, row - 1), headers.Length).SetAutoFilter();
            detail.Columns(21, 22).Style.NumberFormat.Format = "0.000000";
            detail.Column(23).Style.NumberFormat.Format = "0.00%";
            detail.Columns(24, 25).Style.NumberFormat.Format = "0.000000";
            detail.Column(27).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            detail.Columns().AdjustToContents(1, Math.Min(500, row - 1));
            detail.Column(14).Width = 35;
            summary.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] UretimFormuPdfOlustur(AmbalajUretimFormuModel form)
            => AmbalajUretimFormuPdfOlusturucu.Olustur(form);

        private static void Baslik(IContainer container, string text) =>
            container.Background(Colors.Blue.Darken2).Padding(4).AlignMiddle().Text(text).Bold().FontColor(Colors.White);

        private static void Hucre(IContainer container, string text) =>
            container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignMiddle().Text(text);

        private static string EvetHayir(bool value) => value ? "Evet" : "Hayır";

        private static string TurMetni(AmbalajSandikTuru tur) => tur switch
        {
            AmbalajSandikTuru.Normal => "Normal",
            AmbalajSandikTuru.Ilave => "İlave",
            AmbalajSandikTuru.Saha => "Saha",
            AmbalajSandikTuru.Yedek => "Yedek",
            AmbalajSandikTuru.Ic => "İç",
            _ => "Diğer"
        };

        private static string KaynakMetni(AmbalajKaynakModulu kaynak) => kaynak switch
        {
            AmbalajKaynakModulu.Sandik => "Sandık",
            AmbalajKaynakModulu.Saha => "Saha",
            AmbalajKaynakModulu.Yedek => "Yedek",
            AmbalajKaynakModulu.Manuel => "Manuel",
            _ => "Diğer"
        };

        private static string DurumMetni(AmbalajUretimDurumu durum) => durum switch
        {
            AmbalajUretimDurumu.Planlandi => "Bekliyor",
            AmbalajUretimDurumu.Uretimde => "Üretimde",
            _ => "Tamamlandı"
        };
    }
}
