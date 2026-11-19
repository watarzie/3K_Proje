namespace _3K.YuklemeMotoru.Ekipman;

/// <summary>
/// Ekipman konfigürasyonunun teknik verisi (şema sürümü <see cref="GuncelSemaSurumu"/>). Birimler tamsayı milimetre ve
/// kilogramdır. Koordinatlar: X ekipmanın ön ucundan arkaya, Y sol kenardan sağa, Z yer seviyesinden yukarı. Boş alan
/// "bilinmiyor" demektir ve tahminle doldurulmaz; bilinmeyen veriye dayanan kontrol "Teyit edilmedi" olur.
/// </summary>
public sealed record EkipmanTeknikVerisi
{
    public const int GuncelSemaSurumu = 1;

    public int SemaSurumu { get; init; } = GuncelSemaSurumu;

    /// <summary>Dış ölçüler (uzunluk, genişlik, yükseklik).</summary>
    public Olcu? DisOlcu { get; init; }

    /// <summary>Boş ağırlık.</summary>
    public int? DaraKg { get; init; }

    /// <summary>Taşıyabileceği en fazla yük.</summary>
    public int? TeknikKapasiteKg { get; init; }

    /// <summary>Yüklü en fazla ağırlık (dara + yük).</summary>
    public int? AzamiBrutKg { get; init; }

    /// <summary>Uzayan ekipmanda en fazla uzama.</summary>
    public int? UzamaEnFazlaMm { get; init; }

    public KingPin? KingPin { get; init; }

    /// <summary>Yükleme yüzeyleri (ör. boyun, havuz, arka güverte); boyuna sırayla ve çakışmadan.</summary>
    public IReadOnlyList<YukYuzeyi> Yuzeyler { get; init; } = [];

    public IReadOnlyList<Aciklik> Acikliklar { get; init; } = [];
    public IReadOnlyList<Engel> Engeller { get; init; } = [];
    public IReadOnlyList<Dingil> Dingiller { get; init; } = [];
    public IReadOnlyList<BaglamaNoktasi> BaglamaNoktalari { get; init; } = [];
}

public sealed record Olcu(int UzunlukMm, int GenislikMm, int YukseklikMm);

/// <summary>
/// Yükleme yüzeyi: boyuna başlangıcı, uzunluğu, genişliği ve yerden yüksekliği. Kullanılabilir yükseklik kapalı ve
/// tenteli gövdede iç yüksekliktir; açık platformda boştur (istif sınırı yoktur).
/// </summary>
public sealed record YukYuzeyi(
    string Kod,
    string? Ad,
    int BaslangicXMm,
    int UzunlukMm,
    int GenislikMm,
    int YerdenYukseklikMm,
    int? KullanilabilirYukseklikMm,
    int? TabanYukuKgM2,
    int? NoktasalYukKg);

public enum AciklikTarafi
{
    Arka = 1,
    On = 2,
    Sol = 3,
    Sag = 4,
    Ust = 5
}

/// <summary>
/// Yükleme açıklığı. Arka ve ön kapıda genişlik ve yükseklik; yan açıklıkta boyuna başlangıç, uzunluk ve yükseklik;
/// üst açıklıkta boyuna başlangıç, uzunluk ve genişlik verilir (net ölçüler).
/// </summary>
public sealed record Aciklik(AciklikTarafi Taraf, int? BaslangicXMm, int? UzunlukMm, int? GenislikMm, int? YukseklikMm);

public enum EngelTuru
{
    Dikme = 1,
    Travers = 2,
    TekerHavuzu = 3,
    UcDuvar = 4,
    Kapak = 5,
    Diger = 9
}

/// <summary>Yükleme hacmindeki engel (dikme, travers, teker havuzu…) kutu olarak; sökülebilen engel ayrıca işaretlenir.</summary>
public sealed record Engel(
    EngelTuru Tur,
    int XMm,
    int YMm,
    int ZMm,
    int UzunlukMm,
    int GenislikMm,
    int YukseklikMm,
    bool Sokulebilir,
    string? Aciklama);

public enum DingilTuru
{
    Tahriksiz = 1,
    Tahrikli = 2,
    Pandul = 3
}

/// <summary>Dingil: boyuna konumu, türü, grubu (aynı gruptaki dingiller yükü paylaşır) ve teknik kapasitesi.</summary>
public sealed record Dingil(int XMm, DingilTuru Tur, int GrupNo, int? KapasiteKg);

/// <summary>Yarı römorkun çekiciye bağlandığı nokta ve izin verilen bağlantı yükü.</summary>
public sealed record KingPin(int XMm, int? AzamiBaglantiYukuKg);

public sealed record BaglamaNoktasi(int XMm, int YMm, int ZMm, int? KapasiteDaN);
