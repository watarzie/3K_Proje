using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class Revizyon : BaseEntity
    {
        public int ProjeId { get; set; }
        public string Tip { get; set; }
        public string EskiDeger { get; set; }
        public string YeniDeger { get; set; }
        public string Aciklama { get; set; }
        public DateTime Tarih { get; set; }

        [cite_start]// Navigation Properties [cite: 317]
        public virtual Proje Proje { get; set; }
    }
}
