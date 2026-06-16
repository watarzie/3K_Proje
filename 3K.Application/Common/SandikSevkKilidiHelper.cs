using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    public static class SandikSevkKilidiHelper
    {
        public const string SandikKilitliMesaji = "Bu sandık sevk edildiği için üzerinde işlem yapılamaz.";
        public const string UrunKilitliMesaji = "Bu ürün sevk edilmiş bir sandıkta olduğu için üzerinde işlem yapılamaz.";

        public static bool SandikKilitliMi(Sandik sandik)
        {
            return sandik.DurumId == (int)SandikDurum.Sevkedildi && !sandik.SevkiyatDuzeltmeAcikMi;
        }

        public static async Task<HashSet<int>> GetSevkEdilmisSandikCekiSatiriIdleriAsync(
            IUnitOfWork unitOfWork,
            IEnumerable<int> cekiSatiriIdleri)
        {
            var idler = cekiSatiriIdleri
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (idler.Count == 0)
                return new HashSet<int>();

            var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
            var icerikler = (await icerikRepo.FindAsync(i =>
                    i.CekiSatiriId.HasValue &&
                    idler.Contains(i.CekiSatiriId.Value)))
                .ToList();

            if (!icerikler.Any())
                return new HashSet<int>();

            var sandikIdleri = icerikler
                .Select(i => i.SandikId)
                .Distinct()
                .ToList();

            var sandikRepo = unitOfWork.GetRepository<Sandik>();
            var sevkEdilenSandikIdleri = (await sandikRepo.FindAsync(s =>
                    sandikIdleri.Contains(s.Id) &&
                    s.DurumId == (int)SandikDurum.Sevkedildi &&
                    !s.SevkiyatDuzeltmeAcikMi))
                .Select(s => s.Id)
                .ToHashSet();

            return icerikler
                .Where(i => i.CekiSatiriId.HasValue && sevkEdilenSandikIdleri.Contains(i.SandikId))
                .Select(i => i.CekiSatiriId!.Value)
                .ToHashSet();
        }

        public static async Task<bool> CekiSatiriSevkEdilmisSandiktaMiAsync(
            IUnitOfWork unitOfWork,
            CekiSatiri satir)
        {
            var kilitliSatirIdleri = await GetSevkEdilmisSandikCekiSatiriIdleriAsync(
                unitOfWork,
                new[] { satir.Id });

            if (kilitliSatirIdleri.Contains(satir.Id))
                return true;

            var sandikNo = string.IsNullOrWhiteSpace(satir.FiiliSandikNo)
                ? satir.CekideGecenSandikNo
                : satir.FiiliSandikNo;

            if (string.IsNullOrWhiteSpace(sandikNo))
                return false;

            var ceki = satir.Ceki ?? await unitOfWork.GetRepository<Ceki>().GetByIdAsync(satir.CekiId);
            if (ceki == null)
                return false;

            var sandik = (await unitOfWork.GetRepository<Sandik>().FindAsync(s =>
                    s.ProjeId == ceki.ProjeId &&
                    s.SandikNo == sandikNo))
                .FirstOrDefault();

            return sandik != null && SandikKilitliMi(sandik);
        }
    }
}
