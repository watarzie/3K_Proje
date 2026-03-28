using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class Proje : BaseEntity
    {
        public string ProjeNo { get; set; }
        public string Musteri { get; set; }
        public string Durum { get; set; }
        public DateTime PlanlananSevkTarihi { get; set; }
        public string SorumluKisi { get; set; }

        [cite_start]// Navigation Properties [cite: 307, 309, 317, 318]
        public virtual ICollection<Ceki> Cekiler { get; set; } = new List<Ceki>();
        public virtual ICollection<Sandik> Sandiklar { get; set; } = new List<Sandik>();
        public virtual ICollection<Revizyon> Revizyonlar { get; set; } = new List<Revizyon>();
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
    }
}
