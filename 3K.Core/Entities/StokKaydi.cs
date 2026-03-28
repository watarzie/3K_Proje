using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class StokKaydi : BaseEntity
    {
        public string MalzemeKodu { get; set; }
        public string MalzemeAdi { get; set; }
        public int Miktar { get; set; }
        public string Birim { get; set; }
        public string Lokasyon { get; set; }
        public string KaynakProje { get; set; }
        public string Durum { get; set; }

        [cite_start]// Navigation Properties [cite: 316]
        public virtual ICollection<StokHareketi> StokHareketleri { get; set; } = new List<StokHareketi>();
    }
}
