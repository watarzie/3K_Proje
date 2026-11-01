using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Projenin web bağlantılarından biri: fotoğraf sayfası veya gerçekleşen çeki listesi (<see cref="Tur"/>). Proje
    /// tipinden (Normal/Saha/Yedek) ve sevk durumundan bağımsızdır: sevk edilmiş projede de eklenir, değişir ve silinir.
    /// Adres ve açıklama kuralı <see cref="Models.ProjeFotografBaglantisiKurali"/>; proje silinince bağlantıları da silinir.
    /// </summary>
    public class ProjeFotografBaglantisi : BaseEntity
    {
        public int ProjeId { get; set; }

        /// <summary>Bağlantının türü; eklenirken belirlenir, düzenlemede değişmez.</summary>
        public ProjeBaglantiTuru Tur { get; set; } = ProjeBaglantiTuru.Fotograf;

        /// <summary>Normalleştirilmiş mutlak http/https adresi.</summary>
        public string Adres { get; set; } = string.Empty;

        /// <summary>İsteğe bağlı kısa açıklama (ör. "Paketleme"); boşsa ekranda sitenin adı gösterilir.</summary>
        public string? Aciklama { get; set; }

        public virtual Proje Proje { get; set; } = null!;
    }
}
