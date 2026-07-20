using ClosedXML.Excel;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class FinansRaporService : IFinansRaporService
    {
        private readonly AppDbContext _context;
        private readonly IFinansAylikService _aylikService;

        public FinansRaporService(AppDbContext context, IFinansAylikService aylikService)
        {
            _context = context;
            _aylikService = aylikService;
        }

        public async Task<byte[]> IsRaporuPdfAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri, string kullanici, CancellationToken cancellationToken = default)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var kayitlar = await Sorgula(baslangic, bitis, projeNo, musteri, isTurleri, cancellationToken);
            var filtre = FiltreMetni(baslangic, bitis, projeNo, musteri, isTurleri);

            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("3K").FontSize(22).Bold().FontColor("#3584FC");
                        row.RelativeItem(4).AlignCenter().Text("FİNANS İŞ TAKİP RAPORU").FontSize(15).Bold();
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
                    column.Item().PaddingTop(5).Text(filtre).FontColor("#64748B");
                });
                page.Content().PaddingVertical(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.3f); columns.RelativeColumn(1.1f); columns.RelativeColumn(1.2f); columns.RelativeColumn(.6f);
                        columns.RelativeColumn(.7f); columns.RelativeColumn(.8f); columns.RelativeColumn(.8f); columns.RelativeColumn(.8f);
                    });
                    table.Header(header =>
                    {
                        foreach (var baslik in new[] { "Proje", "Müşteri", "Sandık", "İş Türü", "Sandık Adı", "Sandık Tipi", "Ölçü (mm)", "Adet", "Birim m³", "Toplam m³", "Sipariş m³", "Fatura m³" })
                            header.Cell().Background("#EEF2F7").Padding(5).Text(baslik).SemiBold();
                    });
                    foreach (var kayit in kayitlar)
                    {
                        Cell(table, kayit.ProjeNo); Cell(table, kayit.Musteri); Cell(table, kayit.SandikNo); Cell(table, kayit.IsTuru.ToString()); Cell(table, kayit.SandikAdi);
                        Cell(table, kayit.SandikTipi ?? "-"); Cell(table, OlcuMetni(kayit.Boy, kayit.En, kayit.Yukseklik));
                        Cell(table, kayit.Adet.ToString("0.##")); Cell(table, kayit.BirimM3.ToString("0.000")); Cell(table, kayit.ToplamM3.ToString("0.000"));
                        Cell(table, kayit.SiparisM3.ToString("0.000")); Cell(table, kayit.FaturaM3.ToString("0.000"));
                    }
                    table.Cell().ColumnSpan(7).Background("#F8FAFC").Padding(6).AlignRight().Text("TOPLAM").Bold();
                    Cell(table, kayitlar.Sum(k => k.Adet).ToString("0.##"), true); Cell(table, ""); Cell(table, kayitlar.Sum(k => k.ToplamM3).ToString("0.000"), true);
                    Cell(table, kayitlar.Sum(k => k.SiparisM3).ToString("0.000"), true); Cell(table, kayitlar.Sum(k => k.FaturaM3).ToString("0.000"), true);
                });
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Raporu oluşturan: {kullanici}").FontColor("#64748B");
                    row.RelativeItem().AlignRight().Text(text => { text.Span("Sayfa "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
                });
            })).GeneratePdf();
        }

        public async Task<byte[]> IsRaporuExcelAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri, string kullanici, CancellationToken cancellationToken = default)
        {
            var kayitlar = await Sorgula(baslangic, bitis, projeNo, musteri, isTurleri, cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Finans İş Raporu");
            sheet.Cell(1, 1).Value = "3K FİNANS İŞ TAKİP RAPORU";
            sheet.Range(1, 1, 1, 15).Merge().Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#3584FC"));
            sheet.Cell(2, 1).Value = FiltreMetni(baslangic, bitis, projeNo, musteri, isTurleri);
            sheet.Range(2, 1, 2, 15).Merge();
            sheet.Cell(3, 1).Value = $"Raporu oluşturan: {kullanici} | {DateTime.Now:dd.MM.yyyy HH:mm}";
            sheet.Range(3, 1, 3, 15).Merge();
            var headers = new[] { "Proje No", "Müşteri", "Sandık No", "Sandık Adı", "İş Türü", "Sandık Tipi", "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Adet", "Birim m³", "Toplam m³", "Sipariş m³", "Fatura m³", "Üretime Alınma" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(5, column + 1).Value = headers[column];
            sheet.Range(5, 1, 5, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF")).Font.SetBold();
            for (var index = 0; index < kayitlar.Count; index++)
            {
                var row = index + 6; var kayit = kayitlar[index];
                sheet.Cell(row, 1).Value = kayit.ProjeNo; sheet.Cell(row, 2).Value = kayit.Musteri; sheet.Cell(row, 3).Value = kayit.SandikNo;
                sheet.Cell(row, 4).Value = kayit.SandikAdi; sheet.Cell(row, 5).Value = kayit.IsTuru.ToString(); sheet.Cell(row, 6).Value = kayit.SandikTipi ?? string.Empty;
                sheet.Cell(row, 7).Value = kayit.Boy; sheet.Cell(row, 8).Value = kayit.En; sheet.Cell(row, 9).Value = kayit.Yukseklik; sheet.Cell(row, 10).Value = kayit.Adet;
                sheet.Cell(row, 11).Value = kayit.BirimM3; sheet.Cell(row, 12).Value = kayit.ToplamM3; sheet.Cell(row, 13).Value = kayit.SiparisM3;
                sheet.Cell(row, 14).Value = kayit.FaturaM3; sheet.Cell(row, 15).Value = kayit.UretimeAlinmaTarihi;
            }
            var totalRow = kayitlar.Count + 6;
            sheet.Cell(totalRow, 9).Value = "TOPLAM"; sheet.Cell(totalRow, 9).Style.Font.SetBold();
            foreach (var column in new[] { 10, 12, 13, 14 }) sheet.Cell(totalRow, column).FormulaA1 = $"SUM({sheet.Cell(6, column).Address}:{sheet.Cell(totalRow - 1, column).Address})";
            sheet.Range(5, 1, Math.Max(5, totalRow - 1), headers.Length).SetAutoFilter();
            sheet.Column(15).Style.DateFormat.Format = "dd.MM.yyyy";
            sheet.Columns().AdjustToContents(8, 36);
            sheet.SheetView.FreezeRows(5);
            using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
        }

        public async Task<byte[]> AylikRaporPdfAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default) =>
            AylikPdf(await AylikSatirlar(yil, ay, gruplar, cancellationToken), yil, ay, kullanici);

        public async Task<byte[]> AylikRaporExcelAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default) =>
            AylikExcel(await AylikSatirlar(yil, ay, gruplar, cancellationToken), yil, ay, kullanici);

        public async Task<byte[]> AylikRaporZipAsync(int yil, int ay, string[]? gruplar, string kullanici, CancellationToken cancellationToken = default)
        {
            var satirlar = await AylikSatirlar(yil, ay, gruplar, cancellationToken);
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var grup in satirlar.Select(x => x.IsGrubu).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var grupSatirlari = satirlar.Where(x => string.Equals(x.IsGrubu, grup, StringComparison.OrdinalIgnoreCase)).ToList();
                    var dosyaAdi = DosyaAdi(grup);
                    await Yaz(archive.CreateEntry($"{dosyaAdi}.pdf"), AylikPdf(grupSatirlari, yil, ay, kullanici), cancellationToken);
                    await Yaz(archive.CreateEntry($"{dosyaAdi}.xlsx"), AylikExcel(grupSatirlari, yil, ay, kullanici), cancellationToken);
                }
            }
            return stream.ToArray();
        }

        public async Task<byte[]> GiderRaporuPdfAsync(string kullanici, CancellationToken cancellationToken = default)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var giderler = await GiderleriSorgula(cancellationToken);
            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); page.Margin(24); page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("3K").FontSize(22).Bold().FontColor("#3584FC");
                        row.RelativeItem(4).AlignCenter().Text("FİNANS GİDER RAPORU").FontSize(15).Bold();
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
                    column.Item().PaddingTop(5).Text($"Kayıt sayısı: {giderler.Count}").FontColor("#64748B");
                });
                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(.8f); c.RelativeColumn(1.3f); c.RelativeColumn(2.5f); c.RelativeColumn(1f); c.RelativeColumn(.6f); c.RelativeColumn(1f); c.RelativeColumn(1f); c.RelativeColumn(1f); c.RelativeColumn(.8f); });
                        table.Header(header => { foreach (var baslik in new[] { "Tarih", "Kategori", "Açıklama", "Tutar", "Para", "Matrah", "KDV", "Toplam", "Durum" }) header.Cell().Background("#EEF2F7").Padding(5).Text(baslik).SemiBold(); });
                        foreach (var gider in giderler)
                        {
                            Cell(table, gider.Tarih.ToString("dd.MM.yyyy")); Cell(table, gider.Kategori); Cell(table, gider.Aciklama);
                            Cell(table, gider.Tutar.ToString("N2")); Cell(table, gider.ParaBirimi); Cell(table, gider.Matrah.ToString("N2"));
                            Cell(table, $"%{gider.KdvOrani:0.##} · {gider.KdvTutari:N2}"); Cell(table, gider.ToplamTutar.ToString("N2")); Cell(table, gider.IptalEdildi ? "İptal" : "Aktif");
                        }
                    });
                    foreach (var toplam in giderler.Where(g => !g.IptalEdildi).GroupBy(g => g.ParaBirimi).OrderBy(g => g.Key))
                        column.Item().AlignRight().PaddingTop(6).Text($"{toplam.Key} TOPLAM: Matrah {toplam.Sum(x => x.Matrah):N2} | KDV {toplam.Sum(x => x.KdvTutari):N2} | Genel {toplam.Sum(x => x.ToplamTutar):N2}").Bold();
                });
                page.Footer().Row(row => { row.RelativeItem().Text($"Raporu oluşturan: {kullanici}").FontColor("#64748B"); row.RelativeItem().AlignRight().Text(t => { t.Span("Sayfa "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); }); });
            })).GeneratePdf();
        }

        public async Task<byte[]> GiderRaporuExcelAsync(string kullanici, CancellationToken cancellationToken = default)
        {
            var giderler = await GiderleriSorgula(cancellationToken);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Giderler");
            sheet.Cell(1, 1).Value = "3K FİNANS GİDER RAPORU";
            sheet.Range(1, 1, 1, 10).Merge().Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#3584FC"));
            sheet.Cell(2, 1).Value = $"Raporu oluşturan: {kullanici} | {DateTime.Now:dd.MM.yyyy HH:mm}"; sheet.Range(2, 1, 2, 10).Merge();
            var headers = new[] { "Tarih", "Kategori", "Açıklama", "Tutar", "Para Birimi", "KDV %", "Matrah", "KDV", "Toplam", "Durum" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(4, column + 1).Value = headers[column];
            sheet.Range(4, 1, 4, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF")).Font.SetBold();
            for (var index = 0; index < giderler.Count; index++)
            {
                var row = index + 5; var gider = giderler[index];
                sheet.Cell(row, 1).Value = gider.Tarih; sheet.Cell(row, 2).Value = gider.Kategori; sheet.Cell(row, 3).Value = gider.Aciklama;
                sheet.Cell(row, 4).Value = gider.Tutar; sheet.Cell(row, 5).Value = gider.ParaBirimi; sheet.Cell(row, 6).Value = gider.KdvOrani;
                sheet.Cell(row, 7).Value = gider.Matrah; sheet.Cell(row, 8).Value = gider.KdvTutari; sheet.Cell(row, 9).Value = gider.ToplamTutar;
                sheet.Cell(row, 10).Value = gider.IptalEdildi ? "İptal" : "Aktif";
            }
            var toplamSatiri = giderler.Count + 6;
            foreach (var toplam in giderler.Where(g => !g.IptalEdildi).GroupBy(g => g.ParaBirimi).OrderBy(g => g.Key))
            {
                sheet.Cell(toplamSatiri, 5).Value = $"{toplam.Key} TOPLAM"; sheet.Cell(toplamSatiri, 5).Style.Font.SetBold();
                sheet.Cell(toplamSatiri, 7).Value = toplam.Sum(x => x.Matrah); sheet.Cell(toplamSatiri, 8).Value = toplam.Sum(x => x.KdvTutari); sheet.Cell(toplamSatiri, 9).Value = toplam.Sum(x => x.ToplamTutar); toplamSatiri++;
            }
            sheet.Range(4, 1, Math.Max(4, giderler.Count + 4), headers.Length).SetAutoFilter(); sheet.SheetView.FreezeRows(4);
            sheet.Column(1).Style.DateFormat.Format = "dd.MM.yyyy"; sheet.Columns(4, 9).Style.NumberFormat.Format = "#,##0.00"; sheet.Columns().AdjustToContents(8, 42);
            using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
        }

        public async Task<byte[]> SiparisDurumRaporuPdfAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum, string kullanici, CancellationToken cancellationToken = default)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var satirlar = await SiparisDurumSatirlari(baslangic, bitis, projeNo, isGrubu, durum, cancellationToken);
            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); page.Margin(20); page.DefaultTextStyle(x => x.FontSize(7).FontFamily(Fonts.Arial));
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("3K").FontSize(22).Bold().FontColor("#3584FC");
                        row.RelativeItem(4).AlignCenter().Text("SİPARİŞ DURUM RAPORU").FontSize(15).Bold();
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
                    column.Item().PaddingTop(5).Text(SiparisDurumFiltreMetni(baslangic, bitis, projeNo, isGrubu, durum)).FontColor("#64748B");
                });
                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(.6f); c.RelativeColumn(.7f); c.RelativeColumn(.8f); c.RelativeColumn(1f); c.RelativeColumn(.8f); c.RelativeColumn(1f); c.RelativeColumn(.7f); c.RelativeColumn(.8f); c.RelativeColumn(.8f); c.RelativeColumn(.9f); c.RelativeColumn(.8f); c.RelativeColumn(.8f); c.RelativeColumn(.9f); c.RelativeColumn(.7f); });
                        table.Header(header => { foreach (var baslik in new[] { "Dönem", "Proje", "İş Türü", "İş", "Sandık Tipi", "Ölçü (mm)", "Toplam", "Sipariş", "Açılmayan", "Sipariş Durumu", "Faturalanan", "Fatura Bekleyen", "Fatura Durumu", "Para" }) header.Cell().Background("#EEF2F7").Padding(4).Text(baslik).SemiBold(); });
                        foreach (var satir in satirlar)
                        {
                            Cell(table, satir.Donem); Cell(table, satir.ProjeNo); Cell(table, satir.IsGrubu); Cell(table, satir.IsAdi);
                            Cell(table, satir.SandikTipi ?? "-"); Cell(table, OlcuMetni(satir.Boy, satir.En, satir.Yukseklik));
                            Cell(table, satir.ToplamTutar.ToString("N2")); Cell(table, satir.SiparisTutari.ToString("N2")); Cell(table, satir.SiparisBekleyen.ToString("N2")); Cell(table, satir.SiparisDurumu);
                            Cell(table, satir.FaturalananTutar.ToString("N2")); Cell(table, satir.FaturaBekleyen.ToString("N2")); Cell(table, satir.FaturaDurumu); Cell(table, satir.ParaBirimi);
                        }
                    });
                    foreach (var toplam in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key))
                        column.Item().AlignRight().PaddingTop(6).Text($"{toplam.Key}: Toplam {toplam.Sum(x => x.ToplamTutar):N2} | Sipariş {toplam.Sum(x => x.SiparisTutari):N2} | Fatura {toplam.Sum(x => x.FaturalananTutar):N2}").Bold();
                });
                page.Footer().Row(row => { row.RelativeItem().Text($"Raporu oluşturan: {kullanici}").FontColor("#64748B"); row.RelativeItem().AlignRight().Text(t => { t.Span("Sayfa "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); }); });
            })).GeneratePdf();
        }

        public async Task<byte[]> SiparisDurumRaporuExcelAsync(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum, string kullanici, CancellationToken cancellationToken = default)
        {
            var satirlar = await SiparisDurumSatirlari(baslangic, bitis, projeNo, isGrubu, durum, cancellationToken);
            using var workbook = new XLWorkbook(); var sheet = workbook.Worksheets.Add("Sipariş Durumu");
            sheet.Cell(1, 1).Value = "3K SİPARİŞ DURUM RAPORU"; sheet.Range(1, 1, 1, 18).Merge().Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#3584FC"));
            sheet.Cell(2, 1).Value = SiparisDurumFiltreMetni(baslangic, bitis, projeNo, isGrubu, durum); sheet.Range(2, 1, 2, 18).Merge();
            sheet.Cell(3, 1).Value = $"Raporu oluşturan: {kullanici} | {DateTime.Now:dd.MM.yyyy HH:mm}"; sheet.Range(3, 1, 3, 18).Merge();
            var headers = new[] { "Dönem", "Proje", "İş Türü", "İş", "Sandık Tipi", "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Miktar", "Birim", "Toplam", "Sipariş", "Açılmayan", "Sipariş Durumu", "Faturalanan", "Fatura Bekleyen", "Fatura Durumu", "Para Birimi" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(5, column + 1).Value = headers[column];
            sheet.Range(5, 1, 5, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF")).Font.SetBold();
            for (var index = 0; index < satirlar.Count; index++)
            {
                var row = index + 6; var satir = satirlar[index];
                sheet.Cell(row, 1).Value = satir.Donem; sheet.Cell(row, 2).Value = satir.ProjeNo; sheet.Cell(row, 3).Value = satir.IsGrubu; sheet.Cell(row, 4).Value = satir.IsAdi;
                sheet.Cell(row, 5).Value = satir.SandikTipi ?? string.Empty; sheet.Cell(row, 6).Value = satir.Boy; sheet.Cell(row, 7).Value = satir.En; sheet.Cell(row, 8).Value = satir.Yukseklik;
                sheet.Cell(row, 9).Value = satir.Miktar; sheet.Cell(row, 10).Value = satir.Birim; sheet.Cell(row, 11).Value = satir.ToplamTutar; sheet.Cell(row, 12).Value = satir.SiparisTutari;
                sheet.Cell(row, 13).Value = satir.SiparisBekleyen; sheet.Cell(row, 14).Value = satir.SiparisDurumu; sheet.Cell(row, 15).Value = satir.FaturalananTutar;
                sheet.Cell(row, 16).Value = satir.FaturaBekleyen; sheet.Cell(row, 17).Value = satir.FaturaDurumu; sheet.Cell(row, 18).Value = satir.ParaBirimi;
            }
            sheet.Range(5, 1, Math.Max(5, satirlar.Count + 5), headers.Length).SetAutoFilter(); sheet.SheetView.FreezeRows(5);
            sheet.Columns(9, 16).Style.NumberFormat.Format = "#,##0.00"; sheet.Columns().AdjustToContents(8, 38);
            using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
        }

        private async Task<List<SiparisDurumSatiri>> SiparisDurumSatirlari(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum, CancellationToken cancellationToken)
        {
            var ilkProje = await _context.FinansIsKayitlari.AsNoTracking().Where(x => x.KaynakAktif).MinAsync(x => (DateTime?)x.UretimeAlinmaTarihi, cancellationToken);
            var ilkOzel = await _context.FinansOzelIsleri.AsNoTracking().MinAsync(x => (DateTime?)x.IsTarihi, cancellationToken);
            var ilkTarih = baslangic?.Date ?? new[] { ilkProje, ilkOzel }.Where(x => x.HasValue).Min() ?? DateTime.Today;
            var sonTarih = bitis?.Date ?? DateTime.Today;
            if (sonTarih < ilkTarih) return [];
            var satirlar = new List<SiparisDurumSatiri>();
            for (var donem = new DateTime(ilkTarih.Year, ilkTarih.Month, 1); donem <= new DateTime(sonTarih.Year, sonTarih.Month, 1); donem = donem.AddMonths(1))
            {
                foreach (var satir in await _aylikService.ListeleAsync(donem.Year, donem.Month, cancellationToken))
                {
                    if (satir.IptalEdildi || satir.UretimBaslangic.Date < ilkTarih || satir.UretimBaslangic.Date > sonTarih) continue;
                    var siparisDurumu = SiparisDurumu(satir); var faturaDurumu = FaturaDurumu(satir);
                    if (!string.IsNullOrWhiteSpace(projeNo) && !satir.ProjeNo.Contains(projeNo, StringComparison.CurrentCultureIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(isGrubu) && !satir.IsGrubu.Equals(isGrubu, StringComparison.CurrentCultureIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(durum) && !siparisDurumu.Equals(durum, StringComparison.CurrentCultureIgnoreCase) && !faturaDurumu.Equals(durum, StringComparison.CurrentCultureIgnoreCase)) continue;
                    satirlar.Add(new SiparisDurumSatiri($"{donem:MM.yyyy}", satir.ProjeNo, satir.IsGrubu, satir.IsAdi,
                        satir.SandikTipi, satir.Boy, satir.En, satir.Yukseklik, satir.Miktar, satir.Birim,
                        satir.ToplamTutar, satir.SiparisToplamTutar, Math.Max(0, satir.ToplamTutar - satir.SiparisToplamTutar), siparisDurumu,
                        satir.FaturalananToplamTutar, Math.Max(0, satir.SiparisToplamTutar - satir.FaturalananToplamTutar), faturaDurumu, satir.ParaBirimi));
                }
            }
            return satirlar.OrderBy(x => x.Donem).ThenBy(x => x.ProjeNo).ThenBy(x => x.IsGrubu).ToList();
        }

        private static string SiparisDurumu(_3K.Core.Models.FinansAylikIsDto satir) => satir.SiparisMiktari <= 0.000001m
            ? "Açılmadı" : satir.Miktar - satir.SiparisMiktari > 0.000001m ? "Kısmi Açıldı" : "Tam Açıldı";

        private static string FaturaDurumu(_3K.Core.Models.FinansAylikIsDto satir) => satir.FaturalananMiktar <= 0.000001m
            ? "Fatura Yok" : satir.SiparisMiktari - satir.FaturalananMiktar > 0.000001m ? "Kısmi Faturalandı" : "Faturalandı";

        private static string SiparisDurumFiltreMetni(DateTime? baslangic, DateTime? bitis, string? projeNo, string? isGrubu, string? durum) =>
            $"Tarih: {(baslangic.HasValue || bitis.HasValue ? $"{baslangic:dd.MM.yyyy} - {bitis:dd.MM.yyyy}" : "Tüm dönemler")} | Proje: {projeNo ?? "Tümü"} | İş türü: {isGrubu ?? "Tümü"} | Durum: {durum ?? "Tümü"}";

        private async Task<List<_3K.Core.Models.FinansAylikIsDto>> AylikSatirlar(int yil, int ay, string[]? gruplar, CancellationToken cancellationToken)
        {
            var satirlar = (await _aylikService.ListeleAsync(yil, ay, cancellationToken)).Where(x => !x.IptalEdildi).ToList();
            var secilenler = gruplar?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return secilenler?.Count > 0 ? satirlar.Where(x => secilenler.Contains(x.IsGrubu)).ToList() : satirlar;
        }

        private static byte[] AylikPdf(IReadOnlyList<_3K.Core.Models.FinansAylikIsDto> satirlar, int yil, int ay, string kullanici)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(document => document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); page.Margin(24); page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("3K").FontSize(22).Bold().FontColor("#3584FC");
                        row.RelativeItem(4).AlignCenter().Text($"AYLIK FİNANS RAPORU - {ay:00}.{yil}").FontSize(15).Bold();
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });
                    column.Item().PaddingTop(5).Text($"Gruplar: {(satirlar.Count == 0 ? "Kayıt yok" : string.Join(", ", satirlar.Select(x => x.IsGrubu).Distinct()))}").FontColor("#64748B");
                });
                page.Content().PaddingVertical(12).Column(column =>
                {
                    foreach (var grup in satirlar.GroupBy(x => x.IsGrubu))
                    {
                        column.Item().PaddingTop(8).Text(grup.Key).FontSize(11).Bold().FontColor("#3584FC");
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(1f); c.RelativeColumn(1.5f); c.RelativeColumn(1f); c.RelativeColumn(1.2f); c.RelativeColumn(.6f); c.RelativeColumn(.8f); c.RelativeColumn(.9f); c.RelativeColumn(.9f); c.RelativeColumn(.9f); c.RelativeColumn(.9f); c.RelativeColumn(1f); });
                            table.Header(h => { foreach (var baslik in new[] { "Proje", "İş", "Sandık Tipi", "Ölçü (mm)", "Sandık", "Miktar", "Tarife", "Net", "KDV", "Toplam", "Durum" }) h.Cell().Background("#EEF2F7").Padding(4).Text(baslik).SemiBold(); });
                            foreach (var satir in grup)
                            {
                                Cell(table, satir.ProjeNo); Cell(table, satir.IsAdi); Cell(table, satir.SandikTipi ?? "-"); Cell(table, OlcuMetni(satir.Boy, satir.En, satir.Yukseklik));
                                Cell(table, satir.SandikAdedi.ToString("0.##")); Cell(table, $"{satir.Miktar:0.###} {satir.Birim}");
                                Cell(table, $"{satir.BirimFiyat:0.00} {satir.ParaBirimi}"); Cell(table, satir.NetTutar.ToString("0.00")); Cell(table, satir.KdvTutari.ToString("0.00"));
                                Cell(table, $"{satir.ToplamTutar:0.00} {satir.ParaBirimi}"); Cell(table, satir.Durum);
                            }
                        });
                    }
                    foreach (var toplam in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key))
                        column.Item().AlignRight().PaddingTop(6).Text($"{toplam.Key} TOPLAM: Net {toplam.Sum(x => x.NetTutar):N2} | KDV {toplam.Sum(x => x.KdvTutari):N2} | Genel {toplam.Sum(x => x.ToplamTutar):N2}").Bold();
                });
                page.Footer().Row(row => { row.RelativeItem().Text($"Raporu oluşturan: {kullanici}").FontColor("#64748B"); row.RelativeItem().AlignRight().Text(t => { t.Span("Sayfa "); t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); }); });
            })).GeneratePdf();
        }

        private static byte[] AylikExcel(IReadOnlyList<_3K.Core.Models.FinansAylikIsDto> satirlar, int yil, int ay, string kullanici)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Aylık Finans");
            sheet.Cell(1, 1).Value = $"3K AYLIK FİNANS RAPORU - {ay:00}.{yil}";
            sheet.Range(1, 1, 1, 20).Merge().Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#3584FC"));
            sheet.Cell(2, 1).Value = $"Raporu oluşturan: {kullanici} | {DateTime.Now:dd.MM.yyyy HH:mm}"; sheet.Range(2, 1, 2, 20).Merge();
            var headers = new[] { "Grup", "Proje", "İş", "Sandık Tipi", "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Başlangıç", "Bitiş", "Sandık", "Miktar", "Birim", "Tarife", "Para Birimi", "KDV %", "Net", "KDV", "Toplam", "PO", "Fatura" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(4, column + 1).Value = headers[column];
            sheet.Range(4, 1, 4, headers.Length).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF")).Font.SetBold();
            for (var index = 0; index < satirlar.Count; index++)
            {
                var row = index + 5; var satir = satirlar[index];
                sheet.Cell(row, 1).Value = satir.IsGrubu; sheet.Cell(row, 2).Value = satir.ProjeNo; sheet.Cell(row, 3).Value = satir.IsAdi;
                sheet.Cell(row, 4).Value = satir.SandikTipi ?? string.Empty; sheet.Cell(row, 5).Value = satir.Boy; sheet.Cell(row, 6).Value = satir.En; sheet.Cell(row, 7).Value = satir.Yukseklik;
                sheet.Cell(row, 8).Value = satir.UretimBaslangic; sheet.Cell(row, 9).Value = satir.UretimBitis; sheet.Cell(row, 10).Value = satir.SandikAdedi;
                sheet.Cell(row, 11).Value = satir.Miktar; sheet.Cell(row, 12).Value = satir.Birim; sheet.Cell(row, 13).Value = satir.BirimFiyat;
                sheet.Cell(row, 14).Value = satir.ParaBirimi; sheet.Cell(row, 15).Value = satir.KdvOrani; sheet.Cell(row, 16).Value = satir.NetTutar;
                sheet.Cell(row, 17).Value = satir.KdvTutari; sheet.Cell(row, 18).Value = satir.ToplamTutar; sheet.Cell(row, 19).Value = string.Join(", ", satir.PoNumaralari);
                sheet.Cell(row, 20).Value = string.Join(", ", satir.FaturaNumaralari);
            }
            var toplamSatiri = satirlar.Count + 6;
            foreach (var toplam in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key))
            {
                sheet.Cell(toplamSatiri, 14).Value = $"{toplam.Key} TOPLAM"; sheet.Cell(toplamSatiri, 14).Style.Font.SetBold();
                sheet.Cell(toplamSatiri, 16).Value = toplam.Sum(x => x.NetTutar); sheet.Cell(toplamSatiri, 17).Value = toplam.Sum(x => x.KdvTutari); sheet.Cell(toplamSatiri, 18).Value = toplam.Sum(x => x.ToplamTutar); toplamSatiri++;
            }
            sheet.Range(4, 1, Math.Max(4, satirlar.Count + 4), headers.Length).SetAutoFilter(); sheet.SheetView.FreezeRows(4);
            sheet.Columns(8, 9).Style.DateFormat.Format = "dd.MM.yyyy"; sheet.Columns(13, 18).Style.NumberFormat.Format = "#,##0.00"; sheet.Columns().AdjustToContents(8, 36);
            using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
        }

        private static async Task Yaz(ZipArchiveEntry entry, byte[] content, CancellationToken cancellationToken)
        {
            await using var target = entry.Open();
            await target.WriteAsync(content, cancellationToken);
        }

        private static string DosyaAdi(string value)
        {
            var gecersiz = Path.GetInvalidFileNameChars();
            return string.Concat(value.Select(c => gecersiz.Contains(c) ? '_' : c)).Replace(' ', '_');
        }

        private async Task<List<RaporSatiri>> Sorgula(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri, CancellationToken cancellationToken)
        {
            var query = _context.FinansIsKayitlari.AsNoTracking().AsQueryable();
            if (baslangic.HasValue) query = query.Where(k => k.UretimeAlinmaTarihi >= baslangic.Value.Date);
            if (bitis.HasValue) query = query.Where(k => k.UretimeAlinmaTarihi < bitis.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(projeNo)) query = query.Where(k => k.ProjeNo.Contains(projeNo));
            if (!string.IsNullOrWhiteSpace(musteri)) query = query.Where(k => k.Musteri.Contains(musteri));
            if (isTurleri?.Length > 0) query = query.Where(k => isTurleri.Contains((int)k.IsTuru));
            return await query.OrderBy(k => k.ProjeNo).ThenBy(k => k.IsTuru).ThenBy(k => k.SandikNo)
                .Select(k => new RaporSatiri(k.ProjeNo, k.Musteri, k.SandikNo, k.SandikAdi, k.SandikTipi, k.Boy, k.En, k.Yukseklik, k.IsTuru, k.Adet, k.BirimM3, k.Adet * k.BirimM3,
                    k.SiparisKalemleri.Where(s => s.Siparis.Durum != FinansSiparisDurumu.IptalEdildi).Sum(s => s.M3),
                    k.SiparisKalemleri.SelectMany(s => s.FaturaKalemleri).Where(f => f.Fatura.Durum != FinansFaturaDurumu.IptalEdildi).Sum(f => f.M3), k.UretimeAlinmaTarihi))
                .ToListAsync(cancellationToken);
        }

        private async Task<List<GiderRaporSatiri>> GiderleriSorgula(CancellationToken cancellationToken) =>
            await _context.FinansGiderleri.AsNoTracking().Include(g => g.Kategori)
                .OrderByDescending(g => g.Tarih).ThenByDescending(g => g.Id)
                .Select(g => new GiderRaporSatiri(g.Tarih, g.Kategori.Ad, g.Aciklama, g.Tutar, g.ParaBirimi,
                    g.KdvOrani, g.Matrah, g.KdvTutari, g.ToplamTutar, g.IptalEdildi))
                .ToListAsync(cancellationToken);

        private static void Cell(TableDescriptor table, string value, bool bold = false)
        {
            var cell = table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4);
            if (bold) cell.Text(value).Bold(); else cell.Text(value);
        }

        private static string FiltreMetni(DateTime? baslangic, DateTime? bitis, string? projeNo, string? musteri, int[]? isTurleri) =>
            $"Tarih: {baslangic:dd.MM.yyyy} - {bitis:dd.MM.yyyy} | Proje: {projeNo ?? "Tümü"} | Müşteri: {musteri ?? "Tümü"} | İş türleri: {(isTurleri?.Length > 0 ? string.Join(", ", isTurleri) : "Tümü")}";

        private static string OlcuMetni(decimal? boy, decimal? en, decimal? yukseklik) =>
            boy > 0 && en > 0 && yukseklik > 0 ? $"{boy:0.##} × {en:0.##} × {yukseklik:0.##}" : "-";

        private record RaporSatiri(string ProjeNo, string Musteri, string SandikNo, string SandikAdi, string? SandikTipi,
            decimal? Boy, decimal? En, decimal? Yukseklik, FinansIsTuru IsTuru, decimal Adet, decimal BirimM3, decimal ToplamM3,
            decimal SiparisM3, decimal FaturaM3, DateTime UretimeAlinmaTarihi);
        private record GiderRaporSatiri(DateTime Tarih, string Kategori, string Aciklama, decimal Tutar, string ParaBirimi,
            decimal KdvOrani, decimal Matrah, decimal KdvTutari, decimal ToplamTutar, bool IptalEdildi);
        private record SiparisDurumSatiri(string Donem, string ProjeNo, string IsGrubu, string IsAdi, string? SandikTipi,
            decimal? Boy, decimal? En, decimal? Yukseklik, decimal Miktar, string Birim,
            decimal ToplamTutar, decimal SiparisTutari, decimal SiparisBekleyen, string SiparisDurumu,
            decimal FaturalananTutar, decimal FaturaBekleyen, string FaturaDurumu, string ParaBirimi);
    }
}