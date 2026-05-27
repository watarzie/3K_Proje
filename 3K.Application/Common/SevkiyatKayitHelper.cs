using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    internal static class SevkiyatKayitHelper
    {
        public static async Task<Sevkiyat> OlusturAsync(
            IUnitOfWork unitOfWork,
            int projeId,
            IEnumerable<Sandik> sandiklar,
            DateTime sevkTarihi,
            string? aciklama,
            int kullaniciId)
        {
            var sandikList = sandiklar.ToList();
            var sevkiyatRepo = unitOfWork.GetRepository<Sevkiyat>();
            var sevkiyatSandikRepo = unitOfWork.GetRepository<SevkiyatSandik>();

            var sonSevkiyatNo = sevkiyatRepo.Queryable()
                .Where(s => s.ProjeId == projeId)
                .Select(s => (int?)s.SevkiyatNo)
                .Max() ?? 0;

            var sevkiyat = new Sevkiyat
            {
                ProjeId = projeId,
                SevkiyatNo = sonSevkiyatNo + 1,
                SevkTarihi = sevkTarihi,
                Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                KullaniciId = kullaniciId
            };

            await sevkiyatRepo.AddAsync(sevkiyat);

            foreach (var sandik in sandikList)
            {
                await sevkiyatSandikRepo.AddAsync(new SevkiyatSandik
                {
                    Sevkiyat = sevkiyat,
                    SandikId = sandik.Id
                });
            }

            return sevkiyat;
        }
    }
}
