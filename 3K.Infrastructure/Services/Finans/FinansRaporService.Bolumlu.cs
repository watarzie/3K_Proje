using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Bölümlü rapor satırının türü: veri, bölüm başlığı, bölüm ara toplamı ya da genel toplam.</summary>
internal enum RaporSatirTuru
{
    Veri,
    Bolum,
    AraToplam,
    GenelToplam
}

internal sealed record RaporSatiri(RaporSatirTuru Tur, object?[] Degerler)
{
    public static RaporSatiri Bolum(string baslik) => new(RaporSatirTuru.Bolum, [baslik]);
}

/// <summary>
/// Raporun bir tablosu; PDF'te bölüm, Excel'de (çok tabloluda) ayrı sayfa olur. <see cref="UcHaneliSutunlar"/> (m³)
/// PDF, Excel ve önizlemede aynı hassasiyetle, üç ondalıkla yazılır; diğer sayılar iki ondalıktır.
/// </summary>
internal sealed record RaporTablosu(
    string Baslik,
    IReadOnlyList<string> Sutunlar,
    IReadOnlyList<RaporSatiri> Satirlar,
    IReadOnlyList<float> PdfGenislikleri,
    IReadOnlyCollection<int>? UcHaneliSutunlar = null)
{
    public bool UcHaneli(int sutun) => UcHaneliSutunlar?.Contains(sutun) == true;
}

/// <summary>
/// PDF ve Excel'in tek içerik kaynağı: aynı tablolar, aynı satırlar, aynı ara ve genel toplamlar. Yalnız görünüm
/// (sayfa düzeni, renk) çıktıya göre değişir; önizleme de aynı satırları gösterir.
/// </summary>
internal sealed record BolumluRapor(string Baslik, string AltBaslik, IReadOnlyList<RaporTablosu> Tablolar, string? DipNot = null);

