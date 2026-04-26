using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Madde 9: Karşılıklı Not Sistemi.
    /// 3K ve GRİD notları karşılıklı görünür.
    /// BagliReferansId ile CekiSatiri, Sandik, Proje vb. herhangi bir entity'ye bağlanabilir.
    /// </summary>
    public class Not : BaseEntity
    {
        /// <summary>
        /// Notu yazan taraf: Grid veya 3K.
        /// </summary>
        public int YazanTarafId { get; set; }

        /// <summary>
        /// Not içeriği.
        /// </summary>
        public string Icerik { get; set; } = string.Empty;

        /// <summary>
        /// Not yazılma tarihi.
        /// </summary>
        public DateTime Tarih { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Notu yazan kullanıcı.
        /// </summary>
        public int KullaniciId { get; set; }

        /// <summary>
        /// Bağlı referans tipi: "CekiSatiri", "Sandik", "Proje" vb.
        /// </summary>
        public string BagliReferansTipi { get; set; } = string.Empty;

        /// <summary>
        /// Bağlı entity'nin Id'si.
        /// </summary>
        public int BagliReferansId { get; set; }

        /// <summary>
        /// İlişkili CekiSatiri Id (opsiyonel — doğrudan FK ilişkisi için).
        /// </summary>
        public int? CekiSatiriId { get; set; }

        // Navigation Properties
        public virtual Kullanici Kullanici { get; set; } = null!;
        public virtual CekiSatiri? CekiSatiri { get; set; }
    }
}
