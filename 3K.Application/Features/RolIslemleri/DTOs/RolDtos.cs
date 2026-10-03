namespace _3K.Application.Features.RolIslemleri.DTOs
{
    /// <summary>
    /// Rol listesi DTO'su — GET /api/rol
    /// </summary>
    public class RolDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
    }

    /// <summary>
    /// Rol detay DTO'su — yetkilerle birlikte
    /// </summary>
    public class RolDetayDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public List<MenuTreeDto> MenuAgaci { get; set; } = new();
    }

    /// <summary>
    /// Menü ağacı DTO'su — recursive tree yapısı.
    /// Her node'da rolün yetkisi (W/R/N) bulunur.
    /// </summary>
    public class MenuTreeDto
    {
        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty;
        public string LabelKey { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? Route { get; set; }
        public int Sira { get; set; }

        /// <summary>YetkiTipi Id: 1=N, 2=R, 3=W</summary>
        public int YetkiTipiId { get; set; } = 1;
        /// <summary>W = Tam yetki, R = Sadece okuma, N = Yetkisiz</summary>
        public string YetkiTipiMetni { get; set; } = "N";

        public List<MenuTreeDto> Children { get; set; } = new();
    }

    /// <summary>
    /// Yetki kaydetme/güncelleme DTO'su
    /// </summary>
    public class RolYetkiItemDto
    {
        public int MenuTanimiId { get; set; }
        /// <summary>YetkiTipi Id: 1=N, 2=R, 3=W</summary>
        public int YetkiTipiId { get; set; } = 1;
    }
}
