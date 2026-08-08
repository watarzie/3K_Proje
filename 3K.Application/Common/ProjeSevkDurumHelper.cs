using System.Collections.Generic;
using System.Linq;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Common
{
    public static class ProjeSevkDurumHelper
    {
        public static int Hesapla(IReadOnlyCollection<Sandik> sandiklar, int mevcutDurumId)
        {
            if (sandiklar.Count == 0)
                return mevcutDurumId;

            var sevkEdilenSayisi = sandiklar.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi);
            return Hesapla(sandiklar.Count, sevkEdilenSayisi, mevcutDurumId);
        }

        public static int Hesapla(int toplamSandik, int sevkEdilenSandik, int mevcutDurumId)
        {
            if (toplamSandik == 0)
                return mevcutDurumId;

            if (sevkEdilenSandik == 0)
            {
                if (mevcutDurumId == (int)ProjeDurum.SevkEdildi || mevcutDurumId == (int)ProjeDurum.EksikSevkEdildi)
                    return (int)ProjeDurum.Hazirlaniyor;

                return mevcutDurumId;
            }

            return sevkEdilenSandik >= toplamSandik
                ? (int)ProjeDurum.SevkEdildi
                : (int)ProjeDurum.EksikSevkEdildi;
        }
    }
}
