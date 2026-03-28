using ;
using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class Sandik : BaseEntity
    {
        public int ProjeId { get; set; }
        public string SandikNo { get; set; }
        public string Durum { get; set; }
        public string DepoLokasyonu { get; set; }

        [cite_start]// Navigation Properties [cite: 309, 310]
        public virtual Proje Proje { get; set; }
        public virtual ICollection<SandikIcerik> SandikIcerikleri { get; set; } = new List<SandikIcerik>();
    }
}
