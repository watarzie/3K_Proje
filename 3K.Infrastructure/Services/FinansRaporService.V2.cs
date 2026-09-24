using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services;

public sealed partial class FinansRaporService
{
    public async Task<byte[]> OzetRaporAsync(string tur, bool excel, FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var rows = new List<object?[]>();
        string[] headers;
        string title;
        if (tur is "siparis-bekleyen" or "fatura-bekleyen")
        {
            title = tur == "siparis-bekleyen" ? "Sipariş Bekleyen Yaşlandırma" : "Fatura Bekleyen Yaşlandırma";
            headers = ["Proje", "İş", "PO", "Başlangıç", "Gün", "Yaş Grubu", "Para Birimi", "Kalan Net"];
            var pageNumber = 1;
            while (true)
            {
                var page = await _finansService.YaslandirmaAsync(filtre.Bitis ?? TurkeyTime.Now.Date, 0, filtre with { PageNumber = pageNumber, PageSize = 250 }, cancellationToken);
                if (page.TotalCount > 20000) throw new InvalidOperationException("Yaşlandırma raporu 20.000 satırı aşamaz; filtreyi daraltın.");
                foreach (var x in page.Items.Where(x => x.Asama == (tur == "siparis-bekleyen" ? "Sipariş" : "Fatura")))
                    rows.Add([x.ProjeNo, x.IsAdi, x.PoNumarasi, x.Baslangic, x.Gun, x.Grup, x.ParaBirimi, x.KalanNetTutar]);
                if (!page.HasNextPage) break;
                pageNumber++;
            }
        }
        else
        {
            var report = await _finansService.RaporVerisiAsync(filtre with { IptalEdilenleriDahilEt = false }, cancellationToken);
            var expenses = report.Giderler.Where(x => !x.IptalEdildi && !x.AvansMi).ToArray();
            if (tur == "genel")
            {
                title = "Genel Finans Özeti";
                headers = ["Para Birimi", "İş Net Bedeli", "Faturalanan Net Gelir", "Net Gider", "Fark", "Tahmini Kâr"];
                var currencies = report.Isler.Select(x => x.ParaBirimi).Concat(report.GelirToplamlari.Select(x => x.ParaBirimi)).Concat(report.GiderToplamlari.Select(x => x.ParaBirimi)).Distinct().Order();
                foreach (var c in currencies)
                {
                    var work = report.Isler.Where(x => x.ParaBirimi == c).Sum(x => x.NetTutar);
                    var income = report.GelirToplamlari.Where(x => x.ParaBirimi == c).Sum(x => x.NetTutar);
                    var cost = report.GiderToplamlari.Where(x => x.ParaBirimi == c).Sum(x => x.NetTutar);
                    rows.Add([c, work, income, cost, income - cost, work - cost]);
                }
            }
            else if (tur == "gider-kategori")
            {
                title = "Gider Kategori Özeti"; headers = ["Kategori", "Para Birimi", "Kayıt", "Net Gider", "KDV", "Brüt"];
                foreach (var g in expenses.GroupBy(x => new { x.Kategori, x.ParaBirimi }).OrderBy(x => x.Key.Kategori).ThenBy(x => x.Key.ParaBirimi))
                    rows.Add([g.Key.Kategori, g.Key.ParaBirimi, g.Count(), g.Sum(x => x.Matrah), g.Sum(x => x.KdvTutari), g.Sum(x => x.ToplamTutar)]);
            }
            else if (tur == "proje-maliyet")
            {
                title = "Proje Maliyet Özeti"; headers = ["Proje", "Para Birimi", "İş Net Bedeli", "Faturalanan Net", "Net Maliyet", "Tahmini Kâr"];
                var keys = report.Isler.Select(x => (x.ProjeNo, x.ParaBirimi)).Concat(expenses.Select(x => (x.ProjeNo, x.ParaBirimi))).Distinct().OrderBy(x => x.ProjeNo).ThenBy(x => x.ParaBirimi);
                foreach (var key in keys)
                {
                    var works = report.Isler.Where(x => x.ProjeNo == key.ProjeNo && x.ParaBirimi == key.ParaBirimi).ToArray();
                    var cost = expenses.Where(x => x.ProjeNo == key.ProjeNo && x.ParaBirimi == key.ParaBirimi).Sum(x => x.Matrah);
                    var work = works.Sum(x => x.NetTutar);
                    rows.Add([key.ProjeNo, key.ParaBirimi, work, works.Sum(x => x.FaturalananNetTutar), cost, work - cost]);
                }
            }
            else throw new InvalidOperationException("Rapor türü genel, gider-kategori, proje-maliyet, siparis-bekleyen veya fatura-bekleyen olmalıdır.");
        }
        if (excel)
        {
            using var workbook = new XLWorkbook(); var sheet = workbook.Worksheets.Add("Finans Özeti");
            WriteHeaders(sheet, headers); var row = 2;
            foreach (var values in rows) WriteRow(sheet, row++, values);
            FormatSheet(sheet, headers.Length, row - 1);
            var info = workbook.Worksheets.Add("Kapsam");
            info.Cell(1, 1).Value = title;
            info.Cell(2, 1).Value = $"Finans tarihi: {filtre.Baslangic:dd.MM.yyyy}–{filtre.Bitis:dd.MM.yyyy}; yaşlandırma tüm geçmişi kapsar.";
            info.Cell(3, 1).Value = "Tüm filtre sonuçları; para birimleri ayrı; avanslar ve iptal edilmiş kayıtlar maliyete dahil değildir.";
            info.Columns().AdjustToContents(); return Save(workbook);
        }
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page =>
        {
            ConfigurePage(page);
            page.Header().Column(column =>
            {
                column.Item().Text(title).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Finans tarihi: {filtre.Baslangic:dd.MM.yyyy}–{filtre.Bitis:dd.MM.yyyy} | Net tutarlar, ayrı para birimleri; yaşlandırma tüm geçmiş.").FontSize(8);
            });
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns => { foreach (var _ in headers) columns.RelativeColumn(); });
                PdfHeader(table, headers);
                foreach (var values in rows)
                    foreach (var value in values)
                        PdfCell(table, value switch { null => "—", decimal n => Format(n), DateTime date => date.ToString("dd.MM.yyyy"), _ => value.ToString() ?? "" });
            });
            AddFooter(page);
        })).GeneratePdf();
    }
}
