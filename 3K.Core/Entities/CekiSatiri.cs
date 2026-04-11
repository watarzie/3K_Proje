namespace _3K.Core.Entities
{
    public class CekiSatiri : BaseEntity
    {
        public int CekiId { get; set; }
        public int SiraNo { get; set; }
        public string? OlcuResmiPozNo { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public string Birim { get; set; } = string.Empty;
        public string CekideGecenSandikNo { get; set; } = string.Empty;
        public string? FiiliSandikNo { get; set; }
        public string? Remarks { get; set; }

        // ===== Genel Durum (Otomatik hesaplanır) =====
        public string Durum { get; set; } = "Bekliyor";

        // ===== Grid Modülü Alanları =====
        public string GridDurumu { get; set; } = "Bekliyor";
        public int? GridSevkMiktari { get; set; }
        public DateTime? GridSevkTarihi { get; set; }
        public string? GridNotu { get; set; }
        public int? GridPersonelId { get; set; }

        // ===== 3K Modülü Alanları =====
        public string UcKDurumu { get; set; } = "Bekliyor";
        public int GelenMiktar { get; set; } = 0;
        public DateTime? TeslimTarihi { get; set; }
        public string? UcKNotu { get; set; }

        // ===== Diğer =====
        public bool IsManuelEklenen { get; set; } = false;
        public string? EklemeNedeni { get; set; }
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }

        /// <summary>
        /// Hesaplanan alan: IstenenAdet - GelenMiktar
        /// </summary>
        public int EksikMiktar => IstenenAdet - GelenMiktar;

        // Navigation Properties
        public virtual Ceki Ceki { get; set; } = null!;
        public virtual Kullanici? Paketleyen { get; set; }
        public virtual Kullanici? KontrolEden { get; set; }
        public virtual Kullanici? GridPersonel { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
        public virtual ICollection<FBTransfer> FBTransferleri { get; set; } = new List<FBTransfer>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
