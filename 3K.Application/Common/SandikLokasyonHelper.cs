using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    internal static class SandikLokasyonHelper
    {
        public static async Task VarsayilanUcKDepoLokasyonuAtaAsync(IUnitOfWork unitOfWork, int cekiSatiriId)
        {
            var sandikIcerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var ilgiliIcerikler = await sandikIcerikRepo.FindAsync(i => i.CekiSatiriId == cekiSatiriId);
            await VarsayilanUcKDepoLokasyonuAtaAsync(unitOfWork, ilgiliIcerikler);
        }

        public static async Task VarsayilanUcKDepoLokasyonuAtaAsync(IUnitOfWork unitOfWork, IEnumerable<SandikIcerik> ilgiliIcerikler)
        {
            var sandikIdleri = ilgiliIcerikler
                .Select(i => i.SandikId)
                .Distinct()
                .ToList();

            if (!sandikIdleri.Any())
                return;

            // İzlenen örnek üzerinden güncellenir: çağıran sandığı zaten yüklediyse ayrı bir kopyayı Update etmek
            // "already being tracked" hatası verir. Sevkli (düzeltmedeki) sandığın lokasyonu değişmez.
            var sandikRepo = unitOfWork.GetRepository<Sandik>();
            foreach (var sandikId in sandikIdleri)
            {
                var sandik = await sandikRepo.GetByIdAsync(sandikId);
                if (sandik == null ||
                    sandik.DepoLokasyonId != (int)DepoLokasyon.Belirsiz ||
                    sandik.DurumId == (int)SandikDurum.Sevkedildi)
                    continue;

                sandik.DepoLokasyonId = (int)DepoLokasyon.UcK;
                sandikRepo.Update(sandik);
            }
        }
    }
}
