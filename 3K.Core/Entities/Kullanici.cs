namespace _3K.Core.Entities
{
    public class Kullanici : BaseEntity
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string BasHarf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;

        /// <summary>
        /// Bu kullanıcının girişte TOTP tabanlı iki faktörlü doğrulamayı
        /// tamamlamasının zorunlu olup olmadığını belirtir.
        /// </summary>
        public bool IkiFaktorZorunluMu { get; set; }

        /// <summary>
        /// Son başarılı girişten bu yana art arda yapılan hatalı parola denemesi sayısı
        /// (<see cref="Models.GirisKilidiKurali"/>). Başarılı girişte ve yönetici parolayı değiştirince sıfırlanır.
        /// </summary>
        public int BasarisizGirisSayisi { get; set; }

        /// <summary>
        /// Geçici giriş kilidinin bittiği an (<c>TurkeyTime</c>); bu ana kadar doğru parola da reddedilir.
        /// Boş ise kilit yoktur.
        /// </summary>
        public DateTime? GirisKilitBitisTarihi { get; set; }

        /// <summary>
        /// Kullanıcının atandığı rol — Rol tablosuna FK.
        /// </summary>
        public int RolId { get; set; }

        // Navigation Properties
        public virtual Rol Rol { get; set; } = null!;
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
        public virtual ICollection<Bildirim> OlusturduguBildirimler { get; set; } = new List<Bildirim>();
        public virtual ICollection<KullaniciBildirimi> Bildirimleri { get; set; } = new List<KullaniciBildirimi>();
        public virtual ICollection<BildirimAboneligi> BildirimAbonelikleri { get; set; } = new List<BildirimAboneligi>();
    }
}
