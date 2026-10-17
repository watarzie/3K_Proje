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
        /// <summary>Eski katlanır sandık boyut eşleşmesi; artık eşleşmede kullanılmaz (kayıtlı değer korunur).</summary>
        public decimal? Boy { get; set; }
        /// <inheritdoc cref="Boy"/>
        public decimal? En { get; set; }
        /// <inheritdoc cref="Boy"/>
        public decimal? Yukseklik { get; set; }
        public int? IcSandikSablonId { get; set; }
        /// <summary>
        /// Malzemeli tarife: kontrplak/katlanır sandıkta ahşap m³ ahşap tarifesiyle, kontra m³ kontra tarifesiyle fiyatlanır.
        /// Boşsa eşleşme malzemeden bağımsızdır (eski tek m³ tarifesi).
        /// </summary>
        public Models.FinansSandikMalzemesi? Malzeme { get; set; }
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
        public DateTime FinansTarihi { get; set; }
        public bool FinansTarihiManuel { get; set; }
        public bool FinansMiktariManuel { get; set; }
        public decimal? ManuelNetTutar { get; set; }
        public int? TarifeIdSnapshot { get; set; }
        public AmbalajSandikCinsi? SandikCinsi { get; set; }
        /// <summary>
        /// Elle girilen ahşap ve kontra m³ (kontra/katlanır sandıkta Ambalaj m³ hesaplamaz). Boşsa pay eski kayıtlardaki
        /// gibi türetilir; doluysa <see cref="ToplamM3"/> bileşik toplamdır (<see cref="Models.FinansHacimKurallari"/>).
        /// </summary>
        public decimal? AhsapM3 { get; set; }
        public decimal? KontraM3 { get; set; }
        /// <summary>Hacim elle girildi; üretim aktarımı m³ alanlarını ezmez, kayıt otomatik tarife fiyatlamasında kalır.</summary>
        public bool HacimManuel { get; set; }
        /// <summary>
        /// Malzemeli fiyat (kontrplak/katlanır): doluysa <see cref="FinansUrunId"/>/<see cref="BirimFiyatSnapshot"/> kontra
        /// tarifesidir ve net = kontra m³ × kontra fiyatı + ahşap m³ × <see cref="AhsapBirimFiyatSnapshot"/>
        /// (<see cref="Models.FinansTutarKurallari.IsNet"/>). Boşsa iş tek tarifeyle fiyatlanır.
        /// </summary>
        public decimal? AhsapBirimFiyatSnapshot { get; set; }
        public int? AhsapFinansUrunId { get; set; }
        public int? AhsapTarifeIdSnapshot { get; set; }
        public virtual FinansUrun? AhsapFinansUrun { get; set; }
        public string? FiyatBilesenleriJson { get; set; }
        public DateTime KayitTarihi { get; set; }
        public FinansIsDurumu Durum { get; set; } = FinansIsDurumu.SiparisBekliyor;
        public string KaynakTuru { get; set; } = "Manuel";
        public string? KaynakKayitId { get; set; }
        public string KaynakBileseni { get; set; } = "NET";
        public bool KaynakAktif { get; set; } = true;
        public int? DuzenliIsId { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        /// <summary>İşin PO öncesi onay için en son sunulduğu talep; kararı Onay Merkezi'ndedir.</summary>
        public int? PoOnayTalebiId { get; set; }
        /// <summary>
        /// Onaylanan net tutar ve para birimi. Güncel net tutar (<see cref="Models.FinansTutarKurallari.IsNet"/>) veya
        /// para birimi bunlardan farklıysa onay düşmüştür (<see cref="Models.FinansPoOnayKurali"/>).
        /// </summary>
        public decimal? PoOnayNetTutar { get; set; }
        public string? PoOnayParaBirimi { get; set; }
        public DateTime? PoOnayTarihi { get; set; }
        /// <summary>Onayı veren kullanıcı; geçiş betiğiyle onaylı sayılan kayıtta boştur.</summary>
        public int? PoOnaylayanKullaniciId { get; set; }
        public virtual Proje? Proje { get; set; }
        public virtual FinansUrun? FinansUrun { get; set; }
        public virtual FinansDuzenliIs? DuzenliIs { get; set; }
        public virtual FinansPoOnayTalebi? PoOnayTalebi { get; set; }
        public virtual ICollection<FinansSiparisKalemi> SiparisKalemleri { get; set; } = new List<FinansSiparisKalemi>();
    }

    /// <summary>
    /// PO öncesi onaya sunulan grubun (proje × iş türü × para birimi) değişmez görüntüsü. Karar Onay Merkezi'nde
    /// <see cref="OnayBekleyenIslem"/> üzerinde verilir (ReferansTipi FinansPoOnayTalebi); onaylanınca görüntü
    /// güncel kayıtlarla aynıysa işlere uygulanır.
    /// </summary>
    public class FinansPoOnayTalebi : BaseEntity
    {
        public int? ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public string Musteri { get; set; } = string.Empty;
        public FinansIsTuru IsTuru { get; set; }
        public string ParaBirimi { get; set; } = string.Empty;
        public int KalemSayisi { get; set; }
        public decimal NetTutar { get; set; }
        /// <summary>Kalemlerin onaya sunulduğu andaki değerleri (<see cref="Models.FinansPoOnayOnizleme"/>).</summary>
        public string OnizlemeJson { get; set; } = string.Empty;
        public string OnizlemeHash { get; set; } = string.Empty;
        public int TalepEdenKullaniciId { get; set; }
        /// <summary>Onay kalemlere uygulandığında dolar; talep bir kez uygulanır.</summary>
        public DateTime? UygulamaTarihi { get; set; }
    }

    public class FinansSiparis : BaseEntity
    {
        public string KayitNo { get; set; } = string.Empty;
        public string PoNumarasi { get; set; } = string.Empty;
        public string? ParaBirimi { get; set; }
        public DateTime SiparisTarihi { get; set; }
        public string? Aciklama { get; set; }
        public FinansSiparisDurumu Durum { get; set; } = FinansSiparisDurumu.Acik;
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual ICollection<FinansSiparisKalemi> Kalemler { get; set; } = new List<FinansSiparisKalemi>();
        public virtual ICollection<FinansFatura> Faturalar { get; set; } = new List<FinansFatura>();
        /// <summary>PO'nun net sonucuna katılan, kullanıcının PO ile ilişkilendirdiği giderler.</summary>
        public virtual ICollection<FinansGider> Giderler { get; set; } = new List<FinansGider>();
    }

    public class FinansSiparisKalemi : BaseEntity
    {
        public int FinansSiparisId { get; set; }
        public int FinansIsKaydiId { get; set; }
        public decimal Adet { get; set; }
        public decimal M3 { get; set; }
        public bool TutarBazli { get; set; }
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
        /// <summary>Vade tarihi (D3); varsayılanı fatura tarihi + parametredeki vade günüdür, faturada değiştirilebilir.</summary>
        public DateTime VadeTarihi { get; set; }
        /// <summary>Tahsilat dışarıdan gelir; kullanıcı ödendi olarak işaretler (tutar faturanın tamamıdır).</summary>
        public bool OdendiMi { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        public string? OdemeAciklamasi { get; set; }
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
        public bool TutarBazli { get; set; }
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
        public DateTime FinansTarihi { get; set; }
        public DateTime FinansDonemi { get; set; }
        public string? BelgeNo { get; set; }
        public bool AvansMi { get; set; }
        public int? MahsupEdilenAvansId { get; set; }
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
        /// <summary>İsteğe bağlı PO ilişkisi; PO'nun gelir-gider net sonucunda kullanılır.</summary>
        public int? FinansSiparisId { get; set; }
        public bool IptalEdildi { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklamasi { get; set; }
        public virtual FinansGiderKategori Kategori { get; set; } = null!;
        public virtual FinansGiderKalemi? GiderKalemi { get; set; }
        public virtual Proje? Proje { get; set; }
        public virtual FinansSiparis? FinansSiparis { get; set; }
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
        public Guid IslemGrubu { get; set; }
        public string? Referans { get; set; }
    }

    /// <summary>Kalıcı silinen kaynaklı iş kaydının kaynak anahtarı; aynı kaynak sonraki aktarımda yeniden oluşturulmaz.</summary>
    public sealed class FinansKaynakBastirma : BaseEntity
    {
        public string KaynakTuru { get; set; } = string.Empty;
        public string KaynakKayitId { get; set; } = string.Empty;
        public string KaynakBileseni { get; set; } = "NET";
        public string Aciklama { get; set; } = string.Empty;
    }

    /// <summary>PO, fatura veya gidere bağlı PDF belgesinin bir sürümü.</summary>
    public sealed class FinansBelge : BaseEntity
    {
        public string HedefTuru { get; set; } = string.Empty;
        public int HedefId { get; set; }
        public int Surum { get; set; }
        public string OrijinalAd { get; set; } = string.Empty;
        public string GuvenliAd { get; set; } = string.Empty;
        public long Boyut { get; set; }
        public string IcerikTuru { get; set; } = "application/pdf";
        public string Hash { get; set; } = string.Empty;
        public string Yukleyen { get; set; } = string.Empty;
        // PDF ve metadata tek DB transaction'ında saklanır; dosya/DB yarım commit'i oluşmaz.
        public byte[] Icerik { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Kullanıcının yönettiği para birimi tablosu (F1). Finans kayıtları kodu snapshot olarak saklar; yeni
    /// seçimler yalnız aktif para birimlerinden yapılır, pasifleştirme geçmiş kayıtları değiştirmez.
    /// </summary>
    public class FinansParaBirimi : BaseEntity
    {
        public string Kod { get; set; } = string.Empty;
        public string Ad { get; set; } = string.Empty;
        public string? Sembol { get; set; }
        public bool Aktif { get; set; } = true;
        public int Sira { get; set; }
    }

    /// <summary>Finans parametresi (kod/değer). Bilinen kodlar ve varsayılanları <c>FinansParametreKodlari</c>'ndadır.</summary>
    public class FinansParametre : BaseEntity
    {
        public string Kod { get; set; } = string.Empty;
        public string Deger { get; set; } = string.Empty;
    }
}
