using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class HareketGecmisi : BaseEntity
    {
        public int ProjeId { get; set; }
        public string ReferansTipi { get; set; }
        public string ReferansId { get; set; }
        public string Islem { get; set; }
        public int KullaniciId { get; set; }
        public DateTime Tarih { get; set; }

        [cite_start]// Navigation Properties [cite: 318, 321]
        public virtual Proje Proje { get; set; }
        public virtual Kullanici Kullanici { get; set; }
    }
}
