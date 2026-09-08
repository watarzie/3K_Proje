using System.Linq.Expressions;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Helpers
{
    /// <summary>
    /// Saha/yedek iş ilerlemesini merkezi çeki kalan kuralıyla değerlendirir.
    /// Tamamlanan iş, fiziksel olarak paketlenmiş veya sevk edilmiş ürün anlamına gelmez.
    /// Aynı koşul hem SQL sayaçlarında hem yüklenmiş proje satırlarında kullanılır.
    /// </summary>
    public static class SahaYedekUrunTamamlanmaHelper
    {
        public static Expression<Func<SandikIcerik, bool>> TamamlandiKosulu { get; } = icerik =>
            icerik.CekiSatiriId.HasValue
                ? icerik.CekiSatiri != null &&
                  (icerik.CekiSatiri.GridDurumuId == (int)GridDurum.Iptal ||
                   icerik.CekiSatiri.GridDurumuId == (int)GridDurum.GridKapandi ||
                   (icerik.CekiSatiri.HataliMiktar <= 0 &&
                    icerik.CekiSatiri.DurumId != (int)UrunDurum.HataliUyumsuzGonderim &&
                    icerik.CekiSatiri.IstenenAdet - icerik.CekiSatiri.GelenMiktar -
                    icerik.CekiSatiri.StokKarsilanan - icerik.CekiSatiri.ProjeKarsilanan -
                    icerik.CekiSatiri.TedarikciKarsilanan + icerik.CekiSatiri.ProjeGonderilen -
                    icerik.CekiSatiri.TrafoSevkAdet <= 0))
                : icerik.Miktar > 0 && icerik.KonulanAdet >= icerik.Miktar;

        private static readonly Func<SandikIcerik, bool> Tamamlandi = TamamlandiKosulu.Compile();

        public static bool TamamlandiMi(SandikIcerik icerik) => Tamamlandi(icerik);
    }
}
