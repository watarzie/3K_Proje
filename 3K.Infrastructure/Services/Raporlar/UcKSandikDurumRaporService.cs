using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>3K sandık durum raporu (PDF).</summary>
internal sealed class UcKSandikDurumRaporService(AppDbContext context) : IUcKSandikDurumRaporService
{
    public async Task<byte[]> UcKSandikDurumRaporuPdfOlusturAsync(int projeId)
    {
        var proje = await context.Projeler
            .AsNoTracking()
            .Include(p => p.ProjeTipiLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadı: {projeId}");

        var sandiklar = await context.Sandiklar
            .AsNoTracking()
            .Include(s => s.DurumLookup)
            .Include(s => s.DepoLokasyonLookup)
            .Include(s => s.SandikIcerikleri)
            .Where(s => s.ProjeId == projeId)
            .ToListAsync();

        var raporSandiklari = sandiklar
            .OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.Id)
            .ToList();

        QuestPDF.Settings.License = LicenseType.Community;

        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        var toplamSandik = raporSandiklari.Count;
        var kapandi = raporSandiklari.Count(s => s.DurumId == (int)SandikDurum.Kapandi);
        var sevkEdildi = raporSandiklari.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);
        var hazirlaniyor = raporSandiklari.Count(s => s.DurumId == (int)SandikDurum.Hazirlaniyor);

        static string DecimalMetni(decimal? value)
        {
            return value.HasValue ? RaporBicimleri.FormatAdet(value.Value) : "-";
        }

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
                            col.Item().Text("3K Sandık Durum Raporu").Bold().FontSize(16).FontColor(headerText);
                            col.Item().Text("3K Sevkiyat Yönetim Sistemi - Sandık Durum Özeti").FontSize(7).FontColor(Colors.Grey.Lighten3);
                        });
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Rapor Tarihi: {raporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        });
                    });

                    headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                        {
                            col.Item().Text("PROJE").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(3).Text(proje.ProjeNo).Bold().FontSize(13);
                            col.Item().Text($"Müşteri: {proje.Musteri ?? "-"}").FontSize(8);
                        });

                        row.ConstantItem(10);

                        row.RelativeItem().Border(1).BorderColor(tableBorderColor).Padding(8).Column(col =>
                        {
                            col.Item().Text("SANDIK ÖZETİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
                            col.Item().PaddingTop(3).Text($"{toplamSandik} sandık").Bold().FontSize(13);
                            col.Item().Text($"Kapandı: {kapandi} / Sevk Edildi: {sevkEdildi} / Hazırlanıyor: {hazirlaniyor}").FontSize(8);
                        });
                    });

                    headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(tableBorderColor);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(28);
                        columns.ConstantColumn(56);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(1.15f);
                        columns.ConstantColumn(48);
                        columns.ConstantColumn(48);
                        columns.ConstantColumn(58);
                        columns.ConstantColumn(56);
                        columns.ConstantColumn(56);
                        columns.ConstantColumn(48);
                    });

                    table.Header(header =>
                    {
                        void HeaderCell(IContainer c, string text)
                        {
                            c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(4)
                                .Text(text).Bold().FontSize(7).FontColor(headerText);
                        }

                        HeaderCell(header.Cell(), "#");
                        HeaderCell(header.Cell(), "Sandık No");
                        HeaderCell(header.Cell(), "Sandık İsmi");
                        HeaderCell(header.Cell(), "Durum");
                        HeaderCell(header.Cell(), "Lokasyon");
                        HeaderCell(header.Cell(), "Net Kg");
                        HeaderCell(header.Cell(), "Gross Kg");
                        HeaderCell(header.Cell(), "Boy");
                        HeaderCell(header.Cell(), "En");
                        HeaderCell(header.Cell(), "Yükseklik");
                        HeaderCell(header.Cell(), "Ürün");
                    });

                    var index = 1;
                    foreach (var sandik in raporSandiklari)
                    {
                        var bg = index % 2 == 0 ? altRowBg : "#FFFFFF";
                        var durum = sandik.DurumLookup?.Deger ?? sandik.DurumId.ToString();
                        var lokasyon = sandik.DepoLokasyonLookup?.Deger ?? "-";

                        void DataCell(IContainer c, string text, bool bold = false)
                        {
                            var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(3);
                            if (bold)
                                cell.Text(text).FontSize(7).Bold();
                            else
                                cell.Text(text).FontSize(7);
                        }

                        DataCell(table.Cell(), index.ToString());
                        DataCell(table.Cell(), sandik.SandikNo, bold: true);
                        DataCell(table.Cell(), string.IsNullOrWhiteSpace(sandik.Ad) ? "-" : sandik.Ad);
                        DataCell(table.Cell(), durum, bold: true);
                        DataCell(table.Cell(), lokasyon);
                        DataCell(table.Cell(), DecimalMetni(sandik.NetKg));
                        DataCell(table.Cell(), DecimalMetni(sandik.GrossKg));
                        DataCell(table.Cell(), DecimalMetni(sandik.Boy));
                        DataCell(table.Cell(), DecimalMetni(sandik.En));
                        DataCell(table.Cell(), DecimalMetni(sandik.Yukseklik));
                        DataCell(table.Cell(), sandik.SandikIcerikleri.Count.ToString());

                        index++;
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"3K Ambalaj - 3K Sandık Durum Raporu | Proje: {proje.ProjeNo}").FontSize(7).FontColor(Colors.Grey.Medium);
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
