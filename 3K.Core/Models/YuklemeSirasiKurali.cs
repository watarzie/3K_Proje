namespace _3K.Core.Models;

/// <summary>Adımdaki bir yük: görsellerdeki ve yük listesindeki numarası ("No") ile konumu.</summary>
public sealed record YuklemeAdimYuku(int No, PlanYukKonumu Konum);

/// <summary>
/// Bir yükleme adımı: aynı yükleme sırasındaki yükler (istif grubu), alt kattan üste. Grup dışarıda hazırlanır ve
/// bütün hâlinde yüklenir; tek yükte adım o yüktür.
/// </summary>
public sealed record YuklemeAdimi(int AdimNo, int YuklemeSirasi, IReadOnlyList<YuklemeAdimYuku> Yukler);

/// <summary>
/// Araç içi yükleme sırası (Simülasyon isterleri 14, 16): yükler yükleme sırasına, aynı sırada kata göre dizilir; bu
/// listedeki konum yükün numarasıdır ("No"). Adım, aynı yükleme sırasını paylaşan yüklerdir. Rapor bu kuralı kullanır;
/// plan ekranındaki 3B adım adım oynatma aynı numarayı ve adımı ön yüzdeki aynasıyla (yukleme-adimlari.ts) üretir.
/// </summary>
public static class YuklemeSirasiKurali
{
    /// <summary>Yükler yükleme sırasıyla, aynı sırada alt kat önce; listedeki konum + 1 yükün numarasıdır.</summary>
    public static List<PlanYukKonumu> Sirali(IEnumerable<PlanYukKonumu> konumlar) =>
        konumlar.OrderBy(k => k.YuklemeSirasi).ThenBy(k => k.Kat).ToList();

    /// <summary>Yükleme adımları: adım numarası 1'den başlar ve saklanan yükleme sırası boşluklu olsa da ardışıktır.</summary>
    public static List<YuklemeAdimi> Adimlar(IEnumerable<PlanYukKonumu> konumlar) =>
        Sirali(konumlar)
            .Select((k, i) => new YuklemeAdimYuku(i + 1, k))
            .GroupBy(y => y.Konum.YuklemeSirasi)
            .Select((g, i) => new YuklemeAdimi(i + 1, g.Key, g.ToList()))
            .ToList();
}
