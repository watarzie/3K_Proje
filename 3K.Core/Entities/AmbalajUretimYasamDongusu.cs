using _3K.Core.Enums;

namespace _3K.Core.Entities;

public class AmbalajUretimFormuSurumu : BaseEntity
{
    public Guid FormKimligi { get; set; }
    public int Surum { get; set; }
    public Guid IdempotencyAnahtari { get; set; }
    public string IstekHash { get; set; } = string.Empty;
    public string KapsamAnahtari { get; set; } = string.Empty;
    public int? ProjeId { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public int OlusturanKullaniciId { get; set; }
    public string? Aciklama { get; set; }
    public ICollection<AmbalajUretimFormuKaydi> Kayitlar { get; set; } = [];
}

public class AmbalajUretimFormuKaydi : BaseEntity
{
    public int FormSurumuId { get; set; }
    public int AmbalajUretimKaydiId { get; set; }
    public AmbalajUretimFormuSurumu FormSurumu { get; set; } = null!;
}

/// <summary>Fiziksel üretim partisi başına gerçekleşme; düzeltme yeni sürümdür.</summary>
public class AmbalajUretimGerceklesmesi : BaseEntity
{
    public Guid GerceklesmeKimligi { get; set; }
    public int Surum { get; set; }
    public int? OncekiSurumId { get; set; }
    public int AmbalajUretimKaydiId { get; set; }
    public Guid IsAkisKimligi { get; set; }
    public DateTime GerceklesmeTarihi { get; set; }
    public int? ProjeId { get; set; }
    public string ProjeNo { get; set; } = string.Empty;
    public string? ProjeAdi { get; set; }
    public string SandikNo { get; set; } = string.Empty;
    public string? SandikAdi { get; set; }
    public AmbalajSandikCinsi SandikCinsi { get; set; }
    public AmbalajSandikTuru Tur { get; set; }
    public int Adet { get; set; }
    public decimal Boy { get; set; }
    public decimal En { get; set; }
    public decimal Yukseklik { get; set; }
    public decimal? NetM3 { get; set; }
    public decimal? SarfM3 { get; set; }
    public string FormulVersiyonu { get; set; } = string.Empty;
    public int KullaniciId { get; set; }
    public string? Gerekce { get; set; }
}
