using _3K.Core.Enums;

namespace _3K.Core.Entities;

/// <summary>Mevzuat kaynağı: belge, türü, Resmî Gazete tarihi/sayısı ve erişim tarihi.</summary>
public class MevzuatKaynagi : BaseEntity
{
    public string YetkiAlani { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public MevzuatKaynakTuru Tur { get; set; }
    public DateTime? ResmiGazeteTarihi { get; set; }
    public string? ResmiGazeteSayisi { get; set; }
    public string? Url { get; set; }
    public DateTime? ErisimTarihi { get; set; }
    public string? Aciklama { get; set; }
}

/// <summary>
/// Bir yetki alanının (ör. TR) kural seti sürümü. Plan, hesap tarihinde geçerli onaylı sürümü kullanır ve sürüm
/// kimliğini saklar; yeni sürüm onaylanınca eski plan kendiliğinden değişmez.
/// </summary>
public class MevzuatKuralSeti : BaseEntity
{
    public string YetkiAlani { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int SurumNo { get; set; }
    public DateTime GecerlilikBaslangici { get; set; }
    public DateTime? GecerlilikBitisi { get; set; }
    public MevzuatKuralSetiDurumu Durum { get; set; } = MevzuatKuralSetiDurumu.Taslak;
    public int? OnaylayanKullaniciId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? Aciklama { get; set; }

    public virtual ICollection<MevzuatKurali> Kurallar { get; set; } = new List<MevzuatKurali>();
}

/// <summary>
/// Kural seti içindeki tek kural: kapalı kümeden bir tip (<see cref="MevzuatKuralTipi"/>), tipe özgü parametreler
/// (<see cref="ParametrelerJson"/>) ve madde atfı.
/// </summary>
public class MevzuatKurali : BaseEntity
{
    public int KuralSetiId { get; set; }
    public string Kod { get; set; } = string.Empty;
    public MevzuatKuralTipi Tip { get; set; }
    public string ParametrelerJson { get; set; } = "{}";
    public int? KaynakId { get; set; }
    public string? Madde { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public int Sira { get; set; }

    public virtual MevzuatKuralSeti KuralSeti { get; set; } = null!;
    public virtual MevzuatKaynagi? Kaynak { get; set; }
}
