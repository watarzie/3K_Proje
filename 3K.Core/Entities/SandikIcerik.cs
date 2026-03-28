using ;
using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class SandikIcerik : BaseEntity
    {
        public int SandikId { get; set; }
        public int CekiSatiriId { get; set; }
        public int KonulanAdet { get; set; }
        public int EksikAdet { get; set; }

        [cite_start]// Navigation Properties [cite: 310, 312]
        public virtual Sandik Sandik { get; set; }
        public virtual CekiSatiri CekiSatiri { get; set; }
    }
}
