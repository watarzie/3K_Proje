namespace _3K.Core.Entities
{
    /// <summary>
    /// Rol ↔ Menü yetki ilişkisi (bridge table).
    /// YetkiTipi: "W" (Yazma/Tam yetki), "R" (Sadece okuma), "N" (Yetkisiz)
    /// </summary>
    public class RolYetki : BaseEntity
    {
        public int RolId { get; set; }
        public int MenuTanimiId { get; set; }

        /// <summary>
        /// "W" = Write (tam yetki), "R" = Read (sadece okuma), "N" = None (yetkisiz)
        /// </summary>
        public string YetkiTipi { get; set; } = "N";

        // Navigation Properties
        public virtual Rol Rol { get; set; } = null!;
        public virtual MenuTanimi MenuTanimi { get; set; } = null!;
    }
}
