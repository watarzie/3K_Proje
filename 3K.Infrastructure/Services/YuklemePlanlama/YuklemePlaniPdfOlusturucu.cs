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
/// Yükleme planı PDF'leri (Simülasyon isterleri 28–29, saha geri bildirimi 19.11.2026 madde 7–8). Araç talep formu
/// depoya yalnız araç tipini ve adedini bildirir; yükleme planında her araç, çizimleri ve içindeki yüklerin tablosuyla
/// aynı sayfadadır. Çizimlerde ve tabloda yük sandık numarasıyla yazılır (tek projede koli no, çok projede tam kod).
/// Çözülmemiş uygunsuzluk ya da yerleşemeyen yük varsa "TASLAK — YÜKLEMEYE UYGUN DEĞİL" ibaresi basılır.
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

    /// <summary>Tek projeli planda yük yalnız sandık (koli) numarasıyla yazılır (madde 8).</summary>
    internal static bool TekProje(YuklemePlaniDetayi plan) => plan.Projeler.Count <= 1;

    private static Dictionary<string, string> ProjeRenkHaritasi(YuklemePlaniDetayi plan)
    {
        var harita = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < plan.Projeler.Count; i++)
            harita[plan.Projeler[i].ProjeNo] = ProjeRenkleri[i % ProjeRenkleri.Length];
        return harita;
    }

    private static EkipmanTeknikVerisi TeknikVeri(PlanAraci a) => EkipmanTeknikVeriSerilestirici.Oku(a.TeknikVeriJson) ?? new EkipmanTeknikVerisi();

    private static string ProjeMetni(YuklemePlaniDetayi plan) =>
        plan.Projeler.Count == 0 ? "-" : string.Join(", ", plan.Projeler.Select(p => p.Musteri is null ? p.ProjeNo : $"{p.ProjeNo} ({p.Musteri})"));

    private static void UstCubuk(PageDescriptor page, string baslik, YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        page.Header().Column(col =>
        {
            col.Item().Background(Mavi).PaddingVertical(8).PaddingHorizontal(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("3K").Bold().FontSize(20).FontColor(Colors.White);
                    c.Item().Text("All Processes. One Flow.").FontSize(7).Italic().FontColor(Colors.Grey.Lighten3);
                });
                row.RelativeItem(2).AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(15).FontColor(Colors.White);
                    c.Item().AlignCenter().Text($"{plan.PlanNo} · {plan.Ad}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                });
                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignRight().Text($"Rapor tarihi: {Tarih(raporTarihi)}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                    c.Item().AlignRight().Text($"Hazırlayan: {plan.Kaydeden ?? "-"}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                });
            });
            if (Taslak(plan))
                col.Item().PaddingTop(4).Background("#FDECEA").Border(1).BorderColor(Kirmizi).Padding(4).AlignCenter()
                    .Text(YuklemeEtiketleri.TaslakIbaresi).Bold().FontSize(11).FontColor(Kirmizi);
            // Elle düzenlenen yerleşim otomatik öneri değildir; kontroller kullanıcının konumlarıyla yapılmıştır (isterler 20).
            if (Yerlesim(plan).ElleDuzenlendi)
                col.Item().PaddingTop(4).Background("#EEF2FF").Border(1).BorderColor(Mavi).Padding(3).AlignCenter()
                    .Text(YuklemeEtiketleri.ElleDuzenlendiIbaresi).Bold().FontColor(Mavi);
            // Sandık verisi hesaptan sonra değiştiyse rapor eski kopyayla hazırlanmıştır (isterler 27).
            if (plan.KaynakFarklari.Count > 0)
                col.Item().PaddingTop(4).Background("#FFF7ED").Border(1).BorderColor(Turuncu).Padding(3).AlignCenter()
                    .Text($"Sandık verisi hesaptan sonra değişti ({plan.KaynakFarklari.Count} fark); planı yeniden hesaplayın.")
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

    private static void BasliklariYaz(TableDescriptor table, params string[] basliklar) =>
        table.Header(h =>
        {
            foreach (var b in basliklar)
                BaslikHucresi(h.Cell()).Text(b.ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
        });

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

    /// <summary>Kasa dışına taşma (yön ve miktar); taşma kontrolü listede gösterilmediği için raporda bu satırla yazılır.</summary>
    internal static string TasmaMetni(PlanAraci a)
    {
        var parcalar = new List<string>();
        if (a.SolTasmaMm > 0) parcalar.Add($"sol {Mm(a.SolTasmaMm)}");
        if (a.SagTasmaMm > 0) parcalar.Add($"sağ {Mm(a.SagTasmaMm)}");
        if (a.OnTasmaMm > 0) parcalar.Add($"ön {Mm(a.OnTasmaMm)}");
        if (a.ArkaTasmaMm > 0) parcalar.Add($"arka {Mm(a.ArkaTasmaMm)}");
        return parcalar.Count == 0 ? string.Empty : string.Join(", ", parcalar) + " mm";
    }

    /// <summary>Araca atanamayan yükler: yerleşemeyenler nedeniyle, ölçü/ağırlığı eksik olanlar veri sorunuyla.</summary>
    internal static IReadOnlyList<(string Kod, string Neden)> AtanamayanYukler(YuklemePlaniDetayi plan) =>
        Yerlesim(plan).Yerlesemeyenler.Select(u => (u.Kod, u.Neden))
            .Concat(plan.Yukler.Where(s => s.Durum == PlanYukDurumu.EksikVeri)
                .Select(s => (s.Kod, "Hesaplanmadı: " + (s.VeriSorunlari.Count == 0 ? "ölçü/ağırlık eksik." : string.Join(" ", s.VeriSorunlari)))))
            .ToList();

    private static void AtanamayanTablosu(ColumnDescriptor col, IReadOnlyList<(string Kod, string Neden)> satirlar)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.3f);
                c.RelativeColumn(4);
            });
            BasliklariYaz(table, "Sandık", "Neden");
            var i = 0;
            foreach (var (kod, neden) in satirlar)
            {
                var zebra = i++ % 2 == 1;
                Hucre(table.Cell(), zebra).Text(kod).SemiBold();
                Hucre(table.Cell(), zebra).Text(neden);
            }
        });
    }

    // ===== PDF 1: Araç Talep Formu =====

    /// <summary>Talep formunun bir satırı: araç tipi ve adedi; özel araçsa gerekçesi, taşma varsa ölçüsü.</summary>
    internal sealed record AracTipiSatiri(string TipAdi, string AileAdi, int Adet, IReadOnlyList<string> Gerekceler, IReadOnlyList<string> Tasmalar);

    /// <summary>
    /// Araçlar tipe göre (aile + tip) toplanır; sıra plandaki ilk aracın sırasıdır. Aynı tipteki farklı ekipmanlar depo
    /// için ayrı satır değildir (madde 7: depoya araç tipi ve adedi yeter).
    /// </summary>
    internal static IReadOnlyList<AracTipiSatiri> AracTipleri(YuklemePlaniDetayi plan) =>
        Yerlesim(plan).Araclar
            .GroupBy(a => (a.AileAdi, a.TipAdi))
            .OrderBy(g => g.Min(a => a.AracNo))
            .Select(g => new AracTipiSatiri(
                g.Key.TipAdi,
                g.Key.AileAdi,
                g.Count(),
                g.Select(a => a.AileDisiGerekce).OfType<string>().Distinct(StringComparer.Ordinal).ToList(),
                g.Where(a => TasmaMetni(a).Length > 0).Select(a => $"Araç {a.AracNo}: {TasmaMetni(a)}").ToList()))
            .ToList();

    public byte[] AracTalepRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        var tipler = AracTipleri(plan);
        var atanamayanlar = AtanamayanYukler(plan);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(24);
            page.DefaultTextStyle(x => x.FontSize(9).DisableFontFeature(FontFeatures.StandardLigatures));
            UstCubuk(page, "Araç Talep Formu", plan, raporTarihi);
            AltBilgi(page, $"3K – Araç Talep Formu | {plan.PlanNo} Rev. {plan.RevizyonNo}");

            page.Content().Column(col =>
            {
                col.Item().PaddingTop(8).Column(c =>
                {
                    BilgiSatiri(c, "Projeler", ProjeMetni(plan));
                    BilgiSatiri(c, "Taşıma ailesi", plan.TasimaAilesiAdi);
                });

                Bolum(col, "Gereken araçlar");
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.ConstantColumn(70);
                    });
                    BasliklariYaz(table, "Araç tipi", "Adet");
                    var i = 0;
                    foreach (var t in tipler)
                    {
                        var zebra = i++ % 2 == 1;
                        Hucre(table.Cell(), zebra).PaddingVertical(2).Column(c =>
                        {
                            c.Item().Text(t.TipAdi).SemiBold().FontSize(11);
                            c.Item().Text(t.AileAdi).FontSize(7.5f).FontColor(Gri);
                            foreach (var g in t.Gerekceler)
                                c.Item().Text("Özel araç: " + g).FontSize(7.5f).FontColor(Turuncu);
                            foreach (var ts in t.Tasmalar)
                                c.Item().Text($"Taşma ({ts}); izin gerekebilir.").FontSize(7.5f).FontColor(Turuncu);
                        });
                        Hucre(table.Cell(), zebra).AlignRight().AlignMiddle().Text(t.Adet.ToString(Tr)).Bold().FontSize(14);
                    }
                    if (tipler.Count == 0)
                        table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(Kenar).Padding(6).Text("Araç yok.").FontColor(Gri);
                    else
                    {
                        table.Cell().Background(AcikMavi).Border(0.5f).BorderColor(Kenar).Padding(4).Text("TOPLAM").Bold();
                        table.Cell().Background(AcikMavi).Border(0.5f).BorderColor(Kenar).Padding(4).AlignRight().Text(y.Araclar.Count.ToString(Tr)).Bold().FontSize(14);
                    }
                });

                // Eski "mevcut araçlar" planlarında sığmayan yükler için önerilen ek araçlar (plana eklenmemiştir).
                if (y.EkIhtiyaclar.Count > 0)
                {
                    Bolum(col, "Ek araç ihtiyacı (plana eklenmedi)");
                    foreach (var e in y.EkIhtiyaclar)
                        col.Item().Text($"• {e.Adet} × {e.EkipmanKodu} — {e.EkipmanAdi}").Bold().FontColor(Turuncu);
                }

                if (atanamayanlar.Count > 0)
                {
                    Bolum(col, $"Araç atanamayan yükler ({atanamayanlar.Count})");
                    AtanamayanTablosu(col, atanamayanlar);
                }
            });
        })).GeneratePdf();
    }

    // ===== PDF 2: Araç Bazlı Yükleme Planı =====

    public byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        var renkler = ProjeRenkHaritasi(plan);
        var tekProje = TekProje(plan);
        var atanamayanlar = AtanamayanYukler(plan);

        void Sayfa(IDocumentContainer container, string altBilgi, Action<ColumnDescriptor> icerik) =>
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontSize(7.5f).DisableFontFeature(FontFeatures.StandardLigatures));
                UstCubuk(page, "Araç Bazlı Yükleme Planı", plan, raporTarihi);
                AltBilgi(page, altBilgi);
                page.Content().Column(icerik);
            });

        var altMetin = $"3K – Araç Bazlı Yükleme Planı | {plan.PlanNo} Rev. {plan.RevizyonNo}";
        return Document.Create(container =>
        {
            Sayfa(container, altMetin, col => Ozet(col, plan, renkler, atanamayanlar.Count));
            // "Elle düzenlendi" ve "kaynak farkı" çubukları araç sayfasında yaklaşık iki yük satırı yer kaplar.
            var ustSatir = (y.ElleDuzenlendi ? 2 : 0) + (plan.KaynakFarklari.Count > 0 ? 2 : 0);
            foreach (var a in y.Araclar)
                Sayfa(container, $"{altMetin} | Araç {a.AracNo} / {y.Araclar.Count}", col => AracSayfasi(col, a, y.Araclar.Count, renkler, tekProje, ustSatir));
            if (atanamayanlar.Count > 0)
                Sayfa(container, altMetin, col =>
                {
                    Bolum(col, $"Araç atanamayan yükler ({atanamayanlar.Count})");
                    AtanamayanTablosu(col, atanamayanlar);
                });
        }).GeneratePdf();
    }

    /// <summary>İlk sayfa: projeler, araç listesi, genel uyarılar ve proje renkleri.</summary>
    private static void Ozet(ColumnDescriptor col, YuklemePlaniDetayi plan, Dictionary<string, string> renkler, int atanamayan)
    {
        var y = Yerlesim(plan);
        col.Item().PaddingTop(8).Row(row =>
        {
            row.RelativeItem(3).Column(c =>
            {
                BilgiSatiri(c, "Projeler", ProjeMetni(plan));
                BilgiSatiri(c, "Taşıma ailesi", plan.TasimaAilesiAdi);
                BilgiSatiri(c, "Hesap tarihi", Tarih(y.HesapTarihi));
            });
            row.RelativeItem(2).AlignRight().Text(t =>
            {
                t.Span($"{y.Araclar.Count} araç · {y.Araclar.Sum(a => a.Konumlar.Count)} yük").Bold().FontSize(11).FontColor(Mavi);
                if (atanamayan > 0)
                    t.Span($"  ·  {atanamayan} yük araca atanamadı (son sayfa)").Bold().FontColor(Kirmizi);
            });
        });

        Bolum(col, "Araçlar");
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(34);
                c.RelativeColumn(2.4f);
                c.ConstantColumn(40);
                c.RelativeColumn(1.6f);
                c.ConstantColumn(70);
            });
            BasliklariYaz(table, "Araç", "Araç tipi", "Yük", "Ağırlık / kapasite", "Sonuç");
            var i = 0;
            foreach (var a in y.Araclar)
            {
                var zebra = i++ % 2 == 1;
                Hucre(table.Cell(), zebra).Text(a.AracNo.ToString(Tr)).Bold();
                Hucre(table.Cell(), zebra).Text(t =>
                {
                    t.Span(a.TipAdi).SemiBold();
                    t.Span($"  {a.EkipmanKodu}").FontColor(Gri);
                    if (a.AileDisiGerekce != null)
                        t.Span("  · özel araç").FontColor(Turuncu);
                });
                Hucre(table.Cell(), zebra).AlignRight().Text(a.Konumlar.Count.ToString(Tr));
                Hucre(table.Cell(), zebra).Text(a.KapasiteKg is { } k ? $"{Kg(a.ToplamKg)} / {Kg(k)}" : Kg(a.ToplamKg));
                Hucre(table.Cell(), zebra).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontColor(SonucRengi(a.Sonuc));
            }
        });
        if (y.Uyarilar.Count > 0)
        {
            Bolum(col, "Genel uyarılar");
            foreach (var u in y.Uyarilar)
                col.Item().PaddingBottom(2).Text("• " + u);
        }
        if (plan.Projeler.Count > 1)
        {
            // Çok projede satır kaydırır; renk 10 projeden sonra tekrar eder, kimlik sandık kodunda yazılıdır.
            col.Item().PaddingTop(8).Text("Proje renkleri").SemiBold().FontColor(Gri);
            col.Item().PaddingTop(2).Inlined(satir =>
            {
                satir.Spacing(8);
                foreach (var p in plan.Projeler)
                    satir.Item().Row(r =>
                    {
                        r.AutoItem().AlignMiddle().Width(9).Height(9).Background(renkler[p.ProjeNo]);
                        r.AutoItem().AlignMiddle().PaddingLeft(3).Text(p.ProjeNo);
                    });
            });
        }
    }

    /// <summary>
    /// Çizim etiketleri: her yük kendi sandık numarasıyla; istifte üstteki kat bütün katların numaralarını alttan üste
    /// taşır (alttakiler üstten ve izometrik görünümde örtülür). Çok projeli planda bir kat, altındaki katla aynı
    /// projedense yalnız sandık numarasıyla yazılır (PA100-35-10/2; etiket kutuya sığsın), değilse tam koduyla. Sıra,
    /// sahadaki "Adım" numarasıdır (ardışık; saklanan yükleme sırası boşluklu olabilir).
    /// </summary>
    internal static IReadOnlyList<(PlanYukKonumu Konum, string Etiket, string CizimEtiketi, int Sira)> AracYukleri(PlanAraci a, bool tekProje) =>
        YuklemeSirasiKurali.Adimlar(a.Konumlar)
            .SelectMany(adim => adim.Yukler.Select((y, i) =>
            {
                var etiket = YuklemePlaniKurali.SandikEtiketi(y.Konum.Kod, y.Konum.ProjeNo, tekProje);
                if (adim.Yukler.Count == 1 || i < adim.Yukler.Count - 1)
                    return (y.Konum, etiket, etiket, adim.AdimNo);
                var cizim = string.Join("/", adim.Yukler.Select((x, j) => YuklemePlaniKurali.SandikEtiketi(x.Konum.Kod, x.Konum.ProjeNo,
                    tekProje || (j > 0 && string.Equals(x.Konum.ProjeNo, adim.Yukler[j - 1].Konum.ProjeNo, StringComparison.Ordinal)))));
                return (y.Konum, etiket, cizim, adim.AdimNo);
            }))
            .ToList();

    /// <summary>
    /// Çizimlerin yanına sığan yük satırı (taslak ibaresi ve araç başlığı varken; her uyarı satırı bir yük satırı yer
    /// kaplar). Kalan satırlar sonraki sayfada iki sütuna akar; tahmin az kalırsa tablo yine sonraki sayfaya devam eder.
    /// </summary>
    internal const int YanTablosuSatiri = 29;

    private static void AracSayfasi(ColumnDescriptor col, PlanAraci a, int aracSayisi, Dictionary<string, string> renkler, bool tekProje, int ustSatir)
    {
        var veri = TeknikVeri(a);
        var yukler = AracYukleri(a, tekProje);
        var uygunsuz = a.Sonuc == YerlesimKontrolSonucu.Uygunsuz;
        // Elle yerleşimde yük düzeyindeki uygunsuz sonuçlar (çakışma, desteksiz, kasa dışı…) da kırmızı çizilir.
        var sorunluYukler = a.YukSorunlari.Where(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz).Select(s => s.YukAnahtari).ToHashSet(StringComparer.Ordinal);
        var kutular = yukler.Select(x => new CizimKutusu(x.CizimEtiketi, x.Konum.XMm, x.Konum.YMm, x.Konum.ZMm, x.Konum.BoyMm, x.Konum.EnMm, x.Konum.YukseklikMm,
            renkler.GetValueOrDefault(x.Konum.ProjeNo ?? string.Empty, Gri),
            (uygunsuz && Tasiyor(x.Konum, veri)) || sorunluYukler.Contains(x.Konum.YukAnahtari))).ToList();
        // Yandan bakışta istifin katları ayrı görünür; her kat yalnız kendi numarasını taşır.
        var yandanKutular = kutular.Select((k, i) => k with { Etiket = yukler[i].Etiket }).ToList();
        var yontemler = a.Konumlar.Select(k => YuklemeEtiketleri.GirisYonu(k.YuklemeYontemi)).Distinct().ToList();

        var uyariSatiri = ustSatir + (a.AileDisiGerekce != null ? 2 : 0) + (TasmaMetni(a).Length > 0 ? 1 : 0) +
                          a.Kontroller.Count(k => k.Sonuc == YerlesimKontrolSonucu.Uygunsuz) + a.YukSorunlari.Count(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz);
        var yanda = Math.Min(yukler.Count, Math.Max(10, YanTablosuSatiri - uyariSatiri));
        var kalan = yukler.Count - yanda;

        col.Item().PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span($"Araç {a.AracNo} / {aracSayisi} · ").Bold().FontSize(12).FontColor(Mavi);
                t.Span(a.TipAdi).Bold().FontSize(12);
                t.Span($"   {a.EkipmanKodu} · {a.Konumlar.Count} yük · {Kg(a.ToplamKg)}" + (a.KapasiteKg is { } k ? $" / {Kg(k)}" : string.Empty) +
                       (yontemler.Count > 0 ? $" · yükleme: {string.Join(", ", yontemler)}" : string.Empty)).FontColor(Gri);
            });
            row.AutoItem().AlignMiddle().Text("Plaka / konteyner no: ______________________").FontColor(Gri);
            row.AutoItem().AlignMiddle().PaddingLeft(10).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontColor(SonucRengi(a.Sonuc));
        });
        if (a.AileDisiGerekce != null)
            col.Item().PaddingTop(2).Text("Özel araç: " + a.AileDisiGerekce).FontColor(Turuncu).Bold();
        if (TasmaMetni(a) is { Length: > 0 } tasma)
            col.Item().PaddingTop(2).Text($"Taşma: {tasma}").FontColor(Turuncu).Bold();
        // Yalnız uygunsuz sonuçlar yazılır (taslak ibaresinin nedeni); teyit edilemeyen kontroller gösterilmez.
        foreach (var k in a.Kontroller.Where(k => k.Sonuc == YerlesimKontrolSonucu.Uygunsuz))
            col.Item().PaddingTop(2).Text($"{YuklemeEtiketleri.KontrolTuru(k.Tur)}: {k.Mesaj}").FontColor(Kirmizi);
        foreach (var s in a.YukSorunlari.Where(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz))
        {
            var etiket = yukler.Where(x => x.Konum.YukAnahtari == s.YukAnahtari).Select(x => x.Etiket).FirstOrDefault() ?? s.YukAnahtari;
            col.Item().PaddingTop(2).Text($"{etiket}: {s.Mesaj}").FontColor(Kirmizi);
        }

        col.Item().PaddingTop(6).Row(row =>
        {
            // Uyarı satırları arttıkça çizimler kalan yüksekliğe sığacak kadar küçülür (araç tek sayfada kalsın).
            row.RelativeItem(11).ScaleToFit().Column(c =>
            {
                c.Item().Text("3B görünüm (arka-sağ-üstten)").SemiBold().FontColor(Gri);
                c.Item().Height(185).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Izometrik)).FitArea();
                c.Item().PaddingTop(3).Text("Üstten görünüm").SemiBold().FontColor(Gri);
                c.Item().Height(110).AlignCenter().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Ustten)).FitArea();
                c.Item().PaddingTop(3).Text("Yandan görünüm (soldan)").SemiBold().FontColor(Gri);
                c.Item().Height(85).AlignCenter().Svg(YerlesimCizimi.Svg(veri, yandanKutular, CizimGorunumu.Yandan)).FitArea();
            });
            row.RelativeItem(9).PaddingLeft(10).Column(c =>
            {
                c.Item().Element(e => YukTablosu(e, 0, yanda));
                c.Item().PaddingTop(3).Text(kalan > 0
                        ? $"Sıra: yükleme adımı (sahadaki adım no); aynı sıradaki katlar istif hâlinde birlikte yüklenir. Liste sonraki sayfada sürer ({kalan} yük)."
                        : "Sıra: yükleme adımı (sahadaki adım no); aynı sıradaki katlar istif hâlinde birlikte yüklenir.")
                    .FontSize(6.5f).FontColor(Gri);
            });
        });
        if (kalan > 0)
        {
            col.Item().PageBreak();
            col.Item().Text($"Araç {a.AracNo} · {a.TipAdi} — yük listesinin devamı").Bold().FontSize(10).FontColor(Mavi);
            col.Item().PaddingTop(4).MultiColumn(m =>
            {
                m.Columns(2);
                m.Spacing(14);
                m.Content().Element(e => YukTablosu(e, yanda, kalan));
            });
        }

        void YukTablosu(IContainer kap, int bas, int adet) => kap.Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(30);   // sıra
                cd.RelativeColumn(1.6f); // sandık
                cd.RelativeColumn(1.7f); // ölçü
                cd.ConstantColumn(52);   // brüt
                cd.ConstantColumn(26);   // kat
            });
            BasliklariYaz(table, "Sıra", "Sandık", "Ölçü (B×E×Y mm)", "Brüt", "Kat");
            for (var i = bas; i < bas + adet; i++)
            {
                var (k, etiket, _, sira) = yukler[i];
                var zebra = i % 2 == 1;
                var renk = kutular[i].Sorunlu ? Kirmizi : kutular[i].Renk;
                Hucre(table.Cell(), zebra).Row(r =>
                {
                    r.AutoItem().AlignMiddle().Width(6).Height(6).Background(renk);
                    r.RelativeItem().PaddingLeft(3).Text(sira.ToString(Tr)).Bold();
                });
                Hucre(table.Cell(), zebra).Text(etiket).SemiBold();
                Hucre(table.Cell(), zebra).Text($"{Mm(k.BoyMm)} × {Mm(k.EnMm)} × {Mm(k.YukseklikMm)}" + (k.Donduruldu ? " (90°)" : string.Empty));
                Hucre(table.Cell(), zebra).AlignRight().Text(Kg(k.BrutKg));
                Hucre(table.Cell(), zebra).AlignCenter().Text(k.Kat.ToString(Tr));
            }
        });
    }

    private static bool Tasiyor(PlanYukKonumu k, EkipmanTeknikVerisi v) =>
        (v.DisOlcu?.GenislikMm is { } g && (k.YMm < 0 || k.YMm + k.EnMm > g)) ||
        (v.DisOlcu?.UzunlukMm is { } u && (k.XMm < 0 || k.XMm + k.BoyMm > u));
}
