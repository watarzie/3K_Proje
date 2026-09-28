using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Saha sandık PDF'leri: tek sandık ve bir saha projesinin bütün sandıkları.</summary>
internal sealed class SahaSandikRaporService(AppDbContext context) : ISahaSandikRaporService
{
    /// <summary>
    /// QuestPDF ile belirli bir saha sandığına ait PDF raporu oluşturur.
    /// </summary>
    public async Task<byte[]> SahaSandikPdfOlusturAsync(int sandikId)
    {
        var sandik = await context.Sandiklar
            .Include(s => s.Proje)
            .Include(s => s.DurumLookup)
            .Include(s => s.TipLookup)
            .Include(s => s.DepoLokasyonLookup)
            .Include(s => s.SandikIcerikleri)
                .ThenInclude(si => si.CekiSatiri)
                    .ThenInclude(cs => cs!.Ceki)
                        .ThenInclude(c => c!.Proje)
            .Include(s => s.SandikIcerikleri)
                .ThenInclude(si => si.CekiSatiri)
                    .ThenInclude(cs => cs!.BirimLookup)
            .Include(s => s.SandikIcerikleri)
                .ThenInclude(si => si.BirimLookup)
            .FirstOrDefaultAsync(s => s.Id == sandikId);

        if (sandik == null)
            throw new KayitBulunamadiException($"Sandık bulunamadı: {sandikId}");

        var yedekProjesiMi = sandik.Proje?.ProjeTipiId == (int)ProjeTipi.Yedek;
        var kullaniciDict = await context.Kullanicilar.ToDictionaryAsync(k => k.Id.ToString(), k => k.AdSoyad);

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
                    var projeTipiStr = sandik.Proje?.ProjeTipiId == (int)ProjeTipi.Yedek ? "Yedek" : "Saha";
                    headerCol.Item().Text($"{projeTipiStr} Sandığı İçerik Raporu")
                        .Bold().FontSize(16).AlignCenter().FontColor(Colors.Blue.Darken2);
                    
                    headerCol.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"Sandık No: {sandik.SandikNo}").Bold();
                            col.Item().Text($"Sandık Adı / Crate Name: {RaporBicimleri.GetRaporSandikAdi(sandik)}").FontSize(8);
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
                            col.Item().Text($"Ölçüler (B/E/Y): {sandik.Boy ?? 0} x {sandik.En ?? 0} x {sandik.Yukseklik ?? 0} mm");
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
                        columns.RelativeColumn(1.4f); // Proje No
                        columns.RelativeColumn(1.5f); // Barkod
                        columns.RelativeColumn(3); // Ürün Adı
                        columns.RelativeColumn(1); // Miktar
                        columns.RelativeColumn(1); // Birim
                        columns.RelativeColumn(2); // Açıklama
                        columns.RelativeColumn(1.4f); // Ekleyen
                        columns.RelativeColumn(1.4f); // Tarih
                    });

                    // Header Row (TR + EN)
                    table.Header(header =>
                    {
                        void BiHeader(IContainer c, string tr, string en = "")
                        {
                            c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Column(col =>
                            {
                                col.Item().Text(tr).Bold();
                                if (!string.IsNullOrEmpty(en))
                                    col.Item().Text(en).FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                            });
                        }

                        BiHeader(header.Cell(), "Proje No", "Project No");
                        BiHeader(header.Cell(), "Barkod", "Barcode");
                        BiHeader(header.Cell(), "Ürün Adı / Tanımı", "Product Name");
                        BiHeader(header.Cell(), "Miktar", "Quantity");
                        BiHeader(header.Cell(), "Birim", "Unit");
                        BiHeader(header.Cell(), "Açıklama", "Description");
                        BiHeader(header.Cell(), "Ekleyen", "Added By");
                        BiHeader(header.Cell(), "Tarih", "Date");
                    });

                    // Data Rows
                    foreach (var icerik in sandik.SandikIcerikleri)
                    {
                        string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? sandik.Proje?.ProjeNo ?? "-";
                        string barkod = !string.IsNullOrWhiteSpace(icerik.BarkodNo) ? icerik.BarkodNo! : icerik.CekiSatiri?.BarkodNo ?? "-";
                        string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                        string miktar = RaporBicimleri.FormatAdet(GetSahaYedekSandikRaporMiktari(icerik, yedekProjesiMi));
                        string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                        string aciklama = GetSahaYedekSandikRaporAciklama(icerik, yedekProjesiMi);
                        
                        string ekleyenId = icerik.CreatedBy ?? "";
                        string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;
                        
                        string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(projeNo);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(barkod);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(urunAdi);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(miktar);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(birim);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(aciklama);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(ekleyen);
                        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(tarih);
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
        var proje = await context.Projeler
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
                        .ThenInclude(cs => cs!.Ceki)
                            .ThenInclude(c => c!.Proje)
            .Include(p => p.Sandiklar)
                .ThenInclude(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs!.BirimLookup)
            .Include(p => p.Sandiklar)
                .ThenInclude(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.BirimLookup)
            .FirstOrDefaultAsync(p => p.Id == projeId);

        if (proje == null)
            throw new KayitBulunamadiException($"Proje bulunamadı: {projeId}");

        var sandiklar = proje.Sandiklar.OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance).ThenBy(s => s.Id).ToList();

        if (!sandiklar.Any())
            throw new IsKuraliException("Bu projeye ait sandık bulunamadı.");

        var kullaniciDict = await context.Kullanicilar.ToDictionaryAsync(k => k.Id.ToString(), k => k.AdSoyad);

        QuestPDF.Settings.License = LicenseType.Community;

        // Renk paleti (eksik raporu ile uyumlu)
        var headerBg = Colors.Blue.Darken3;
        var headerText = Colors.White;
        var tableBorderColor = Colors.Grey.Lighten2;
        var altRowBg = "#F8FAFE";
        var accentColor = "#1565C0";
        var projeTipiStr = proje.ProjeTipiId == (int)ProjeTipi.Yedek ? "Yedek" : "Saha";
        var yedekProjesiMi = proje.ProjeTipiId == (int)ProjeTipi.Yedek;
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
                                col.Item().Text($"Sandık Adı / Crate Name: {RaporBicimleri.GetRaporSandikAdi(sandik)}").FontSize(8);
                                col.Item().Text($"Ölçüler (B/E/Y): {sandik.Boy ?? 0} x {sandik.En ?? 0} x {sandik.Yukseklik ?? 0} mm").FontSize(8);
                                col.Item().Text($"Ağırlık (Net/Gross): {sandik.NetKg ?? 0} / {sandik.GrossKg ?? 0} kg").FontSize(8);
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
                            columns.RelativeColumn(1.35f); // Proje No
                            columns.RelativeColumn(1.45f); // Barkod
                            columns.RelativeColumn(3);    // Ürün Adı
                            columns.ConstantColumn(50);   // Miktar
                            columns.RelativeColumn(1);    // Birim
                            columns.RelativeColumn(2);    // Açıklama
                            columns.RelativeColumn(1.5f); // Ekleyen
                            columns.RelativeColumn(1.5f); // Tarih
                        });

                        // Başlık satırı (TR + EN)
                        table.Header(header =>
                        {
                            void BiHeader(IContainer c, string tr, string en = "")
                            {
                                c.Border(0.5f).BorderColor(headerBg).Background(headerBg).Padding(3).Column(col =>
                                {
                                    col.Item().Text(tr).Bold().FontSize(7).FontColor(headerText);
                                    if (!string.IsNullOrEmpty(en))
                                        col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
                                });
                            }

                            BiHeader(header.Cell(), "#");
                            BiHeader(header.Cell(), "PROJE NO", "Project No");
                            BiHeader(header.Cell(), "BARKOD", "Barcode");
                            BiHeader(header.Cell(), "ÜRÜN ADI / TANIMI", "Product Name");
                            BiHeader(header.Cell(), "MİKTAR", "Quantity");
                            BiHeader(header.Cell(), "BİRİM", "Unit");
                            BiHeader(header.Cell(), "AÇIKLAMA", "Description");
                            BiHeader(header.Cell(), "EKLEYEN", "Added By");
                            BiHeader(header.Cell(), "TARİH", "Date");
                        });

                        // Data Rows
                        int sira = 1;
                        foreach (var icerik in sandik.SandikIcerikleri)
                        {
                            var bg = sira % 2 == 0 ? altRowBg : "#FFFFFF";
                            string projeNo = icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? proje.ProjeNo;
                            string barkod = !string.IsNullOrWhiteSpace(icerik.BarkodNo) ? icerik.BarkodNo! : icerik.CekiSatiri?.BarkodNo ?? "-";
                            string urunAdi = icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-";
                            string miktar = RaporBicimleri.FormatAdet(GetSahaYedekSandikRaporMiktari(icerik, yedekProjesiMi));
                            string birim = icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet";
                            string aciklama = GetSahaYedekSandikRaporAciklama(icerik, yedekProjesiMi);

                            string ekleyenId = icerik.CreatedBy ?? "";
                            string ekleyen = kullaniciDict.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId;

                            string tarih = icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm");

                            void DataCell(IContainer c, string text, bool bold = false, string? fontColor = null)
                            {
                                var cell = c.Background(bg).Border(0.5f).BorderColor(tableBorderColor).Padding(2);
                                if (bold)
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
                                else
                                    cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
                            }

                            DataCell(table.Cell(), sira.ToString());
                            DataCell(table.Cell(), projeNo, bold: true, fontColor: accentColor);
                            DataCell(table.Cell(), barkod);
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

    private static decimal GetSahaYedekSandikRaporMiktari(SandikIcerik icerik, bool yedekProjesiMi)
    {
        // Saha raporlarının mevcut davranışını koru. Yedek projelerinde ise çeki
        // yüklenirken fiziksel konulan miktar henüz sıfır olduğundan tahsis edilen
        // (planlanan) miktarı raporla.
        if (!yedekProjesiMi)
            return icerik.CekiSatiriId == null ? icerik.Miktar : icerik.KonulanAdet;

        if (icerik.TahsisMiktari > 0)
            return icerik.TahsisMiktari;

        if (icerik.CekiSatiri?.IstenenAdet > 0)
            return icerik.CekiSatiri.IstenenAdet;

        if (icerik.Miktar > 0)
            return icerik.Miktar;

        return icerik.KonulanAdet;
    }

    private static string GetSahaYedekSandikRaporAciklama(SandikIcerik icerik, bool yedekProjesiMi)
    {
        if (!yedekProjesiMi)
            return icerik.Aciklama ?? icerik.CekiSatiri?.GridAciklama ?? "-";

        var aciklamalar = new List<string>(4);
        var icerikAciklamasi = icerik.Aciklama?.Trim();
        if (!string.IsNullOrWhiteSpace(icerikAciklamasi))
            aciklamalar.Add(icerikAciklamasi);

        if (icerik.CekiSatiri is { } satir)
        {
            var cekiAciklamasi = satir.Remarks?.Trim();
            var gridAciklamasi = satir.GridAciklama?.Trim();
            var ucKAciklamasi = satir.UcKAciklama?.Trim();

            // Yedek çeki içe aktarımında üretim depo yeri hem içerik açıklamasına
            // hem Remarks alanına yazılabilir. Aynı metni raporda iki kez gösterme.
            if (!string.IsNullOrWhiteSpace(cekiAciklamasi)
                && !string.Equals(cekiAciklamasi, icerikAciklamasi, StringComparison.OrdinalIgnoreCase))
                aciklamalar.Add($"Çeki: {cekiAciklamasi}");
            if (!string.IsNullOrWhiteSpace(gridAciklamasi))
                aciklamalar.Add($"Grid: {gridAciklamasi}");
            if (!string.IsNullOrWhiteSpace(ucKAciklamasi))
                aciklamalar.Add($"3K: {ucKAciklamasi}");
        }

        return aciklamalar.Count > 0 ? string.Join("\n", aciklamalar) : "-";
    }
}
