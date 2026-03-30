using _3K.Core.Enums;

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

        public GridDurum GridDurumu { get; set; } = GridDurum.Bekliyor;
        public UcKDurum UcKDurumu { get; set; } = UcKDurum.Bekliyor;
        public bool IsManuelEklenen { get; set; } = false;
        public string? EklemeNedeni { get; set; }

        // İş akışı 3: Ürün durumu (State Diagram)
        public UrunDurum Durum { get; set; } = UrunDurum.Bekliyor;

        // İş akışı 7: Paketleyen / Kontrol Eden
        public int? PaketleyenId { get; set; }
        public int? KontrolEdenId { get; set; }

        // Navigation Properties
        public virtual Ceki Ceki { get; set; } = null!;
        public virtual Kullanici? Paketleyen { get; set; }
        public virtual Kullanici? KontrolEden { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
        public virtual ICollection<FBTransfer> FBTransferleri { get; set; } = new List<FBTransfer>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
