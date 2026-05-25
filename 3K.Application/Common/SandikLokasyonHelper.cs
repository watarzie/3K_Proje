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

            var sandikRepo = unitOfWork.GetRepository<Sandik>();
            var sandiklar = await sandikRepo.FindAsync(s =>
                sandikIdleri.Contains(s.Id) &&
                s.DepoLokasyonId == (int)DepoLokasyon.Belirsiz);

            foreach (var sandik in sandiklar)
            {
                sandik.DepoLokasyonId = (int)DepoLokasyon.UcK;
                sandikRepo.Update(sandik);
            }
        }
    }
}
