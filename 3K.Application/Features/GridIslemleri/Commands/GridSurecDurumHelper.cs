using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    internal static class GridSurecDurumHelper
    {
        public static void SyncSurecTamamlandi(CekiSatiri satir)
        {
            if (satir.GridDurumuId != (int)GridDurum.Iptal && satir.GridEksikMiktar <= 0)
            {
                satir.SurecDurumId = (int)SurecDurum.Tamamlandi;
                return;
            }

            if (satir.SurecDurumId == (int)SurecDurum.Tamamlandi)
            {
                satir.SurecDurumId = null;
            }
        }
    }
}
