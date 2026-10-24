using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Müşteri gerçekleşen çeki listesi ("Gerçekleşen Çeki Listesi / Actual Packing List"): yalnız sevk edilen sandıklar ve
/// sandıklara konulan ürünler. Bütün başlıklar Türkçe / İngilizce; PDF ve Excel aynı sandık ve ürün satırlarını yazar.
/// İç notlar (düzeltme işareti, depo, kalan) müşteri belgesinde yoktur.
/// </summary>
public sealed partial class SevkiyatRaporDosyaService
{
    private const string MusteriBasligi = "Gerçekleşen Çeki Listesi";
    private const string MusteriBasligiEn = "Actual Packing List";

    public byte[] MusteriCekiPdfOlustur(MusteriCekiListesi liste)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(22);
            page.DefaultTextStyle(x => x.FontSize(8.5f));

            page.Header().Element(c => Baslik(c, MusteriBasligi, MusteriBasligiEn,
                $"Tarih / Date: {Tarih(liste.OlusturmaTarihi, false)}", liste.ProjeNo));

            page.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(105);
                        c.RelativeColumn();
                        c.ConstantColumn(105);
                        c.RelativeColumn();
                    });
                    // Müşteriye sevk tarihi verilmez (24.10.2026); müşteri adı uzun olabildiği için tam satırdır.
                    void Bilgi(string etiket, string deger, uint genislik = 1)
                    {
                        table.Cell().Background(AcikMavi).Border(0.5f).BorderColor(Kenar).Padding(5).Text(etiket).SemiBold().FontColor(Mavi);
                        table.Cell().ColumnSpan(genislik).Border(0.5f).BorderColor(Kenar).Padding(5).Text(deger);
                    }
                    Bilgi("Proje No / Project No", liste.ProjeNo);
                    Bilgi("Lokasyon / Location", Metin(liste.Lokasyon));
                    Bilgi("Müşteri / Customer", Metin(liste.Musteri), genislik: 3);
                    Bilgi("Koli / Cases", Sayi(liste.Sandiklar.Count));
                    Bilgi("Net / Brüt (Gross) kg", $"{Sayi(liste.NetKg)} / {Sayi(liste.BrutKg)}");
                });

                col.Item().Text("Sandık Listesi / Case List").Bold().FontSize(10).FontColor(Mavi);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(22);
                        c.ConstantColumn(55);
                        c.RelativeColumn(2.6f);
                        c.RelativeColumn(1.4f);
                        c.ConstantColumn(52);
                        c.ConstantColumn(58);
                    });
                    table.Header(h =>
                    {
                        BaslikHucresi(h.Cell(), "#");
                        BaslikHucresi(h.Cell(), "KOLİ NO / CASE NO");
                        BaslikHucresi(h.Cell(), "SANDIK ADI / DESCRIPTION");
                        BaslikHucresi(h.Cell(), "BOY × EN × YÜK. / L × W × H (mm)");
                        BaslikHucresi(h.Cell(), "NET KG", sag: true);
                        BaslikHucresi(h.Cell(), "BRÜT / GROSS KG", sag: true);
                    });
                    var sira = 0;
                    foreach (var sandik in liste.Sandiklar)
                    {
                        var zebra = sira++ % 2 == 1;
                        Hucre(table.Cell(), Sayi(sira), zebra);
                        Hucre(table.Cell(), sandik.SandikNo, zebra, kalin: true, renk: Mavi);
                        Hucre(table.Cell(), MusteriSandikAdi(sandik), zebra);
                        Hucre(table.Cell(), MusteriOlcu(sandik), zebra);
                        Hucre(table.Cell(), Kg(sandik.NetKg), zebra, sag: true);
                        Hucre(table.Cell(), Kg(sandik.BrutKg), zebra, sag: true);
                    }
                    ToplamHucresi(table.Cell().ColumnSpan(4), $"TOPLAM / TOTAL · {Sayi(liste.Sandiklar.Count)} koli / cases");
                    ToplamHucresi(table.Cell(), Sayi(liste.NetKg), sag: true);
                    ToplamHucresi(table.Cell(), Sayi(liste.BrutKg), sag: true);
                });

                col.Item().Text("Ürün Listesi / Item List").Bold().FontSize(10).FontColor(Mavi);
                foreach (var sandik in liste.Sandiklar)
                {
                    col.Item().EnsureSpace(70).Column(sc =>
                    {
                        sc.Item().Background(AcikMavi).PaddingVertical(5).PaddingHorizontal(8).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span($"Koli No / Case No: {sandik.SandikNo}").Bold().FontColor(Mavi);
                                t.Span($"   {MusteriSandikAdi(sandik)}").FontColor(Colors.Grey.Darken2);
                            });
                            row.AutoItem().AlignMiddle().Text($"{Sayi(sandik.Urunler.Count)} kalem / items").FontColor(Gri);
                        });
                        if (sandik.Urunler.Count == 0)
                        {
                            sc.Item().Border(0.5f).BorderColor(Kenar).Padding(5)
                                .Text("Bu sandıkta ürün kaydı yok / No items recorded for this case").FontColor(Gri);
                            return;
                        }
                        sc.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(22);
                                c.ConstantColumn(55);
                                c.ConstantColumn(85);
                                c.RelativeColumn();
                                c.ConstantColumn(50);
                                c.ConstantColumn(45);
                            });
                            table.Header(h =>
                            {
                                BaslikHucresi(h.Cell(), "#");
                                BaslikHucresi(h.Cell(), "POZ / ITEM");
                                BaslikHucresi(h.Cell(), "MALZEME KODU / MATERIAL CODE");
                                BaslikHucresi(h.Cell(), "AÇIKLAMA / DESCRIPTION");
                                BaslikHucresi(h.Cell(), "MİKTAR / QTY", sag: true);
                                BaslikHucresi(h.Cell(), "BİRİM / UNIT");
                            });
                            var sira = 0;
                            foreach (var urun in sandik.Urunler)
                            {
                                var zebra = sira++ % 2 == 1;
                                Hucre(table.Cell(), Sayi(sira), zebra);
                                Hucre(table.Cell(), Metin(urun.PozNo), zebra);
                                Hucre(table.Cell(), Metin(urun.MalzemeKodu), zebra);
                                Hucre(table.Cell(), Metin(urun.Aciklama), zebra);
                                Hucre(table.Cell(), urun.Miktar.ToString("#,##0.####", Tr), zebra, sag: true, kalin: true);
                                Hucre(table.Cell(), Metin(urun.Birim), zebra);
                            }
                        });
                    });
                }
            });

            page.Footer().Element(c => Altbilgi(c, $"3K – {MusteriBasligi} / {MusteriBasligiEn} | {liste.ProjeNo}"));
        })).GeneratePdf();
    }

    public byte[] MusteriCekiExcelOlustur(MusteriCekiListesi liste)
    {
        using var wb = new XLWorkbook();
        var baslik = $"{MusteriBasligi} / {MusteriBasligiEn} – {liste.ProjeNo}";
        var bilgi1 = $"Müşteri / Customer: {Metin(liste.Musteri)} · Lokasyon / Location: {Metin(liste.Lokasyon)}";
        var bilgi2 = $"Koli / Cases: {Sayi(liste.Sandiklar.Count)} · Net kg: {Sayi(liste.NetKg)} · Brüt / Gross kg: " +
                     $"{Sayi(liste.BrutKg)} · Tarih / Date: {Tarih(liste.OlusturmaTarihi, false)}";

        Sayfa(wb, "Sandıklar - Cases", baslik, bilgi1, bilgi2,
            ["#", "Koli No / Case No", "Sandık Adı / Description", "Sandık Adı (EN) / Description (EN)", "Boy / Length (mm)",
             "En / Width (mm)", "Yükseklik / Height (mm)", "Net kg", "Brüt / Gross kg", "Kalem / Items"],
            liste.Sandiklar.Select((s, i) => new object?[]
            {
                i + 1, s.SandikNo, s.Ad, s.AdIngilizce, s.Boy, s.En, s.Yukseklik, s.NetKg, s.BrutKg, s.Urunler.Count
            }));

        Sayfa(wb, "Ürünler - Items", baslik, bilgi1, bilgi2,
            ["Koli No / Case No", "Sandık Adı / Description", "#", "Poz / Item", "Malzeme Kodu / Material Code",
             "Açıklama / Description", "Miktar / Qty", "Birim / Unit"],
            liste.Sandiklar.SelectMany(s => s.Urunler.Select((u, i) => new object?[]
            {
                s.SandikNo, MusteriSandikAdi(s), i + 1, u.PozNo, u.MalzemeKodu, u.Aciklama, u.Miktar, u.Birim
            })));

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static string MusteriSandikAdi(MusteriCekiSandigi sandik) =>
        string.IsNullOrWhiteSpace(sandik.AdIngilizce) ? Metin(sandik.Ad) : $"{Metin(sandik.Ad)} / {sandik.AdIngilizce.Trim()}";

    private static string MusteriOlcu(MusteriCekiSandigi sandik) =>
        sandik.Boy is null && sandik.En is null && sandik.Yukseklik is null
            ? "-"
            : $"{Kg(sandik.Boy)} × {Kg(sandik.En)} × {Kg(sandik.Yukseklik)}";
}
