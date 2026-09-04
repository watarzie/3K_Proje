using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Finans iş/fiyat kütüphanesi. Fiyat bu tabloda tutulmaz; yıllık ve tarihsel
    /// tarifeler FinansFiyatTarifesi tablosunda versiyonlanır.
    /// </summary>
    public class FinansUrun : BaseEntity
    {
        public string Kod { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; set; }
        public bool Aktif { get; set; } = true;
        public int Sira { get; set; }
        public virtual ICollection<FinansUrunEslesmesi> Eslesmeler { get; set; } = new List<FinansUrunEslesmesi>();
        public virtual ICollection<FinansFiyatTarifesi> FiyatTarifeleri { get; set; } = new List<FinansFiyatTarifesi>();
    }

    public class FinansUrunEslesmesi : BaseEntity
    {
        public int FinansUrunId { get; set; }
        public FinansIsTuru IsTuru { get; set; }
        public string? SandikAdi { get; set; }
        public string? SandikTipi { get; set; }
        public decimal? Boy { get; set; }
        public decimal? En { get; set; }
        public decimal? Yukseklik { get; set; }
        public int? IcSandikSablonId { get; set; }
        public bool Aktif { get; set; } = true;
        public virtual FinansUrun FinansUrun { get; set; } = null!;
    }

    public class FinansFiyatTarifesi : BaseEntity
    {
        public int FinansUrunId { get; set; }
        public int Yil { get; set; }
        public DateTime GecerlilikBaslangici { get; set; }
        public DateTime GecerlilikBitisi { get; set; }
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public bool Aktif { get; set; } = true;
        public virtual FinansUrun FinansUrun { get; set; } = null!;
    }

    /// <summary>
    /// Üretimden idempotent aktarılan veya kullanıcı tarafından girilen iş kaydı.
    /// Fiyat alanları, kayıt/sipariş anındaki tarifenin değiştirilemez snapshot'ıdır.
    /// </summary>
    public class FinansIsKaydi : BaseEntity
    {
        public int? ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public bool ManuelProjeMi { get; set; }
        public FinansIsTuru IsTuru { get; set; }
        public string IsAdi { get; set; } = string.Empty;
        public string? OzelIsTuru { get; set; }
        public FinansHesaplamaYontemi? HesaplamaYontemi { get; set; }
        public string? RaporGrubu { get; set; }
        public string? Aciklama { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? SandikNo { get; set; }
        public string? SandikAdi { get; set; }
        public string? SandikTipi { get; set; }
        public decimal? Boy { get; set; }
        public decimal? En { get; set; }
        public decimal? Yukseklik { get; set; }
        public int? IcSandikSablonId { get; set; }
        public decimal Adet { get; set; }
        public string Birim { get; set; } = "Adet";
        public decimal BirimM3 { get; set; }
        public decimal ToplamM3 { get; set; }
        public int? FinansUrunId { get; set; }
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimiSnapshot { get; set; }
        public decimal BirimFiyatSnapshot { get; set; }
        public string ParaBirimiSnapshot { get; set; } = "EUR";
        public decimal KdvOraniSnapshot { get; set; }
        public int? TarifeYiliSnapshot { get; set; }
        public DateTime UretimTarihi { get; set; }
        public DateTime FinansDonemi { get; set; }
        public DateTime KayitTarihi { get; set; }
        public FinansIsDurumu Durum { get; set; } = FinansIsDurumu.SiparisBekliyor;
        public string KaynakTuru { get; set; } = "Manuel";
        public string? KaynakKayitId { get; set; }
        public bool KaynakAktif { get; set; } = true;
        public int? DuzenliIsId { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual Proje? Proje { get; set; }
        public virtual FinansUrun? FinansUrun { get; set; }
        public virtual FinansDuzenliIs? DuzenliIs { get; set; }
        public virtual ICollection<FinansSiparisKalemi> SiparisKalemleri { get; set; } = new List<FinansSiparisKalemi>();
    }

    public class FinansSiparis : BaseEntity
    {
        public string KayitNo { get; set; } = string.Empty;
        public string PoNumarasi { get; set; } = string.Empty;
        public DateTime SiparisTarihi { get; set; }
        public string? Aciklama { get; set; }
        public FinansSiparisDurumu Durum { get; set; } = FinansSiparisDurumu.Acik;
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual ICollection<FinansSiparisKalemi> Kalemler { get; set; } = new List<FinansSiparisKalemi>();
        public virtual ICollection<FinansFatura> Faturalar { get; set; } = new List<FinansFatura>();
    }

    public class FinansSiparisKalemi : BaseEntity
    {
        public int FinansSiparisId { get; set; }
        public int FinansIsKaydiId { get; set; }
        public decimal Adet { get; set; }
        public decimal M3 { get; set; }
        public int? FinansUrunId { get; set; }
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimiSnapshot { get; set; }
        public decimal BirimFiyatSnapshot { get; set; }
        public string ParaBirimiSnapshot { get; set; } = "EUR";
        public decimal KdvOraniSnapshot { get; set; }
        public decimal NetTutarSnapshot { get; set; }
        public decimal KdvTutariSnapshot { get; set; }
        public decimal ToplamTutarSnapshot { get; set; }
        public virtual FinansSiparis FinansSiparis { get; set; } = null!;
        public virtual FinansIsKaydi FinansIsKaydi { get; set; } = null!;
        public virtual FinansUrun? FinansUrun { get; set; }
        public virtual ICollection<FinansFaturaKalemi> FaturaKalemleri { get; set; } = new List<FinansFaturaKalemi>();
    }

    public class FinansFatura : BaseEntity
    {
        public int FinansSiparisId { get; set; }
        public string KayitNo { get; set; } = string.Empty;
        public string FaturaNumarasi { get; set; } = string.Empty;
        public DateTime FaturaTarihi { get; set; }
        public string? Aciklama { get; set; }
        public string? BelgeParaBirimiSnapshot { get; set; }
        public decimal? BelgeNetTutarSnapshot { get; set; }
        public decimal? BelgeKdvTutariSnapshot { get; set; }
        public decimal? BelgeToplamTutarSnapshot { get; set; }
        public decimal MutabakatFarkiSnapshot { get; set; }
        public string? MutabakatAciklamasi { get; set; }
        public FinansFaturaDurumu Durum { get; set; } = FinansFaturaDurumu.Aktif;
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual FinansSiparis FinansSiparis { get; set; } = null!;
        public virtual ICollection<FinansFaturaKalemi> Kalemler { get; set; } = new List<FinansFaturaKalemi>();
    }

    public class FinansFaturaKalemi : BaseEntity
    {
        public int FinansFaturaId { get; set; }
        public int FinansSiparisKalemiId { get; set; }
        public decimal Adet { get; set; }
        public decimal M3 { get; set; }
        public decimal NetTutarSnapshot { get; set; }
        public decimal KdvTutariSnapshot { get; set; }
        public decimal ToplamTutarSnapshot { get; set; }
        public virtual FinansFatura FinansFatura { get; set; } = null!;
        public virtual FinansSiparisKalemi FinansSiparisKalemi { get; set; } = null!;
    }

    public class FinansDuzenliIs : BaseEntity
    {
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public string? ManuelProjeAdi { get; set; }
        public string IsAdi { get; set; } = string.Empty;
        public FinansIsTuru IsTuru { get; set; } = FinansIsTuru.OzelIs;
        public string? OzelIsTuru { get; set; }
        public FinansHesaplamaYontemi HesaplamaYontemi { get; set; } = FinansHesaplamaYontemi.DegiskenAdet;
        public string RaporGrubu { get; set; } = "Özel İş";
        public string Musteri { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public FinansTekrarSikligi TekrarSikligi { get; set; } = FinansTekrarSikligi.Aylik;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int OlusturmaGunu { get; set; } = 1;
        public decimal Miktar { get; set; }
        public string Birim { get; set; } = "Adet";
        public int? FinansUrunId { get; set; }
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public bool Aktif { get; set; } = true;
        public virtual Proje? Proje { get; set; }
        public virtual FinansUrun? FinansUrun { get; set; }
        public virtual ICollection<FinansIsKaydi> OlusanKayitlar { get; set; } = new List<FinansIsKaydi>();
    }

    public class FinansGiderKategori : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public bool Aktif { get; set; } = true;
        public virtual ICollection<FinansGiderKalemi> Kalemler { get; set; } = new List<FinansGiderKalemi>();
        public virtual ICollection<FinansGider> Giderler { get; set; } = new List<FinansGider>();
    }

    public class FinansGiderKalemi : BaseEntity
    {
        public int FinansGiderKategoriId { get; set; }
        public string Kod { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public string? VarsayilanFirmaVeyaKisi { get; set; }
        public decimal? VarsayilanMiktar { get; set; }
        public string? VarsayilanBirim { get; set; }
        public decimal? VarsayilanBirimFiyat { get; set; }
        public string? VarsayilanParaBirimi { get; set; }
        public bool VarsayilanKdvDahil { get; set; }
        public decimal? VarsayilanKdvOrani { get; set; }
        public bool Aktif { get; set; } = true;
        public virtual FinansGiderKategori Kategori { get; set; } = null!;
    }

    public class FinansGider : BaseEntity
    {
        public DateTime Tarih { get; set; }
        public DateTime FinansDonemi { get; set; }
        public int FinansGiderKategoriId { get; set; }
        public int? FinansGiderKalemiId { get; set; }
        public string? AltKategori { get; set; }
        public string? FirmaVeyaKisi { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public decimal Miktar { get; set; } = 1;
        public string Birim { get; set; } = "Adet";
        public decimal BirimFiyat { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public bool KdvDahil { get; set; }
        public decimal KdvOrani { get; set; }
        public decimal Matrah { get; set; }
        public decimal KdvTutari { get; set; }
        public decimal ToplamTutar { get; set; }
        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public FinansIsTuru? IsTuru { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual FinansGiderKategori Kategori { get; set; } = null!;
        public virtual FinansGiderKalemi? GiderKalemi { get; set; }
        public virtual Proje? Proje { get; set; }
    }

    /// <summary>
    /// Finans tablolarındaki kritik alan değişimlerini eski/yeni değerleriyle saklar.
    /// Kayıtlar uygulama üzerinden değiştirilemez ve silinmez.
    /// </summary>
    public class FinansDegisiklikGecmisi : BaseEntity
    {
        public string VarlikTuru { get; set; } = string.Empty;
        public int VarlikId { get; set; }
        public string Islem { get; set; } = string.Empty;
        public string AlanAdi { get; set; } = string.Empty;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public string IslemYapan { get; set; } = "SYSTEM";
    }
}
