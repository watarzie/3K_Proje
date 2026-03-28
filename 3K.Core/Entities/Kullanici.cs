using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class Kullanici : BaseEntity
    {
        public string AdSoyad { get; set; }
        public string BasHarf { get; set; }
        public string Rol { get; set; }

        [cite_start]// Navigation Properties [cite: 321, 323]
        public virtual ICollection<HareketGecmisi> HareketGecmisleri { get; set; } = new List<HareketGecmisi>();
        public virtual ICollection<Revizyon> Revizyonlar { get; set; } = new List<Revizyon>();
    }
}
