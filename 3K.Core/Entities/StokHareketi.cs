using System;
using System.Collections.Generic;
using System.Text;

namespace 3K.Core.Entities
{
    public class StokHareketi : BaseEntity
    {
        public int StokKaydiId { get; set; }
        public int CekiSatiriId { get; set; }
        public int ProjeId { get; set; }
        public int Miktar { get; set; }
        public string IslemTipi { get; set; }
        public DateTime Tarih { get; set; }

        [cite_start]// Navigation Properties [cite: 315, 316]
        public virtual StokKaydi StokKaydi { get; set; }
        public virtual CekiSatiri CekiSatiri { get; set; }
        public virtual Proje Proje { get; set; }
    }
}
