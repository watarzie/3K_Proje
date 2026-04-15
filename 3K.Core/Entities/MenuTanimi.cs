namespace _3K.Core.Entities
{
    /// <summary>
    /// Sistemdeki menü/sayfa tanımı — ağaç yapısında (self-referencing).
    /// Sidebar menüsü bu tablodan dinamik olarak oluşturulur.
    /// </summary>
    public class MenuTanimi : BaseEntity
    {
        /// <summary>Benzersiz menü kodu — route guard ve yetki kontrolünde kullanılır.</summary>
        public string Kod { get; set; } = string.Empty;

        /// <summary>i18n çeviri anahtarı — "MENU.DASHBOARD"</summary>
        public string LabelKey { get; set; } = string.Empty;

        /// <summary>Remix icon sınıfı — "ri-dashboard-line"</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>Angular route path — "/dashboard" (null = sadece parent grup)</summary>
        public string? Route { get; set; }

        /// <summary>Sıralama numarası</summary>
        public int Sira { get; set; }

        /// <summary>Üst menü ID (null = root düzey)</summary>
        public int? ParentId { get; set; }

        // Navigation Properties
        public virtual MenuTanimi? Parent { get; set; }
        public virtual ICollection<MenuTanimi> Children { get; set; } = new List<MenuTanimi>();
        public virtual ICollection<RolYetki> Yetkiler { get; set; } = new List<RolYetki>();
    }
}
