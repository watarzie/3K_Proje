using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands
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
            IReadOnlyCollection<SandikIcerik> icerikler,
            IReadOnlyDictionary<int, CekiSatiri> cekiSatirlari)
        {
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return false;

            if (icerikler.Any(i => GetCekiSatiri(i, cekiSatirlari)?.GridDurumuId == (int)GridDurum.GridKapandi))
                return true;

            return icerikler.Any(i =>
            {
                var satir = GetCekiSatiri(i, cekiSatirlari);
                if (satir == null)
                    return i.Miktar > 0 || i.KonulanAdet > 0 || i.StokKarsilanan > 0 || i.ProjeKarsilanan > 0 || i.TedarikciKarsilanan > 0;

                return satir.GelenMiktar > 0
                    || satir.ProjeKarsilanan > 0
                    || satir.StokKarsilanan > 0
                    || satir.TedarikciKarsilanan > 0;
            });
        }

        private static CekiSatiri? GetCekiSatiri(SandikIcerik icerik, IReadOnlyDictionary<int, CekiSatiri> cekiSatirlari)
        {
            if (!icerik.CekiSatiriId.HasValue)
                return null;

            return cekiSatirlari.GetValueOrDefault(icerik.CekiSatiriId.Value);
        }
    }
}
