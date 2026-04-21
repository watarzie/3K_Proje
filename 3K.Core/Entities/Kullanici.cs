namespace _3K.Core.Entities
{
    public class Kullanici : BaseEntity
    {
        public string AdSoyad { get; set; } = string.Empty;
        public string BasHarf { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcının atandığı rol — Rol tablosuna FK.
        /// </summary>
        public int RolId { get; set; }

        // Navigation Properties
        public virtual Rol Rol { get; set; } = null!;
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
    }
}
