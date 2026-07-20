using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Services
{
    public static class FinansTarifeSecici
    {
        private const string KatlanirSandik = "Katlanır Sandık";

        public static FinansUrun? Sec(IEnumerable<FinansUrun> urunler, FinansIsKaydi isKaydi)
        {
            return urunler
                .SelectMany(urun => urun.Eslesmeler.Select(eslesme => new { Urun = urun, Eslesme = eslesme }))
                .Where(x => x.Eslesme.Aktif && x.Eslesme.IsTuru == isKaydi.IsTuru)
                .Where(x => Eslesir(x.Eslesme, isKaydi))
                .OrderBy(x => x.Urun.Sira)
                .ThenBy(x => x.Urun.Id)
                .Select(x => x.Urun)
                .FirstOrDefault();
        }

        private static bool Eslesir(FinansUrunEslesmesi eslesme, FinansIsKaydi isKaydi)
        {
            if (isKaydi.IsTuru == FinansIsTuru.IcSandik)
                return isKaydi.IcSandikSablonId.HasValue && eslesme.IcSandikSablonId == isKaydi.IcSandikSablonId;

            if (isKaydi.IsTuru is FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik)
            {
                if (string.IsNullOrWhiteSpace(isKaydi.SandikTipi)
                    || !string.Equals(eslesme.SandikTipi, isKaydi.SandikTipi, StringComparison.OrdinalIgnoreCase))
                    return false;

                return !string.Equals(isKaydi.SandikTipi, KatlanirSandik, StringComparison.OrdinalIgnoreCase)
                    || eslesme.Boy == isKaydi.Boy && eslesme.En == isKaydi.En && eslesme.Yukseklik == isKaydi.Yukseklik;
            }

            return eslesme.IcSandikSablonId == null
                && (eslesme.SandikAdi == null || string.Equals(eslesme.SandikAdi, isKaydi.SandikAdi, StringComparison.OrdinalIgnoreCase));
        }
    }
}