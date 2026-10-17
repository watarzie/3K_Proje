using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Projeden otomatik alınan ve bağımsız girilen bütün sandıkları tek bir
    /// üretim/audit yaşam döngüsünde tutar. Kayıtlar fiziksel olarak silinmez.
    /// </summary>
    public class AmbalajUretimKaydi : BaseEntity
    {
        /// <summary>Kaynak sandığı kaybolduğu için sistemin iptal ettiği kaydın iptal nedeni.</summary>
        public const string SistemKaynakEksigiNedeni = "system-source-missing";
        private const string KullaniciIptalNedeni = "Kullanıcı tarafından silindi.";

        /// <summary>
        /// Finans gibi aşağı akış modüllerinin idempotent biçimde referans alabileceği,
        /// kaydın bütün yaşamı boyunca değişmeyen iş anahtarıdır.
        /// </summary>
        public Guid IsAkisKimligi { get; set; } = Guid.NewGuid();
        public uint Version { get; set; }

        public int? ProjeId { get; set; }
        public string? ManuelProjeNo { get; set; }
        public string? ManuelProjeAdi { get; set; }

        public int? UstKayitId { get; set; }
        public int? IcSandikSablonId { get; set; }
        /// <summary>
        /// Referans üretim ekranındaki proje planı kalemleri ile bağımsız
        /// ilave/iç/saha/yedek kuyruğunu aynı tabloda güvenle ayırır.
        /// </summary>
        public bool BagimsizKayitMi { get; set; }
        public AmbalajSandikTuru Tur { get; set; } = AmbalajSandikTuru.Normal;
        public AmbalajKaynakModulu KaynakModul { get; set; } = AmbalajKaynakModulu.Manuel;
        public int? KaynakKayitId { get; set; }
        public bool KaynakSenkronizasyonuKilitliMi { get; set; }
        public DateTime? KaynakSonSenkronizasyonTarihi { get; set; }

        public string SandikNo { get; set; } = string.Empty;
        public string? Ad { get; set; }
        public AmbalajSandikCinsi SandikCinsi { get; set; } = AmbalajSandikCinsi.AhsapKapali;
        public string? DigerSandikCinsi { get; set; }
        public int Adet { get; set; } = 1;

        /// <summary>
        /// Milimetre cinsinden ölçüler. Çeki listesinden gelen kaynak kayıtlarda dış
        /// ölçü, bağımsız ve manuel üretim kalemlerinde doğrudan üretim ölçüsüdür.
        /// Hesaplayıcı bu ayrımı tek merkezde iç ölçüye dönüştürür.
        /// </summary>
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }

        public bool AmbalajaDahil { get; set; } = true;
        public bool UretimeAlindi { get; set; }

        /// <summary>Hesap anındaki değerler saklanır; sonradan formül değişse bile geçmiş korunur.</summary>
        public decimal HesaplananBirimM3 { get; set; }
        public decimal HesaplananToplamM3 { get; set; }
        public decimal? M3Override { get; set; }
        public string? M3OverrideNedeni { get; set; }
        public string M3HesaplamaVersiyonu { get; set; } = AmbalajHesaplayici.FormulVersiyonu;
        public decimal SarfOrani { get; set; } = AmbalajHesaplayici.VarsayilanSarfOrani;
        public decimal SarfM3 { get; set; }
        public decimal ToplamM3 { get; set; }

        public string? KullanimAmaci { get; set; }
        public string? TalepEdenKisi { get; set; }
        public string? TalepEdenBolum { get; set; }
        public string? TalimatVeren { get; set; }
        public string? FirinPartiNo { get; set; }
        public string? Aciklama { get; set; }

        public AmbalajUretimDurumu UretimDurumu { get; set; } = AmbalajUretimDurumu.Planlandi;
        public DateTime? UretimTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        /// <summary>
        /// Üretim formu oluşturulurken isteğe bağlı girilen tarih (A6); finans kaleminin varsayılan Finans
        /// Tarihi'dir. Boşsa finans tarihi üretim tarihinden gelir.
        /// </summary>
        public DateTime? FinansTarihi { get; set; }

        public bool IptalMi { get; set; }
        public AmbalajUretimDurumu? IptalOncesiUretimDurumu { get; set; }
        public DateTime? IptalTarihi { get; set; }
        public int? IptalEdenKullaniciId { get; set; }
        public string? IptalNedeni { get; set; }

        public virtual Proje? Proje { get; set; }
        public virtual AmbalajUretimKaydi? UstKayit { get; set; }
        public virtual AmbalajIcSandikSablonu? IcSandikSablonu { get; set; }
        public virtual ICollection<AmbalajUretimKaydi> IcKayitlar { get; set; } = new List<AmbalajUretimKaydi>();
        public virtual ICollection<AmbalajUretimHareketi> Hareketler { get; set; } = new List<AmbalajUretimHareketi>();

        /// <summary>
        /// Ambalaj kararını uygular. Hariç tutulan kayıt üretim seçiminden de çıkar;
        /// veritabanı aynı kuralı CK_AmbalajUretimKayitlari_UretimSecimi ile korur.
        /// </summary>
        public void AmbalajKarariniUygula(bool dahil)
        {
            AmbalajaDahil = dahil;
            if (!dahil) UretimeAlindi = false;
        }

        /// <summary>Kullanıcının sildiği kayıt fiziksel olarak silinmez, iptal edilir.</summary>
        public void KullaniciAdinaIptalEt(int kullaniciId, string? neden)
        {
            IptalMi = true;
            IptalTarihi = TurkeyTime.Now;
            IptalEdenKullaniciId = kullaniciId;
            IptalNedeni = neden ?? KullaniciIptalNedeni;
        }

        /// <summary>
        /// Kaynağı kaybolduğunda yalnız planlı ve kilitsiz kaynak kaydı sistem tarafından
        /// iptal edilir; üretime girmiş kayıtlar korunur.
        /// </summary>
        public bool KaynakEksikligindeIptalEdilebilirMi() =>
            KaynakKayitId.HasValue &&
            !IptalMi &&
            !KaynakSenkronizasyonuKilitliMi &&
            UretimDurumu == AmbalajUretimDurumu.Planlandi;

        /// <summary>
        /// Üretim formu oluşturulurken (yeniden oluşturma dahil) kayıt güncel kaynak sandıktan tazelenir mi?
        /// Otomatik senkronizasyon ilk formdan sonra kaydı kilitler ve ezmez; ölçüsü yalnız sandıktan gelen kayıt
        /// başka yoldan düzeltilemediği için kullanıcının açık form işlemi sandıktaki son değeri alır. Tamamlanan
        /// üretim fiziksel gerçekleşmedir, iptal ve bağımsız kayıt kaynağa bağlı değildir; bunlar tazelenmez.
        /// </summary>
        public bool FormdaKaynaktanTazelenirMi() =>
            KaynakKayitId.HasValue &&
            !BagimsizKayitMi &&
            !IptalMi &&
            UretimDurumu != AmbalajUretimDurumu.Tamamlandi;

        /// <summary>
        /// Kaynak sandığı kaybolan kaydı sistem adına iptal eder. İptal öncesi durum,
        /// kaynak yeniden görüldüğünde geri yüklenmek üzere saklanır.
        /// </summary>
        public void KaynakEksikligindenIptalEt()
        {
            IptalOncesiUretimDurumu = UretimDurumu;
            IptalMi = true;
            IptalTarihi = TurkeyTime.Now;
            IptalEdenKullaniciId = null;
            IptalNedeni = SistemKaynakEksigiNedeni;
        }

        /// <summary>Kullanıcı iptali geri alınmaz; yalnız sistemin kaynak eksikliği iptali geri döner.</summary>
        public bool SistemIptaliGeriAlinabilirMi() =>
            KaynakKayitId.HasValue &&
            IptalMi &&
            IptalEdenKullaniciId == null &&
            string.Equals(IptalNedeni, SistemKaynakEksigiNedeni, StringComparison.Ordinal);

        /// <summary>Sistem iptalini geri alır ve iptal öncesi üretim durumunu yükler.</summary>
        public void SistemIptaliniGeriAl()
        {
            IptalMi = false;
            IptalTarihi = null;
            IptalEdenKullaniciId = null;
            IptalNedeni = null;
            UretimDurumu = IptalOncesiUretimDurumu ?? AmbalajUretimDurumu.Planlandi;
            IptalOncesiUretimDurumu = null;
        }
    }

    /// <summary>
    /// Ambalaj kaydındaki her kritik alan değişikliğini eski/yeni değerleriyle tutar.
    /// Aynı komuttaki alanlar IslemGrubu üzerinden beraber izlenebilir.
    /// </summary>
    public class AmbalajUretimHareketi : BaseEntity
    {
        public int AmbalajUretimKaydiId { get; set; }
        public Guid IslemGrubu { get; set; } = Guid.NewGuid();
        public int KullaniciId { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;
        public string Islem { get; set; } = string.Empty;
        public string AlanAdi { get; set; } = string.Empty;
        public string? EskiDeger { get; set; }
        public string? YeniDeger { get; set; }
        public string? Aciklama { get; set; }

        public virtual AmbalajUretimKaydi AmbalajUretimKaydi { get; set; } = null!;
    }

    public class AmbalajIcSandikSablonu : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;
        public AmbalajSandikCinsi SandikCinsi { get; set; } = AmbalajSandikCinsi.AhsapKapali;
        public string? DigerSandikCinsi { get; set; }
        public decimal Boy { get; set; }
        public decimal En { get; set; }
        public decimal Yukseklik { get; set; }
    }
}
