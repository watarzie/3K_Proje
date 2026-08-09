using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    /// <summary>
    /// Ürün işlemi geri alındığında daha önce kapatılmış sandığın yanlışlıkla
    /// sevke hazır kalmasını önler. Sevk edilmiş sandıklara hiçbir zaman dokunmaz.
    /// </summary>
    public static class SandikDurumSenkronizasyonHelper
    {
        public static async Task<int> IslemGeriAlindigindaSandiklariYenidenAcAsync(
            IUnitOfWork unitOfWork,
            IEnumerable<int> cekiSatiriIds,
            IEnumerable<int>? yalnizcaSandikIds = null)
        {
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(cekiSatiriIds);

            var satirIdleri = cekiSatiriIds.Where(id => id > 0).Distinct().ToList();
            if (satirIdleri.Count == 0)
                return 0;

            var izinliSandikIdleri = yalnizcaSandikIds?
                .Where(id => id > 0)
                .Distinct()
                .ToHashSet();

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var sandikIdleri = (await icerikRepo.FindAsync(i =>
                    i.CekiSatiriId.HasValue && satirIdleri.Contains(i.CekiSatiriId.Value)))
                .Select(i => i.SandikId)
                .Where(id => izinliSandikIdleri == null || izinliSandikIdleri.Contains(id))
                .Distinct()
                .ToList();

            if (sandikIdleri.Count == 0)
                return 0;

            var sandikRepo = unitOfWork.GetRepository<Sandik>();
            var kapaliSandiklar = (await sandikRepo.FindAsync(s =>
                    sandikIdleri.Contains(s.Id) &&
                    s.DurumId == (int)SandikDurum.Kapandi))
                .ToList();

            foreach (var sandik in kapaliSandiklar)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                sandikRepo.Update(sandik);
            }

            return kapaliSandiklar.Count;
        }
    }
}
