using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Helpers
{
    public static class CekiSatiriKalanHelper
    {
        public static decimal HesaplaEtkinKalan(CekiSatiri satir, IReadOnlyDictionary<int, decimal> sevkEdilenSahaTamamlamaMap)
        {
            var hamKalan = satir.KalanMiktar;

            if (satir.KaynakCekiSatiriId.HasValue)
                return hamKalan;

            var sahaTamamlanan = sevkEdilenSahaTamamlamaMap.TryGetValue(satir.Id, out var value) ? value : 0;
            return Math.Max(hamKalan - sahaTamamlanan, 0);
        }

        public static decimal HesaplaHamKalan(
            decimal istenenAdet,
            decimal gelenMiktar,
            decimal stokKarsilanan,
            decimal projeKarsilanan,
            decimal tedarikciKarsilanan,
            decimal projeGonderilen,
            decimal trafoSevkAdet,
            decimal hataliMiktar,
            int durumId,
            int gridDurumuId)
        {
            if (gridDurumuId == (int)GridDurum.GridKapandi || gridDurumuId == (int)GridDurum.Iptal)
                return 0;

            var kalan = istenenAdet
                - gelenMiktar
                - stokKarsilanan
                - projeKarsilanan
                - tedarikciKarsilanan
                + projeGonderilen
                - trafoSevkAdet;

            if ((hataliMiktar > 0 || durumId == (int)UrunDurum.HataliUyumsuzGonderim) && kalan <= 0)
                return 1;

            return Math.Max(kalan, 0);
        }
    }
}
