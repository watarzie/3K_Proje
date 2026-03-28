using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    public class Kullanici : BaseEntity
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string BasHarf { get; set; } = string.Empty;
        public KullaniciRol Rol { get; set; } = KullaniciRol.Personel3K;
        public string Email { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
        public virtual ICollection<Revizyon> Revizyonlar { get; set; } = new List<Revizyon>();
    }
}