internal sealed partial class FinansRaporService
{
    private static readonly CultureInfo TrKultur = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>Filtrenin rapor başlığına yazılan özeti (yalnız dolu alanlar).</summary>
    private static string FiltreMetni(FinansListeFiltre filtre)
    {
        var parcalar = new List<string>();
        if (filtre.Baslangic.HasValue || filtre.Bitis.HasValue)
            parcalar.Add($"Finans tarihi: {filtre.Baslangic?.ToString("dd.MM.yyyy") ?? "…"}–{filtre.Bitis?.ToString("dd.MM.yyyy") ?? "…"}");
        if (!string.IsNullOrWhiteSpace(filtre.ProjeNo)) parcalar.Add($"Proje: {filtre.ProjeNo.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtre.ParaBirimi)) parcalar.Add($"Para birimi: {filtre.ParaBirimi.Trim().ToUpperInvariant()}");
        if (filtre.IsTuru.HasValue) parcalar.Add($"İş türü: {FinansEtiketleri.IsTuru(filtre.IsTuru.Value)}");
        if (filtre.Kategoriler is { Count: > 0 } kategoriler)
            parcalar.Add($"Kategori: {string.Join(", ", kategoriler.Select(FinansKategoriKurali.Ad))}");
        if (filtre.Durum.HasValue) parcalar.Add($"Durum: {FinansEtiketleri.IsDurumu(filtre.Durum.Value)}");
        if (filtre.SandikCinsi.HasValue) parcalar.Add($"Sandık cinsi: {FinansEtiketleri.SandikCinsi(filtre.SandikCinsi)}");
        if (filtre.SiparisKarsilanma.HasValue) parcalar.Add($"Sipariş: {SiparisKarsilanmaMetni(filtre.SiparisKarsilanma.Value)}");
        if (filtre.FaturaKarsilanma.HasValue) parcalar.Add($"Fatura: {FaturaKarsilanmaMetni(filtre.FaturaKarsilanma.Value)}");
        if (!string.IsNullOrWhiteSpace(filtre.PoNumarasi)) parcalar.Add($"PO: {filtre.PoNumarasi.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtre.FaturaNumarasi)) parcalar.Add($"Fatura no: {filtre.FaturaNumarasi.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtre.Firma)) parcalar.Add($"Firma/kişi: {filtre.Firma.Trim()}");
        if (!string.IsNullOrWhiteSpace(filtre.TalepEden)) parcalar.Add($"Talep eden: {filtre.TalepEden.Trim()}");
        if (filtre.GiderKategoriId.HasValue) parcalar.Add("Gider kategorisi seçili");
        if (!string.IsNullOrWhiteSpace(filtre.Arama)) parcalar.Add($"Arama: {filtre.Arama.Trim()}");
        parcalar.Add(filtre.IptalEdilenleriDahilEt ? "İptaller dahil" : "İptaller hariç");
        parcalar.Add($"Oluşturma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}");
        return string.Join(" · ", parcalar);
    }

    internal static byte[] BolumluRaporExcel(BolumluRapor rapor)
    {
        using var workbook = new XLWorkbook();
        foreach (var tablo in rapor.Tablolar)
        {
            var sheet = workbook.Worksheets.Add(SayfaAdi(tablo.Baslik, workbook));
            var sutunSayisi = tablo.Sutunlar.Count;
            sheet.Cell(1, 1).Value = rapor.Tablolar.Count > 1 ? $"{rapor.Baslik} — {tablo.Baslik}" : rapor.Baslik;
            sheet.Range(1, 1, 1, sutunSayisi).Merge().Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontColor(XLColor.FromHtml("#1F3A8A"));
            sheet.Cell(2, 1).Value = rapor.AltBaslik;
            sheet.Range(2, 1, 2, sutunSayisi).Merge().Style.Font.SetFontColor(XLColor.FromHtml("#475569"));

            const int baslikSatiri = 4;
            for (var i = 0; i < sutunSayisi; i++) sheet.Cell(baslikSatiri, i + 1).Value = tablo.Sutunlar[i];
            var baslik = sheet.Range(baslikSatiri, 1, baslikSatiri, sutunSayisi);
            baslik.Style.Font.Bold = true;
            baslik.Style.Font.FontColor = XLColor.White;
            baslik.Style.Fill.BackgroundColor = XLColor.FromHtml("#3F51B5");
            baslik.Style.Alignment.WrapText = true;

            var row = baslikSatiri + 1;
            foreach (var satir in tablo.Satirlar)
            {
                if (satir.Tur == RaporSatirTuru.Bolum)
                {
                    sheet.Cell(row, 1).Value = Convert.ToString(satir.Degerler[0], TrKultur) ?? string.Empty;
                    var bolum = sheet.Range(row, 1, row, sutunSayisi);
                    bolum.Merge().Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#EAF2FF"));
                    row++;
                    continue;
                }

                WriteRow(sheet, row, satir.Degerler);
                for (var i = 0; i < satir.Degerler.Length; i++)
                    if (satir.Degerler[i] is decimal && tablo.UcHaneli(i))
                        sheet.Cell(row, i + 1).Style.NumberFormat.Format = "#,##0.000";
                if (satir.Tur != RaporSatirTuru.Veri)
                {
                    var toplam = sheet.Range(row, 1, row, sutunSayisi);
                    toplam.Style.Font.Bold = true;
                    toplam.Style.Fill.BackgroundColor = XLColor.FromHtml(satir.Tur == RaporSatirTuru.GenelToplam ? "#DBEAFE" : "#F1F5F9");
                }
                row++;
            }

            if (!string.IsNullOrWhiteSpace(rapor.DipNot))
            {
                sheet.Cell(row + 1, 1).Value = rapor.DipNot;
                sheet.Range(row + 1, 1, row + 1, sutunSayisi).Merge().Style.Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#64748B"));
            }

            sheet.SheetView.FreezeRows(baslikSatiri);
            sheet.Columns(1, sutunSayisi).AdjustToContents(baslikSatiri, Math.Max(baslikSatiri, row - 1), 8, 50);
            sheet.Range(baslikSatiri, 1, Math.Max(baslikSatiri, row - 1), sutunSayisi).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            sheet.Range(baslikSatiri, 1, Math.Max(baslikSatiri, row - 1), sutunSayisi).Style.Border.BottomBorderColor = XLColor.LightGray;
        }

        return Save(workbook);
    }

    internal static byte[] BolumluRaporPdf(BolumluRapor rapor)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                // Geniş tablo (iş raporu 29 sütun) yatay A4'e sığsın diye yazı küçülür.
                var enGenis = rapor.Tablolar.Max(x => x.Sutunlar.Count);
                page.DefaultTextStyle(x => x.FontSize(enGenis > 24 ? 5.5f : 6.5f));
                page.Header().PaddingBottom(6).Column(column =>
                {
                    column.Item().Text(rapor.Baslik).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(rapor.AltBaslik).FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    foreach (var tablo in rapor.Tablolar)
                    {
                        if (rapor.Tablolar.Count > 1)
                            column.Item().Text(tablo.Baslik).FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var genislik in tablo.PdfGenislikleri) columns.RelativeColumn(genislik);
                            });
                            PdfHeader(table, tablo.Sutunlar.ToArray());
                            foreach (var satir in tablo.Satirlar)
                            {
                                if (satir.Tur == RaporSatirTuru.Bolum)
                                {
                                    table.Cell().ColumnSpan((uint)tablo.Sutunlar.Count).Background("#EAF2FF").PaddingVertical(3).PaddingHorizontal(4)
                                        .Text(Convert.ToString(satir.Degerler[0], TrKultur) ?? string.Empty).Bold().FontColor(Colors.Blue.Darken3);
                                    continue;
                                }

                                var arkaPlan = satir.Tur switch
                                {
                                    RaporSatirTuru.GenelToplam => "#DBEAFE",
                                    RaporSatirTuru.AraToplam => "#F1F5F9",
                                    _ => "#FFFFFF"
                                };
                                for (var i = 0; i < satir.Degerler.Length; i++)
                                {
                                    var text = table.Cell().Background(arkaPlan).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f)
                                        .Text(PdfDegeri(satir.Degerler[i], tablo.UcHaneli(i)));
                                    if (satir.Tur != RaporSatirTuru.Veri) text.Bold();
                                }
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(rapor.DipNot))
                        column.Item().PaddingTop(4).Text(rapor.DipNot).Italic().FontColor(Colors.Grey.Darken1);
                });
                AddFooter(page);
            });
        });
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    /// <summary>Hücre metni: PDF ve önizleme aynı biçimi kullanır (tutar iki, m³ üç ondalık; tr-TR).</summary>
    private static string PdfDegeri(object? deger, bool ucHaneli = false) => deger switch
    {
        null => string.Empty,
        decimal sayi => ucHaneli ? sayi.ToString("N3", TrKultur) : Format(sayi),
        DateTime tarih => tarih.ToString("dd.MM.yyyy"),
        int sayi => sayi.ToString(TrKultur),
        _ => Convert.ToString(deger, TrKultur) ?? string.Empty
    };

    private static string SayfaAdi(string baslik, XLWorkbook workbook)
    {
        var gecersiz = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var ad = new string(baslik.Select(c => gecersiz.Contains(c) ? ' ' : c).ToArray()).Trim();
        if (ad.Length > 31) ad = ad[..31];
        var aday = ad;
        for (var i = 2; workbook.Worksheets.Any(w => string.Equals(w.Name, aday, StringComparison.OrdinalIgnoreCase)); i++)
            aday = $"{ad[..Math.Min(ad.Length, 28)]} {i}";
        return aday;
    }
}
