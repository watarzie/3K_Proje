using _3K.Core.Enums;

namespace _3K.Core.Entities;

/// <summary>
/// Araç yükleme oturumu: sahada bir planın yüklerinin araçlara konduğunun kaydı. Başladığı plan revizyonuna sabittir
/// (revizyon değişmez; plan sonradan revize edilse de oturum aynı yerleşimle sürer). Planda aynı anda tek açık oturum
/// olur. Yükleme işareti planlama kaydıdır: sandık durumunu, sevki ya da hareket geçmişini değiştirmez.
/// </summary>
public class YuklemeOturumu : BaseEntity
{
    public int PlanId { get; set; }
    public int RevizyonId { get; set; }

    /// <summary>Oturumun sabitlendiği revizyonun numarası (kopya; planın son revizyonuyla karşılaştırılır).</summary>
    public int RevizyonNo { get; set; }

    public YuklemeOturumDurumu Durum { get; set; } = YuklemeOturumDurumu.Acik;

    /// <summary>Yerleşimdeki araç ve yük sayısı (başlangıçtaki kopya; liste ve ilerleme için).</summary>
    public int AracSayisi { get; set; }
    public int ToplamYuk { get; set; }

    public int? BaslatanKullaniciId { get; set; }
    public DateTime BaslamaTarihi { get; set; }
    public int? KapatanKullaniciId { get; set; }
    public DateTime? KapanisTarihi { get; set; }

    /// <summary>İptalde ve eksik yükle tamamlamada zorunlu.</summary>
    public string? KapanisGerekcesi { get; set; }

    /// <summary>PostgreSQL xmin ile iyimser eşzamanlılık (iki kişi aynı anda kapatırsa biri 409).</summary>
    public uint Version { get; set; }

    public virtual YuklemePlani Plan { get; set; } = null!;
    public virtual YuklemePlanRevizyonu Revizyon { get; set; } = null!;
    public virtual ICollection<YuklemeOturumYuku> Yukler { get; set; } = new List<YuklemeOturumYuku>();
}

/// <summary>
/// Bir yükün araca yüklendiği işareti. Geri alınan işaret silinmez (kim, ne zaman, neden); yük yeniden işaretlenince
/// yeni satır yazılır. Oturumda bir yükün en çok bir etkin (geri alınmamış) işareti olur.
/// </summary>
public class YuklemeOturumYuku : BaseEntity
{
    public int OturumId { get; set; }
    public int AracNo { get; set; }

    /// <summary>Plan yük anahtarı: "s:{sandıkId}" ya da "m:{grupNo}:{birimNo}".</summary>
    public string YukAnahtari { get; set; } = string.Empty;

    /// <summary>Sandık yükünde kaynak sandık; sandık silinirse bağ boşalır, kod kopyası kalır.</summary>
    public int? SandikId { get; set; }
    public string Kod { get; set; } = string.Empty;

    /// <summary>Yükün yükleme adımı (işaret anındaki).</summary>
    public int AdimNo { get; set; }
    public YuklemeIsaretKaynagi Kaynak { get; set; }

    /// <summary>Sıradaki adım tamamlanmadan yüklendi (operatör onayıyla).</summary>
    public bool SiraDisi { get; set; }

    public int? YukleyenKullaniciId { get; set; }
    public DateTime YuklemeTarihi { get; set; }
    public int? GeriAlanKullaniciId { get; set; }
    public DateTime? GeriAlinmaTarihi { get; set; }
    public string? GeriAlmaGerekcesi { get; set; }

    public virtual YuklemeOturumu Oturum { get; set; } = null!;
    public virtual Sandik? Sandik { get; set; }
}
