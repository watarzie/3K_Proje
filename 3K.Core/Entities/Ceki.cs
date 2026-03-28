using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class Ceki : BaseEntity
    {
        public int ProjeId { get; set; }
        public string OrijinalDosyaYolu { get; set; }
        public DateTime YuklemeTarihi { get; set; }

        [cite_start]// Navigation Properties [cite: 307, 308]
        public virtual Proje Proje { get; set; }
        public virtual ICollection<CekiSatiri> CekiSatirlari { get; set; } = new List<CekiSatiri>();
    }
}
