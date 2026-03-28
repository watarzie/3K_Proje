using ;
using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class CekiSatiri : BaseEntity
    {
        public int CekiId { get; set; }
        public int SiraNo { get; set; }
        public string BarkodNo { get; set; }
        public string Aciklama { get; set; }
        public int IstenenAdet { get; set; }
        public string Birim { get; set; }
        public string CekideGecenSandikNo { get; set; }
        public string FiiliSandikNo { get; set; }
        public string Remarks { get; set; }

        [cite_start]// Navigation Properties [cite: 308, 312, 314, 315]
        public virtual Ceki Ceki { get; set; }
        public virtual SandikIcerik SandikIcerik { get; set; } // 0..1 ilişki
        public virtual ICollection<FBTransfer> FBTransferleri { get; set; } = new List<FBTransfer>();
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
