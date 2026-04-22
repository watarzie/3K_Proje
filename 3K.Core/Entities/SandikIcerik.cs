namespace _3K.Core.Entities
{
    public class SandikIcerik : BaseEntity
    {
        public int SandikId { get; set; }

        /// <summary>
        /// Normal projeler için CekiSatiri referansı. Saha/Yedek projelerinde null olabilir.
        /// </summary>
        public int? CekiSatiriId { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }

        // ===== Manuel Malzeme Alanları (Saha/Yedek projeleri için) =====
        /// <summary>
        /// Barkod numarası (opsiyonel).
        /// </summary>
        public string? BarkodNo { get; set; }
        /// <summary>
        /// Malzeme ismi. CekiSatiriId doluysa CekiSatiri.Aciklama kullanılır.
        /// CekiSatiriId null ise bu alan zorunludur.
        /// </summary>
        public string? Isim { get; set; }
        /// <summary>
        /// Miktar (decimal). CekiSatiriId null olan kayıtlarda zorunludur.
        /// </summary>
        public decimal Miktar { get; set; }
        /// <summary>
        /// Birim (ör: Adet, Kg, Metre). Opsiyonel.
        /// </summary>
        public string? Birim { get; set; }

        // Navigation Properties
        public virtual Sandik Sandik { get; set; } = null!;
        public virtual CekiSatiri? CekiSatiri { get; set; }
    }
}

