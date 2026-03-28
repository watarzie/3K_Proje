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
    /// İş akışı 9: PDF oluşturma
    /// Sistem dijital operasyon verilerini ilgili alanlara işler:
    /// paketleyen baş harfleri, kontrol baş harfleri, açıklama/remarks
    /// </summary>
    public class PdfService : IPdfService
    {
        private readonly AppDbContext _context;

        public PdfService(AppDbContext context)
        {
            _context = context;
        }

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

                    page.Header().Text($"Çeki Listesi — Proje: {proje.ProjeNo} | Müşteri: {proje.Musteri}")
                        .Bold().FontSize(12).AlignCenter();

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
                                        columns.RelativeColumn(2);    // Barkod
                                        columns.RelativeColumn(3);    // Açıklama
                                        columns.ConstantColumn(50);   // İstenen
                                        columns.ConstantColumn(50);   // Konulan
                                        columns.ConstantColumn(50);   // Eksik
                                        columns.ConstantColumn(30);   // Birim
                                        columns.ConstantColumn(40);   // Paketleyen
                                        columns.ConstantColumn(40);   // Kontrol
                                        columns.RelativeColumn(2);    // Remarks
                                    });

                                    // Başlık satırı
                                    table.Header(header =>
                                    {
                                        header.Cell().Border(1).Padding(2).Text("#").Bold();
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

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }
    }
}
