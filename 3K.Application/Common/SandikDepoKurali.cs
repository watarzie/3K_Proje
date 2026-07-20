using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Common
{
    internal static class SandikDepoKurali
    {
        public const string LokasyonAtamaUyariMesaji =
            "Sandık depo envanterine dahil olmadan lokasyon atanamaz. Lokasyon atayabilmek için sandıkta en az bir üründe 3K gelen, projeden/stoktan/tedarikçiden karşılanan veya Grid Kapandı hareketi olmalıdır.";

        public static bool BelirsizLokasyonMu(int depoLokasyonId)
        {
            return depoLokasyonId == (int)DepoLokasyon.Belirsiz;
        }

        public static bool DepoLokasyonuAtanabilir(
            Sandik sandik,
            IReadOnlyCollection<SandikIcerik> etkinIcerikler)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return false;

            return etkinIcerikler.Any(DepoEnvanterHareketiVarMi);
        }

        private static bool DepoEnvanterHareketiVarMi(SandikIcerik icerik)
        {
            var satir = icerik.CekiSatiri;
            if (satir == null)
            {
                return icerik.Miktar > 0
                    || icerik.KonulanAdet > 0
                    || icerik.StokKarsilanan > 0
                    || icerik.ProjeKarsilanan > 0
                    || icerik.TedarikciKarsilanan > 0;
            }

            return satir.GridDurumuId == (int)GridDurum.GridKapandi
                || satir.GelenMiktar > 0
                || satir.ProjeKarsilanan > 0
                || satir.StokKarsilanan > 0
                || satir.TedarikciKarsilanan > 0;
        }
    }
}
