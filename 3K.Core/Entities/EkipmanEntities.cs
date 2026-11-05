using _3K.Core.Enums;

namespace _3K.Core.Entities;

/// <summary>
/// Genel taşıma ailesi (Simülasyon isterleri 9): kullanıcının planda seçtiği üst grup (Konteyner, Açık kasa/platform,
/// Tenteli…). Normal yükler yalnız seçilen ailenin tiplerine yerleşir. Tohumu betik ve EF modeli birlikte kurar.
/// </summary>
public class EkipmanAilesi : BaseEntity
{
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Sira { get; set; }

    public virtual ICollection<EkipmanTipi> Tipler { get; set; } = new List<EkipmanTipi>();
}

/// <summary>
/// Ailenin alt tipi (20' DV, 40' HC, Mega tenteli, Lowbed…). Davranış <see cref="Kategori"/>'den türetilir; kitaplık
/// yeni tiple genişletilebilir.
/// </summary>
public class EkipmanTipi : BaseEntity
{
    public int AileId { get; set; }
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public EkipmanKategorisi Kategori { get; set; }
    public int Sira { get; set; }
    public bool Aktif { get; set; } = true;

    public virtual EkipmanAilesi Aile { get; set; } = null!;
}

/// <summary>Üretici ve model (ör. "Jenerik ISO / 40' HC", "Üretici X / 3 dingil lowbed").</summary>
public class EkipmanModeli : BaseEntity
{
    public int TipId { get; set; }
    public string Uretici { get; set; } = string.Empty;
    public string ModelAdi { get; set; } = string.Empty;

    public virtual EkipmanTipi Tip { get; set; } = null!;
    public virtual ICollection<EkipmanKonfigurasyonu> Konfigurasyonlar { get; set; } = new List<EkipmanKonfigurasyonu>();
}

/// <summary>
/// Planda kullanılan ekipman birimi: modelin belirli bir konfigürasyonu (uzama, dingil, havuz…). Teknik verisi
/// değişmez revizyonlarda tutulur; <see cref="Kod"/> içe aktarımda konfigürasyonu tanır.
/// </summary>
public class EkipmanKonfigurasyonu : BaseEntity
{
    public int ModelId { get; set; }
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;

    /// <summary>En son revizyonun numarası; yeni revizyon bu sayıyı bir artırır.</summary>
    public int SonRevizyonNo { get; set; }

    /// <summary>PostgreSQL xmin ile iyimser eşzamanlılık: aynı konfigürasyona aynı anda iki revizyon yazılamaz.</summary>
    public uint Version { get; set; }

    public virtual EkipmanModeli Model { get; set; } = null!;
    public virtual ICollection<EkipmanKonfigurasyonRevizyonu> Revizyonlar { get; set; } = new List<EkipmanKonfigurasyonRevizyonu>();
}

/// <summary>
/// Konfigürasyonun değişmez teknik verisi (<see cref="TeknikVeriJson"/>, yükleme motorunun şemasında) ve kaynağı.
/// Teknik veri değişince yeni revizyon yazılır; yalnız doğrulama bilgisi sonradan işlenir.
/// </summary>
public class EkipmanKonfigurasyonRevizyonu : BaseEntity
{
    public int KonfigurasyonId { get; set; }
    public int RevizyonNo { get; set; }
    public string TeknikVeriJson { get; set; } = "{}";
    public EkipmanKaynakTuru KaynakTuru { get; set; }
    public string? KaynakAciklama { get; set; }
    public DateTime? KaynakTarihi { get; set; }
    public string? Aciklama { get; set; }
    public EkipmanDogrulamaDurumu DogrulamaDurumu { get; set; } = EkipmanDogrulamaDurumu.Dogrulanmadi;
    public int? DogrulayanKullaniciId { get; set; }
    public DateTime? DogrulamaTarihi { get; set; }
    public int? OlusturanKullaniciId { get; set; }

    public virtual EkipmanKonfigurasyonu Konfigurasyon { get; set; } = null!;
}
