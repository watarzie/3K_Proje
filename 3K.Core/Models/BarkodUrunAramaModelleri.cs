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
    public const int VarsayilanSayfaBoyutu = 25;
    public const int EnFazlaSayfaBoyutu = 100;
    public const int ProjeAramaEnFazlaUzunluk = 100;

    /// <summary>Proje no / müşteri süzgeci: kırpılmış, büyük harfe çevrilmiş; boşsa null (süzgeç yok).</summary>
    public static string? ProjeAramaNormalize(string? metin) =>
        string.IsNullOrWhiteSpace(metin) ? null : metin.Trim().ToUpperInvariant();

    /// <summary>
    /// Ekran için tek sayfa: satırlar süzülmüş ve sıralanmış bütün sonuçtan kesilir; özet (kartlar) ve
    /// <see cref="BarkodUrunAramaSonucu.FiltreliSatir"/> bütün sonucundur. Sayfa sınır dışındaysa son sayfa döner.
    /// </summary>
    public static BarkodUrunAramaSonucu Sayfala(BarkodUrunAramaSonucu sonuc, int sayfa, int boyut)
    {
        var sayfaBoyutu = Math.Clamp(boyut, 1, EnFazlaSayfaBoyutu);
        var toplamSayfa = Math.Max(1, (int)Math.Ceiling(sonuc.FiltreliSatir / (double)sayfaBoyutu));
        var gecerliSayfa = Math.Clamp(sayfa, 1, toplamSayfa);
        return sonuc with
        {
            Satirlar = sonuc.Satirlar.Skip((gecerliSayfa - 1) * sayfaBoyutu).Take(sayfaBoyutu).ToList(),
            Sayfa = gecerliSayfa,
            SayfaBoyutu = sayfaBoyutu
        };
    }

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

/// <summary>Arama ölçütleri: barkod (tam eşleşme), yalnız eksikler, proje tipi ve proje no / müşteri süzgeci.</summary>
public sealed record BarkodUrunAramaKriteri(string Barkod, bool YalnizEksikler, int? ProjeTipiId, string? ProjeArama = null);

/// <summary>
/// Arama sonucu: süzülmüş satırlar ve "yalnız eksikler" süzgecinden bağımsız özet (seçilen proje tipi ve proje/müşteri
/// süzgecindeki bütün satırlar). Proje tipi ve proje/müşteri veritabanında süzülür; satır sınırı aşılırsa
/// (<see cref="Sinirli"/>) özet ve liste ilk satırlardandır. Ekranda <see cref="BarkodUrunAramaKurali.Sayfala"/> ile tek
/// sayfa döner; rapor bütün satırları yazar.
/// </summary>
public sealed record BarkodUrunAramaSonucu
{
    public string Barkod { get; init; } = string.Empty;
    /// <summary>Ürünün açıklaması (ilk dolu satırdan); ekranda ve rapor başlığında.</summary>
    public string? Aciklama { get; init; }
    public bool YalnizEksikler { get; init; }
    public int? ProjeTipiId { get; init; }
    /// <summary>Uygulanan proje no / müşteri süzgeci (kullanıcının yazdığı biçimde); yoksa null.</summary>
    public string? ProjeArama { get; init; }
    public DateTime OlusturmaTarihi { get; init; }
    public IReadOnlyList<BarkodUrunSatiri> Satirlar { get; init; } = [];

    /// <summary>Bütün süzgeçlerden (yalnız eksikler dahil) sonra, sayfalamadan önce satır sayısı.</summary>
    public int FiltreliSatir { get; init; }
    public int Sayfa { get; init; } = 1;
    public int SayfaBoyutu { get; init; }
    public int ToplamSayfa => SayfaBoyutu <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(FiltreliSatir / (double)SayfaBoyutu));

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
