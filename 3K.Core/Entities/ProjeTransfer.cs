using _3K.Core.Helpers;
using _3K.Core.Enums;

namespace _3K.Core.Entities
{
    /// <summary>
    /// Projeler arasi urun transfer hareket defteri.
    /// </summary>
    public class ProjeTransfer : BaseEntity
    {
        public int KaynakProjeId { get; set; }
        public int HedefProjeId { get; set; }
        public int KaynakCekiSatiriId { get; set; }
        public int? HedefCekiSatiriId { get; set; }
        public string BarkodNo { get; set; } = string.Empty;
        public string UrunAdi { get; set; } = string.Empty;
        public decimal Miktar { get; set; }
        public int TransferTipiId { get; set; } = (int)ProjeTransferTipi.Karsilama;
        public int DurumId { get; set; } = (int)ProjeTransferDurum.Aktif;
        public int? ParentTransferId { get; set; }
        public int? RootTransferId { get; set; }
        public int ZincirSeviyesi { get; set; } = 0;
        public int KullaniciId { get; set; }
        public string? Aciklama { get; set; }
        public DateTime Tarih { get; set; } = TurkeyTime.Now;
        public DateTime? IptalTarihi { get; set; }
        public string? IptalAciklama { get; set; }

        public virtual Proje KaynakProje { get; set; } = null!;
        public virtual Proje HedefProje { get; set; } = null!;
        public virtual CekiSatiri KaynakCekiSatiri { get; set; } = null!;
        public virtual CekiSatiri? HedefCekiSatiri { get; set; }
        public virtual ProjeTransfer? ParentTransfer { get; set; }
        public virtual ProjeTransfer? RootTransfer { get; set; }
        public virtual ICollection<ProjeTransfer> ChildTransfers { get; set; } = new List<ProjeTransfer>();
        public virtual ICollection<ProjeTransfer> RootChildTransfers { get; set; } = new List<ProjeTransfer>();
        public virtual Kullanici Kullanici { get; set; } = null!;
    }
}
