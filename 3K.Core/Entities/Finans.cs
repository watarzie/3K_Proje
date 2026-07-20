namespace _3K.Core.Entities
{
    public enum FinansIsTuru
    {
        NormalSandik = 1,
        IlaveSandik = 2,
        IcSandik = 3,
        SahaSandigi = 4,
        YedekSandik = 5,
        Tadilat = 6,
        DigerAmbalajIsi = 7,
        OzelIs = 8,
        SarfKereste = 9
    }

    public enum FinansSiparisDurumu
    {
        Bekliyor = 1,
        KismiAcildi = 2,
        Acildi = 3,
        IptalEdildi = 4
    }

    public enum FinansFaturaDurumu
    {
        Bekliyor = 1,
        KismiFaturalandi = 2,
        Faturalandi = 3,
        IptalEdildi = 4
    }

    public enum FinansBelgeTuru
    {
        Siparis = 1,
        Fatura = 2,
        OzelIs = 3,
        Gider = 4
    }

    public enum FinansFiyatlandirmaBirimi
    {
        Adet = 1,
        M3 = 2
    }

    public enum FinansHesaplamaYontemi
    {
        SabitAylik = 1,
        DegiskenTutar = 2,
        DegiskenAdet = 3
    }

    public class FinansIsKaydi : BaseEntity
    {
        public int? ProjeId { get; set; }
        public int? OzelIsId { get; set; }
        public int? KaynakKayitId { get; set; }
        public string KaynakModul { get; set; } = string.Empty;
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public string SandikNo { get; set; } = string.Empty;
        public string SandikAdi { get; set; } = string.Empty;
        public string? SandikTipi { get; set; }
        public decimal? Boy { get; set; }
        public decimal? En { get; set; }
        public decimal? Yukseklik { get; set; }
        public int? IcSandikSablonId { get; set; }
        public FinansIsTuru IsTuru { get; set; }
        public decimal Adet { get; set; }
        public decimal BirimM3 { get; set; }
        public DateTime UretimeAlinmaTarihi { get; set; }
        public DateTime? UretimTamamlanmaTarihi { get; set; }
        public string UretimDurumu { get; set; } = string.Empty;
        public DateTime AktarimTarihi { get; set; }
        public bool KaynakAktif { get; set; } = true;

        public decimal ToplamM3 => Adet * BirimM3;
        public virtual Proje? Proje { get; set; }
        public virtual FinansOzelIs? OzelIs { get; set; }
        public virtual ICollection<FinansSiparisKalemi> SiparisKalemleri { get; set; } = new List<FinansSiparisKalemi>();
    }

    public class FinansSiparis : BaseEntity
    {
        public string KayitNo { get; set; } = string.Empty;
        public int? ProjeId { get; set; }
        public string AnaProjeNo { get; set; } = string.Empty;
        public string PoNumarasi { get; set; } = string.Empty;
        public DateTime SiparisTarihi { get; set; }
        public string? Aciklama { get; set; }
        public FinansSiparisDurumu Durum { get; set; } = FinansSiparisDurumu.Acildi;
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }

        public virtual Proje? Proje { get; set; }
        public virtual ICollection<FinansSiparisKalemi> Kalemler { get; set; } = new List<FinansSiparisKalemi>();
        public virtual ICollection<FinansBelge> Belgeler { get; set; } = new List<FinansBelge>();
    }

    public class FinansSiparisKalemi : BaseEntity
    {
        public int SiparisId { get; set; }
        public int IsKaydiId { get; set; }
        public int? UrunId { get; set; }
        public decimal Adet { get; set; }
        public decimal M3 { get; set; }
        public string UrunKodu { get; set; } = string.Empty;
        public string UrunAdi { get; set; } = string.Empty;
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; set; } = FinansFiyatlandirmaBirimi.M3;
        public decimal FiyatlandirmaMiktari { get; set; }
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public decimal NetTutar { get; set; }
        public decimal KdvTutari { get; set; }
        public decimal ToplamTutar { get; set; }
        public bool FiyatManuelDegistirildi { get; set; }

        public virtual FinansSiparis Siparis { get; set; } = null!;
        public virtual FinansIsKaydi IsKaydi { get; set; } = null!;
        public virtual FinansUrun? Urun { get; set; }
        public virtual ICollection<FinansFaturaKalemi> FaturaKalemleri { get; set; } = new List<FinansFaturaKalemi>();
    }

    public class FinansUrun : BaseEntity
    {
        public string Kod { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; set; } = FinansFiyatlandirmaBirimi.M3;
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public bool Aktif { get; set; } = true;
        public int Sira { get; set; }

        public virtual ICollection<FinansUrunEslesmesi> Eslesmeler { get; set; } = new List<FinansUrunEslesmesi>();
        public virtual ICollection<FinansSiparisKalemi> SiparisKalemleri { get; set; } = new List<FinansSiparisKalemi>();
    }

    public class FinansUrunEslesmesi : BaseEntity
    {
        public int UrunId { get; set; }
        public FinansIsTuru IsTuru { get; set; }
        public string? SandikAdi { get; set; }
        public string? SandikTipi { get; set; }
        public decimal? Boy { get; set; }
        public decimal? En { get; set; }
        public decimal? Yukseklik { get; set; }
        public int? IcSandikSablonId { get; set; }
        public bool Aktif { get; set; } = true;

        public virtual FinansUrun Urun { get; set; } = null!;
    }

    public class FinansFatura : BaseEntity
    {
        public string KayitNo { get; set; } = string.Empty;
        public int SiparisId { get; set; }
        public string FaturaNumarasi { get; set; } = string.Empty;
        public DateTime FaturaTarihi { get; set; }
        public string? Aciklama { get; set; }
        public FinansFaturaDurumu Durum { get; set; } = FinansFaturaDurumu.Faturalandi;
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }

        public virtual FinansSiparis Siparis { get; set; } = null!;
        public virtual ICollection<FinansFaturaKalemi> Kalemler { get; set; } = new List<FinansFaturaKalemi>();
        public virtual ICollection<FinansBelge> Belgeler { get; set; } = new List<FinansBelge>();
    }

    public class FinansFaturaKalemi : BaseEntity
    {
        public int FaturaId { get; set; }
        public int SiparisKalemiId { get; set; }
        public decimal Adet { get; set; }
        public decimal M3 { get; set; }

        public virtual FinansFatura Fatura { get; set; } = null!;
        public virtual FinansSiparisKalemi SiparisKalemi { get; set; } = null!;
    }

    public class FinansOzelIs : BaseEntity
    {
        public string KayitNo { get; set; } = string.Empty;
        public string IsTuru { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public int? ProjeId { get; set; }
        public string IsAdi { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public decimal Miktar { get; set; }
        public string Birim { get; set; } = string.Empty;
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public FinansHesaplamaYontemi HesaplamaYontemi { get; set; } = FinansHesaplamaYontemi.DegiskenAdet;
        public string RaporGrubu { get; set; } = "Özel İş";
        public DateTime IsTarihi { get; set; }
        public int? DuzenliIsId { get; set; }
        public string? DonemAnahtari { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }

        public virtual Proje? Proje { get; set; }
        public virtual FinansDuzenliIs? DuzenliIs { get; set; }
        public virtual FinansIsKaydi? FinansKaydi { get; set; }
        public virtual ICollection<FinansBelge> Belgeler { get; set; } = new List<FinansBelge>();
    }

    public class FinansDuzenliIs : BaseEntity
    {
        public int? ProjeId { get; set; }
        public string IsAdi { get; set; } = string.Empty;
        public string IsTuru { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public string? Aciklama { get; set; }
        public string TekrarSikligi { get; set; } = string.Empty;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public int OlusturmaGunu { get; set; }
        public decimal Miktar { get; set; } = 1;
        public string Birim { get; set; } = "Adet";
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; } = "EUR";
        public decimal KdvOrani { get; set; }
        public FinansHesaplamaYontemi HesaplamaYontemi { get; set; } = FinansHesaplamaYontemi.DegiskenAdet;
        public string RaporGrubu { get; set; } = "Özel İş";
        public bool Aktif { get; set; } = true;
        public DateTime? SonOlusturulanDonem { get; set; }

        public virtual Proje? Proje { get; set; }
        public virtual ICollection<FinansOzelIs> DonemKayitlari { get; set; } = new List<FinansOzelIs>();
    }

    public class FinansIsTuruTanimi : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public bool Aktif { get; set; } = true;
        public int Sira { get; set; }
    }

    public class FinansGiderKategorisi : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public bool Aktif { get; set; } = true;
        public virtual ICollection<FinansGider> Giderler { get; set; } = new List<FinansGider>();
    }

    public class FinansGider : BaseEntity
    {
        public DateTime Tarih { get; set; }
        public int KategoriId { get; set; }
        public string? AltKategori { get; set; }
        public string? FirmaVeyaKisi { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "TRY";
        public bool KdvDahil { get; set; }
        public decimal KdvOrani { get; set; }
        public decimal Matrah { get; set; }
        public decimal KdvTutari { get; set; }
        public decimal ToplamTutar { get; set; }
        public int? ProjeId { get; set; }
        public FinansIsTuru? IsTuru { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }

        public virtual FinansGiderKategorisi Kategori { get; set; } = null!;
        public virtual Proje? Proje { get; set; }
        public virtual ICollection<FinansBelge> Belgeler { get; set; } = new List<FinansBelge>();
    }

    public class FinansBelge : BaseEntity
    {
        public FinansBelgeTuru BelgeTuru { get; set; }
        public int? SiparisId { get; set; }
        public int? FaturaId { get; set; }
        public int? OzelIsId { get; set; }
        public int? GiderId { get; set; }
        public string DosyaAdi { get; set; } = string.Empty;
        public string SaklananDosyaAdi { get; set; } = string.Empty;
        public string DosyaUzantisi { get; set; } = string.Empty;
        public string DosyaYolu { get; set; } = string.Empty;
        public string IcerikTuru { get; set; } = string.Empty;
        public long Boyut { get; set; }
        public string YukleyenKullanici { get; set; } = string.Empty;

        public virtual FinansSiparis? Siparis { get; set; }
        public virtual FinansFatura? Fatura { get; set; }
        public virtual FinansOzelIs? OzelIs { get; set; }
        public virtual FinansGider? Gider { get; set; }
    }

    public class FinansIslemGecmisi : BaseEntity
    {
        public string ReferansTipi { get; set; } = string.Empty;
        public int ReferansId { get; set; }
        public string Islem { get; set; } = string.Empty;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }
        public DateTime IslemTarihi { get; set; }
    }
}