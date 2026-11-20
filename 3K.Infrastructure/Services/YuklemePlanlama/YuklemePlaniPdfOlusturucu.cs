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
/// depoya yalnız araç tipini ve adedini bildirir (lokasyona göre ayrı planda lokasyon kırılımıyla; saha isteği
/// 20.11.2026). Yükleme planı ayrıntılı düzendedir (kullanıcının örnek raporu, 20.11.2026): ilk sayfa planın özeti
/// (araç sayısı, tip dağılımı, yük sayısı, toplam ağırlık ve hacim); her araç kendi sayfasında 3B çizim, yandan/üstten/
/// arkadan görünümler, araç–kullanım–toplam–ağırlık merkezi blokları ve yük tablosuyla başlar. Her sandık ayrı renktedir
/// (çizimlerde ve tabloda aynı renk) ve sandık numarasıyla yazılır (tek projede koli no, çok projede tam kod). Ölçüler
/// mm, ağırlık kg, hacim m³; maliyet ve ücrete esas ağırlık yazılmaz.
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

    /// <summary>Ön yüzdeki 3B görünümle aynı proje renkleri (raporda özet sayfasında ve yük tablosunun proje sütununda).</summary>
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
    internal static IReadOnlyList<AracTipiSatiri> AracTipleri(YuklemePlaniDetayi plan) => AracTipleri(Yerlesim(plan).Araclar);

    private static IReadOnlyList<AracTipiSatiri> AracTipleri(IEnumerable<PlanAraci> araclar) =>
        araclar
            .GroupBy(a => (a.AileAdi, a.TipAdi))
            .OrderBy(g => g.Min(a => a.AracNo))
            .Select(g => new AracTipiSatiri(
                g.Key.TipAdi,
                g.Key.AileAdi,
                g.Count(),
                g.Select(a => a.AileDisiGerekce).OfType<string>().Distinct(StringComparer.Ordinal).ToList(),
                g.Where(a => TasmaMetni(a).Length > 0).Select(a => $"Araç {a.AracNo}: {TasmaMetni(a)}").ToList()))
            .ToList();

    /// <summary>Talep formunun lokasyon kırılımı: lokasyondan istenen araç tipleri ve lokasyonun araç sayısı (ara toplam).</summary>
    internal sealed record LokasyonAracSatiri(string Lokasyon, IReadOnlyList<AracTipiSatiri> Tipler, int Adet);

    /// <summary>
    /// Aracın yükleneceği lokasyon. Lokasyona göre ayrı planda tek değerdir (lokasyonu belirsiz yükler kendi grubundadır);
    /// elle düzenlemeyle karışmış araçta lokasyonlar birlikte yazılır, yükü olmayan araçta lokasyon belirsizdir.
    /// </summary>
    internal static string AracLokasyonu(PlanAraci a) =>
        a.Lokasyonlar.Count == 0 ? YuklemeEtiketleri.BelirsizLokasyon : string.Join(" + ", a.Lokasyonlar);

    /// <summary>
    /// Lokasyona göre ayrı planda araçlar önce lokasyona, sonra tipe göre toplanır (depo her lokasyona kaç araç
    /// göndereceğini görür); lokasyon sırası plandaki ilk aracının sırasıdır.
    /// </summary>
    internal static IReadOnlyList<LokasyonAracSatiri> LokasyonAracTipleri(YuklemePlaniDetayi plan) =>
        Yerlesim(plan).Araclar
            .GroupBy(AracLokasyonu, StringComparer.Ordinal)
            .OrderBy(g => g.Min(a => a.AracNo))
            .Select(g => new LokasyonAracSatiri(g.Key, AracTipleri(g), g.Count()))
            .ToList();

    /// <summary>
    /// Tek yerden (karışık) hesaplanan planda yükler birden çok lokasyondaysa talep formunun notu: lokasyonlar ve lokasyon
    /// başına araç sayısı için ne yapılacağı. Tek lokasyonda ya da lokasyona göre ayrı planda not yoktur (null).
    /// </summary>
    internal static string? KarisikLokasyonNotu(YuklemePlaniDetayi plan)
    {
        var y = Yerlesim(plan);
        if (y.LokasyonaGoreAyri)
            return null;
        var lokasyonlar = y.Araclar.SelectMany(a => a.Lokasyonlar).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Create(Tr, ignoreCase: false)).ToList();
        return lokasyonlar.Count <= 1
            ? null
            : $"Yükler {lokasyonlar.Count} lokasyonda: {string.Join(", ", lokasyonlar)}. " +
              "Lokasyon başına araç sayısı için \"Lokasyona göre ayrı\" seçeneğiyle hesaplayın.";
    }

    public byte[] AracTalepRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        var tipler = AracTipleri(plan);
        var lokasyonlar = y.LokasyonaGoreAyri ? LokasyonAracTipleri(plan) : [];
        var lokasyonNotu = KarisikLokasyonNotu(plan);
        var atanamayanlar = AtanamayanYukler(plan);

        static void TipHucresi(IContainer hucre, AracTipiSatiri t) => hucre.PaddingVertical(2).Column(c =>
        {
            c.Item().Text(t.TipAdi).SemiBold().FontSize(11);
            c.Item().Text(t.AileAdi).FontSize(7.5f).FontColor(Gri);
            foreach (var g in t.Gerekceler)
                c.Item().Text("Özel araç: " + g).FontSize(7.5f).FontColor(Turuncu);
            foreach (var ts in t.Tasmalar)
                c.Item().Text($"Taşma ({ts}); izin gerekebilir.").FontSize(7.5f).FontColor(Turuncu);
        });

        static IContainer ToplamHucresi(IContainer hucre, string arkaPlan) => hucre.Background(arkaPlan).Border(0.5f).BorderColor(Kenar).Padding(4);

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
                if (y.LokasyonaGoreAyri)
                    // Lokasyona göre ayrı plan: depo her lokasyona hangi tipten kaç araç göndereceğini görür.
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.ConstantColumn(70);
                        });
                        BasliklariYaz(table, "Lokasyon", "Araç tipi", "Adet");
                        foreach (var l in lokasyonlar)
                        {
                            for (var i = 0; i < l.Tipler.Count; i++)
                            {
                                // Lokasyon adı grubun ilk satırında yazılır; grup ara toplamıyla kapanır.
                                Hucre(table.Cell(), false).AlignMiddle().Text(i == 0 ? l.Lokasyon : string.Empty).SemiBold().FontSize(11);
                                TipHucresi(Hucre(table.Cell(), false), l.Tipler[i]);
                                Hucre(table.Cell(), false).AlignRight().AlignMiddle().Text(l.Tipler[i].Adet.ToString(Tr)).Bold().FontSize(14);
                            }
                            ToplamHucresi(table.Cell().ColumnSpan(2), Zebra).Text($"{l.Lokasyon} toplamı").SemiBold();
                            ToplamHucresi(table.Cell(), Zebra).AlignRight().Text(l.Adet.ToString(Tr)).Bold().FontSize(11);
                        }
                        if (lokasyonlar.Count == 0)
                            table.Cell().ColumnSpan(3).Border(0.5f).BorderColor(Kenar).Padding(6).Text("Araç yok.").FontColor(Gri);
                        else
                        {
                            ToplamHucresi(table.Cell().ColumnSpan(2), AcikMavi).Text("TOPLAM").Bold();
                            ToplamHucresi(table.Cell(), AcikMavi).AlignRight().Text(y.Araclar.Count.ToString(Tr)).Bold().FontSize(14);
                        }
                    });
                else
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
                            TipHucresi(Hucre(table.Cell(), zebra), t);
                            Hucre(table.Cell(), zebra).AlignRight().AlignMiddle().Text(t.Adet.ToString(Tr)).Bold().FontSize(14);
                        }
                        if (tipler.Count == 0)
                            table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(Kenar).Padding(6).Text("Araç yok.").FontColor(Gri);
                        else
                        {
                            ToplamHucresi(table.Cell(), AcikMavi).Text("TOPLAM").Bold();
                            ToplamHucresi(table.Cell(), AcikMavi).AlignRight().Text(y.Araclar.Count.ToString(Tr)).Bold().FontSize(14);
                        }
                    });
                if (lokasyonNotu != null)
                    col.Item().PaddingTop(4).Text(lokasyonNotu).FontSize(8).FontColor(Gri);

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

    private static string Sayi(int adet) => adet.ToString("#,0", Tr);
    private static string MmBirimli(int mm) => Mm(mm) + " mm";
    private static string M3(decimal m3) => m3.ToString("#,0.00", Tr) + " m³";
    private static string Yuzde(decimal yuzde) => "%" + yuzde.ToString("0.#", Tr);

    public byte[] YuklemePlaniRaporu(YuklemePlaniDetayi plan, DateTime raporTarihi)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var y = Yerlesim(plan);
        var renkler = ProjeRenkHaritasi(plan);
        var tekProje = TekProje(plan);
        var atanamayanlar = AtanamayanYukler(plan);
        var ustuneKonmazlar = YuklemePlaniRaporHesabi.UstuneKonmazlar(plan);

        void Sayfa(IDocumentContainer container, string altBilgi, Action<ColumnDescriptor> icerik) =>
            container.Page(page =>
            {
                // Dikey sayfa: büyük 3B çizim, altında üç görünüm, bilgi blokları ve yük tablosu alt alta sığar.
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(7.5f).DisableFontFeature(FontFeatures.StandardLigatures));
                UstCubuk(page, "Araç Bazlı Yükleme Planı", plan, raporTarihi);
                AltBilgi(page, altBilgi);
                page.Content().Column(icerik);
            });

        var altMetin = $"3K – Araç Bazlı Yükleme Planı | {plan.PlanNo} Rev. {plan.RevizyonNo}";
        return Document.Create(container =>
        {
            Sayfa(container, altMetin, col => Ozet(col, plan, renkler, atanamayanlar.Count));
            // "Elle düzenlendi" ve "kaynak farkı" çubukları araç sayfasında yaklaşık iki uyarı satırı yer kaplar.
            var ustSatir = (y.ElleDuzenlendi ? 2 : 0) + (plan.KaynakFarklari.Count > 0 ? 2 : 0);
            foreach (var a in y.Araclar)
                Sayfa(container, $"{altMetin} | Araç {a.AracNo} / {y.Araclar.Count}",
                    col => AracSayfasi(col, a, y.Araclar.Count, renkler, tekProje, y.LokasyonaGoreAyri, ustuneKonmazlar, ustSatir));
            if (atanamayanlar.Count > 0)
                Sayfa(container, altMetin, col =>
                {
                    Bolum(col, $"Araç atanamayan yükler ({atanamayanlar.Count})");
                    AtanamayanTablosu(col, atanamayanlar);
                });
        }).GeneratePdf();
    }

    /// <summary>
    /// İlk sayfa: projeler, planın özeti (araç sayısı ve kaçında yük kasa dışına taşıyor, tip dağılımı, yüklenen yük
    /// sayısı, toplam ağırlık ve hacim), araç listesi, genel uyarılar ve proje renkleri.
    /// </summary>
    private static void Ozet(ColumnDescriptor col, YuklemePlaniDetayi plan, Dictionary<string, string> renkler, int atanamayan)
    {
        var y = Yerlesim(plan);
        var ozet = YuklemePlaniRaporHesabi.Ozet(plan, TeknikVeri);
        col.Item().PaddingTop(8).Column(c =>
        {
            BilgiSatiri(c, "Projeler", ProjeMetni(plan));
            BilgiSatiri(c, "Taşıma ailesi", plan.TasimaAilesiAdi);
            BilgiSatiri(c, "Hesap tarihi", Tarih(y.HesapTarihi));
        });

        col.Item().PaddingTop(8).Border(0.75f).BorderColor(Kenar).Background(Zebra).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text($"{ozet.AracSayisi.ToString(Tr)} araç kullanıldı").Bold().FontSize(14).FontColor(Mavi);
                if (ozet.OlcuDisiAracSayisi > 0)
                    c.Item().Text($"{ozet.OlcuDisiAracSayisi.ToString(Tr)} araçta yük kasa dışına taşıyor (ölçü dışı)").SemiBold().FontSize(8.5f).FontColor(Turuncu);
                foreach (var t in AracTipleri(plan))
                    c.Item().PaddingTop(2).Text($"{t.Adet.ToString(Tr)} × {t.TipAdi}").FontSize(9);
            });
            row.RelativeItem().Column(c =>
            {
                c.Item().Text(YuklenenMetni(ozet)).Bold().FontSize(11);
                c.Item().PaddingTop(3).Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(9));
                    t.Span("Toplam yük ağırlığı: ").FontColor(Gri);
                    t.Span(Kg(ozet.ToplamKg)).SemiBold();
                });
                c.Item().PaddingTop(2).Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(9));
                    t.Span("Toplam hacim: ").FontColor(Gri);
                    t.Span(M3(ozet.ToplamHacimM3)).SemiBold();
                });
                if (atanamayan > 0)
                    c.Item().PaddingTop(3).Text($"{atanamayan.ToString(Tr)} yük araca atanamadı (son sayfa)").Bold().FontSize(9).FontColor(Kirmizi);
            });
        });

        Bolum(col, "Araçlar");
        col.Item().Table(table =>
        {
            // Lokasyon sütunu yalnız lokasyona göre ayrı planda vardır (her araç tek lokasyondan yüklenir).
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(30);
                if (y.LokasyonaGoreAyri)
                    c.RelativeColumn(1.5f);
                c.RelativeColumn(2.4f);
                c.ConstantColumn(30);
                c.RelativeColumn(1.9f);
                c.ConstantColumn(52);
                c.ConstantColumn(52);
            });
            if (y.LokasyonaGoreAyri)
                BasliklariYaz(table, "Araç", "Lokasyon", "Araç tipi", "Yük", "Ağırlık / kapasite", "Hacim", "Sonuç");
            else
                BasliklariYaz(table, "Araç", "Araç tipi", "Yük", "Ağırlık / kapasite", "Hacim", "Sonuç");
            var i = 0;
            foreach (var a in y.Araclar)
            {
                var zebra = i++ % 2 == 1;
                Hucre(table.Cell(), zebra).Text(a.AracNo.ToString(Tr)).Bold();
                if (y.LokasyonaGoreAyri)
                    Hucre(table.Cell(), zebra).Text(AracLokasyonu(a)).SemiBold();
                Hucre(table.Cell(), zebra).Text(t =>
                {
                    t.Span(a.TipAdi).SemiBold();
                    t.Span($"  {a.EkipmanKodu}").FontColor(Gri);
                    if (a.AileDisiGerekce != null)
                        t.Span("  · özel araç").FontColor(Turuncu);
                });
                Hucre(table.Cell(), zebra).AlignRight().Text(a.Konumlar.Count.ToString(Tr));
                Hucre(table.Cell(), zebra).Text(a.KapasiteKg is { } k ? $"{Kg(a.ToplamKg)} / {Kg(k)}" : Kg(a.ToplamKg));
                Hucre(table.Cell(), zebra).AlignRight().Text(M3(YuklemePlaniRaporHesabi.YukHacmiM3(a.Konumlar)));
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
            // Çok projede satır kaydırır; renk 10 projeden sonra tekrar eder, kimlik sandık kodunda yazılıdır. Çizimlerde
            // her sandık kendi rengindedir; proje rengi araç sayfalarındaki yük tablosunun proje sütunundadır.
            col.Item().PaddingTop(8).Text("Proje renkleri (yük tablosunun proje sütunu)").SemiBold().FontColor(Gri);
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

    /// <summary>Özetteki "yüklenen" satırı: yüklerin hepsi sandıksa sandık sayısı, elle eklenen yük de varsa ikisi birlikte.</summary>
    internal static string YuklenenMetni(YuklemePlaniRaporHesabi.PlanOzeti ozet) =>
        ozet.SandikSayisi == ozet.YukSayisi
            ? $"{Sayi(ozet.YukSayisi)} sandık yüklendi"
            : $"{Sayi(ozet.YukSayisi)} yük yüklendi ({Sayi(ozet.SandikSayisi)} sandık)";

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
    /// Araç sayfasındaki yük: yükleme sırasıyla, sandık rengiyle. <see cref="Renk"/> yükün bütün çizimlerde ve tabloda
    /// kullanılan rengidir; <see cref="Sorunlu"/> yük çizimde ve tabloda kırmızıyla vurgulanır.
    /// </summary>
    internal sealed record AracYukSatiri(PlanYukKonumu Konum, string Etiket, string CizimEtiketi, int Sira, string Renk, bool Sorunlu, bool UstuneKonmaz);

    /// <summary>
    /// Aracın yük satırları (<see cref="AracYukleri"/> sırası): her yüke sandık rengi, uygunsuz/taşan yük işareti ve
    /// "üstüne konmaz" izni. Çizimler ve tablo bu tek listeden üretilir (aynı sandık her yerde aynı renkte).
    /// </summary>
    internal static IReadOnlyList<AracYukSatiri> AracYukSatirlari(PlanAraci a, bool tekProje, IReadOnlySet<string> ustuneKonmazlar)
    {
        var veri = TeknikVeri(a);
        var yukler = AracYukleri(a, tekProje);
        var renkler = YuklemePlaniRaporHesabi.SandikRenkleri(yukler.Select(x => x.Konum).ToList());
        var uygunsuz = a.Sonuc == YerlesimKontrolSonucu.Uygunsuz;
        // Elle yerleşimde yük düzeyindeki uygunsuz sonuçlar (çakışma, desteksiz, kasa dışı…) da kırmızı çizilir.
        var sorunluYukler = a.YukSorunlari.Where(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz).Select(s => s.YukAnahtari).ToHashSet(StringComparer.Ordinal);
        return yukler.Select((x, i) => new AracYukSatiri(x.Konum, x.Etiket, x.CizimEtiketi, x.Sira, renkler[i],
            (uygunsuz && Tasiyor(x.Konum, veri)) || sorunluYukler.Contains(x.Konum.YukAnahtari),
            ustuneKonmazlar.Contains(x.Konum.YukAnahtari))).ToList();
    }

    /// <summary>
    /// Çizim kutuları. İstifin alt katları 3B ve üstten görünümde örtülür: üst kat birleşik etiketi taşır
    /// (<paramref name="katEtiketi"/> false). Yandan ve arkadan görünümde katlar ayrı görünür; her kat kendi numarasını
    /// taşır (<paramref name="katEtiketi"/> true).
    /// </summary>
    internal static IReadOnlyList<CizimKutusu> CizimKutulari(IReadOnlyList<AracYukSatiri> satirlar, bool katEtiketi) =>
        satirlar.Select(s => new CizimKutusu(katEtiketi ? s.Etiket : s.CizimEtiketi, s.Konum.XMm, s.Konum.YMm, s.Konum.ZMm, s.Konum.BoyMm, s.Konum.EnMm,
            s.Konum.YukseklikMm, s.Renk, s.Sorunlu)).ToList();

    /// <summary>
    /// Bu sayıya kadar yük tablosu sayfa genişliğinde tek sütundur; daha uzun liste iki sütuna akar (çizimlerin altından
    /// başlar, sığmayan satırlar sonraki sayfada iki sütun olarak sürer).
    /// </summary>
    internal const int TekSutunSatiri = 12;

    private static void AracSayfasi(
        ColumnDescriptor col, PlanAraci a, int aracSayisi, Dictionary<string, string> projeRenkleri, bool tekProje, bool lokasyonaGoreAyri,
        IReadOnlySet<string> ustuneKonmazlar, int ustSatir)
    {
        var veri = TeknikVeri(a);
        var satirlar = AracYukSatirlari(a, tekProje, ustuneKonmazlar);
        var kutular = CizimKutulari(satirlar, katEtiketi: false);
        var katKutulari = CizimKutulari(satirlar, katEtiketi: true);
        var yontemler = a.Konumlar.Select(k => YuklemeEtiketleri.GirisYonu(k.YuklemeYontemi)).Distinct().ToList();
        var olcu = YuklemePlaniRaporHesabi.Olculer(veri);
        var kullanim = YuklemePlaniRaporHesabi.Kullanim(a, olcu);
        var toplam = YuklemePlaniRaporHesabi.Toplam(a, veri);
        var merkez = YuklemePlaniRaporHesabi.AgirlikMerkezi(a, veri);
        var isaret = merkez == null ? null : new CizimNoktasi(merkez.XMm, merkez.YMm, merkez.ZYerdenMm);

        // Başlık: "1 : 40' High Cube" (araç no ve tip); lokasyona göre ayrı planda aracın yükleneceği yer.
        col.Item().PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span($"{a.AracNo.ToString(Tr)} : ").Bold().FontSize(15).FontColor(Mavi);
                t.Span(a.TipAdi).Bold().FontSize(15);
                if (lokasyonaGoreAyri)
                    t.Span($"   {AracLokasyonu(a)}").Bold().FontSize(11).FontColor(Mavi);
            });
            row.AutoItem().AlignBottom().PaddingLeft(10).Text(YuklemeEtiketleri.KontrolSonucu(a.Sonuc)).Bold().FontSize(10).FontColor(SonucRengi(a.Sonuc));
        });
        col.Item().PaddingTop(1).Row(row =>
        {
            row.RelativeItem().Text($"{a.EkipmanKodu} · Araç {a.AracNo.ToString(Tr)} / {aracSayisi.ToString(Tr)} · {a.Konumlar.Count.ToString(Tr)} yük" +
                                    (yontemler.Count > 0 ? $" · yükleme: {string.Join(", ", yontemler)}" : string.Empty)).FontColor(Gri);
            row.AutoItem().PaddingLeft(10).Text("Plaka / konteyner no: ______________________").FontColor(Gri);
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
            var etiket = satirlar.Where(x => x.Konum.YukAnahtari == s.YukAnahtari).Select(x => x.Etiket).FirstOrDefault() ?? s.YukAnahtari;
            col.Item().PaddingTop(2).Text($"{etiket}: {s.Mesaj}").FontColor(Kirmizi);
        }

        // Büyük çizimin yüksekliği sayfadaki yere göredir: yük tablosu kısaysa boş kalacak yer çizime verilir, uyarı
        // satırları arttıkça çizim alçalır (çizimler, bloklar ve tablo aynı sayfada başlasın). Çizimler bölünmez: yer
        // kalmazsa bütün hâlinde sonraki sayfaya geçer, kırpılmaz.
        var uyariSatiri = ustSatir + (a.AileDisiGerekce != null ? 2 : 0) + (TasmaMetni(a).Length > 0 ? 1 : 0) +
                          a.Kontroller.Count(k => k.Sonuc == YerlesimKontrolSonucu.Uygunsuz) + a.YukSorunlari.Count(s => s.Sonuc == YerlesimKontrolSonucu.Uygunsuz);
        var bosSatirPayi = Math.Min(Math.Max(0, TekSutunSatiri - satirlar.Count) * TabloSatiriYuksekligi, BuyukCizimEkEnFazla);
        var buyukCizim = Math.Clamp(BuyukCizimYuksekligi + bosSatirPayi - Math.Max(0, uyariSatiri - 3) * UyariSatiriYuksekligi,
            BuyukCizimEnAz, BuyukCizimYuksekligi + BuyukCizimEkEnFazla);
        col.Item().PaddingTop(5).Text("3B görünüm (arka-sağ-üstten)").SemiBold().FontColor(Gri);
        col.Item().Height(buyukCizim).AlignCenter().AlignMiddle().Svg(YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Izometrik, isaret)).FitArea();
        col.Item().PaddingTop(4).Row(row =>
        {
            row.Spacing(8);
            Gorunum(row.RelativeItem(5), "Yandan (soldan)", YerlesimCizimi.Svg(veri, katKutulari, CizimGorunumu.Yandan, isaret));
            Gorunum(row.RelativeItem(5), "Üstten", YerlesimCizimi.Svg(veri, kutular, CizimGorunumu.Ustten, isaret));
            Gorunum(row.RelativeItem(3), "Arkadan", YerlesimCizimi.Svg(veri, katKutulari, CizimGorunumu.Arkadan, isaret));
        });

        col.Item().PaddingTop(6).Row(row =>
        {
            row.Spacing(10);
            Blok(row.RelativeItem(10), "Araç", c =>
            {
                Deger(c, "İç uzunluk", olcu.IcUzunlukMm is { } u ? MmBirimli(u) : Tanimsiz);
                Deger(c, "İç genişlik", olcu.IcGenislikMm is { } g ? MmBirimli(g) : Tanimsiz);
                Deger(c, "İç yükseklik", olcu.IcYukseklikMm is { } h ? MmBirimli(h) : Tanimsiz);
                Deger(c, "Teknik kapasite", a.KapasiteKg is { } kapasite ? Kg(kapasite) : Tanimsiz);
                if (veri.DaraKg is { } dara)
                    Deger(c, "Dara", Kg(dara));
                // Birden çok yükleme yüzeyi (lowbed boynu ve havuzu): ölçülerin hangi yüzeyden geldiği yazılır.
                if (olcu.YuzeySayisi > 1)
                    c.Item().PaddingTop(2).Text($"{olcu.YuzeySayisi.ToString(Tr)} yükleme yüzeyi: uzunluk toplam; genişlik ve yükseklik ana yüzey ({olcu.AnaYuzeyAdi}).")
                        .FontSize(6).FontColor(Gri);
            });
            Blok(row.RelativeItem(11), "Kullanım", c =>
            {
                Deger(c, "Uzunluk", MmBirimli(kullanim.UzunlukMm));
                Deger(c, "Genişlik", MmBirimli(kullanim.GenislikMm));
                Deger(c, "Yükseklik", MmBirimli(kullanim.YukseklikMm));
                Deger(c, "Yük ağırlığı", kullanim.AgirlikYuzde is { } ay ? $"{Kg(kullanim.YukKg)} ({Yuzde(ay)})" : Kg(kullanim.YukKg));
                Deger(c, "Yük hacmi", kullanim.HacimYuzde is { } hy ? $"{M3(kullanim.YukHacmiM3)} ({Yuzde(hy)})" : M3(kullanim.YukHacmiM3));
                if (kullanim.KalanHacimM3 is { } kalan)
                    Deger(c, "Kalan hacim", M3(kalan));
                Deger(c, "Taban doluluğu", Yuzde(a.TabanDolulukYuzde));
            });
            Blok(row.RelativeItem(10), "Toplam (yüklü araç)", c =>
            {
                Deger(c, "Uzunluk", MmBirimli(toplam.UzunlukMm));
                Deger(c, "Genişlik", MmBirimli(toplam.GenislikMm));
                Deger(c, "Yükseklik", MmBirimli(toplam.YukseklikMm));
                if (toplam.BrutKg is { } brut)
                    Deger(c, "Brüt ağırlık", Kg(brut));
                if (a.SolTasmaMm > 0) Deger(c, "Sol taşma", MmBirimli(a.SolTasmaMm), Turuncu);
                if (a.SagTasmaMm > 0) Deger(c, "Sağ taşma", MmBirimli(a.SagTasmaMm), Turuncu);
                if (a.OnTasmaMm > 0) Deger(c, "Ön taşma", MmBirimli(a.OnTasmaMm), Turuncu);
                if (a.ArkaTasmaMm > 0) Deger(c, "Arka taşma", MmBirimli(a.ArkaTasmaMm), Turuncu);
                if (toplam.UsttenTasmaMm > 0) Deger(c, "Üstten taşma", MmBirimli(toplam.UsttenTasmaMm), Turuncu);
            });
            Blok(row.RelativeItem(12), "Ağırlık merkezi", c =>
            {
                if (merkez == null)
                {
                    c.Item().PaddingTop(1.5f).Text("Yük yok ya da ağırlık bilinmiyor.").FontColor(Gri);
                    return;
                }
                Deger(c, "X (boyuna, ön uçtan)", MmBirimli(merkez.XMm), etiketPayi: 14);
                Deger(c, "Y (enine, sol kenardan)", MmBirimli(merkez.YMm), etiketPayi: 14);
                Deger(c, merkez.ZYuzeyden ? "Z (yükseklik, yüzeyden)" : "Z (yükseklik, yerden)", MmBirimli(merkez.ZMm), etiketPayi: 14);
                c.Item().PaddingTop(3).Row(r =>
                {
                    r.ConstantItem(9).Height(9).Svg(YerlesimCizimi.AgirlikMerkeziIsareti()).FitArea();
                    r.RelativeItem().PaddingLeft(3).AlignMiddle().Text("Çizimlerdeki işaret; yüklerin ortak ağırlık merkezidir (araç darası hariç).").FontSize(6).FontColor(Gri);
                });
            });
        });

        var cokProje = !tekProje;
        if (satirlar.Count <= TekSutunSatiri)
            col.Item().PaddingTop(8).EnsureSpace(TabloIcinEnAzYer).Element(e => YukTablosu(e, dar: false));
        else
            col.Item().PaddingTop(8).EnsureSpace(TabloIcinEnAzYer).MultiColumn(m =>
            {
                m.Columns(2);
                m.Spacing(10);
                m.BalanceHeight();
                m.Content().Element(e => YukTablosu(e, dar: true));
            });
        col.Item().PaddingTop(3).Text("Sıra: yükleme adımı (sahadaki adım no); aynı sıradaki katlar istif hâlinde birlikte yüklenir. " +
                                      "Boy ve en araçtaki duruşa göredir (90°: sandık döndürülerek yerleşti). X: üstüne yük konmaz.")
            .FontSize(6.5f).FontColor(Gri);

        static void Gorunum(IContainer kap, string baslik, string svg) => kap.Column(c =>
        {
            c.Item().Text(baslik).SemiBold().FontColor(Gri);
            c.Item().Height(KucukCizimYuksekligi).AlignCenter().AlignMiddle().Svg(svg).FitArea();
        });

        static void Blok(IContainer kap, string baslik, Action<ColumnDescriptor> icerik) => kap.Column(c =>
        {
            c.Item().BorderBottom(0.75f).BorderColor(Mavi).PaddingBottom(2).Text(baslik).Bold().FontSize(9).FontColor(Mavi);
            c.Item().PaddingTop(1).Column(icerik);
        });

        // Etiket ve değer oranla bölünür (değer sağa yaslı); uzun değer taşmaz, alt satıra kayar.
        static void Deger(ColumnDescriptor c, string ad, string deger, string? renk = null, int etiketPayi = 10) => c.Item().PaddingTop(1.5f).Row(r =>
        {
            r.RelativeItem(etiketPayi).Text(ad).FontColor(Gri);
            r.RelativeItem(20 - etiketPayi).AlignRight().Text(deger).SemiBold().FontColor(renk ?? Colors.Black);
        });

        void YukTablosu(IContainer kap, bool dar) => kap.Table(table =>
        {
            var yazi = dar ? 6.5f : 7.5f;
            var baslik = dar ? 5.5f : 6.5f;
            var olcuSutunu = dar ? 25 : 52;
            // Hücre içeriği bölünmez: uzun kod alt satıra kayarsa satır sütun ya da sayfa sonunda ikiye ayrılmaz.
            IContainer H(IContainer c, bool zebra) => c.Background(zebra ? Zebra : Colors.White).Border(0.5f).BorderColor(Kenar)
                .PaddingVertical(2).PaddingHorizontal(dar ? 2 : 3).ShowEntire();
            IContainer B(IContainer c) => c.Background(Mavi).Border(0.5f).BorderColor(Mavi).PaddingVertical(2).PaddingHorizontal(dar ? 2 : 3);
            IContainer T(IContainer c) => c.Background(AcikMavi).Border(0.5f).BorderColor(Kenar).PaddingVertical(2.5f).PaddingHorizontal(dar ? 2 : 3);

            table.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(dar ? 22 : 36);          // renk + sıra
                cd.RelativeColumn(cokProje ? 1.25f : 1);   // sandık
                if (cokProje)
                    cd.RelativeColumn(1);                  // proje
                cd.ConstantColumn(dar ? 27 : 52);          // boy (en uzun ölçü: 12.032)
                cd.ConstantColumn(olcuSutunu);             // en
                cd.ConstantColumn(olcuSutunu);             // yükseklik
                cd.ConstantColumn(dar ? 28 : 56);          // brüt
                cd.ConstantColumn(dar ? 15 : 30);          // kat
                cd.ConstantColumn(dar ? 30 : 56);          // üstüne konmaz
            });
            table.Header(h =>
            {
                void Yaz(IContainer c, string metin, bool orta = false)
                {
                    var hucre = B(c);
                    (orta ? hucre.AlignCenter() : hucre).Text(metin).Bold().FontSize(baslik).FontColor(Colors.White);
                }
                // Üst satır birimleri taşır (örnek rapordaki gibi): ölçüler mm, ağırlık kg.
                Yaz(h.Cell().ColumnSpan(cokProje ? 3u : 2u), string.Empty);
                Yaz(h.Cell().ColumnSpan(3), "MM", orta: true);
                Yaz(h.Cell(), "KG", orta: true);
                Yaz(h.Cell(), string.Empty);
                Yaz(h.Cell(), "ÜSTÜNE", orta: true);
                Yaz(h.Cell(), "SIRA");
                Yaz(h.Cell(), "SANDIK");
                if (cokProje)
                    Yaz(h.Cell(), "PROJE");
                Yaz(h.Cell(), "BOY", orta: true);
                Yaz(h.Cell(), "EN", orta: true);
                Yaz(h.Cell(), dar ? "YÜKS." : "YÜKSEKLİK", orta: true);
                Yaz(h.Cell(), "BRÜT", orta: true);
                Yaz(h.Cell(), "KAT", orta: true);
                Yaz(h.Cell(), "KONMAZ", orta: true);
            });
            for (var i = 0; i < satirlar.Count; i++)
            {
                var s = satirlar[i];
                var k = s.Konum;
                var zebra = i % 2 == 1;
                H(table.Cell(), zebra).Row(r =>
                {
                    // Renk karesi çizimdeki kutunun rengidir (uygunsuz yükte kırmızı).
                    r.AutoItem().AlignMiddle().Width(7).Height(7).Border(0.4f).BorderColor("#1e293b").Background(s.Sorunlu ? Kirmizi : s.Renk);
                    r.RelativeItem().PaddingLeft(3).Text(s.Sira.ToString(Tr)).Bold().FontSize(yazi);
                });
                H(table.Cell(), zebra).Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(yazi));
                    t.Span(s.Etiket).SemiBold();
                    if (k.Donduruldu)
                        t.Span(" 90°").FontSize(yazi - 1).FontColor(Gri);
                });
                if (cokProje)
                    H(table.Cell(), zebra).Row(r =>
                    {
                        if (k.ProjeNo != null && projeRenkleri.TryGetValue(k.ProjeNo, out var projeRengi))
                            r.AutoItem().AlignMiddle().PaddingRight(2).Width(5).Height(5).Background(projeRengi);
                        r.RelativeItem().Text(k.ProjeNo ?? "-").FontSize(yazi);
                    });
                H(table.Cell(), zebra).AlignRight().Text(Mm(k.BoyMm)).FontSize(yazi);
                H(table.Cell(), zebra).AlignRight().Text(Mm(k.EnMm)).FontSize(yazi);
                H(table.Cell(), zebra).AlignRight().Text(Mm(k.YukseklikMm)).FontSize(yazi);
                H(table.Cell(), zebra).AlignRight().Text(k.BrutKg.ToString("#,0", Tr)).FontSize(yazi);
                H(table.Cell(), zebra).AlignCenter().Text(k.Kat.ToString(Tr)).FontSize(yazi);
                H(table.Cell(), zebra).AlignCenter().Text(s.UstuneKonmaz ? "X" : string.Empty).Bold().FontSize(yazi).FontColor(Turuncu);
            }
            // Toplam satırı: adet ve toplam ağırlık (aracın saklanan toplamı; başlıktaki ve özetteki değerle aynı).
            T(table.Cell().ColumnSpan(cokProje ? 3u : 2u)).Text($"Toplam: {satirlar.Count.ToString(Tr)} yük").Bold().FontSize(yazi);
            T(table.Cell().ColumnSpan(3)).Text(string.Empty).FontSize(yazi);
            T(table.Cell()).AlignRight().Text(a.ToplamKg.ToString("#,0", Tr)).Bold().FontSize(yazi);
            T(table.Cell().ColumnSpan(2)).Text(string.Empty).FontSize(yazi);
        });
    }

    private const string Tanimsiz = "tanımlı değil";

    /// <summary>
    /// Araç sayfasındaki çizim yükseklikleri (pt): büyük 3B çizim (tek sütunluk tablo dolu ve uyarı yokken) ve altındaki
    /// üç görünüm. Büyük çizim kısa tabloda en çok <see cref="BuyukCizimEkEnFazla"/> kadar büyür, uyarılar çoğalınca
    /// <see cref="BuyukCizimEnAz"/> değerine kadar küçülür.
    /// </summary>
    private const float BuyukCizimYuksekligi = 195;
    private const float BuyukCizimEnAz = 120;
    private const float BuyukCizimEkEnFazla = 80;
    private const float UyariSatiriYuksekligi = 10;
    private const float TabloSatiriYuksekligi = 13;
    private const float KucukCizimYuksekligi = 62;

    /// <summary>Tablo, başlığı ve birkaç satırı sığmayacak kadar az yer kaldıysa sonraki sayfadan başlar (başlık tek başına kalmaz).</summary>
    private const float TabloIcinEnAzYer = 70;

    private static bool Tasiyor(PlanYukKonumu k, EkipmanTeknikVerisi v) =>
        (v.DisOlcu?.GenislikMm is { } g && (k.YMm < 0 || k.YMm + k.EnMm > g)) ||
        (v.DisOlcu?.UzunlukMm is { } u && (k.XMm < 0 || k.XMm + k.BoyMm > u));
}
