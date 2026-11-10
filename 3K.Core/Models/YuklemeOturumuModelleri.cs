using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Araç yükleme ekranının plan listesi satırı: yerleşimi olan ya da yüklemesi süren plan.</summary>
public sealed class AracYuklemePlanOzeti
{
    public int Id { get; init; }
    public string PlanNo { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public DateTime? YuklemeTarihi { get; init; }
    public int RevizyonNo { get; init; }

    /// <summary>Son revizyonda yerleşim var mı (yeni oturum başlatılabilir mi).</summary>
    public bool YerlesimVar { get; init; }

    /// <summary>Süren oturum; yoksa null.</summary>
    public AracYuklemeOturumu? AcikOturum { get; init; }
}

/// <summary>Yükleme oturumunun başlığı ve ilerlemesi.</summary>
public sealed class AracYuklemeOturumu
{
    public int Id { get; init; }
    public int RevizyonNo { get; init; }
    public YuklemeOturumDurumu Durum { get; init; }
    public string? Baslatan { get; init; }
    public DateTime BaslamaTarihi { get; init; }
    public string? Kapatan { get; init; }
    public DateTime? KapanisTarihi { get; init; }
    public string? KapanisGerekcesi { get; init; }
    public int AracSayisi { get; init; }
    public int ToplamYuk { get; init; }
    public int YuklenenYuk { get; init; }

    /// <summary>Plan oturum başladıktan sonra revize edildi; oturum başladığı revizyonla sürer.</summary>
    public bool PlanDegisti { get; init; }
}

/// <summary>Oturumdaki (ya da başlatılacak yerleşimdeki) bir aracın özeti.</summary>
public sealed class AracYuklemeAracOzeti
{
    public int AracNo { get; init; }
    public string EkipmanKodu { get; init; } = string.Empty;
    public string EkipmanAdi { get; init; } = string.Empty;
    public string TipAdi { get; init; } = string.Empty;
    public IReadOnlyList<string> ProjeNolari { get; init; } = [];
    public decimal ToplamKg { get; init; }
    public int ToplamYuk { get; init; }
    public int YuklenenYuk { get; init; }
    public YerlesimKontrolSonucu Sonuc { get; init; }
}

/// <summary>
/// Bir planın araç yükleme durumu: süren (yoksa en son) oturum ve araçları. Oturum yoksa araçlar son revizyonun
/// yerleşimindendir (başlatılınca yüklenecek olanlar).
/// </summary>
public sealed class AracYuklemeDurumu
{
    public int PlanId { get; init; }
    public string PlanNo { get; init; } = string.Empty;
    public string PlanAdi { get; init; } = string.Empty;
    public DateTime? YuklemeTarihi { get; init; }
    public int SonRevizyonNo { get; init; }

    /// <summary>Son revizyonda yerleşim var mı.</summary>
    public bool YerlesimVar { get; init; }

    /// <summary>Son revizyonun yerleşim sonucu (uygunsuz yerleşimde başlatırken uyarı için); yerleşim yoksa null.</summary>
    public YerlesimKontrolSonucu? YerlesimSonucu { get; init; }

    /// <summary>Yeni oturum başlatılabilir mi: süren oturum yok ve son revizyonda yerleşim var.</summary>
    public bool Baslatilabilir { get; init; }

    public AracYuklemeOturumu? Oturum { get; init; }
    public IReadOnlyList<AracYuklemeAracOzeti> Araclar { get; init; } = [];
}

/// <summary>Etkin (geri alınmamış) yükleme işareti.</summary>
public sealed class YukIsareti
{
    public int Id { get; init; }
    public string YukAnahtari { get; init; } = string.Empty;
    public int AdimNo { get; init; }
    public YuklemeIsaretKaynagi Kaynak { get; init; }
    public bool SiraDisi { get; init; }
    public string? Yukleyen { get; init; }
    public DateTime YuklemeTarihi { get; init; }
}

/// <summary>
/// Araç yükleme ekranı: aracın yerleşimi (konumlar ve 3B için ekipman geometrisi), etkin işaretler ve sıradaki adım.
/// Adımlar ve yük numaraları konumlardan <see cref="YuklemeSirasiKurali"/> ile türetilir.
/// </summary>
public sealed class AracYuklemeAraci
{
    public int PlanId { get; init; }
    public string PlanNo { get; init; } = string.Empty;
    public string PlanAdi { get; init; } = string.Empty;
    public AracYuklemeOturumu Oturum { get; init; } = new();
    public PlanAraci Arac { get; init; } = new();
    public IReadOnlyList<YukIsareti> Isaretler { get; init; } = [];

    /// <summary>Sıradaki adım (yüklenmemiş yükü olan ilk adım); araç tamamlandıysa null.</summary>
    public int? SiradakiAdimNo { get; init; }
}

/// <summary>
/// Okutulan barkodun çözümü: yük nerede, ne yapılabilir. Durum değiştirmez; işaretleme ayrı komuttur. Yük ve konum
/// alanları yük oturumun yerleşimindeyse doludur.
/// </summary>
public sealed class YukCozumu
{
    public YukCozumSonucu Sonuc { get; init; }
    public string Mesaj { get; init; } = string.Empty;
    public string? YukAnahtari { get; init; }
    public string? Kod { get; init; }
    public int? AracNo { get; init; }
    public string? EkipmanKodu { get; init; }
    public int? AdimNo { get; init; }

    /// <summary>Yük listesindeki ve rapordaki numara.</summary>
    public int? No { get; init; }

    /// <summary>Yük daha önce yüklendiyse işareti.</summary>
    public YukIsareti? Isaret { get; init; }
}

/// <summary>İşaretleme sonucu: yük zaten yüklüyse yeni kayıt yazılmaz (aynı okutma iki kez gelebilir).</summary>
public sealed class YukIsaretSonucu
{
    public bool ZatenYuklendi { get; init; }
    public AracYuklemeAraci Arac { get; init; } = new();
}

/// <summary>Yükü yüklendi işaretleme isteği.</summary>
public sealed record YukIsaretleModeli(
    int OturumId, int AracNo, string YukAnahtari, YuklemeIsaretKaynagi Kaynak, bool SiraDisiOnayi, string? Barkod);
