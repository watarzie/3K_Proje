using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class FBTransfer : BaseEntity
    {
        public int CekiSatiriId { get; set; }
        public string AsilFB { get; set; }
        public string AlinanFB { get; set; }
        public int Miktar { get; set; }
        public string Neden { get; set; }
        public string IadeDurumu { get; set; }

        [cite_start]// Navigation Properties [cite: 314]
        public virtual CekiSatiri CekiSatiri { get; set; }
    }
}
