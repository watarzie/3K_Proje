namespace _3K.Core.Entities
{
    /// <summary>
    /// Projenin fotoğraflarının bulunduğu web sayfalarından biri. Proje tipinden (Normal/Saha/Yedek) ve sevk durumundan
    /// bağımsızdır: sevk edilmiş projede de eklenir, değişir ve silinir. Adres ve açıklama kuralı
    /// <see cref="Models.ProjeFotografBaglantisiKurali"/>; proje silinince bağlantıları da silinir.
    /// </summary>
    public class ProjeFotografBaglantisi : BaseEntity
    {
        public int ProjeId { get; set; }

        /// <summary>Normalleştirilmiş mutlak http/https adresi.</summary>
        public string Adres { get; set; } = string.Empty;

        /// <summary>İsteğe bağlı kısa açıklama (ör. "Paketleme"); boşsa ekranda sitenin adı gösterilir.</summary>
        public string? Aciklama { get; set; }

        public virtual Proje Proje { get; set; } = null!;
    }
}
