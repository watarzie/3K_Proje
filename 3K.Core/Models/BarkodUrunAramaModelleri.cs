using _3K.Core.Common;

namespace _3K.Core.Models;

/// <summary>
/// Barkodla ürün arama: bir barkodun bütün projelerdeki (Normal, Saha, Yedek) çeki satırları ve saha/yedek sandıklarına
/// elle eklenen malzemeleri. Barkod tam eşleşir (baştaki/sondaki boşluk ve büyük/küçük harf önemsiz); "FC4324045" ile
/// "FC4324045-01" ayrı üründür. Eksik miktarı Eksik Ürünler raporunun kuralıdır. Ekran, PDF ve Excel aynı satırları
/// gösterir.
/// </summary>
public static class BarkodUrunAramaKurali
{
    public const int EnFazlaUzunluk = 100;

    /// <summary>Bir aramada en fazla satır; aşılırsa ilk satırlar döner ve sonuç sınırlı işaretlenir.</summary>
    public const int EnFazlaSatir = 2000;

    /// <summary>Kırpılmış, büyük harfe çevrilmiş (kültürden bağımsız) barkod; boşsa null.</summary>
    public static string? Normalize(string? barkod) =>
        string.IsNullOrWhiteSpace(barkod) ? null : barkod.Trim().ToUpperInvariant();

    public static bool Gecerli(string? barkod) =>
        Normalize(barkod) is { Length: <= EnFazlaUzunluk };

    /// <summary>
    /// Eksikler önce (büyük eksik önce), sonra proje no, doğal sandık no ve sıra no. Süzgeç: istenirse yalnız eksikler,
    /// proje tipi.
    /// </summary>
    public static IReadOnlyList<BarkodUrunSatiri> SuzVeSirala(IEnumerable<BarkodUrunSatiri> satirlar, bool yalnizEksikler, int? projeTipiId) =>
        satirlar
            .Where(s => !yalnizEksikler || s.Eksik > 0)
            .Where(s => projeTipiId is not > 0 || s.ProjeTipiId == projeTipiId)
            .OrderByDescending(s => s.Eksik > 0)
            .ThenBy(s => s.ProjeNo, SandikNumarasiComparer.Instance)
            // Sandığı belli olmayan satır ("-") projenin sonunda.
            .ThenBy(s => s.SandikNo == "-" ? string.Empty : s.SandikNo, SandikNumarasiComparer.Instance)
            .ThenBy(s => s.SiraNo ?? int.MaxValue)
            .ToList();
}

/// <summary>
/// Arama sonucu: süzülmüş satırlar ve "yalnız eksikler" süzgecinden bağımsız özet (seçilen proje tipindeki bütün satırlar).
/// Proje tipi veritabanında süzülür; satır sınırı aşılırsa (<see cref="Sinirli"/>) özet ve liste ilk satırlardandır.
/// </summary>
public sealed class BarkodUrunAramaSonucu
{
    public string Barkod { get; init; } = string.Empty;
    /// <summary>Ürünün açıklaması (ilk dolu satırdan); ekranda ve rapor başlığında.</summary>
    public string? Aciklama { get; init; }
    public bool YalnizEksikler { get; init; }
    public int? ProjeTipiId { get; init; }
    public DateTime OlusturmaTarihi { get; init; }
    public IReadOnlyList<BarkodUrunSatiri> Satirlar { get; init; } = [];

    /// <summary>"Yalnız eksikler" süzgecinden önce barkodun (seçilen proje tipindeki) bütün satırları.</summary>
    public int ToplamSatir { get; init; }
    public int ProjeSayisi { get; init; }
    public int EksikSatir { get; init; }
    public int EksikProjeSayisi { get; init; }
    public decimal ToplamEksik { get; init; }

    /// <summary>Satır sayısı <see cref="BarkodUrunAramaKurali.EnFazlaSatir"/>'ı aştı; yalnız ilk satırlar döndü.</summary>
    public bool Sinirli { get; init; }
}

/// <summary>
/// Bir projedeki tek satır. <see cref="ElleEklenen"/> satır saha/yedek sandığına çeki satırı olmadan eklenmiş
/// malzemedir: istenen girilen miktar, eksik konulmayan kısım, Grid/3K durumu yoktur.
/// </summary>
public sealed record BarkodUrunSatiri(
    int ProjeId,
    string ProjeNo,
    int ProjeTipiId,
    string ProjeTipi,
    string Musteri,
    string ProjeDurumu,
    string SandikNo,
    int? SiraNo,
    string? PozNo,
    string Aciklama,
    string? Birim,
    decimal Istenen,
    decimal GridGelen,
    decimal UcKGelen,
    decimal Eksik,
    string GridDurumu,
    string UcKDurumu,
    string SevkDurumu,
    bool ElleEklenen);
