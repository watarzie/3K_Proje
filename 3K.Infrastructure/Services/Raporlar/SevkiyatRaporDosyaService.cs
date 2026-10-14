using System.Globalization;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Sevkiyat raporu dosyaları (Sevkiyatlar sayfası). Dönem PDF'i özet ve sefer × proje tablosu, dönem Excel'i bütün
/// döküm (Seferler, Araçlar, Projeler, Sandıklar, Ürünler), sefer PDF'i tek seferin yazdırılabilir belgesidir.
/// Sayı ve tarihler tr-TR biçimindedir; ağırlığı girilmemiş sandık "-" yazılır ve toplamlara katılmaz.
/// </summary>
public sealed class SevkiyatRaporDosyaService : ISevkiyatRaporDosyaService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private const string Mavi = "#1565C0";
    private const string AcikMavi = "#E3F2FD";
    private const string Zebra = "#F8FAFE";
    private const string Kenar = "#D9E2EC";
    private const string Gri = "#64748B";
    private const string DuzeltmeNotu =
        "* işaretli seferlerde / sandıklarda sevkiyat düzeltmesi açık (sandık sevk sonrası düzenleme için açıldı).";
    private const string AnlikNotu =
        "Ölçü, ağırlık, depo ve içerik bilgileri raporun alındığı andaki sandık kayıtlarıdır.";

    // ===== Dönem PDF =====

    public byte[] DonemPdfOlustur(SevkiyatRaporu rapor)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var araclar = rapor.AracOzetleri();
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(18);
            page.DefaultTextStyle(x => x.FontSize(8));

            page.Header().Column(col =>
            {
                col.Item().Element(c => Baslik(c, rapor.Baslik, "3K Sevkiyat Yönetim Sistemi – Tır seferleri, projeler ve sandıklar",
                    $"Rapor tarihi: {Tarih(rapor.OlusturmaTarihi, true)}", $"{rapor.Seferler.Count} sefer"));
                col.Item().PaddingTop(6).Text(t =>
                {
                    t.Span("Süzgeç: ").SemiBold();
                    t.Span(rapor.SuzgecAciklamasi);
                });
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(10);
                col.Item().Row(row =>
                {
                    row.Spacing(8);
                    Kutu(row.RelativeItem(), "Sefer", Sayi(rapor.Seferler.Count));
                    Kutu(row.RelativeItem(), "Araç", Sayi(araclar.Count));
                    Kutu(row.RelativeItem(), "Proje", Sayi(rapor.ProjeSayisi));
                    Kutu(row.RelativeItem(), "Sandık", Sayi(rapor.SandikSayisi));
                    Kutu(row.RelativeItem(), "Net kg", Sayi(rapor.NetKg));
                    Kutu(row.RelativeItem(), "Brüt kg", Sayi(rapor.BrutKg));
                });

                col.Item().Text("Araç özeti").Bold().FontSize(10).FontColor(Mavi);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(90);
                        c.ConstantColumn(45);
                        c.ConstantColumn(45);
                        c.ConstantColumn(50);
                        c.ConstantColumn(65);
                        c.ConstantColumn(85);
                        c.ConstantColumn(85);
                        c.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        BaslikHucresi(h.Cell(), "ARAÇ PLAKASI");
                        BaslikHucresi(h.Cell(), "SEFER", sag: true);
                        BaslikHucresi(h.Cell(), "PROJE", sag: true);
                        BaslikHucresi(h.Cell(), "SANDIK", sag: true);
                        BaslikHucresi(h.Cell(), "BRÜT KG", sag: true);
                        BaslikHucresi(h.Cell(), "İLK SEVK");
                        BaslikHucresi(h.Cell(), "SON SEVK");
                        BaslikHucresi(h.Cell(), "TAŞINAN PROJELER");
                    });
                    var sira = 0;
                    foreach (var arac in araclar)
                    {
                        var zebra = sira++ % 2 == 1;
                        Hucre(table.Cell(), arac.Plaka, zebra, kalin: true);
                        Hucre(table.Cell(), Sayi(arac.SeferSayisi), zebra, sag: true);
                        Hucre(table.Cell(), Sayi(arac.ProjeSayisi), zebra, sag: true);
                        Hucre(table.Cell(), Sayi(arac.SandikSayisi), zebra, sag: true);
                        Hucre(table.Cell(), Sayi(arac.BrutKg), zebra, sag: true);
                        Hucre(table.Cell(), Tarih(arac.IlkSevk, false), zebra);
                        Hucre(table.Cell(), Tarih(arac.SonSevk, false), zebra);
                        Hucre(table.Cell(), string.Join(", ", arac.Projeler), zebra);
                    }
                });

                col.Item().Text("Seferler").Bold().FontSize(10).FontColor(Mavi);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(62);
                        c.ConstantColumn(70);
                        c.ConstantColumn(80);
                        c.RelativeColumn(0.9f);
                        c.RelativeColumn(0.9f);
                        ProjeSutunlari(c);
                    });
                    table.Header(h =>
                    {
                        BaslikHucresi(h.Cell(), "TARİH");
                        BaslikHucresi(h.Cell(), "SEFER NO");
                        BaslikHucresi(h.Cell(), "ARAÇ / DORSE");
                        BaslikHucresi(h.Cell(), "ŞOFÖR");
                        BaslikHucresi(h.Cell(), "GÖNDEREN");
                        BaslikHucresi(h.Cell(), "PROJE");
                        BaslikHucresi(h.Cell(), "MÜŞTERİ");
                        BaslikHucresi(h.Cell(), "SANDIK", sag: true);
                        BaslikHucresi(h.Cell(), "BRÜT KG", sag: true);
                        BaslikHucresi(h.Cell(), "SANDIK NUMARALARI");
                    });
                    // Sefer tek satırdır (sayfa sonunda bölünmez); projeler aynı sütun genişlikli iç tabloda alt alta.
                    var seferSira = 0;
                    foreach (var sefer in rapor.Seferler)
                    {
                        var zebra = seferSira++ % 2 == 1;
                        Hucre(table.Cell(), SeferTarihi(sefer), zebra);
                        Hucre(table.Cell(), SeferEtiketi(sefer), zebra, kalin: true, renk: Mavi);
                        Hucre(table.Cell(), AracDorse(sefer), zebra);
                        Hucre(table.Cell(), Metin(sefer.Sofor), zebra);
                        Hucre(table.Cell(), Metin(sefer.Gonderen), zebra);
                        table.Cell().ColumnSpan(5).Background(zebra ? Zebra : Colors.White).Border(0.5f).BorderColor(Kenar).Table(ic =>
                        {
                            ic.ColumnsDefinition(ProjeSutunlari);
                            var son = sefer.Projeler.Count - 1;
                            for (var i = 0; i <= son; i++)
                            {
                                var proje = sefer.Projeler[i];
                                IContainer IcHucre() => ic.Cell().BorderBottom(i < son ? 0.5f : 0).BorderColor(Kenar).PaddingVertical(3).PaddingHorizontal(5);
                                IcHucre().Text(proje.ProjeNo).SemiBold();
                                IcHucre().Text(Metin(proje.Musteri));
                                IcHucre().AlignRight().Text(Sayi(proje.Sandiklar.Count));
                                IcHucre().AlignRight().Text(Sayi(proje.BrutKg));
                                IcHucre().Text(SandikNumaralari(proje.Sandiklar));
                            }
                        });
                    }
                    ToplamHucresi(table.Cell().ColumnSpan(7), "TOPLAM");
                    ToplamHucresi(table.Cell(), Sayi(rapor.SandikSayisi), sag: true);
                    ToplamHucresi(table.Cell(), Sayi(rapor.BrutKg), sag: true);
                    ToplamHucresi(table.Cell(), string.Empty);
                });

                col.Item().Element(c => Notlar(c, rapor.Seferler.Any(s => s.DuzeltmeAcikSandikVar)));
            });

            page.Footer().Element(c => Altbilgi(c, $"3K – {rapor.Baslik} | {rapor.SuzgecAciklamasi}"));
        })).GeneratePdf();
    }

    // ===== Sefer PDF =====

    public byte[] SeferPdfOlustur(SevkiyatRaporSeferi sefer, DateTime olusturmaTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(22);
            page.DefaultTextStyle(x => x.FontSize(8.5f));

            page.Header().Element(c => Baslik(c, "Sefer Raporu", $"{SeferEtiketi(sefer)} · {SeferTarihi(sefer)}",
                $"Rapor tarihi: {Tarih(olusturmaTarihi, true)}", AracDorse(sefer)));

            page.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(85);
                        c.RelativeColumn();
                        c.ConstantColumn(85);
                        c.RelativeColumn();
                    });
                    void Bilgi(string etiket, string deger)
                    {
                        table.Cell().Background(AcikMavi).Border(0.5f).BorderColor(Kenar).Padding(5).Text(etiket).SemiBold().FontColor(Mavi);
                        table.Cell().Border(0.5f).BorderColor(Kenar).Padding(5).Text(deger);
                    }
                    Bilgi("Sefer no", SeferEtiketi(sefer));
                    Bilgi("Sevk tarihi", SeferTarihi(sefer));
                    Bilgi("Araç plakası", Metin(sefer.AracPlaka));
                    Bilgi("Dorse plakası", Metin(sefer.DorsePlaka));
                    Bilgi("Şoför", Metin(sefer.Sofor));
                    Bilgi("Gönderen", Metin(sefer.Gonderen));
                    Bilgi("Proje / sandık", $"{Sayi(sefer.Projeler.Count)} proje · {Sayi(sefer.SandikSayisi)} sandık");
                    Bilgi("Net / brüt kg", $"{Sayi(sefer.NetKg)} / {Sayi(sefer.BrutKg)}");
                    if (!string.IsNullOrWhiteSpace(sefer.Aciklama))
                    {
                        table.Cell().Background(AcikMavi).Border(0.5f).BorderColor(Kenar).Padding(5).Text("Açıklama").SemiBold().FontColor(Mavi);
                        table.Cell().ColumnSpan(3).Border(0.5f).BorderColor(Kenar).Padding(5).Text(sefer.Aciklama!.Trim());
                    }
                });

                foreach (var proje in sefer.Projeler)
                {
                    col.Item().EnsureSpace(90).Column(pc =>
                    {
                        pc.Item().Background(AcikMavi).PaddingVertical(6).PaddingHorizontal(8).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span(proje.ProjeNo).Bold().FontSize(10).FontColor(Mavi);
                                t.Span($"   {Metin(proje.Musteri)}").FontColor(Colors.Grey.Darken2);
                            });
                            row.AutoItem().AlignMiddle().Text(
                                $"{Metin(proje.ProjeTipi)} · Sevkiyat #{proje.SevkiyatNo} · {Sayi(proje.Sandiklar.Count)} sandık · {Sayi(proje.BrutKg)} kg brüt")
                                .FontColor(Gri);
                        });
                        pc.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(22);
                                c.ConstantColumn(52);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(1.3f);
                                c.ConstantColumn(46);
                                c.ConstantColumn(50);
                                c.ConstantColumn(48);
                            });
                            table.Header(h =>
                            {
                                BaslikHucresi(h.Cell(), "#");
                                BaslikHucresi(h.Cell(), "SANDIK NO");
                                BaslikHucresi(h.Cell(), "SANDIK ADI");
                                BaslikHucresi(h.Cell(), "TİP");
                                BaslikHucresi(h.Cell(), "BOY × EN × YÜK. (mm)");
                                BaslikHucresi(h.Cell(), "NET KG", sag: true);
                                BaslikHucresi(h.Cell(), "BRÜT KG", sag: true);
                                BaslikHucresi(h.Cell(), "DEPO");
                            });
                            var sira = 0;
                            foreach (var sandik in proje.Sandiklar)
                            {
                                var zebra = sira++ % 2 == 1;
                                Hucre(table.Cell(), Sayi(sira), zebra);
                                Hucre(table.Cell(), sandik.SandikNo + (sandik.DuzeltmeAcik ? " *" : string.Empty), zebra, kalin: true, renk: Mavi);
                                Hucre(table.Cell(), SandikAdi(sandik), zebra);
                                Hucre(table.Cell(), Metin(sandik.Tip), zebra);
                                Hucre(table.Cell(), Olcu(sandik), zebra);
                                Hucre(table.Cell(), Kg(sandik.NetKg), zebra, sag: true);
                                Hucre(table.Cell(), Kg(sandik.BrutKg), zebra, sag: true);
                                Hucre(table.Cell(), Metin(sandik.DepoLokasyonu), zebra);
                            }
                            ToplamHucresi(table.Cell().ColumnSpan(5), $"TOPLAM · {Sayi(proje.Sandiklar.Count)} sandık");
                            ToplamHucresi(table.Cell(), Sayi(proje.NetKg), sag: true);
                            ToplamHucresi(table.Cell(), Sayi(proje.BrutKg), sag: true);
                            ToplamHucresi(table.Cell(), string.Empty);
                        });
                    });
                }

                col.Item().Element(c => Notlar(c, sefer.DuzeltmeAcikSandikVar));

                // Teslim belgesi olarak yazdırıldığında imza alanı.
                col.Item().PaddingTop(16).ShowEntire().Row(row =>
                {
                    row.Spacing(40);
                    foreach (var etiket in new[] { "Sevk eden", "Teslim alan (şoför)" })
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(etiket).SemiBold().FontColor(Mavi);
                            c.Item().PaddingTop(34).LineHorizontal(0.6f).LineColor(Colors.Grey.Medium);
                            c.Item().PaddingTop(3).Text("Ad soyad / imza / tarih").FontSize(7).FontColor(Gri);
                        });
                    }
                });
            });

            page.Footer().Element(c => Altbilgi(c, $"3K – Sefer Raporu | {SeferEtiketi(sefer)} | {AracDorse(sefer)}"));
        })).GeneratePdf();
    }

    // ===== Dönem Excel =====

    public byte[] DonemExcelOlustur(SevkiyatRaporu rapor)
    {
        using var wb = new XLWorkbook();
        var seferler = rapor.Seferler;

        Sayfa(wb, "Seferler", rapor,
            ["Sefer no", "Kayıt türü", "Sevk tarihi", "Araç plakası", "Dorse plakası", "Şoför", "Gönderen", "Proje sayısı",
             "Sandık sayısı", "Net kg", "Brüt kg", "Projeler", "Düzeltmesi açık sandık", "Açıklama"],
            seferler.Select(s => new object?[]
            {
                s.Etiket, s.EskiKayit ? "Eski kayıt (sefer öncesi)" : "Sefer", s.SevkTarihi, s.AracPlaka, s.DorsePlaka, s.Sofor,
                s.Gonderen, s.Projeler.Count, s.SandikSayisi, s.NetKg, s.BrutKg,
                string.Join(", ", s.Projeler.Select(p => p.ProjeNo)), s.DuzeltmeAcikSandikVar, s.Aciklama
            }));

        Sayfa(wb, "Araçlar", rapor,
            ["Araç plakası", "Sefer sayısı", "Proje sayısı", "Sandık sayısı", "Brüt kg", "İlk sevk", "Son sevk", "Taşınan projeler"],
            rapor.AracOzetleri().Select(a => new object?[]
            {
                a.Plaka, a.SeferSayisi, a.ProjeSayisi, a.SandikSayisi, a.BrutKg, a.IlkSevk, a.SonSevk, string.Join(", ", a.Projeler)
            }));

        Sayfa(wb, "Projeler", rapor,
            ["Sefer no", "Sevk tarihi", "Araç plakası", "Şoför", "Proje no", "Müşteri", "Proje tipi", "Sevkiyat no", "Lokasyon",
             "Sandık sayısı", "Net kg", "Brüt kg", "Sandık numaraları"],
            seferler.SelectMany(s => s.Projeler.Select(p => new object?[]
            {
                s.Etiket, s.SevkTarihi, s.AracPlaka, s.Sofor, p.ProjeNo, p.Musteri, p.ProjeTipi, p.SevkiyatNo, p.Lokasyon,
                p.Sandiklar.Count, p.NetKg, p.BrutKg, SandikNumaralari(p.Sandiklar)
            })));

        Sayfa(wb, "Sandıklar", rapor,
            ["Sefer no", "Sevk tarihi", "Araç plakası", "Proje no", "Sandık no", "Sandık adı", "Sandık adı (EN)", "Tip",
             "Boy (mm)", "En (mm)", "Yükseklik (mm)", "Net kg", "Brüt kg", "Depo", "Sevkiyat düzeltmesi açık", "Ürün kalemi"],
            seferler.SelectMany(s => s.Projeler.SelectMany(p => p.Sandiklar.Select(k => new object?[]
            {
                s.Etiket, s.SevkTarihi, s.AracPlaka, p.ProjeNo, k.SandikNo, k.Ad, k.AdIngilizce, k.Tip, k.Boy, k.En, k.Yukseklik,
                k.NetKg, k.BrutKg, k.DepoLokasyonu, k.DuzeltmeAcik, k.Urunler.Count
            }))));

        Sayfa(wb, "Ürünler", rapor,
            ["Sefer no", "Sevk tarihi", "Araç plakası", "Proje no", "Sandık no", "Poz no", "Malzeme kodu", "Açıklama",
             "Sandık miktarı", "Konulan", "Eksik", "Birim"],
            seferler.SelectMany(s => s.Projeler.SelectMany(p => p.Sandiklar.SelectMany(k => k.Urunler.Select(u => new object?[]
            {
                s.Etiket, s.SevkTarihi, s.AracPlaka, p.ProjeNo, k.SandikNo, u.PozNo, u.MalzemeKodu, u.Aciklama, u.Miktar,
                u.Konulan, u.Eksik, u.Birim
            })))));

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>Başlık (1), süzgeç ve rapor tarihi (2–3), tablo başlığı (5), veri (6+); otomatik süzgeç ve dondurma.</summary>
    private static void Sayfa(XLWorkbook wb, string ad, SevkiyatRaporu rapor, string[] basliklar, IEnumerable<object?[]> satirlar)
    {
        const int baslikSatiri = 5;
        var ws = wb.Worksheets.Add(ad);
        var sonSutun = basliklar.Length;
        ws.Cell(1, 1).Value = $"{rapor.Baslik} – {ad}";
        RaporExcelStilleri.StyleExcelTitle(ws, sonSutun);
        ws.Cell(2, 1).Value = $"Süzgeç: {rapor.SuzgecAciklamasi}";
        ws.Cell(3, 1).Value = $"Rapor tarihi: {Tarih(rapor.OlusturmaTarihi, true)} · {AnlikNotu}";
        RaporExcelStilleri.StyleExcelInfoRows(ws, sonSutun);
        for (var i = 0; i < basliklar.Length; i++)
            ws.Cell(baslikSatiri, i + 1).Value = basliklar[i];
        RaporExcelStilleri.StyleExcelHeader(ws, baslikSatiri, sonSutun);

        var satir = baslikSatiri + 1;
        foreach (var degerler in satirlar)
        {
            for (var i = 0; i < degerler.Length; i++)
                Yaz(ws.Cell(satir, i + 1), degerler[i]);
            satir++;
        }
        var sonSatir = satir - 1;
        if (sonSatir > baslikSatiri)
            ws.Range(baslikSatiri, 1, sonSatir, sonSutun).SetAutoFilter();
        RaporExcelStilleri.FinalizeExcelWorksheet(ws, baslikSatiri, sonSatir, sonSutun);
        // Uzun bilgi satırları ilk sütunu genişletmesin.
        if (ws.Column(1).Width > 24)
            ws.Column(1).Width = 24;
    }

    private static void Yaz(IXLCell hucre, object? deger)
    {
        switch (deger)
        {
            case null:
                return;
            case string metin:
                hucre.SetValue(metin);
                break;
            case int sayi:
                hucre.SetValue(sayi);
                break;
            case decimal ondalik:
                hucre.SetValue(ondalik);
                hucre.Style.NumberFormat.Format = "#,##0.####";
                break;
            case DateTime tarih:
                hucre.SetValue(tarih);
                hucre.Style.DateFormat.Format = tarih.TimeOfDay == TimeSpan.Zero ? "dd.mm.yyyy" : "dd.mm.yyyy hh:mm";
                break;
            case bool evet:
                hucre.SetValue(evet ? "Evet" : "Hayır");
                break;
            default:
                hucre.SetValue(Convert.ToString(deger, Tr));
                break;
        }
    }

    /// <summary>Dönem PDF'inde proje sütunları; başlık ve seferin iç proje tablosu aynı genişlikleri kullanır.</summary>
    private static void ProjeSutunlari(TableColumnsDefinitionDescriptor c)
    {
        c.ConstantColumn(72);
        c.RelativeColumn(1.1f);
        c.ConstantColumn(40);
        c.ConstantColumn(55);
        c.RelativeColumn(1.4f);
    }

    // ===== Ortak PDF parçaları =====

    private static void Baslik(IContainer container, string baslik, string altBaslik, string sagUst, string sagAlt)
    {
        container.Background(Mavi).PaddingVertical(10).PaddingHorizontal(12).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("3K").Bold().FontSize(22).FontColor(Colors.White);
                col.Item().Text("All Processes. One Flow.").FontSize(7).Italic().FontColor(Colors.Grey.Lighten3);
            });
            row.RelativeItem(2).AlignMiddle().Column(col =>
            {
                col.Item().AlignCenter().Text(baslik).Bold().FontSize(16).FontColor(Colors.White);
                col.Item().AlignCenter().Text(altBaslik).FontSize(8).FontColor(Colors.Grey.Lighten3);
            });
            row.RelativeItem().AlignMiddle().Column(col =>
            {
                col.Item().AlignRight().Text(sagUst).FontSize(8).FontColor(Colors.Grey.Lighten3);
                col.Item().AlignRight().Text(sagAlt).FontSize(8).FontColor(Colors.Grey.Lighten3);
            });
        });
    }

    private static void Kutu(IContainer container, string etiket, string deger)
    {
        container.Border(0.5f).BorderColor(Kenar).Background(Zebra).PaddingVertical(6).PaddingHorizontal(8).Column(col =>
        {
            col.Item().Text(etiket.ToUpper(Tr)).FontSize(7).SemiBold().FontColor(Gri);
            col.Item().Text(deger).Bold().FontSize(13).FontColor(Mavi);
        });
    }

    private static void BaslikHucresi(IContainer container, string metin, bool sag = false)
    {
        var hucre = container.Background(Mavi).Border(0.5f).BorderColor(Mavi).PaddingVertical(4).PaddingHorizontal(5);
        (sag ? hucre.AlignRight() : hucre).Text(metin).Bold().FontSize(7.5f).FontColor(Colors.White);
    }

    private static void Hucre(IContainer container, string metin, bool zebra, bool sag = false, bool kalin = false, string? renk = null)
    {
        var hucre = container.Background(zebra ? Zebra : Colors.White).Border(0.5f).BorderColor(Kenar).PaddingVertical(3).PaddingHorizontal(5);
        var yazi = (sag ? hucre.AlignRight() : hucre).Text(metin).FontColor(renk ?? Colors.Black);
        if (kalin)
            yazi.SemiBold();
    }

    private static void ToplamHucresi(IContainer container, string metin, bool sag = false)
    {
        var hucre = container.Background(AcikMavi).Border(0.5f).BorderColor(Kenar).PaddingVertical(4).PaddingHorizontal(5);
        (sag ? hucre.AlignRight() : hucre).Text(metin).Bold();
    }

    private static void Notlar(IContainer container, bool duzeltmeVar)
    {
        container.Column(col =>
        {
            if (duzeltmeVar)
                col.Item().Text(DuzeltmeNotu).FontSize(7.5f).FontColor("#9A6700");
            col.Item().Text(AnlikNotu).FontSize(7.5f).FontColor(Gri);
        });
    }

    private static void Altbilgi(IContainer container, string metin)
    {
        container.PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(metin).FontSize(7).FontColor(Colors.Grey.Medium);
            row.AutoItem().Text(x =>
            {
                x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Medium));
                x.Span("Sayfa ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }

    // ===== Biçimler =====

    private static string SeferEtiketi(SevkiyatRaporSeferi sefer) => sefer.Etiket + (sefer.DuzeltmeAcikSandikVar ? " *" : string.Empty);

    /// <summary>Gerçek seferde saatle; eski kayıtta (gün bazında gruplanır) yalnız tarih.</summary>
    private static string SeferTarihi(SevkiyatRaporSeferi sefer) => Tarih(sefer.SevkTarihi, !sefer.EskiKayit);

    private static string AracDorse(SevkiyatRaporSeferi sefer) =>
        string.IsNullOrWhiteSpace(sefer.DorsePlaka) ? Metin(sefer.AracPlaka) : $"{Metin(sefer.AracPlaka)} + {sefer.DorsePlaka}";

    private static string SandikAdi(SevkiyatRaporSandigi sandik) =>
        string.IsNullOrWhiteSpace(sandik.AdIngilizce) ? Metin(sandik.Ad) : $"{Metin(sandik.Ad)} / {sandik.AdIngilizce}";

    private static string SandikNumaralari(IEnumerable<SevkiyatRaporSandigi> sandiklar) =>
        string.Join(", ", sandiklar.Select(s => s.SandikNo + (s.DuzeltmeAcik ? "*" : string.Empty)));

    private static string Olcu(SevkiyatRaporSandigi sandik) =>
        sandik.Boy is null && sandik.En is null && sandik.Yukseklik is null
            ? "-"
            : $"{Kg(sandik.Boy)} × {Kg(sandik.En)} × {Kg(sandik.Yukseklik)}";

    private static string Kg(decimal? deger) => deger.HasValue ? Sayi(deger.Value) : "-";
    private static string Sayi(decimal deger) => deger.ToString("#,##0.##", Tr);
    private static string Sayi(int deger) => deger.ToString("#,##0", Tr);
    private static string Tarih(DateTime tarih, bool saat) => tarih.ToString(saat ? "dd.MM.yyyy HH:mm" : "dd.MM.yyyy", Tr);
    private static string Metin(string? metin) => string.IsNullOrWhiteSpace(metin) ? "-" : metin.Trim();
}
