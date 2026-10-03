using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Alacak (vade/tahsilat) raporu (D3, §3.14): fatura listesiyle aynı filtreler; satırlar faturadır, özet para birimi
/// bazındadır ve para birimleri toplanmaz. Ödenmemiş alacak gecikme gününe göre kovalara ayrılır.
/// </summary>
internal sealed partial class FinansRaporService
{
    private static readonly string[] AlacakBasliklari =
    [
        "Fatura No", "PO No", "Proje No", "Fatura Tarihi", "Vade Tarihi", "Para Birimi", "Brüt Tutar",
        "Ödeme Durumu", "Ödeme Tarihi", "Gün", "Gecikme Grubu", "Ödeme Açıklaması"
    ];

    private static readonly string[] AlacakOzetBasliklari =
    [
        "Para Birimi", "Ödenmemiş", "Ödenmemiş Adet", "Vadesi Gelmedi", "1–30 Gün", "31–60 Gün", "61–90 Gün", "90+ Gün",
        "Ödenen", "Zamanında Ödenen Adet", "Geç Ödenen Adet"
    ];

    public async Task<byte[]> AlacakExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var faturalar = await AlacakFaturalariAsync(filtre, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Alacaklar");
        WriteHeaders(sheet, AlacakBasliklari);
        var row = 2;
        foreach (var satir in AlacakSatirlari(faturalar))
            WriteRow(sheet, row++, satir);
        FormatSheet(sheet, AlacakBasliklari.Length, row - 1);

        var ozet = workbook.Worksheets.Add("Özet");
        WriteHeaders(ozet, AlacakOzetBasliklari);
        row = 2;
        foreach (var satir in AlacakOzeti(faturalar))
            WriteRow(ozet, row++, satir);
        FormatSheet(ozet, AlacakOzetBasliklari.Length, row - 1);
        return Save(workbook);
    }

    public async Task<byte[]> AlacakPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var faturalar = await AlacakFaturalariAsync(filtre, cancellationToken);
        var satirlar = AlacakSatirlari(faturalar).ToList();
        var ozet = AlacakOzeti(faturalar).ToList();
        QuestPDF.Settings.License = LicenseType.Community;
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Column(column =>
                {
                    column.Item().Text("Alacak (Vade / Tahsilat) Raporu").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm} · Fatura: {satirlar.Count}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in AlacakOzetBasliklari) columns.RelativeColumn();
                        });
                        PdfHeader(table, AlacakOzetBasliklari);
                        foreach (var satir in ozet)
                            foreach (var deger in satir)
                                PdfCell(table, PdfMetni(deger));
                    });
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(0.7f);
                            columns.RelativeColumn(1.1f); columns.RelativeColumn(1.2f); columns.RelativeColumn();
                            columns.RelativeColumn(0.6f); columns.RelativeColumn(0.9f); columns.RelativeColumn(1.6f);
                        });
                        PdfHeader(table, AlacakBasliklari);
                        foreach (var satir in satirlar)
                            foreach (var deger in satir)
                                PdfCell(table, PdfMetni(deger));
                    });
                });
                AddFooter(page);
            });
        });
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    /// <summary>Fatura listesinin filtreleriyle bütün sayfalar; rapor 20.000 faturayı aşamaz.</summary>
    private async Task<List<FinansFaturaModel>> AlacakFaturalariAsync(FinansListeFiltre filtre, CancellationToken cancellationToken)
    {
        var sonuc = new List<FinansFaturaModel>();
        var pageNumber = 1;
        while (true)
        {
            var page = await _faturalar.FaturalarAsync(filtre with { PageNumber = pageNumber, PageSize = 250, IptalEdilenleriDahilEt = false }, cancellationToken);
            if (page.TotalCount > FinansSorgulari.RaporKayitSiniri)
                throw IsKuraliException.Cakisma("Alacak raporu 20.000 faturayı aşamaz; filtreyi daraltın.");
            sonuc.AddRange(page.Items);
            if (!page.HasNextPage) break;
            pageNumber++;
        }
        return sonuc.OrderBy(x => x.OdendiMi).ThenBy(x => x.VadeTarihi).ThenBy(x => x.Id).ToList();
    }

    private static IEnumerable<object?[]> AlacakSatirlari(IEnumerable<FinansFaturaModel> faturalar)
        => faturalar.SelectMany(fatura => fatura.Tutarlar.Select(tutar => new object?[]
        {
            fatura.FaturaNumarasi, fatura.PoNumarasi, fatura.ProjeNo, fatura.FaturaTarihi, fatura.VadeTarihi, tutar.ParaBirimi,
            tutar.ToplamTutar, FinansVadeKurallari.DurumMetni(fatura.OdemeDurumu), fatura.OdemeTarihi, fatura.VadeFarkiGun,
            fatura.OdendiMi ? null : FinansVadeKurallari.GecikmeGrubu(fatura.VadeFarkiGun), fatura.OdemeAciklamasi
        }));

    private static IEnumerable<object?[]> AlacakOzeti(IReadOnlyCollection<FinansFaturaModel> faturalar)
    {
        var satirlar = faturalar.SelectMany(fatura => fatura.Tutarlar.Select(tutar => (Fatura: fatura, tutar.ParaBirimi, Tutar: tutar.ToplamTutar)))
            .ToList();
        foreach (var grup in satirlar.GroupBy(x => x.ParaBirimi).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var acik = grup.Where(x => !x.Fatura.OdendiMi).ToList();
            var odenen = grup.Where(x => x.Fatura.OdendiMi).ToList();
            decimal Kova(string ad) => acik.Where(x => FinansVadeKurallari.GecikmeGrubu(x.Fatura.VadeFarkiGun) == ad).Sum(x => x.Tutar);
            yield return
            [
                grup.Key, acik.Sum(x => x.Tutar), acik.Count,
                Kova(FinansVadeKurallari.GecikmeGruplari[0]), Kova(FinansVadeKurallari.GecikmeGruplari[1]),
                Kova(FinansVadeKurallari.GecikmeGruplari[2]), Kova(FinansVadeKurallari.GecikmeGruplari[3]),
                Kova(FinansVadeKurallari.GecikmeGruplari[4]),
                odenen.Sum(x => x.Tutar),
                odenen.Count(x => x.Fatura.OdemeDurumu == FinansOdemeDurumu.ZamanindaOdendi),
                odenen.Count(x => x.Fatura.OdemeDurumu == FinansOdemeDurumu.GecOdendi)
            ];
        }
    }

    private static string PdfMetni(object? deger) => deger switch
    {
        null => string.Empty,
        DateTime tarih => tarih.ToString("dd.MM.yyyy"),
        decimal sayi => Format(sayi),
        _ => Convert.ToString(deger, System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) ?? string.Empty
    };
}
