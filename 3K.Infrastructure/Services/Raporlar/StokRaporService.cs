using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Stok modülündeki ürünlerin PDF raporu.</summary>
internal sealed class StokRaporService(AppDbContext context) : IStokRaporService
{
    public async Task<byte[]> StokRaporuPdfOlusturAsync(int? durumId = null)
    {
        var stoklar = await StokListeFiltresi.Uygula(context.StokKayitlari, null, durumId)
            .OrderBy(s => s.MalzemeKodu)
            .ToListAsync();
        var kapsam = durumId switch
        {
            (int)StokDurum.Aktif => " (Aktif Stok)",
            (int)StokDurum.Tukendi => " (Tükendiler)",
            _ => string.Empty
        };

        QuestPDF.Settings.License = LicenseType.Community;

        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var raporTarihi = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
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
                            col.Item().Text("Stok Raporu" + kapsam).Bold().FontSize(16).FontColor(headerText);
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
