using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>
/// Barkodla ürün arama (<see cref="BarkodUrunAramaKurali"/>): barkodun bütün projelerdeki çeki satırları ve saha/yedek
/// sandıklarına elle eklenen malzemeler. Eksik, Eksik Ürünler raporunun kalanıdır
/// (<see cref="RaporSorgulari.GetEksikRaporKalan"/>: sahaya aktarılıp tamamlanan miktar kaynak projede eksik sayılmaz);
/// sevk durumu raporların ortak kuralıdır (<see cref="UrunSevkDurumuKurali"/>). PDF ve Excel ekrandaki satırları yazar.
/// </summary>
internal sealed class BarkodUrunAramaService(AppDbContext context) : IBarkodUrunAramaService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private const string Mavi = "#1565C0";
    private const string AcikMavi = "#E3F2FD";
    private const string Zebra = "#F8FAFE";
    private const string Kenar = "#D9E2EC";
    private const string Kirmizi = "#D32F2F";
    private const string ElleEklenenDurum = "Elle eklenen";

    public async Task<BarkodUrunAramaSonucu> AraAsync(string barkod, bool yalnizEksikler, int? projeTipiId, CancellationToken cancellationToken)
    {
        var aranan = BarkodUrunAramaKurali.Normalize(barkod) ?? throw new IsKuraliException("Aranacak barkodu girin.");
        var sinir = BarkodUrunAramaKurali.EnFazlaSatir;
        var tip = projeTipiId is > 0 ? projeTipiId : null;

        // Barkod tam eşleşir; büyük/küçük harf ve kenar boşluğu sorguda yok sayılır.
        var cekiSatirlari = await context.CekiSatirlari
            .AsNoTracking()
            .AsSplitQuery()
            .Include(cs => cs.Ceki).ThenInclude(c => c.Proje).ThenInclude(p => p.ProjeTipiLookup)
            .Include(cs => cs.Ceki).ThenInclude(c => c.Proje).ThenInclude(p => p.DurumLookup)
            .Include(cs => cs.BirimLookup)
            .Include(cs => cs.GridDurumLookup)
            .Include(cs => cs.UcKDurumLookup)
            .Include(cs => cs.SandikIcerikleri).ThenInclude(si => si.Sandik)
            .Where(cs => cs.BarkodNo.Trim().ToUpper() == aranan)
            .Where(cs => tip == null || cs.Ceki.Proje.ProjeTipiId == tip)
            .OrderBy(cs => cs.Id)
            .Take(sinir + 1)
            .ToListAsync(cancellationToken);
        var tamamlamaPlanMap = await context.GetTamamlamaPlanMapAsync(cekiSatirlari, sadeceSevkEdilenSandiklar: false, cancellationToken);
        var tahsissizSandiklar = await TahsissizSatirSandiklariAsync(cekiSatirlari, cancellationToken);

        var elleEklenenler = await context.SandikIcerikleri
            .AsNoTracking()
            .Include(si => si.Sandik).ThenInclude(s => s.Proje).ThenInclude(p => p.ProjeTipiLookup)
            .Include(si => si.Sandik).ThenInclude(s => s.Proje).ThenInclude(p => p.DurumLookup)
            .Include(si => si.BirimLookup)
            .Where(si => si.CekiSatiriId == null && si.BarkodNo != null && si.BarkodNo.Trim().ToUpper() == aranan)
            .Where(si => tip == null || si.Sandik.Proje.ProjeTipiId == tip)
            .OrderBy(si => si.Id)
            .Take(sinir + 1)
            .ToListAsync(cancellationToken);

        var tumu = cekiSatirlari.Select(cs => CekiSatiriSatiri(cs, tamamlamaPlanMap, tahsissizSandiklar.GetValueOrDefault(cs.Id)))
            .Concat(elleEklenenler.Select(ElleEklenenSatiri))
            .ToList();
        var sinirli = tumu.Count > sinir;
        var satirlar = BarkodUrunAramaKurali.SuzVeSirala(tumu, yalnizEksikler, tip);
        var eksikler = tumu.Where(s => s.Eksik > 0).ToList();

        return new BarkodUrunAramaSonucu
        {
            Barkod = aranan,
            Aciklama = cekiSatirlari.Select(cs => cs.Aciklama)
                .Concat(elleEklenenler.Select(si => si.Isim))
                .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a))?.Trim(),
            YalnizEksikler = yalnizEksikler,
            ProjeTipiId = tip,
            OlusturmaTarihi = TurkeyTime.Now,
            Satirlar = satirlar.Take(sinir).ToList(),
            ToplamSatir = tumu.Count,
            ProjeSayisi = tumu.Select(s => s.ProjeId).Distinct().Count(),
            EksikSatir = eksikler.Count,
            EksikProjeSayisi = eksikler.Select(s => s.ProjeId).Distinct().Count(),
            ToplamEksik = eksikler.Sum(s => s.Eksik),
            Sinirli = sinirli
        };
    }

    /// <summary>
    /// Hiç sandık içeriği olmayan eski satırın sandığı (Eksik Ürünler raporuyla aynı kural, R-07): fiili, yoksa çekideki
    /// sandık no projede tek sandıkla eşleşiyorsa o sandık. Bütün projeler için tek sorgu.
    /// </summary>
    private async Task<Dictionary<int, Sandik>> TahsissizSatirSandiklariAsync(List<CekiSatiri> satirlar, CancellationToken cancellationToken)
    {
        var tahsissizler = satirlar.Where(s => s.SandikIcerikleri.Count == 0).ToList();
        if (tahsissizler.Count == 0)
            return [];
        var projeIdleri = tahsissizler.Select(s => s.Ceki.ProjeId).Distinct().ToList();
        var benzersizler = (await context.Sandiklar.AsNoTracking()
                .Where(s => projeIdleri.Contains(s.ProjeId))
                .ToListAsync(cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
            .GroupBy(s => (s.ProjeId, No: s.SandikNo.Trim().ToUpperInvariant()))
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single());
        var sonuc = new Dictionary<int, Sandik>();
        foreach (var satir in tahsissizler)
        {
            var sandikNo = string.IsNullOrWhiteSpace(satir.FiiliSandikNo) ? satir.CekideGecenSandikNo : satir.FiiliSandikNo;
            if (!string.IsNullOrWhiteSpace(sandikNo) &&
                benzersizler.TryGetValue((satir.Ceki.ProjeId, sandikNo.Trim().ToUpperInvariant()), out var sandik))
                sonuc[satir.Id] = sandik;
        }
        return sonuc;
    }

    private static BarkodUrunSatiri CekiSatiriSatiri(CekiSatiri cs, IReadOnlyDictionary<int, decimal> tamamlamaPlanMap, Sandik? tahsissizSandik)
    {
        var proje = cs.Ceki.Proje;
        var sevk = UrunSevkDurumuKurali.Belirle(cs, proje.Id, tahsissizSandik);
        return new BarkodUrunSatiri(
            proje.Id,
            proje.ProjeNo,
            proje.ProjeTipiId,
            Metin(proje.ProjeTipiLookup?.Deger),
            Metin(proje.Musteri),
            Metin(proje.DurumLookup?.Deger),
            SandikNolari(cs),
            cs.SiraNo,
            Bos(cs.OlcuResmiPozNo),
            Metin(cs.Aciklama),
            Bos(cs.BirimLookup?.Deger),
            cs.IstenenAdet,
            cs.GridGelenAdet,
            cs.GelenMiktar,
            RaporSorgulari.GetEksikRaporKalan(cs, tamamlamaPlanMap),
            Metin(cs.GridDurumLookup?.Deger),
            Metin(cs.UcKDurumLookup?.Deger),
            RaporBicimleri.SevkMetni(sevk),
            ElleEklenen: false);
    }

    /// <summary>
    /// Saha/yedek sandığına çeki satırı olmadan eklenen malzeme (<see cref="ElleEklenenMalzemeKurali"/>): istenen
    /// sandık miktarı, "3K'ya gelen" sandığa konulan miktar.
    /// </summary>
    private static BarkodUrunSatiri ElleEklenenSatiri(SandikIcerik si)
    {
        var proje = si.Sandik.Proje;
        var (istenen, konulan) = ElleEklenenMalzemeKurali.Miktarlar(si.TahsisMiktari, si.Miktar, si.KonulanAdet);
        return new BarkodUrunSatiri(
            proje.Id,
            proje.ProjeNo,
            proje.ProjeTipiId,
            Metin(proje.ProjeTipiLookup?.Deger),
            Metin(proje.Musteri),
            Metin(proje.DurumLookup?.Deger),
            Metin(si.Sandik.SandikNo),
            null,
            null,
            Metin(si.Isim),
            Bos(si.BirimLookup?.Deger),
            istenen,
            0,
            konulan,
            Math.Max(istenen - konulan, 0),
            ElleEklenenDurum,
            ElleEklenenDurum,
            UrunSevkDurumuKurali.Etiket(UrunSevkDurumuKurali.SandikDurumu(si.Sandik)),
            ElleEklenen: true);
    }

    /// <summary>Satırın sandıkları (doğal sırada); tahsisi yoksa fiili, o da yoksa çekideki sandık no.</summary>
    private static string SandikNolari(CekiSatiri cs)
    {
        var nolar = cs.SandikIcerikleri
            .Where(si => si.Sandik != null && si.Sandik.ProjeId == cs.Ceki.ProjeId && !string.IsNullOrWhiteSpace(si.Sandik.SandikNo))
            .Select(si => si.Sandik.SandikNo.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(SandikNumarasiComparer.Instance)
            .ToList();
        if (nolar.Count > 0)
            return string.Join(", ", nolar);
        return Metin(string.IsNullOrWhiteSpace(cs.FiiliSandikNo) ? cs.CekideGecenSandikNo : cs.FiiliSandikNo);
    }

    // ===== Rapor =====

    private static readonly string[] Basliklar =
    [
        "Proje No", "Proje Tipi", "Müşteri", "Proje Durumu", "Sandık", "Sıra", "Açıklama", "Birim", "İstenen",
        "Grid'e Gelen", "3K'ya Gelen", "Eksik", "Grid Durumu", "3K Durumu", "Sevk"
    ];

    private static object?[] Degerler(BarkodUrunSatiri s) =>
    [
        s.ProjeNo, s.ProjeTipi, s.Musteri, s.ProjeDurumu, s.SandikNo, s.SiraNo, s.Aciklama, s.Birim, s.Istenen,
        s.GridGelen, s.UcKGelen, s.Eksik, s.GridDurumu, s.UcKDurumu, s.SevkDurumu
    ];

    public byte[] PdfOlustur(BarkodUrunAramaSonucu sonuc)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(18);
            page.DefaultTextStyle(x => x.FontSize(7.5f));

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
                        c.Item().AlignCenter().Text("Barkodla Ürün Arama Raporu").Bold().FontSize(16).FontColor(Colors.White);
                        c.Item().AlignCenter().Text($"Barkod: {sonuc.Barkod}{(sonuc.Aciklama is null ? string.Empty : " · " + TekSatir(sonuc.Aciklama))}")
                            .FontSize(8).FontColor(Colors.Grey.Lighten3);
                    });
                    row.RelativeItem().AlignMiddle().Column(c =>
                    {
                        c.Item().AlignRight().Text($"Rapor tarihi: {sonuc.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm", Tr)}").FontSize(8).FontColor(Colors.Grey.Lighten3);
                        c.Item().AlignRight().Text(SuzgecMetni(sonuc)).FontSize(8).FontColor(Colors.Grey.Lighten3);
                    });
                });
                col.Item().PaddingTop(6).Text(OzetMetni(sonuc)).SemiBold().FontColor(Mavi);
            });

            page.Content().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);   // proje no
                    c.ConstantColumn(40);   // tip
                    c.RelativeColumn(0.9f); // müşteri
                    c.ConstantColumn(55);   // proje durumu
                    c.ConstantColumn(42);   // sandık
                    c.ConstantColumn(24);   // sıra
                    c.RelativeColumn(1.8f); // açıklama
                    c.ConstantColumn(30);   // birim
                    c.ConstantColumn(36);   // istenen
                    c.ConstantColumn(36);   // grid gelen
                    c.ConstantColumn(36);   // 3K gelen
                    c.ConstantColumn(36);   // eksik
                    c.ConstantColumn(52);   // grid durumu
                    c.ConstantColumn(52);   // 3K durumu
                    c.ConstantColumn(58);   // sevk
                });
                table.Header(h =>
                {
                    for (var i = 0; i < Basliklar.Length; i++)
                    {
                        var hucre = h.Cell().Background(Mavi).Border(0.5f).BorderColor(Mavi).PaddingVertical(4).PaddingHorizontal(3);
                        (i is >= 8 and <= 11 ? hucre.AlignRight() : hucre).Text(Basliklar[i].ToUpper(Tr)).Bold().FontSize(6.5f).FontColor(Colors.White);
                    }
                });
                var sira = 0;
                foreach (var s in sonuc.Satirlar)
                {
                    var zebra = sira++ % 2 == 1;
                    IContainer Hucre() => table.Cell().Background(zebra ? Zebra : Colors.White).Border(0.5f).BorderColor(Kenar).PaddingVertical(3).PaddingHorizontal(3);
                    Hucre().Text(s.ProjeNo).SemiBold().FontColor(Mavi);
                    Hucre().Text(s.ProjeTipi);
                    Hucre().Text(s.Musteri);
                    Hucre().Text(s.ProjeDurumu);
                    Hucre().Text(s.SandikNo);
                    Hucre().Text(s.SiraNo?.ToString(Tr) ?? "-");
                    Hucre().Text(s.Aciklama);
                    Hucre().Text(s.Birim ?? "-");
                    Hucre().AlignRight().Text(RaporBicimleri.FormatAdet(s.Istenen));
                    Hucre().AlignRight().Text(RaporBicimleri.FormatAdet(s.GridGelen));
                    Hucre().AlignRight().Text(RaporBicimleri.FormatAdet(s.UcKGelen));
                    var eksik = Hucre().AlignRight().Text(RaporBicimleri.FormatAdet(s.Eksik));
                    if (s.Eksik > 0) eksik.Bold().FontColor(Kirmizi);
                    Hucre().Text(s.GridDurumu);
                    Hucre().Text(s.UcKDurumu);
                    Hucre().Text(s.SevkDurumu);
                }
                if (sonuc.Satirlar.Count == 0)
                    table.Cell().ColumnSpan((uint)Basliklar.Length).Border(0.5f).BorderColor(Kenar).Padding(6)
                        .Text("Süzgece uyan satır yok.").FontColor(Colors.Grey.Darken1);
                else
                {
                    IContainer Toplam(uint genislik = 1) => table.Cell().ColumnSpan(genislik).Background(AcikMavi).Border(0.5f).BorderColor(Kenar).PaddingVertical(4).PaddingHorizontal(3);
                    Toplam(8).Text($"TOPLAM · {sonuc.Satirlar.Count} satır · {sonuc.Satirlar.Select(x => x.ProjeId).Distinct().Count()} proje").Bold();
                    Toplam().AlignRight().Text(RaporBicimleri.FormatAdet(sonuc.Satirlar.Sum(x => x.Istenen))).Bold();
                    Toplam().AlignRight().Text(RaporBicimleri.FormatAdet(sonuc.Satirlar.Sum(x => x.GridGelen))).Bold();
                    Toplam().AlignRight().Text(RaporBicimleri.FormatAdet(sonuc.Satirlar.Sum(x => x.UcKGelen))).Bold();
                    Toplam().AlignRight().Text(RaporBicimleri.FormatAdet(sonuc.Satirlar.Sum(x => x.Eksik))).Bold().FontColor(Kirmizi);
                    Toplam(3).Text(string.Empty);
                }
            });

            page.Footer().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"3K – Barkodla Ürün Arama | {sonuc.Barkod} | Eksik: Eksik Ürünler raporunun kalan miktarı")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
                row.AutoItem().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Medium));
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    public byte[] ExcelOlustur(BarkodUrunAramaSonucu sonuc)
    {
        const int baslikSatiri = 5;
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Barkod Arama");
        var sonSutun = Basliklar.Length;
        ws.Cell(1, 1).Value = $"Barkodla Ürün Arama Raporu – {sonuc.Barkod}" +
                              (sonuc.Aciklama is null ? string.Empty : $" · {TekSatir(sonuc.Aciklama)}");
        RaporExcelStilleri.StyleExcelTitle(ws, sonSutun);
        ws.Cell(2, 1).Value = $"{SuzgecMetni(sonuc)} · {OzetMetni(sonuc)}";
        ws.Cell(3, 1).Value = $"Rapor tarihi: {sonuc.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm", Tr)} · " +
                              "Eksik: Eksik Ürünler raporunun kalan miktarı";
        RaporExcelStilleri.StyleExcelInfoRows(ws, sonSutun);
        for (var i = 0; i < Basliklar.Length; i++)
            ws.Cell(baslikSatiri, i + 1).Value = Basliklar[i];
        RaporExcelStilleri.StyleExcelHeader(ws, baslikSatiri, sonSutun);

        var satir = baslikSatiri + 1;
        foreach (var s in sonuc.Satirlar)
        {
            var degerler = Degerler(s);
            for (var i = 0; i < degerler.Length; i++)
            {
                var hucre = ws.Cell(satir, i + 1);
                switch (degerler[i])
                {
                    case null: break;
                    case decimal d: RaporExcelStilleri.SetDecimalCell(hucre, d); break;
                    case int n: hucre.SetValue(n); break;
                    default: hucre.SetValue(Convert.ToString(degerler[i], Tr)); break;
                }
            }
            if (s.Eksik > 0)
                ws.Cell(satir, 12).Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml(Kirmizi));
            satir++;
        }
        var sonSatir = satir - 1;
        if (sonSatir > baslikSatiri)
            ws.Range(baslikSatiri, 1, sonSatir, sonSutun).SetAutoFilter();
        RaporExcelStilleri.FinalizeExcelWorksheet(ws, baslikSatiri, sonSatir, sonSutun);
        if (ws.Column(1).Width > 24)
            ws.Column(1).Width = 24;

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static string SuzgecMetni(BarkodUrunAramaSonucu sonuc)
    {
        var tip = sonuc.ProjeTipiId switch
        {
            (int)ProjeTipi.Normal => "Normal projeler",
            (int)ProjeTipi.Saha => "Saha projeleri",
            (int)ProjeTipi.Yedek => "Yedek projeleri",
            _ => "Tüm projeler"
        };
        return $"Süzgeç: {tip} · {(sonuc.YalnizEksikler ? "Yalnız eksikler" : "Bütün satırlar")}";
    }

    private static string OzetMetni(BarkodUrunAramaSonucu sonuc) =>
        $"{sonuc.ProjeSayisi} projede {sonuc.ToplamSatir} satır · {sonuc.EksikProjeSayisi} projede {sonuc.EksikSatir} eksik satır " +
        $"(toplam eksik {RaporBicimleri.FormatAdet(sonuc.ToplamEksik)})" +
        (sonuc.Sinirli ? $" · ilk {BarkodUrunAramaKurali.EnFazlaSatir} satır" : string.Empty);

    /// <summary>Çeki açıklamasındaki satır sonları ("TR\n\EN") başlıkta tek satır olur.</summary>
    private static string TekSatir(string metin) => string.Join(" ", metin.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string Metin(string? metin) => string.IsNullOrWhiteSpace(metin) ? "-" : metin.Trim();
    private static string? Bos(string? metin) => string.IsNullOrWhiteSpace(metin) ? null : metin.Trim();
}
