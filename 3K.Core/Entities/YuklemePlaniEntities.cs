using _3K.Core.Enums;

namespace _3K.Core.Entities;

/// <summary>
/// Yükleme planı başlığı (Simülasyon isterleri 27): numara, çalışma biçimi, taşıma ailesi ve proje/lokasyon tercihi.
/// İçerik değişmez revizyonlardadır; her kaydetme yeni revizyon yazar.
/// </summary>
public class YuklemePlani : BaseEntity
{
    public string PlanNo { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public YuklemeCalismaBicimi CalismaBicimi { get; set; }
    public int TasimaAilesiId { get; set; }
    public YuklemeProjeKarisimi ProjeKarisimi { get; set; }
    public YuklemeLokasyonTercihi LokasyonTercihi { get; set; } = YuklemeLokasyonTercihi.HenuzSecilmedi;
    public DateTime? YuklemeTarihi { get; set; }
    public string? Aciklama { get; set; }
    public int? OlusturanKullaniciId { get; set; }

    /// <summary>En son kaydedilen revizyonun numarası; kaydetme isteği bunu bekler (eşzamanlı kayıt 409).</summary>
    public int SonRevizyonNo { get; set; }

    /// <summary>PostgreSQL xmin ile iyimser eşzamanlılık.</summary>
    public uint Version { get; set; }

    public virtual EkipmanAilesi TasimaAilesi { get; set; } = null!;
    public virtual ICollection<YuklemePlanRevizyonu> Revizyonlar { get; set; } = new List<YuklemePlanRevizyonu>();
}

/// <summary>
/// Planın değişmez revizyonu: kaydeden, ayarlar (yerleşim boşluğu ve güvenlik payları) ve o andaki proje ile yük
/// listesi. Yük satırları hesapta kullanılan ölçü, ağırlık ve lokasyonun kopyasını taşır.
/// </summary>
public class YuklemePlanRevizyonu : BaseEntity
{
    public int PlanId { get; set; }
    public int RevizyonNo { get; set; }
    public int? KaydedenKullaniciId { get; set; }
    public string? Aciklama { get; set; }
    public string AyarlarJson { get; set; } = "{}";

    public virtual YuklemePlani Plan { get; set; } = null!;
    public virtual ICollection<YuklemePlanProjesi> Projeler { get; set; } = new List<YuklemePlanProjesi>();
    public virtual ICollection<YuklemePlanYuku> Yukler { get; set; } = new List<YuklemePlanYuku>();
}

/// <summary>Revizyondaki proje; proje silinirse bağ boşalır, numara kopyası kalır.</summary>
public class YuklemePlanProjesi : BaseEntity
{
    public int RevizyonId { get; set; }
    public int? ProjeId { get; set; }
    public string ProjeNo { get; set; } = string.Empty;

    public virtual YuklemePlanRevizyonu Revizyon { get; set; } = null!;
    public virtual Proje? Proje { get; set; }
}

/// <summary>
/// Revizyondaki tek fiziksel yük (isterler 3): projedeki sandık ya da elle eklenen yükün bir birimi. Ölçü, ağırlık,
/// lokasyon ve yük türü kaydetme anındaki kopyadır; kaynak değişince plan kendiliğinden değişmez, fark gösterilir.
/// </summary>
public class YuklemePlanYuku : BaseEntity
{
    public int RevizyonId { get; set; }
    public int SiraNo { get; set; }
    public PlanYukKaynagi Kaynak { get; set; }

    /// <summary>Kaynak sandık; sandık silinirse bağ boşalır, kopya kalır.</summary>
    public int? SandikId { get; set; }

    /// <summary>Elle eklenen yükün grup numarası (revizyon içinde); adetli kayıt bir grubun birimleridir.</summary>
    public int? ManuelGrupNo { get; set; }

    public int BirimNo { get; set; } = 1;
    public int BirimSayisi { get; set; } = 1;
    public string Kod { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public int? ProjeId { get; set; }
    public string? ProjeNo { get; set; }
    public int YukTuruId { get; set; } = (int)YukTuru.Sandik;
    public decimal? BoyMm { get; set; }
    public decimal? EnMm { get; set; }
    public decimal? YukseklikMm { get; set; }
    public decimal? BrutKg { get; set; }
    public int? LokasyonId { get; set; }

    /// <summary>Lokasyon adının kopyası; lokasyon kaydı silinse de plan okunur.</summary>
    public string? LokasyonAdi { get; set; }

    /// <summary>Hesaplamaya dahil mi (seçim kutusu).</summary>
    public bool Dahil { get; set; } = true;

    public bool DondurulebilirMi { get; set; }
    public bool UsteKonabilirMi { get; set; }
    public bool AltaKalabilirMi { get; set; }
    public decimal? UstYukSiniriKg { get; set; }

    /// <summary>Kullanıcının elle değiştirdiği izinler; kaynaktan yenilemede korunur.</summary>
    public PlanYukIzni ElleDegisenIzinler { get; set; }

    /// <summary>Elle eklenen yükün özel taşıma ve destek koşulları (serbest metin).</summary>
    public string? OzelTasimaNotu { get; set; }

    public virtual YuklemePlanRevizyonu Revizyon { get; set; } = null!;
    public virtual Sandik? Sandik { get; set; }
    public virtual Proje? Proje { get; set; }
    public virtual LookupYukTuru? YukTuruLookup { get; set; }
    public virtual LookupDepoLokasyon? LokasyonLookup { get; set; }
}
