using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    internal static class GridSurecDurumHelper
    {
        public static bool IsTamamlandi(CekiSatiri satir)
        {
            return satir.SurecDurumId == (int)SurecDurum.Tamamlandi;
        }

        public static void SyncSurecTamamlandi(CekiSatiri satir)
        {
            // Tamamlandı süreci nihai durumdur; başka bir Grid işlemi bu değeri geri açamaz.
            if (IsTamamlandi(satir))
                return;

            if (satir.GridDurumuId != (int)GridDurum.Iptal && satir.GridEksikMiktar <= 0)
            {
                satir.SurecDurumId = (int)SurecDurum.Tamamlandi;
                return;
            }
        }
    }
}
