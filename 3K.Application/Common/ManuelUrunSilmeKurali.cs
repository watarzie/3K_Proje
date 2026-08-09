using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Common
{
    /// <summary>
    /// Manuel eklenen bir CekiSatiri kaydinin silinmeden once tamamen baslangic
    /// durumuna donmus olmasini tek noktadan dogrular.
    /// </summary>
    public static class ManuelUrunSilmeKurali
    {
        public static bool IslemGormusMu(CekiSatiri satir)
        {
            ArgumentNullException.ThrowIfNull(satir);

            return satir.GridDurumuId != (int)GridDurum.Gelmedi ||
                satir.GridGelenAdet != 0 ||
                satir.TrafoSevkAdet != 0 ||
                satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdilmedi ||
                (satir.GridSevkMiktari ?? 0) != 0 ||
                satir.YenidenSevkGerekliAdet != 0 ||
                satir.UcKDurumuId != (int)UcKDurum.Bekliyor ||
                satir.UcKKarsilamaTipiId != (int)UcKDurum.Bekliyor ||
                satir.GelenMiktar != 0 ||
                satir.KarsilananMiktar != 0 ||
                satir.HataliMiktar != 0 ||
                satir.StokKarsilanan != 0 ||
                satir.ProjeKarsilanan != 0 ||
                satir.ProjeGonderilen != 0 ||
                satir.TedarikciKarsilanan != 0 ||
                satir.GeriGonderilenMiktar != 0 ||
                satir.KaliteDurumId.HasValue ||
                satir.SurecDurumId.HasValue;
        }
    }
}
