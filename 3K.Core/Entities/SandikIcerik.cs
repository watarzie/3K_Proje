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

        // ===== Madde 2: Parçalı Karşılama Detay Kolonları =====
        /// <summary>
        /// Stoktan karşılanan adet.
        /// </summary>
        public int StokKarsilanan { get; set; } = 0;

        /// <summary>
        /// Başka projeden (FB) karşılanan adet.
        /// </summary>
        public int ProjeKarsilanan { get; set; } = 0;

        /// <summary>
        /// Tedarikçiden karşılanan adet.
        /// </summary>
        public int TedarikciKarsilanan { get; set; } = 0;

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
        /// Madde 7: Birim artık Enum tabanlı. Opsiyonel (çeki satırına bağlı ise CekiSatiri.BirimId kullanılır).
        /// </summary>
        public int? BirimId { get; set; }

        /// <summary>
        /// Saha/Yedek sandıklara eklenen ürünlerin kaynak proje numarası.
        /// Projeden seçilerek eklenen ürünlerin hangi projeden geldiğini gösterir.
        /// </summary>
        public string? KaynakProjeNo { get; set; }

        // Navigation Properties
        public virtual Sandik Sandik { get; set; } = null!;
        public virtual CekiSatiri? CekiSatiri { get; set; }
        public virtual LookupBirim? BirimLookup { get; set; }
    }
}


