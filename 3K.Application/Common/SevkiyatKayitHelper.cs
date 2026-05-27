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
            string? aracPlaka,
            int kullaniciId)
        {
            var sandikList = sandiklar.ToList();
            var sevkiyatRepo = unitOfWork.GetRepository<Sevkiyat>();
            var sevkiyatSandikRepo = unitOfWork.GetRepository<SevkiyatSandik>();
            var temizAracPlaka = NormalizeAracPlaka(aracPlaka);

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
                AracPlaka = temizAracPlaka,
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

        private static string? NormalizeAracPlaka(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim().ToUpperInvariant();
            return trimmed.Length > 30 ? trimmed[..30] : trimmed;
        }
    }
}
