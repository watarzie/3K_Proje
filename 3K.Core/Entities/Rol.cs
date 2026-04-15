namespace _3K.Core.Entities
{
    /// <summary>
    /// Sistem rolü tanımı.
    /// Her kullanıcı bir Role atanır, her Rolün menü bazlı yetkileri vardır.
    /// </summary>
    public class Rol : BaseEntity
    {
        public string Ad { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<RolYetki> Yetkiler { get; set; } = new List<RolYetki>();
        public virtual ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
    }
}
