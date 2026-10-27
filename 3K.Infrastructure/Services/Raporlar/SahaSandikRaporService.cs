using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Common;
using _3K.Core.Exceptions;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Saha/Yedek sandık raporları: projenin bütün sandıkları (PDF) ve beyanname için yalnız sevke hazır sandıklar
/// (PDF ve Excel, aynı içerik).
/// </summary>
internal sealed class SahaSandikRaporService(AppDbContext context) : ISahaSandikRaporService
{
    // Renk paleti (eksik raporu ile uyumlu)
    private static readonly string HeaderBg = Colors.Blue.Darken3;
    private static readonly string HeaderText = Colors.White;
    private static readonly string TableBorderColor = Colors.Grey.Lighten2;
    private const string AltRowBg = "#F8FAFE";
    private const string AccentColor = "#1565C0";
    private const string NotRengi = "#E65100";
    private const string NotZemini = "#FFF8E1";

    /// <summary>Sayfalarda ortak proje, başlık ve kullanıcı adı bilgisi.</summary>
    private sealed record RaporBaglami(
        Proje Proje,
        string ProjeTipiStr,
        bool YedekProjesiMi,
        string RaporTarihi,
        string Baslik,
        string AltBaslik,
        string AltBilgi,
        IReadOnlyDictionary<string, string> Kullanicilar);

