using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Rol ↔ Menü yetki ilişkisi (bridge table).
    /// YetkiTipiId: 1=N (Yetkisiz), 2=R (Okuma), 3=W (Yazma)
    /// </summary>
    public class RolYetki : BaseEntity
    {
        public int RolId { get; set; }
        public int MenuTanimiId { get; set; }

        /// <summary>
        /// LookupYetkiTipi Id: 1=N (None), 2=R (Read), 3=W (Write)
        /// </summary>
        public int YetkiTipiId { get; set; } = (int)Enums.YetkiTipi.N;

        // Navigation Properties
        public virtual Rol Rol { get; set; } = null!;
        public virtual MenuTanimi MenuTanimi { get; set; } = null!;
        public virtual LookupYetkiTipi? YetkiTipiLookup { get; set; }
    }
}
