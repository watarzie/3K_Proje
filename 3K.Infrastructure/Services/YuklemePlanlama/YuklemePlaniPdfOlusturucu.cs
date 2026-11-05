using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.YuklemeMotoru.Cizim;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>
/// Yükleme planı PDF'leri (Simülasyon isterleri 28–29). İki rapor aynı plan, revizyon ve araç numaralarını kullanır.
/// Çözülmemiş uygunsuzluk ya da yerleşemeyen yük varsa "TASLAK — YÜKLEMEYE UYGUN DEĞİL" ibaresi basılır. Görsellerdeki
/// numaralar araç yük listesindeki "No" ile aynıdır; projeler renkle ayrılır ama kimlik yalnız renge bağlı değildir.
/// Lokasyonu belirsiz yük varsa "Ön planlama" notu yazılır.
/// </summary>
internal sealed class YuklemePlaniPdfOlusturucu : IYuklemePlaniRaporOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private const string Mavi = "#1565C0";
    private const string AcikMavi = "#E3F2FD";
    private const string Zebra = "#F8FAFE";
    private const string Kenar = "#D9E2EC";
    private const string Kirmizi = "#D32F2F";
    private const string Turuncu = "#B45309";
    private const string Yesil = "#2E7D32";
    private const string Gri = "#64748B";

    /// <summary>Ön yüzdeki 3B görünümle aynı proje renkleri.</summary>
    private static readonly string[] ProjeRenkleri =
        ["#0e7490", "#7c3aed", "#ea580c", "#16a34a", "#2563eb", "#be185d", "#a16207", "#0f766e", "#4f46e5", "#65a30d"];

    // ===== Ortak =====

    private static string Mm(int mm) => mm.ToString("#,0", Tr);
    private static string Kg(decimal kg) => kg.ToString("#,0", Tr) + " kg";
    private static string Tarih(DateTime t) => t.ToString("dd.MM.yyyy HH:mm", Tr);

    private static PlanYerlesimi Yerlesim(YuklemePlaniDetayi plan) =>
        plan.Yerlesim ?? throw new InvalidOperationException("Yerleşimi olmayan plan için rapor üretilmez.");

    internal static bool Taslak(YuklemePlaniDetayi plan) =>
        Yerlesim(plan).Sonuc == YerlesimKontrolSonucu.Uygunsuz || Yerlesim(plan).Araclar.Any(a => a.Sonuc == YerlesimKontrolSonucu.Uygunsuz);

    internal static bool BelirsizLokasyonVar(YuklemePlaniDetayi plan) =>
        plan.Yukler.Any(y => y.Dahil && YuklemePlaniKurali.LokasyonBelirsizMi(y.LokasyonId));

    private static Dictionary<string, string> ProjeRenkHaritasi(YuklemePlaniDetayi plan)
    {
        var harita = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < plan.Projeler.Count; i++)
            harita[plan.Projeler[i].ProjeNo] = ProjeRenkleri[i % ProjeRenkleri.Length];
        return harita;
    }

    private static EkipmanTeknikVerisi TeknikVeri(PlanAraci a) => EkipmanTeknikVeriSerilestirici.Oku(a.TeknikVeriJson) ?? new EkipmanTeknikVerisi();

    /// <summary>Araçtaki yükler yükleme sırasıyla; "No" bu sıradaki konumdur (çizim etiketiyle aynı).</summary>
    private static List<PlanYukKonumu> Sirali(PlanAraci a) => a.Konumlar.OrderBy(k => k.YuklemeSirasi).ThenBy(k => k.Kat).ToList();

    private static void UstCubuk(PageDescriptor page, string baslik, YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        page.Header().Column(col =>
        {
            col.Item().Background(Mavi).PaddingVertical(10).PaddingHorizontal(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("3K").Bold().FontSize(22).FontColor(Colors.White);
                    c.Item().Text("All Processes. One Flow.").FontSize(7).Italic().FontColor(Colors.Grey.Lighten3);
                });
                row.RelativeItem(2).AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(15).FontColor(Colors.White);
                    c.Item().AlignCenter().Text($"{plan.PlanNo} · Revizyon {plan.RevizyonNo} · {plan.Ad}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                });
                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text($"Rapor tarihi: {Tarih(raporTarihi)}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                    c.Item().AlignRight().Text($"Hazırlayan: {plan.Kaydeden ?? "-"}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                    c.Item().AlignRight().Text($"Hesap: {Tarih(Yerlesim(plan).HesapTarihi)}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                });
            });
            if (Taslak(plan))
                col.Item().PaddingTop(4).Background("#FDECEA").Border(1).BorderColor(Kirmizi).Padding(5).AlignCenter()
                    .Text(YuklemeEtiketleri.TaslakIbaresi).Bold().FontSize(12).FontColor(Kirmizi);
            // Elle düzenlenen yerleşim otomatik öneri değildir; kontroller kullanıcının konumlarıyla yapılmıştır (isterler 20).
            if (Yerlesim(plan).ElleDuzenlendi)
                col.Item().PaddingTop(4).Background("#EEF2FF").Border(1).BorderColor(Mavi).Padding(4).AlignCenter()
                    .Text(YuklemeEtiketleri.ElleDuzenlendiIbaresi).Bold().FontColor(Mavi);
            // Kaynak sandık verisi plan kopyasından farklılaştıysa rapor eski kopyayla hazırlanmıştır (isterler 27).
            if (plan.KaynakFarklari.Count > 0)
                col.Item().PaddingTop(4).Background("#FFF7ED").Border(1).BorderColor(Turuncu).Padding(4).AlignCenter()
                    .Text($"Kaynak veri değişti ({plan.KaynakFarklari.Count} fark): rapor son hesaplanan kopyayla hazırlandı; kaynaktan güncelleyip yeniden hesaplayın.")
                    .Bold().FontColor(Turuncu);
        });
    }

    private static void AltBilgi(PageDescriptor page, string metin)
    {
        page.Footer().PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text($"{metin} · {YuklemeEtiketleri.IzinNotu}").FontSize(6.5f).FontColor(Colors.Grey.Medium);
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

    private static void Bolum(ColumnDescriptor col, string baslik) =>
        col.Item().PaddingTop(10).PaddingBottom(4).BorderBottom(1).BorderColor(Mavi).Text(baslik.ToUpper(Tr)).Bold().FontSize(9).FontColor(Mavi);

    private static IContainer BaslikHucresi(IContainer c) => c.Background(Mavi).Border(0.5f).BorderColor(Mavi).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer Hucre(IContainer c, bool zebra) => c.Background(zebra ? Zebra : Colors.White).Border(0.5f).BorderColor(Kenar).PaddingVertical(2.5f).PaddingHorizontal(3);

    private static void BilgiSatiri(ColumnDescriptor col, string ad, string deger) =>
        col.Item().Text(t =>
        {
            t.Span(ad + ": ").SemiBold().FontColor(Gri);
            t.Span(deger);
        });

    private static string SonucRengi(YerlesimKontrolSonucu s) => s switch
    {
        YerlesimKontrolSonucu.Uygun => Yesil,
        YerlesimKontrolSonucu.Uygunsuz => Kirmizi,
        _ => Turuncu
    };

    /// <summary>Plan bilgileri kartı (iki raporda aynı).</summary>
    private static void PlanKarti(ColumnDescriptor col, YuklemePlaniDetayi plan)
    {
        var y = Yerlesim(plan);
        var lokasyonlar = y.Araclar.SelectMany(a => a.Lokasyonlar).Distinct().Order(StringComparer.Create(Tr, false)).ToList();
        col.Item().PaddingTop(8).Border(1).BorderColor(Kenar).Padding(8).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                BilgiSatiri(c, "Plan", $"{plan.PlanNo} · Revizyon {plan.RevizyonNo}");
                BilgiSatiri(c, "Çalışma biçimi", YuklemeEtiketleri.CalismaBicimi(plan.CalismaBicimi));
                BilgiSatiri(c, "Genel taşıma ailesi", plan.TasimaAilesiAdi);
                BilgiSatiri(c, "Yükleme tarihi", plan.YuklemeTarihi?.ToString("dd.MM.yyyy", Tr) ?? "Belirtilmedi");
            });
            row.RelativeItem().Column(c =>
            {
                BilgiSatiri(c, "Projeler", string.Join(", ", plan.Projeler.Select(p => p.Musteri is null ? p.ProjeNo : $"{p.ProjeNo} ({p.Musteri})")));
                BilgiSatiri(c, "Proje tercihi", YuklemeEtiketleri.ProjeKarisimi(plan.ProjeKarisimi));
                BilgiSatiri(c, "Lokasyon tercihi", YuklemeEtiketleri.LokasyonTercihi(plan.LokasyonTercihi));
                BilgiSatiri(c, "Lokasyonlar", lokasyonlar.Count == 0 ? "-" : string.Join(", ", lokasyonlar));
            });
        });
        var s = plan.Sayaclar;
        col.Item().PaddingTop(4).Row(row =>
        {
            void Sayac(string ad, int deger, string renk)
            {
                row.RelativeItem().Border(0.5f).BorderColor(Kenar).Background(Zebra).Padding(4).Column(c =>
                {
                    c.Item().Text(ad).FontSize(6.5f).FontColor(Gri);
                    c.Item().Text(deger.ToString(Tr)).Bold().FontSize(10).FontColor(renk);
                });
            }
            Sayac("Seçilen", s.Secilen, Mavi);
            Sayac("Hesaplanan", s.Hesaplanabilir, Mavi);
            Sayac("Yerleşen", s.Yerlesen, Yesil);
            Sayac("Yerleşemeyen", s.Yerlesemeyen, s.Yerlesemeyen > 0 ? Kirmizi : Gri);
            Sayac("Veri eksik (hesaplanmadı)", s.VeriEksik, s.VeriEksik > 0 ? Turuncu : Gri);
            Sayac("Hariç tutulan", s.HaricTutulan, Gri);
        });
        if (BelirsizLokasyonVar(plan))
            col.Item().PaddingTop(4).Background("#FFF7ED").Padding(5).Text(YuklemeEtiketleri.OnPlanlamaNotu).Bold().FontColor(Turuncu);
        if (!string.IsNullOrWhiteSpace(plan.Aciklama))
            col.Item().PaddingTop(4).Text($"Açıklama: {plan.Aciklama}").FontColor(Gri);
    }

    private static string IcOlcu(EkipmanTeknikVerisi v) =>
        v.Yuzeyler.Count == 0
            ? "-"
            : string.Join("; ", v.Yuzeyler.Select(y => $"{Mm(y.UzunlukMm)} × {Mm(y.GenislikMm)}" +
                (y.KullanilabilirYukseklikMm is { } h ? $" × {Mm(h)}" : string.Empty) + " mm" +
                (v.Yuzeyler.Count > 1 ? $" ({y.Ad ?? y.Kod})" : string.Empty)));

    private static string DisOlcu(EkipmanTeknikVerisi v) =>
        v.DisOlcu is { } d ? $"{Mm(d.UzunlukMm)} × {Mm(d.GenislikMm)} × {Mm(d.YukseklikMm)} mm" : "-";

    // ===== PDF 1: Araç Talep ve Planlama Raporu =====

    public byte[] AracTalepRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        // Tip bazında: aynı ekipman ve aynı aile içi/dışı durumu bir satır; karışık lokasyonlu araç bir kez sayılır.
        var tipler = y.Araclar
            .GroupBy(a => (a.KonfigurasyonId, a.RevizyonNo, a.ZorunluOzelEkipman))
            .OrderBy(g => g.Key.ZorunluOzelEkipman).ThenBy(g => g.Min(a => a.AracNo))
            .Select(g => new
            {
                Ilk = g.First(),
                Adet = g.Count(),
                Araclar = string.Join(", ", g.Select(a => a.AracNo)),
                Projeler = string.Join(", ", g.SelectMany(a => a.ProjeNolari).Distinct().Order(StringComparer.Ordinal)),
                Lokasyonlar = string.Join(", ", g.SelectMany(a => a.Lokasyonlar).Distinct().Order(StringComparer.Ordinal)),
                YukAdedi = g.Sum(a => a.Konumlar.Count),
                Kg = g.Sum(a => a.ToplamKg),
                Veri = TeknikVeri(g.First())
            }).ToList();

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(22);
            page.DefaultTextStyle(x => x.FontSize(8).DisableFontFeature(FontFeatures.StandardLigatures));
            UstCubuk(page, "Araç Talep ve Planlama Raporu", plan, raporTarihi);
            AltBilgi(page, $"3K – Araç Talep ve Planlama Raporu | {plan.PlanNo} Rev. {plan.RevizyonNo}");

            page.Content().Column(col =>
            {
                PlanKarti(col, plan);

                Bolum(col, "Araç ihtiyacı (tip bazında)");
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2.2f); // ekipman
                        c.ConstantColumn(34);   // adet
                        c.RelativeColumn(2.2f); // iç ölçü
                        c.ConstantColumn(58);   // kapasite
                        c.RelativeColumn(1.3f); // araç no
                        c.RelativeColumn(1.6f); // projeler
                        c.RelativeColumn(1.4f); // lokasyon
                    });
                    table.Header(h =>
                    {
                        foreach (var b in new[] { "Ekipman", "Adet", "Kullanılabilir iç ölçü (U×G×Y)", "Kapasite", "Araç no", "Projeler", "Lokasyonlar" })
                            BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
                    });
                    var i = 0;
                    foreach (var t in tipler)
                    {
                        var zebra = i++ % 2 == 1;
                        Hucre(table.Cell(), zebra).Column(c =>
                        {
                            c.Item().Text($"{t.Ilk.EkipmanKodu} · {t.Ilk.EkipmanAdi}").SemiBold();
                            c.Item().Text($"{t.Ilk.AileAdi} / {t.Ilk.TipAdi}").FontSize(7).FontColor(Gri);
                            if (t.Ilk.ZorunluOzelEkipman)
                                c.Item().Text("Zorunlu özel ekipman (aile dışı)").FontSize(7).Bold().FontColor(Turuncu);
                            else if (t.Ilk.AileDisi)
                                c.Item().Text("Aile dışı (mevcut araç listesi)").FontSize(7).FontColor(Turuncu);
                        });
                        Hucre(table.Cell(), zebra).AlignRight().Text(t.Adet.ToString(Tr)).Bold();
                        Hucre(table.Cell(), zebra).Text(IcOlcu(t.Veri));
                        Hucre(table.Cell(), zebra).AlignRight().Text(t.Ilk.KapasiteKg is { } k ? Kg(k) : "-");
                        Hucre(table.Cell(), zebra).Text(t.Araclar);
                        Hucre(table.Cell(), zebra).Text(t.Projeler);
                        Hucre(table.Cell(), zebra).Text(t.Lokasyonlar);
                    }
                    if (tipler.Count == 0)
                        table.Cell().ColumnSpan(7).Border(0.5f).BorderColor(Kenar).Padding(6).Text("Araç yok.").FontColor(Gri);
                    else
                    {
                        IContainer Toplam(uint genislik = 1) => table.Cell().ColumnSpan(genislik).Background(AcikMavi).Border(0.5f).BorderColor(Kenar).PaddingVertical(3).PaddingHorizontal(3);
                        Toplam().Text("GENEL TOPLAM").Bold();
                        Toplam().AlignRight().Text(y.Araclar.Count.ToString(Tr)).Bold();
                        Toplam(5).Text($"{y.Araclar.Sum(a => a.Konumlar.Count)} yük · {Kg(y.Araclar.Sum(a => a.ToplamKg))}").Bold();
                    }
                });

                var gerekceler = y.Araclar.Where(a => a.AileDisiGerekce != null).ToList();
                if (gerekceler.Count > 0)
                {
                    Bolum(col, "Özel ekipman şartları ve gerekçeleri");
                    foreach (var a in gerekceler)
                        col.Item().PaddingBottom(3).Text(t =>
                        {
                            t.Span($"Araç {a.AracNo} ({a.EkipmanKodu}): ").Bold().FontColor(Turuncu);
                            t.Span(a.AileDisiGerekce!);
                        });
                }

                Bolum(col, "Gerekli ölçüler ve kapasite (araç bazında)");
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);
                        c.RelativeColumn(1.1f);
                        c.ConstantColumn(26);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.6f);
                        c.RelativeColumn(1.1f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                        c.ConstantColumn(52);
                    });
                    table.Header(h =>
                    {
                        foreach (var b in new[] { "No", "Ekipman", "Yük", "Ağırlık / kapasite", "Yüklü ölçü (U×G×Y)", "Taşma", "Projeler", "Lokasyonlar", "Sonuç" })
                            BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
                    });
                    var i = 0;
                    foreach (var a in y.Araclar)
                    {
                        var zebra = i++ % 2 == 1;
                        Hucre(table.Cell(), zebra).Text(a.AracNo.ToString(Tr)).Bold();
                        Hucre(table.Cell(), zebra).Text(a.EkipmanKodu);
                        Hucre(table.Cell(), zebra).AlignRight().Text(a.Konumlar.Count.ToString(Tr));
                        Hucre(table.Cell(), zebra).Text(a.KapasiteKg is { } k ? $"{Kg(a.ToplamKg)} / {Kg(k)}" : Kg(a.ToplamKg));
                        Hucre(table.Cell(), zebra).Text($"{Mm(a.YukluUzunlukMm)} × {Mm(a.YukluGenislikMm)} × {Mm(a.YukluYukseklikMm)} mm");
                        Hucre(table.Cell(), zebra).Text(TasmaMetni(a));
                        Hucre(table.Cell(), zebra).Text(string.Join(", ", a.ProjeNolari));
                        Hucre(table.Cell(), zebra).Text(string.Join(", ", a.Lokasyonlar));
                        Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontColor(SonucRengi(a.Sonuc));
                    }
                });

                if (y.EkIhtiyaclar.Count > 0)
                {
                    Bolum(col, "Ek araç ihtiyacı (mevcut araçlara sığmayan yükler için; plana eklenmedi)");
                    foreach (var e in y.EkIhtiyaclar)
                        col.Item().Text($"• {e.Adet} × {e.EkipmanKodu} — {e.EkipmanAdi}").Bold().FontColor(Turuncu);
                }

                Bolum(col, "İzin ve organizasyon uyarıları");
                foreach (var u in y.Uyarilar)
                    col.Item().PaddingBottom(2).Text("• " + u);
                foreach (var a in y.Araclar)
                {
                    foreach (var k in a.Kontroller.Where(k => k.Sonuc == YerlesimKontrolSonucu.Uygunsuz ||
                                                              k.Tur is YerlesimKontrolTuru.Tasma or YerlesimKontrolTuru.YasalGenislik or YerlesimKontrolTuru.YasalYukseklik)
                                 .Where(k => k.Sonuc != YerlesimKontrolSonucu.Uygun))
                        col.Item().PaddingBottom(2).Text(t =>
                        {
                            t.Span($"• Araç {a.AracNo} · {YuklemeEtiketleri.KontrolTuru(k.Tur)} ({YuklemeEtiketleri.KontrolSonucu(k.Sonuc)}): ").SemiBold().FontColor(SonucRengi(k.Sonuc));
                            t.Span(k.Mesaj);
                        });
                }
                if (y.Uyarilar.Count == 0 && y.Araclar.All(a => a.Kontroller.All(k => k.Sonuc == YerlesimKontrolSonucu.Uygun ||
                        (k.Sonuc == YerlesimKontrolSonucu.TeyitEdilmedi && k.Tur is not (YerlesimKontrolTuru.Tasma or YerlesimKontrolTuru.YasalGenislik or YerlesimKontrolTuru.YasalYukseklik)))))
                    col.Item().Text("İzin ve organizasyon uyarısı yok.").FontColor(Gri);

                // Teyit gereksinimleri: veri ya da pay eksik olduğu için kesin sonuç verilemeyen kontroller (isterler 29).
                var teyitler = y.Araclar
                    .SelectMany(a => a.Kontroller.Where(k => k.Sonuc == YerlesimKontrolSonucu.TeyitEdilmedi &&
                                                             k.Tur is not (YerlesimKontrolTuru.Tasma or YerlesimKontrolTuru.YasalGenislik or YerlesimKontrolTuru.YasalYukseklik))
                        .Select(k => (k.Tur, a.AracNo)))
                    .GroupBy(x => x.Tur)
                    .OrderBy(g => g.Key)
                    .ToList();
                Bolum(col, "Teyit gereksinimleri");
                foreach (var g in teyitler)
                    col.Item().PaddingBottom(2).Text(t =>
                    {
                        t.Span($"• {YuklemeEtiketleri.KontrolTuru(g.Key)}: ").SemiBold().FontColor(Turuncu);
                        t.Span($"araç {string.Join(", ", g.Select(x => x.AracNo).Distinct())} (ayrıntı araç bazlı yükleme planında)");
                    });
                if (teyitler.Count == 0)
                    col.Item().Text("Teyit gerektiren kontrol yok.").FontColor(Gri);

                var eksik = plan.Sayaclar.VeriEksik;
                if (y.Yerlesemeyenler.Count > 0 || eksik > 0)
                {
                    Bolum(col, "Plana girmeyen yükler");
                    if (y.Yerlesemeyenler.Count > 0)
                        col.Item().Text($"Yerleşemeyen: {y.Yerlesemeyenler.Count} yük ({string.Join(", ", y.Yerlesemeyenler.Select(x => x.Kod).Take(15))}{(y.Yerlesemeyenler.Count > 15 ? ", …" : string.Empty)}).").FontColor(Kirmizi);
                    if (eksik > 0)
                        col.Item().Text($"Ölçü/ağırlık eksik olduğu için hesaplanmayan: {eksik} yük.").FontColor(Turuncu);
                    col.Item().Text("Nedenler araç bazlı yükleme planı raporunun son bölümündedir.").FontColor(Gri);
                }
            });
        })).GeneratePdf();
    }

    private static string TasmaMetni(PlanAraci a)
    {
        var parcalar = new List<string>();
        if (a.SolTasmaMm > 0) parcalar.Add($"sol {Mm(a.SolTasmaMm)}");
        if (a.SagTasmaMm > 0) parcalar.Add($"sağ {Mm(a.SagTasmaMm)}");
        if (a.OnTasmaMm > 0) parcalar.Add($"ön {Mm(a.OnTasmaMm)}");
        if (a.ArkaTasmaMm > 0) parcalar.Add($"arka {Mm(a.ArkaTasmaMm)}");
        return parcalar.Count == 0 ? "Yok" : string.Join(", ", parcalar) + " mm";
    }

    // ===== PDF 2: Araç Bazlı Yükleme Planı Raporu =====

    public byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        var renkler = ProjeRenkHaritasi(plan);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7.5f).DisableFontFeature(FontFeatures.StandardLigatures));
                UstCubuk(page, "Araç Bazlı Yükleme Planı", plan, raporTarihi);
                AltBilgi(page, $"3K – Araç Bazlı Yükleme Planı | {plan.PlanNo} Rev. {plan.RevizyonNo}");
                page.Content().Column(col =>
                {
                    PlanKarti(col, plan);
                    Bolum(col, "Araçlar");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(34);
                            c.RelativeColumn(2);
                            c.ConstantColumn(40);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.6f);
                            c.ConstantColumn(64);
                        });
                        table.Header(h =>
                        {
                            foreach (var b in new[] { "Araç", "Ekipman", "Yük", "Ağırlık / kapasite", "Yüklü ölçü (U×G×Y)", "Projeler", "Lokasyonlar", "Sonuç" })
                                BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
                        });
                        var i = 0;
                        foreach (var a in y.Araclar)
                        {
                            var zebra = i++ % 2 == 1;
                            Hucre(table.Cell(), zebra).Text(a.AracNo.ToString(Tr)).Bold();
                            Hucre(table.Cell(), zebra).Text($"{a.EkipmanKodu} · {a.EkipmanAdi}" +
                                (a.ZorunluOzelEkipman ? " (zorunlu özel ekipman)" : a.AileDisi ? " (aile dışı, mevcut araç)" : string.Empty));
                            Hucre(table.Cell(), zebra).AlignRight().Text(a.Konumlar.Count.ToString(Tr));
                            Hucre(table.Cell(), zebra).Text(a.KapasiteKg is { } k ? $"{Kg(a.ToplamKg)} / {Kg(k)}" : Kg(a.ToplamKg));
                            Hucre(table.Cell(), zebra).Text($"{Mm(a.YukluUzunlukMm)} × {Mm(a.YukluGenislikMm)} × {Mm(a.YukluYukseklikMm)} mm");
                            Hucre(table.Cell(), zebra).Text(string.Join(", ", a.ProjeNolari));
                            Hucre(table.Cell(), zebra).Text(string.Join(", ", a.Lokasyonlar));
                            Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontColor(SonucRengi(a.Sonuc));
                        }
                    });
                    if (y.Uyarilar.Count > 0)
                    {
                        Bolum(col, "Genel uyarılar");
                        foreach (var u in y.Uyarilar)
                            col.Item().PaddingBottom(2).Text("• " + u);
                    }
                    Renkler(col, plan, renkler);
                });
            });

            foreach (var a in y.Araclar)
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontSize(7.5f).DisableFontFeature(FontFeatures.StandardLigatures));
                    UstCubuk(page, "Araç Bazlı Yükleme Planı", plan, raporTarihi);
                    AltBilgi(page, $"3K – Araç Bazlı Yükleme Planı | {plan.PlanNo} Rev. {plan.RevizyonNo} | Araç {a.AracNo} / {y.Araclar.Count}");
                    page.Content().Column(col => AracSayfasi(col, a, y.Araclar.Count, renkler));
                });

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7.5f).DisableFontFeature(FontFeatures.StandardLigatures));
                UstCubuk(page, "Araç Bazlı Yükleme Planı", plan, raporTarihi);
                AltBilgi(page, $"3K – Araç Bazlı Yükleme Planı | {plan.PlanNo} Rev. {plan.RevizyonNo}");
                page.Content().Column(col => PlanaGirmeyenler(col, plan));
            });
        }).GeneratePdf();
    }

    private static void Renkler(ColumnDescriptor col, YuklemePlaniDetayi plan, Dictionary<string, string> renkler)
    {
        // Çok projede satır kaydırır (proje sayısı sınırsız); renk 10 projeden sonra tekrar eder, kimlik listede yazılıdır.
        col.Item().PaddingTop(8).Text("Renkler").SemiBold().FontColor(Gri);
        col.Item().PaddingTop(2).Inlined(satir =>
        {
            satir.Spacing(8);
            void Oge(string renk, string metin) => satir.Item().Row(r =>
            {
                r.AutoItem().AlignMiddle().Width(9).Height(9).Background(renk);
                r.AutoItem().AlignMiddle().PaddingLeft(3).Text(metin);
            });
            foreach (var p in plan.Projeler)
                Oge(renkler[p.ProjeNo], p.ProjeNo);
            Oge(Gri, "Projesiz");
            Oge(Kirmizi, "Uygunsuz araçta kasa dışına taşan ya da konumu uygunsuz yük");
        });
    }

    private static void AracSayfasi(ColumnDescriptor col, PlanAraci a, int aracSayisi, Dictionary<string, string> renkler)
    {
        var veri = TeknikVeri(a);
        var sirali = Sirali(a);
        var uygunsuz = a.Sonuc == YerlesimKontrolSonucu.Uygunsuz;
        // Üstteki katın etiketi istifin bütün numaralarını taşır (alttakiler üstten ve 3B görünümde örtülür).
        var numara = sirali.Select((k, i) => (k, No: i + 1)).ToDictionary(x => x.k, x => x.No);
        string Etiket(PlanYukKonumu k)
        {
            var istif = sirali.Where(x => x.IstifNo == k.IstifNo).OrderBy(x => x.Kat).ToList();
            return istif.Count > 1 && istif[^1] == k ? string.Join("/", istif.Select(x => numara[x].ToString(Tr))) : numara[k].ToString(Tr);
        }
        // Elle yerleşimde yük düzeyindeki uygunsuz sonuçlar (çakışma, desteksiz, kasa dışı…) da kırmızı çizilir.
        var sorunluYukler = a.YukSorunlari.Where(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz).Select(s => s.YukAnahtari).ToHashSet(StringComparer.Ordinal);
        var kutular = sirali.Select(k => new CizimKutusu(Etiket(k), k.XMm, k.YMm, k.ZMm, k.BoyMm, k.EnMm, k.YukseklikMm,
            renkler.GetValueOrDefault(k.ProjeNo ?? string.Empty, Gri), (uygunsuz && Tasiyor(k, veri)) || sorunluYukler.Contains(k.YukAnahtari))).ToList();

        col.Item().PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span($"Araç {a.AracNo} / {aracSayisi} · ").Bold().FontSize(12).FontColor(Mavi);
                t.Span($"{a.EkipmanKodu} — {a.EkipmanAdi}").Bold().FontSize(12);
                t.Span($"   {a.AileAdi} / {a.TipAdi} · ekipman revizyonu {a.RevizyonNo}").FontColor(Gri);
            });
            row.AutoItem().AlignMiddle().Text($"Plaka / konteyner no: ______________________").FontColor(Gri);
            row.AutoItem().AlignMiddle().PaddingLeft(10).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontColor(SonucRengi(a.Sonuc));
        });
        if (a.AileDisiGerekce != null)
            col.Item().PaddingTop(3).Background("#FFF7ED").Padding(4).Text(a.AileDisiGerekce).FontColor(Turuncu).Bold();

        col.Item().PaddingTop(6).Row(row =>
        {
            row.RelativeItem(3).Column(c =>
            {
                c.Item().Text("3B görünüm (arka-sağ-üstten)").SemiBold().FontColor(Gri);
                c.Item().Height(165).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Izometrik)).FitArea();
                c.Item().PaddingTop(4).Text("Üstten görünüm").SemiBold().FontColor(Gri);
                c.Item().Height(105).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Ustten)).FitArea();
            });
            row.RelativeItem(2).PaddingLeft(10).Column(c =>
            {
                BilgiSatiri(c, "Dış ölçü", DisOlcu(veri));
                BilgiSatiri(c, "Kullanılabilir iç ölçü", IcOlcu(veri));
                if (veri.Acikliklar.FirstOrDefault(x => x.Taraf == AciklikTarafi.Arka) is { GenislikMm: { } kg, YukseklikMm: { } kh })
                    BilgiSatiri(c, "Arka kapı", $"{Mm(kg)} × {Mm(kh)} mm");
                BilgiSatiri(c, "Toplam", $"{a.Konumlar.Count} yük · {Kg(a.ToplamKg)}" + (a.KapasiteKg is { } k ? $" / kapasite {Kg(k)}" : string.Empty));
                BilgiSatiri(c, "Taban doluluğu", $"%{a.TabanDolulukYuzde}");
                BilgiSatiri(c, "Toplam yüklü ölçü", $"{Mm(a.YukluUzunlukMm)} × {Mm(a.YukluGenislikMm)} × {Mm(a.YukluYukseklikMm)} mm");
                BilgiSatiri(c, "Taşma", TasmaMetni(a));
                if (a.AgirlikMerkeziXMm is { } am)
                    BilgiSatiri(c, "Ağırlık merkezi (X)", $"{Mm(am)} mm (önden)");
                BilgiSatiri(c, "Yükleme yöntemi", string.Join(", ", sirali.Select(k => YuklemeEtiketleri.GirisYonu(k.YuklemeYontemi)).Distinct()));
                BilgiSatiri(c, "Projeler", string.Join(", ", a.ProjeNolari));
                BilgiSatiri(c, "Lokasyonlar", string.Join(", ", a.Lokasyonlar));
                if (a.Lokasyonlar.Count > 1)
                    c.Item().PaddingTop(2).Text("Farklı lokasyonlardaki yükler bu araca atandı; fiziksel yükleme organizasyonu belirlenmelidir.").FontColor(Turuncu);
                c.Item().PaddingTop(6).Text("Yandan görünüm (soldan)").SemiBold().FontColor(Gri);
                c.Item().Height(85).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Yandan)).FitArea();
                if (a.SolTasmaMm > 0 || a.SagTasmaMm > 0)
                {
                    c.Item().PaddingTop(4).Text("Arkadan görünüm").SemiBold().FontColor(Gri);
                    c.Item().Height(115).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Arkadan)).FitArea();
                }
            });
        });

        col.Item().PageBreak();
        col.Item().Text($"Araç {a.AracNo} · {a.EkipmanKodu} — yük listesi ve yükleme sırası").Bold().FontSize(10).FontColor(Mavi);
        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);   // no
                c.ConstantColumn(30);   // sıra
                c.RelativeColumn(2);    // kod
                c.RelativeColumn(1.1f); // proje
                c.RelativeColumn(1.5f); // ölçü
                c.ConstantColumn(54);   // kg
                c.ConstantColumn(56);   // kat / istif
                c.ConstantColumn(46);   // yöntem
                c.RelativeColumn(1.4f); // konum
            });
            table.Header(h =>
            {
                foreach (var b in new[] { "No", "Sıra", "Kod", "Proje", "Ölçü (B×E×Y mm)", "Brüt", "Kat / istif", "Yöntem", "Konum X/Y/Z mm" })
                    BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
            });
            for (var i = 0; i < sirali.Count; i++)
            {
                var k = sirali[i];
                var zebra = i % 2 == 1;
                Hucre(table.Cell(), zebra).Row(r =>
                {
                    r.AutoItem().AlignMiddle().Width(6).Height(6).Background(kutular[i].Sorunlu ? Kirmizi : kutular[i].Renk);
                    r.RelativeItem().PaddingLeft(3).Text((i + 1).ToString(Tr)).Bold();
                });
                Hucre(table.Cell(), zebra).AlignRight().Text(k.YuklemeSirasi.ToString(Tr));
                Hucre(table.Cell(), zebra).Text(k.Kod).SemiBold();
                Hucre(table.Cell(), zebra).Text(k.ProjeNo ?? "-");
                Hucre(table.Cell(), zebra).Text($"{Mm(k.BoyMm)} × {Mm(k.EnMm)} × {Mm(k.YukseklikMm)}" + (k.Donduruldu ? " (90°)" : string.Empty));
                Hucre(table.Cell(), zebra).AlignRight().Text(Kg(k.BrutKg));
                Hucre(table.Cell(), zebra).Text(k.Kat > 1 ? $"{k.Kat}. kat · istif {k.IstifNo}" : $"1. kat · istif {k.IstifNo}");
                Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.GirisYonu(k.YuklemeYontemi));
                Hucre(table.Cell(), zebra).Text($"{Mm(k.XMm)} / {Mm(k.YMm)} / {Mm(k.ZMm)}");
            }
        });
        var kapali = sirali.Any(k => k.YuklemeYontemi.HasFlag(YerlesimGirisYonu.Arkadan));
        col.Item().PaddingTop(3).Text(kapali
                ? "No: görsellerdeki numara. Sıra: yükleme sırası; aynı sıradaki katlar dışarıda hazırlanan istif grubudur ve bütün hâlinde kapıdan yüklenir."
                : "No: görsellerdeki numara. Sıra: yükleme sırası; aynı sıradaki katlar bir istif grubudur ve birlikte yüklenir.")
            .FontSize(6.5f).FontColor(Gri);

        var gruplar = sirali.GroupBy(k => k.IstifNo).Where(g => g.Count() > 1).ToList();
        if (gruplar.Count > 0)
        {
            Bolum(col, kapali ? "Dışarıda hazırlanacak istif grupları" : "İstif grupları");
            foreach (var g in gruplar)
            {
                var numaralar = g.Select(k => (sirali.IndexOf(k) + 1).ToString(Tr));
                col.Item().Text($"• İstif {g.Key} (yükleme sırası {g.First().YuklemeSirasi}): No {string.Join(" + ", numaralar)} · {g.Count()} kat · toplam yükseklik {Mm(g.Sum(k => k.YukseklikMm))} mm · {Kg(g.Sum(k => k.BrutKg))}");
            }
        }

        Bolum(col, "Kontroller, uyarılar ve teyit gereksinimleri");
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(130);
                c.ConstantColumn(72);
                c.RelativeColumn();
            });
            var i = 0;
            foreach (var k in a.Kontroller.OrderBy(k => k.Sonuc == YerlesimKontrolSonucu.Uygunsuz ? 0 : k.Sonuc == YerlesimKontrolSonucu.TeyitEdilmedi ? 1 : 2))
            {
                var zebra = i++ % 2 == 1;
                Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolTuru(k.Tur)).SemiBold();
                Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolSonucu(k.Sonuc)).Bold().FontColor(SonucRengi(k.Sonuc));
                Hucre(table.Cell(), zebra).Text(k.Mesaj);
            }
        });

        if (a.YukSorunlari.Count > 0)
        {
            Bolum(col, "Yük sorunları (elle yerleşim)");
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(24);
                    c.RelativeColumn(1.2f);
                    c.ConstantColumn(72);
                    c.RelativeColumn(3);
                });
                var i = 0;
                foreach (var s in a.YukSorunlari.OrderBy(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz ? 0 : 1))
                {
                    var zebra = i++ % 2 == 1;
                    var k = sirali.FirstOrDefault(x => x.YukAnahtari == s.YukAnahtari);
                    Hucre(table.Cell(), zebra).Text(k == null ? "-" : (sirali.IndexOf(k) + 1).ToString(Tr)).Bold();
                    Hucre(table.Cell(), zebra).Text(k?.Kod ?? s.YukAnahtari).SemiBold();
                    Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolSonucu(s.Sonuc)).Bold().FontColor(SonucRengi(s.Sonuc));
                    Hucre(table.Cell(), zebra).Text(s.Mesaj);
                }
            });
        }
    }

    private static bool Tasiyor(PlanYukKonumu k, EkipmanTeknikVerisi v) =>
        (v.DisOlcu?.GenislikMm is { } g && (k.YMm < 0 || k.YMm + k.EnMm > g)) ||
        (v.DisOlcu?.UzunlukMm is { } u && (k.XMm < 0 || k.XMm + k.BoyMm > u));

    /// <summary>Son bölüm: yerleşmeyen, hesaplanmayan (veri eksik) ve lokasyonu belirsiz yükler nedenleriyle (isterler 29).</summary>
    private static void PlanaGirmeyenler(ColumnDescriptor col, YuklemePlaniDetayi plan)
    {
        var y = Yerlesim(plan);
        var eksikler = plan.Yukler.Where(s => s.Durum == PlanYukDurumu.EksikVeri).ToList();
        var belirsiz = plan.Yukler.Where(s => s.Dahil && YuklemePlaniKurali.LokasyonBelirsizMi(s.LokasyonId)).ToList();
        var haric = plan.Yukler.Count(s => s.Durum == PlanYukDurumu.HaricTutuldu);

        void Liste(string baslik, IReadOnlyList<(string Kod, string? Proje, string Neden)> satirlar, string bosMetin)
        {
            Bolum(col, $"{baslik} ({satirlar.Count})");
            if (satirlar.Count == 0)
            {
                col.Item().Text(bosMetin).FontColor(Gri);
                return;
            }
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1);
                    c.RelativeColumn(4);
                });
                table.Header(h =>
                {
                    foreach (var b in new[] { "Kod", "Proje", "Neden" })
                        BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
                });
                var i = 0;
                foreach (var s in satirlar)
                {
                    var zebra = i++ % 2 == 1;
                    Hucre(table.Cell(), zebra).Text(s.Kod).SemiBold();
                    Hucre(table.Cell(), zebra).Text(s.Proje ?? "-");
                    Hucre(table.Cell(), zebra).Text(s.Neden);
                }
            });
        }

        var satirlarByAnahtar = plan.Yukler.ToDictionary(s => YuklemePlaniKurali.YukAnahtari(s.Kaynak, s.SandikId, s.ManuelGrupNo, s.BirimNo));
        Liste("Yerleşemeyen yükler",
            y.Yerlesemeyenler.Select(u => (u.Kod, satirlarByAnahtar.GetValueOrDefault(u.YukAnahtari)?.ProjeNo, u.Neden)).ToList(),
            "Bütün hesaplanan yükler yerleşti.");
        Liste("Veri eksikliği nedeniyle hesaplanmayan yükler",
            eksikler.Select(s => (s.Kod, s.ProjeNo, string.Join(" ", s.VeriSorunlari))).ToList(),
            "Ölçü ve ağırlığı eksik yük yok.");
        Liste("Lokasyonu belirsiz yükler",
            belirsiz.Select(s => (s.Kod, s.ProjeNo, YuklemeEtiketleri.BelirsizLokasyon)).ToList(),
            "Lokasyonu belirsiz yük yok.");
        if (plan.KaynakFarklari.Count > 0)
            Liste("Kaynak veri farkları (yeniden hesap gerekir)",
                plan.KaynakFarklari.Select(f => (f.Kod, (string?)null,
                    $"{YuklemeEtiketleri.KaynakFarki(f.Tur)}: {f.Onceki ?? "-"} → {f.Guncel ?? "-"}")).ToList(),
                string.Empty);
        if (y.EkIhtiyaclar.Count > 0)
        {
            Bolum(col, "Önerilen ek ekipman (plana eklenmedi)");
            foreach (var e in y.EkIhtiyaclar)
                col.Item().Text($"• {e.Adet} × {e.EkipmanKodu} — {e.EkipmanAdi}");
        }
        if (haric > 0)
            col.Item().PaddingTop(8).Text($"Kullanıcının planlamadan çıkardığı (hariç tutulan) yük: {haric}.").FontColor(Gri);
    }
}
