using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Rapor önizlemesi: indirilecek dosyanın başlığı, sütunları ve ilk satırları. Satırlar rapor dosyalarıyla aynı
/// üreticilerden gelir; kullanıcı PDF/Excel indirmeden önce filtrenin neyi kapsadığını görür.
/// </summary>
internal sealed partial class FinansRaporService
{
    public async Task<FinansRaporOnizlemeModel> OnizlemeAsync(
        string tur,
        FinansListeFiltre filtre,
        FinansRaporSecenekleri secenekler,
        CancellationToken cancellationToken)
    {
        switch (tur)
        {
            case "is":
                return Onizleme(await IsRaporuAsync(filtre, secenekler.Kapsam, cancellationToken));
            case "gider":
                return Onizleme(await GiderRaporuAsync(filtre, secenekler.Gruplama, cancellationToken));
            case "finansal-durum":
                return Onizleme(await FinansalDurumRaporuAsync(filtre, cancellationToken));
            case "alacak":
                return DuzOnizleme(
                    "Alacak (Vade/Tahsilat) Raporu",
                    AlacakBasliklari,
                    AlacakSatirlari(await AlacakFaturalariAsync(filtre, cancellationToken)).ToList());
            default:
                var (baslik, basliklar, ozet) = await OzetTablosuAsync(tur, filtre, cancellationToken);
                return DuzOnizleme(baslik, basliklar, ozet);
        }
    }

    private static FinansRaporOnizlemeModel DuzOnizleme(string baslik, string[] basliklar, IReadOnlyList<object?[]> satirlar)
        => new(
            baslik,
            basliklar,
            satirlar.Take(FinansRaporOnizlemeModel.EnFazlaSatir)
                .Select(satir => (IReadOnlyList<string>)satir.Select(OnizlemeDegeri).ToList())
                .ToList(),
            satirlar.Count);

    /// <summary>
    /// Bölümlü raporun ilk tablosu. Satır sınırı aşılırsa ilk satırlar kesilir ama son bölüm başlığından sonrası yalnız
    /// toplam satırıysa (raporun "Genel toplam" bölümü) her zaman eklenir: kullanıcı genel toplamı önizlemede görür,
    /// kesilen bir bölümün ara toplamı başka bölümün altına düşmez.
    /// </summary>
    internal static FinansRaporOnizlemeModel Onizleme(BolumluRapor rapor)
    {
        var tablo = rapor.Tablolar[0];
        var satirlar = tablo.Satirlar;
        var sonBolum = satirlar.Select((s, i) => (s, i)).LastOrDefault(x => x.s.Tur == RaporSatirTuru.Bolum).i;
        var dip = satirlar.Count > 0 && satirlar[sonBolum].Tur == RaporSatirTuru.Bolum &&
                  satirlar.Skip(sonBolum + 1).All(s => s.Tur is RaporSatirTuru.AraToplam or RaporSatirTuru.GenelToplam)
            ? satirlar.Skip(sonBolum).ToList()
            : [];
        var gosterilen = satirlar.Count <= FinansRaporOnizlemeModel.EnFazlaSatir
            ? satirlar.ToList()
            : satirlar.Take(dip.Count > 0 ? sonBolum : satirlar.Count)
                .Take(Math.Max(FinansRaporOnizlemeModel.EnFazlaSatir - dip.Count, 0))
                .Concat(dip)
                .ToList();

        return new FinansRaporOnizlemeModel(
            rapor.Baslik,
            tablo.Sutunlar,
            gosterilen.Select(s => (IReadOnlyList<string>)OnizlemeHucreleri(s, tablo)).ToList(),
            satirlar.Count(s => s.Tur == RaporSatirTuru.Veri),
            gosterilen.Select(s => SatirTuruKodu(s.Tur)).ToList(),
            rapor.Tablolar.Select(x => x.Baslik).ToList());
    }

    /// <summary>Bölüm satırı tek hücrelidir (başlık), toplam satırlarında boş hücre "—" değil boş yazılır.</summary>
    private static List<string> OnizlemeHucreleri(RaporSatiri satir, RaporTablosu tablo) => satir.Tur switch
    {
        RaporSatirTuru.Bolum => [OnizlemeDegeri(satir.Degerler[0]), .. Enumerable.Repeat(string.Empty, tablo.Sutunlar.Count - 1)],
        RaporSatirTuru.Veri => satir.Degerler.Select((d, i) => d is null ? "—" : PdfDegeri(d, tablo.UcHaneli(i))).ToList(),
        _ => satir.Degerler.Select((d, i) => PdfDegeri(d, tablo.UcHaneli(i))).ToList()
    };

    private static string SatirTuruKodu(RaporSatirTuru tur) => tur switch
    {
        RaporSatirTuru.Bolum => FinansRaporOnizlemeModel.SatirBolum,
        RaporSatirTuru.AraToplam => FinansRaporOnizlemeModel.SatirAraToplam,
        RaporSatirTuru.GenelToplam => FinansRaporOnizlemeModel.SatirGenelToplam,
        _ => FinansRaporOnizlemeModel.SatirVeri
    };

    private static string OnizlemeDegeri(object? deger) => deger switch
    {
        null => "—",
        decimal sayi => Format(sayi),
        DateTime tarih => tarih.ToString("dd.MM.yyyy"),
        _ => deger.ToString() ?? string.Empty
    };
}