    /// <summary>
    /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
    /// Eksik raporu tarzında premium tasarım.
    /// </summary>
    public async Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId)
    {
        var proje = await ProjeYukleAsync(projeId);

        var sandiklar = proje.Sandiklar.OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance).ThenBy(s => s.Id).ToList();

        if (!sandiklar.Any())
            throw new IsKuraliException("Bu projeye ait sandık bulunamadı.");

        var projeTipiStr = ProjeTipiMetni(proje);
        var baglam = await BaglamOlusturAsync(
            proje,
            $"{projeTipiStr} Sandığı İçerik Raporu",
            $"3K Sevkiyat Yönetim Sistemi – {projeTipiStr} Yönetimi Çıktısı",
            $"3K Ambalaj – {projeTipiStr} Raporu");

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            foreach (var sandik in sandiklar)
                SandikSayfasi(container, baglam, sandik);
        });

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);
        return pdfStream.ToArray();
    }

    /// <summary>
    /// Sevke hazır sandık raporu (PDF): ilk sayfada kapsam notu, sevke hazır sandıkların ölçü/ağırlık özeti ve
    /// listelenmeyen sandıkların nedeni; ardından her sevke hazır sandığın içerik sayfası.
    /// </summary>
    public async Task<byte[]> SevkeHazirSandiklarPdfOlusturAsync(int projeId)
    {
        var (proje, ayrim) = await SevkeHazirAyrimiAsync(projeId);
        var baglam = await SevkeHazirBaglamiAsync(proje);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            OzetSayfasi(container, baglam, ayrim);
            foreach (var sandik in ayrim.Hazir)
                SandikSayfasi(container, baglam, sandik);
        });

        using var pdfStream = new MemoryStream();
        document.GeneratePdf(pdfStream);
        return pdfStream.ToArray();
    }

    /// <summary>
    /// Sevke hazır sandık raporu (Excel): PDF ile aynı not, sandık özeti, listelenmeyen sandıklar ve içerik satırları.
    /// </summary>
    public async Task<byte[]> SevkeHazirSandiklarExcelOlusturAsync(int projeId)
    {
        var (proje, ayrim) = await SevkeHazirAyrimiAsync(projeId);
        var baglam = await SevkeHazirBaglamiAsync(proje);
        var not = $"{SevkeHazirSandikKurali.KapsamMetni} {ayrim.Not}";
        var sevkTarihi = SevkTarihi(proje);

        using var workbook = new XLWorkbook();

        // Sevke hazır sandıklar: ölçü ve ağırlık özeti
        var ozetSheet = workbook.Worksheets.Add("Sevke Hazır Sandıklar");
        const int ozetLastColumn = 9;
        const int headerRow = 6;
        BilgiSatirlariniYaz(ozetSheet, ozetLastColumn, baglam, sevkTarihi, ayrim, not);

        var ozetHeaders = new[] { "#", "Sandık No", "Sandık Adı / Crate Name", "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Net (kg)", "Brüt (kg)", "Ürün Satırı" };
        for (var i = 0; i < ozetHeaders.Length; i++)
            ozetSheet.Cell(headerRow, i + 1).Value = ozetHeaders[i];
        RaporExcelStilleri.StyleExcelHeader(ozetSheet, headerRow, ozetLastColumn);

        var row = headerRow + 1;
        var sira = 1;
        foreach (var sandik in ayrim.Hazir)
        {
            ozetSheet.Cell(row, 1).SetValue(sira);
            ozetSheet.Cell(row, 2).Value = sandik.SandikNo;
            ozetSheet.Cell(row, 3).Value = RaporBicimleri.GetRaporSandikAdi(sandik);
            RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 4), sandik.Boy ?? 0);
            RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 5), sandik.En ?? 0);
            RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 6), sandik.Yukseklik ?? 0);
            RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 7), sandik.NetKg ?? 0);
            RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 8), sandik.GrossKg ?? 0);
            ozetSheet.Cell(row, 9).SetValue(sandik.SandikIcerikleri.Count);
            if (sira % 2 == 0)
                ozetSheet.Range(row, 1, row, ozetLastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowBg);
            row++;
            sira++;
        }

        ozetSheet.Cell(row, 2).Value = "Toplam";
        RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 7), ayrim.Hazir.Sum(s => s.NetKg ?? 0));
        RaporExcelStilleri.SetDecimalCell(ozetSheet.Cell(row, 8), ayrim.Hazir.Sum(s => s.GrossKg ?? 0));
        ozetSheet.Cell(row, 9).SetValue(ayrim.Hazir.Sum(s => s.SandikIcerikleri.Count));
        ozetSheet.Range(row, 1, row, ozetLastColumn).Style.Font.Bold = true;
        RaporExcelStilleri.FinalizeExcelWorksheet(ozetSheet, headerRow, row, ozetLastColumn);

        // Listelenmeyen sandıklar ve nedeni
        if (ayrim.Disarida.Count > 0)
        {
            var disSheet = workbook.Worksheets.Add("Listelenmeyen Sandıklar");
            const int disLastColumn = 7;
            BilgiSatirlariniYaz(disSheet, disLastColumn, baglam, sevkTarihi, ayrim, not);

            var disHeaders = new[] { "#", "Sandık No", "Sandık Adı / Crate Name", "Durum", "Neden", "Eksik Ölçü/Ağırlık", "Ürün Satırı" };
            for (var i = 0; i < disHeaders.Length; i++)
                disSheet.Cell(headerRow, i + 1).Value = disHeaders[i];
            RaporExcelStilleri.StyleExcelHeader(disSheet, headerRow, disLastColumn);

            row = headerRow + 1;
            sira = 1;
            foreach (var disarida in ayrim.Disarida)
            {
                disSheet.Cell(row, 1).SetValue(sira);
                disSheet.Cell(row, 2).Value = disarida.Sandik.SandikNo;
                disSheet.Cell(row, 3).Value = RaporBicimleri.GetRaporSandikAdi(disarida.Sandik);
                disSheet.Cell(row, 4).Value = DurumMetni(disarida.Sandik);
                disSheet.Cell(row, 5).Value = SevkeHazirSandikKurali.NedenMetni(disarida.Neden);
                disSheet.Cell(row, 5).Style.Font.Bold = true;
                disSheet.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml(NotRengi);
                disSheet.Cell(row, 6).Value = EksikAlanMetni(disarida);
                disSheet.Cell(row, 7).SetValue(disarida.Sandik.SandikIcerikleri.Count);
                if (sira % 2 == 0)
                    disSheet.Range(row, 1, row, disLastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowBg);
                row++;
                sira++;
            }

            RaporExcelStilleri.FinalizeExcelWorksheet(disSheet, headerRow, row - 1, disLastColumn);
        }

        // Sevke hazır sandıkların içerikleri (PDF'teki sandık sayfalarıyla aynı sütunlar ve sıra)
        var icerikSheet = workbook.Worksheets.Add("Sandık İçerikleri");
        const int icerikLastColumn = 10;
        BilgiSatirlariniYaz(icerikSheet, icerikLastColumn, baglam, sevkTarihi, ayrim, not);

        var icerikHeaders = new[] { "Sandık No", "#", "Proje No", "Barkod", "Ürün Adı / Tanımı", "Miktar", "Birim", "Açıklama", "Ekleyen", "Tarih" };
        for (var i = 0; i < icerikHeaders.Length; i++)
            icerikSheet.Cell(headerRow, i + 1).Value = icerikHeaders[i];
        RaporExcelStilleri.StyleExcelHeader(icerikSheet, headerRow, icerikLastColumn);

        row = headerRow + 1;
        var satirSirasi = 0;
        foreach (var sandik in ayrim.Hazir)
        {
            var icerikSira = 1;
            foreach (var icerik in sandik.SandikIcerikleri)
            {
                var satir = IcerikSatiri(baglam, icerik);
                icerikSheet.Cell(row, 1).Value = sandik.SandikNo;
                icerikSheet.Cell(row, 2).SetValue(icerikSira);
                icerikSheet.Cell(row, 3).Value = satir.ProjeNo;
                icerikSheet.Cell(row, 4).Value = satir.Barkod;
                icerikSheet.Cell(row, 5).Value = satir.UrunAdi;
                RaporExcelStilleri.SetDecimalCell(icerikSheet.Cell(row, 6), satir.Miktar);
                icerikSheet.Cell(row, 7).Value = satir.Birim;
                icerikSheet.Cell(row, 8).Value = satir.Aciklama;
                icerikSheet.Cell(row, 9).Value = satir.Ekleyen;
                icerikSheet.Cell(row, 10).Value = satir.Tarih;
                if (++satirSirasi % 2 == 0)
                    icerikSheet.Range(row, 1, row, icerikLastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowBg);
                row++;
                icerikSira++;
            }
        }

        icerikSheet.Column(5).Style.Alignment.WrapText = true;
        icerikSheet.Column(8).Style.Alignment.WrapText = true;
        RaporExcelStilleri.FinalizeExcelWorksheet(icerikSheet, headerRow, row - 1, icerikLastColumn);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<Proje> ProjeYukleAsync(int projeId)
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

        return proje;
    }

    private async Task<(Proje Proje, SevkeHazirSandikAyrimi Ayrim)> SevkeHazirAyrimiAsync(int projeId)
    {
        var proje = await ProjeYukleAsync(projeId);
        if (proje.Sandiklar.Count == 0)
            throw new IsKuraliException("Bu projeye ait sandık bulunamadı.");

        var ayrim = SevkeHazirSandikKurali.Ayir(proje.Sandiklar);
        // Boş rapor beyannamede yanıltır; listelenmeme nedenleri doğrudan kullanıcıya gösterilir.
        if (ayrim.Hazir.Count == 0)
            throw new IsKuraliException($"Sevke hazır sandık bulunamadı. {ayrim.Not}");

        return (proje, ayrim);
    }

    private Task<RaporBaglami> SevkeHazirBaglamiAsync(Proje proje)
    {
        var projeTipiStr = ProjeTipiMetni(proje);
        return BaglamOlusturAsync(
            proje,
            "Sevke Hazır Sandık Raporu",
            $"3K Sevkiyat Yönetim Sistemi – {projeTipiStr} Yönetimi Çıktısı · Kapalı, ölçü ve ağırlığı girilmiş sandıklar",
            $"3K Ambalaj – {projeTipiStr} Sevke Hazır Sandık Raporu");
    }

    private async Task<RaporBaglami> BaglamOlusturAsync(Proje proje, string baslik, string altBaslik, string altBilgi)
    {
        var kullaniciDict = await context.Kullanicilar.ToDictionaryAsync(k => k.Id.ToString(), k => k.AdSoyad);
        return new RaporBaglami(
            proje,
            ProjeTipiMetni(proje),
            proje.ProjeTipiId == (int)ProjeTipi.Yedek,
            TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm"),
            baslik,
            altBaslik,
            altBilgi,
            kullaniciDict);
    }

    private static string ProjeTipiMetni(Proje proje) => proje.ProjeTipiId == (int)ProjeTipi.Yedek ? "Yedek" : "Saha";

    private static string SevkTarihi(Proje proje) =>
        proje.GerceklesenSevkTarihi?.ToString("dd.MM.yyyy") ?? proje.PlanlananSevkTarihi?.ToString("dd.MM.yyyy") ?? "-";

    private static string DurumMetni(Sandik sandik) => sandik.DurumLookup?.Deger ?? "-";

    private static string EksikAlanMetni(SevkeHazirOlmayanSandik sandik) =>
        sandik.EksikAlanlar.Count == 0 ? "-" : string.Join(", ", sandik.EksikAlanlar);

    private static string OlcuMetni(Sandik sandik) => $"{sandik.Boy ?? 0} x {sandik.En ?? 0} x {sandik.Yukseklik ?? 0} mm";

    private static string AgirlikMetni(Sandik sandik) => $"{sandik.NetKg ?? 0} / {sandik.GrossKg ?? 0} kg";

    /// <summary>İçerik satırının PDF ve Excel'de aynı yazılan değerleri.</summary>
    private sealed record IcerikSatirMetni(
        string ProjeNo, string Barkod, string UrunAdi, decimal Miktar, string Birim, string Aciklama, string Ekleyen, string Tarih);

    private static IcerikSatirMetni IcerikSatiri(RaporBaglami baglam, SandikIcerik icerik)
    {
        var ekleyenId = icerik.CreatedBy ?? "";
        return new IcerikSatirMetni(
            icerik.KaynakProjeNo ?? icerik.CekiSatiri?.Ceki?.Proje?.ProjeNo ?? baglam.Proje.ProjeNo,
            !string.IsNullOrWhiteSpace(icerik.BarkodNo) ? icerik.BarkodNo! : icerik.CekiSatiri?.BarkodNo ?? "-",
            icerik.Isim ?? icerik.CekiSatiri?.Aciklama ?? "-",
            GetSahaYedekSandikRaporMiktari(icerik, baglam.YedekProjesiMi),
            icerik.BirimLookup?.Deger ?? icerik.CekiSatiri?.BirimLookup?.Deger ?? "Adet",
            GetSahaYedekSandikRaporAciklama(icerik, baglam.YedekProjesiMi),
            baglam.Kullanicilar.TryGetValue(ekleyenId, out var isim) ? isim : ekleyenId,
            icerik.CreatedDate.ToString("dd.MM.yyyy HH:mm"));
    }

    /// <summary>Excel sayfasının başlık, proje bilgisi ve not satırları (satır 1–4).</summary>
    private static void BilgiSatirlariniYaz(
        IXLWorksheet sheet, int lastColumn, RaporBaglami baglam, string sevkTarihi, SevkeHazirSandikAyrimi ayrim, string not)
    {
        sheet.Cell(1, 1).Value = baglam.Baslik;
        RaporExcelStilleri.StyleExcelTitle(sheet, lastColumn);
        sheet.Cell(2, 1).Value = "Proje No";
        sheet.Cell(2, 2).Value = baglam.Proje.ProjeNo;
        sheet.Cell(2, 4).Value = "Müşteri";
        sheet.Cell(2, 5).Value = baglam.Proje.Musteri;
        sheet.Cell(2, 6).Value = "Rapor Tarihi";
        sheet.Cell(2, 7).Value = baglam.RaporTarihi;
        sheet.Cell(3, 1).Value = "Sevke Hazır Sandık";
        sheet.Cell(3, 2).Value = $"{ayrim.Hazir.Count} / {ayrim.ToplamSandik}";
        sheet.Cell(3, 4).Value = "Sevk Tarihi";
        sheet.Cell(3, 5).Value = sevkTarihi;
        sheet.Cell(3, 6).Value = "FB No";
        sheet.Cell(3, 7).Value = string.IsNullOrWhiteSpace(baglam.Proje.FBNo) ? "-" : baglam.Proje.FBNo;
        RaporExcelStilleri.SetNotSatiri(sheet, 4, lastColumn, not);
        RaporExcelStilleri.StyleExcelInfoRows(sheet, lastColumn);
    }

    /// <summary>Raporların üst çubuğu: 3K logosu, başlık ve rapor tarihi.</summary>
    private static void UstCubuk(ColumnDescriptor headerCol, RaporBaglami baglam)
    {
        headerCol.Item().Background(HeaderBg).Padding(12).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("3K").Bold().FontSize(22).FontColor(HeaderText);
                col.Item().Text("All Processes. One Flow.").FontSize(7).FontColor(Colors.Grey.Lighten3).Italic();
            });
            row.RelativeItem(2).AlignCenter().Column(col =>
            {
                col.Item().Text(baglam.Baslik).Bold().FontSize(16).FontColor(HeaderText);
                col.Item().Text(baglam.AltBaslik).FontSize(7).FontColor(Colors.Grey.Lighten3);
            });
            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"Rapor Tarihi: {baglam.RaporTarihi}").FontSize(8).FontColor(Colors.Grey.Lighten3);
            });
        });
    }

    private static void ProjeKarti(RowDescriptor row, Proje proje)
    {
        row.RelativeItem().Border(1).BorderColor(TableBorderColor).Padding(8).Column(col =>
        {
            col.Item().Text("PROJE BİLGİSİ").Bold().FontSize(7).FontColor(Colors.Blue.Darken1);
            col.Item().PaddingTop(3).Text(proje.ProjeNo).Bold().FontSize(13);
            col.Item().Text($"Müşteri: {proje.Musteri}").FontSize(8);
            if (!string.IsNullOrWhiteSpace(proje.FBNo))
                col.Item().Text($"FB No: {proje.FBNo}").FontSize(8);
            col.Item().Text($"Sevk Tarihi: {SevkTarihi(proje)}").FontSize(8);
        });
    }

    private static void AltBilgi(PageDescriptor page, string metin)
    {
        page.Footer().Row(footer =>
        {
            footer.RelativeItem().Text(metin).FontSize(7).FontColor(Colors.Grey.Medium);
            footer.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Sayfa ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                x.Span(" / ").FontSize(7).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void BiHeader(IContainer c, string tr, string en = "")
    {
        c.Border(0.5f).BorderColor(HeaderBg).Background(HeaderBg).Padding(3).Column(col =>
        {
            col.Item().Text(tr).Bold().FontSize(7).FontColor(HeaderText);
            if (!string.IsNullOrEmpty(en))
                col.Item().Text(en).FontSize(5).FontColor(Colors.Grey.Lighten3).Italic();
        });
    }

    private static void DataCell(IContainer c, string bg, string text, bool bold = false, string? fontColor = null)
    {
        var cell = c.Background(bg).Border(0.5f).BorderColor(TableBorderColor).Padding(2);
        if (bold)
            cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black).Bold();
        else
            cell.Text(text).FontSize(7).FontColor(fontColor ?? Colors.Black);
    }

    /// <summary>Tek sandığın içerik sayfası: proje ve sandık kartı, içerik tablosu.</summary>
    private static void SandikSayfasi(IDocumentContainer container, RaporBaglami baglam, Sandik sandik)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(8));

            // ===== HEADER =====
            page.Header().Column(headerCol =>
            {
                UstCubuk(headerCol, baglam);

                // Proje bilgisi kartları
                headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                {
                    ProjeKarti(row, baglam.Proje);

                    row.ConstantItem(10); // Spacer

                    // Sandık Bilgisi
                    row.RelativeItem().Border(1).BorderColor(AccentColor).Padding(8).Column(col =>
                    {
                        col.Item().Text("SANDIK BİLGİSİ").Bold().FontSize(7).FontColor(AccentColor);
                        col.Item().PaddingTop(3).Text($"Sandık No: {sandik.SandikNo}").Bold().FontSize(13);
                        col.Item().Text($"Sandık Adı / Crate Name: {RaporBicimleri.GetRaporSandikAdi(sandik)}").FontSize(8);
                        col.Item().Text($"Ölçüler (B/E/Y): {OlcuMetni(sandik)}").FontSize(8);
                        col.Item().Text($"Ağırlık (Net/Gross): {AgirlikMetni(sandik)}").FontSize(8);
                    });
                });

                headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(TableBorderColor);
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
                    var bg = sira % 2 == 0 ? AltRowBg : "#FFFFFF";
                    var satir = IcerikSatiri(baglam, icerik);

                    DataCell(table.Cell(), bg, sira.ToString());
                    DataCell(table.Cell(), bg, satir.ProjeNo, bold: true, fontColor: AccentColor);
                    DataCell(table.Cell(), bg, satir.Barkod);
                    DataCell(table.Cell(), bg, satir.UrunAdi);
                    DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(satir.Miktar), bold: true);
                    DataCell(table.Cell(), bg, satir.Birim);
                    DataCell(table.Cell(), bg, satir.Aciklama);
                    DataCell(table.Cell(), bg, satir.Ekleyen);
                    DataCell(table.Cell(), bg, satir.Tarih);

                    sira++;
                }
            });

            // ===== FOOTER =====
            AltBilgi(page, $"{baglam.AltBilgi} | Proje: {baglam.Proje.ProjeNo} | Sandık: {sandik.SandikNo}");
        });
    }

    /// <summary>Sevke hazır raporunun ilk sayfası: not, sevke hazır sandık özeti ve listelenmeyen sandıklar.</summary>
    private static void OzetSayfasi(IDocumentContainer container, RaporBaglami baglam, SevkeHazirSandikAyrimi ayrim)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(8));

            page.Header().Column(headerCol =>
            {
                UstCubuk(headerCol, baglam);

                headerCol.Item().PaddingTop(10).PaddingBottom(5).Row(row =>
                {
                    ProjeKarti(row, baglam.Proje);

                    row.ConstantItem(10); // Spacer

                    row.RelativeItem().Border(1).BorderColor(AccentColor).Padding(8).Column(col =>
                    {
                        col.Item().Text("RAPOR KAPSAMI").Bold().FontSize(7).FontColor(AccentColor);
                        col.Item().PaddingTop(3).Text($"Sevke Hazır Sandık: {ayrim.Hazir.Count} / {ayrim.ToplamSandik}").Bold().FontSize(13);
                        col.Item().Text($"Toplam Ağırlık (Net/Gross): {RaporBicimleri.FormatAdet(ayrim.Hazir.Sum(s => s.NetKg ?? 0))} / {RaporBicimleri.FormatAdet(ayrim.Hazir.Sum(s => s.GrossKg ?? 0))} kg").FontSize(8);
                        col.Item().Text($"Ürün Satırı: {ayrim.Hazir.Sum(s => s.SandikIcerikleri.Count)}").FontSize(8);
                    });
                });

                headerCol.Item().PaddingTop(5).LineHorizontal(1).LineColor(TableBorderColor);
            });

            page.Content().PaddingVertical(8).Column(content =>
            {
                // Not: kaç sandığın neden listelenmediği
                content.Item().Background(NotZemini).BorderLeft(3).BorderColor(NotRengi).Padding(8).Column(col =>
                {
                    col.Item().Text("NOT").Bold().FontSize(7).FontColor(NotRengi);
                    col.Item().PaddingTop(2).Text(ayrim.Not).Bold().FontSize(9);
                    col.Item().PaddingTop(2).Text(SevkeHazirSandikKurali.KapsamMetni).FontSize(7).FontColor(Colors.Grey.Darken1).Italic();
                });

                content.Item().PaddingTop(10).Text("SEVKE HAZIR SANDIKLAR").Bold().FontSize(8).FontColor(AccentColor);
                content.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // #
                        columns.RelativeColumn(1.2f); // Sandık No
                        columns.RelativeColumn(3);    // Sandık Adı
                        columns.RelativeColumn(1);    // Boy
                        columns.RelativeColumn(1);    // En
                        columns.RelativeColumn(1);    // Yükseklik
                        columns.RelativeColumn(1);    // Net
                        columns.RelativeColumn(1);    // Brüt
                        columns.RelativeColumn(1);    // Ürün Satırı
                    });

                    table.Header(header =>
                    {
                        BiHeader(header.Cell(), "#");
                        BiHeader(header.Cell(), "SANDIK NO", "Crate No");
                        BiHeader(header.Cell(), "SANDIK ADI", "Crate Name");
                        BiHeader(header.Cell(), "BOY (MM)", "Length");
                        BiHeader(header.Cell(), "EN (MM)", "Width");
                        BiHeader(header.Cell(), "YÜKSEKLİK (MM)", "Height");
                        BiHeader(header.Cell(), "NET (KG)", "Net Weight");
                        BiHeader(header.Cell(), "BRÜT (KG)", "Gross Weight");
                        BiHeader(header.Cell(), "ÜRÜN SATIRI", "Item Lines");
                    });

                    var sira = 1;
                    foreach (var sandik in ayrim.Hazir)
                    {
                        var bg = sira % 2 == 0 ? AltRowBg : "#FFFFFF";
                        DataCell(table.Cell(), bg, sira.ToString());
                        DataCell(table.Cell(), bg, sandik.SandikNo, bold: true, fontColor: AccentColor);
                        DataCell(table.Cell(), bg, RaporBicimleri.GetRaporSandikAdi(sandik));
                        DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(sandik.Boy ?? 0));
                        DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(sandik.En ?? 0));
                        DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(sandik.Yukseklik ?? 0));
                        DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(sandik.NetKg ?? 0));
                        DataCell(table.Cell(), bg, RaporBicimleri.FormatAdet(sandik.GrossKg ?? 0));
                        DataCell(table.Cell(), bg, sandik.SandikIcerikleri.Count.ToString());
                        sira++;
                    }

                    const string toplamBg = "#EEF3FB";
                    DataCell(table.Cell(), toplamBg, "");
                    DataCell(table.Cell(), toplamBg, "Toplam", bold: true);
                    DataCell(table.Cell(), toplamBg, "");
                    DataCell(table.Cell(), toplamBg, "");
                    DataCell(table.Cell(), toplamBg, "");
                    DataCell(table.Cell(), toplamBg, "");
                    DataCell(table.Cell(), toplamBg, RaporBicimleri.FormatAdet(ayrim.Hazir.Sum(s => s.NetKg ?? 0)), bold: true);
                    DataCell(table.Cell(), toplamBg, RaporBicimleri.FormatAdet(ayrim.Hazir.Sum(s => s.GrossKg ?? 0)), bold: true);
                    DataCell(table.Cell(), toplamBg, ayrim.Hazir.Sum(s => s.SandikIcerikleri.Count).ToString(), bold: true);
                });

                if (ayrim.Disarida.Count > 0)
                {
                    content.Item().PaddingTop(12).Text("LİSTELENMEYEN SANDIKLAR").Bold().FontSize(8).FontColor(NotRengi);
                    content.Item().PaddingTop(3).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);   // #
                            columns.RelativeColumn(1.2f); // Sandık No
                            columns.RelativeColumn(3);    // Sandık Adı
                            columns.RelativeColumn(1.3f); // Durum
                            columns.RelativeColumn(1.5f); // Neden
                            columns.RelativeColumn(2.5f); // Eksik Ölçü/Ağırlık
                            columns.RelativeColumn(1);    // Ürün Satırı
                        });

                        table.Header(header =>
                        {
                            BiHeader(header.Cell(), "#");
                            BiHeader(header.Cell(), "SANDIK NO", "Crate No");
                            BiHeader(header.Cell(), "SANDIK ADI", "Crate Name");
                            BiHeader(header.Cell(), "DURUM", "Status");
                            BiHeader(header.Cell(), "NEDEN", "Reason");
                            BiHeader(header.Cell(), "EKSİK ÖLÇÜ/AĞIRLIK", "Missing Dimension/Weight");
                            BiHeader(header.Cell(), "ÜRÜN SATIRI", "Item Lines");
                        });

                        var sira = 1;
                        foreach (var disarida in ayrim.Disarida)
                        {
                            var bg = sira % 2 == 0 ? AltRowBg : "#FFFFFF";
                            DataCell(table.Cell(), bg, sira.ToString());
                            DataCell(table.Cell(), bg, disarida.Sandik.SandikNo, bold: true);
                            DataCell(table.Cell(), bg, RaporBicimleri.GetRaporSandikAdi(disarida.Sandik));
                            DataCell(table.Cell(), bg, DurumMetni(disarida.Sandik));
                            DataCell(table.Cell(), bg, SevkeHazirSandikKurali.NedenMetni(disarida.Neden), bold: true, fontColor: NotRengi);
                            DataCell(table.Cell(), bg, EksikAlanMetni(disarida));
                            DataCell(table.Cell(), bg, disarida.Sandik.SandikIcerikleri.Count.ToString());
                            sira++;
                        }
                    });
                }
            });

            AltBilgi(page, $"{baglam.AltBilgi} | Proje: {baglam.Proje.ProjeNo} | Özet");
        });
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
