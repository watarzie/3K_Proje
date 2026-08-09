using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.UcKIslemleri.Commands
{
    internal static class UcKStokHareketGeriAlHelper
    {
        public static async Task<Result> GeriAlAsync(IUnitOfWork unitOfWork, int cekiSatiriId)
        {
            return await GeriAlAsync(unitOfWork, new[] { cekiSatiriId });
        }

        public static async Task<Result> GeriAlAsync(IUnitOfWork unitOfWork, IEnumerable<int> cekiSatiriIds)
        {
            var stokHareketRepo = unitOfWork.GetRepository<StokHareketi>();
            var satirIdleri = cekiSatiriIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (satirIdleri.Count == 0)
                return Result.Success();

            var stokHareketleri = (await stokHareketRepo.FindAsync(h => satirIdleri.Contains(h.CekiSatiriId))).ToList();
            if (stokHareketleri.Count == 0)
                return Result.Success();

            var stokRepo = unitOfWork.GetRepository<StokKaydi>();
            var stokIdler = stokHareketleri.Select(h => h.StokKaydiId).Distinct().ToList();
            var stoklar = (await stokRepo.FindAsync(s => stokIdler.Contains(s.Id))).ToDictionary(s => s.Id);
            var tumIlgiliStokHareketleri = (await stokHareketRepo.FindAsync(h => stokIdler.Contains(h.StokKaydiId))).ToList();
            var geriAlinacakHareketIdleri = stokHareketleri.Select(h => h.Id).ToHashSet();

            var geriAlmaPlanlari = new List<(
                StokKaydi Stok,
                decimal StoktanKarsilanan,
                decimal FazlaTeslimdenAktarilan)>();

            // Once butun stok gruplarini dogrula. Bir grupta engel varsa daha onceki
            // gruplarin tracked state'ini degistirmeden islemi fail-closed sonlandir.
            foreach (var grup in stokHareketleri.GroupBy(h => h.StokKaydiId))
            {
                if (!stoklar.TryGetValue(grup.Key, out var stok))
                    continue;

                var stoktanKarsilanan = grup
                    .Where(h => h.IslemTipiId == (int)IslemTipi.StoktanKarsilandi)
                    .Sum(h => Math.Abs(h.Miktar));

                var fazlaTeslimdenAktarilan = grup
                    .Where(h => h.IslemTipiId == (int)IslemTipi.FazlaTeslimStogaAktarildi)
                    .Sum(h => Math.Abs(h.Miktar));

                if (fazlaTeslimdenAktarilan > 0)
                {
                    var baskaHareketVarMi = tumIlgiliStokHareketleri.Any(h =>
                        h.StokKaydiId == grup.Key &&
                        !geriAlinacakHareketIdleri.Contains(h.Id));

                    if (baskaHareketVarMi)
                    {
                        return Result.Failure(
                            "Fazla teslimden oluşan stok kaydı başka bir işlemde kullanılmış. Önce bu stoku kullanan işlemleri geri alın.",
                            400);
                    }

                    if (stok.Miktar < fazlaTeslimdenAktarilan)
                    {
                        return Result.Failure(
                            "Fazla teslimden oluşan stok miktarı geri almak için yetersiz. Önce bu stok üzerindeki kullanımları geri alın.",
                            400);
                    }

                }

                geriAlmaPlanlari.Add((stok, stoktanKarsilanan, fazlaTeslimdenAktarilan));
            }

            foreach (var plan in geriAlmaPlanlari)
            {
                if (plan.FazlaTeslimdenAktarilan > 0)
                {
                    plan.Stok.Miktar = Math.Max(
                        plan.Stok.Miktar - plan.FazlaTeslimdenAktarilan,
                        0);
                }

                if (plan.StoktanKarsilanan > 0)
                    plan.Stok.Miktar += plan.StoktanKarsilanan;

                plan.Stok.DurumId = plan.Stok.Miktar > 0
                    ? (int)StokDurum.Aktif
                    : (int)StokDurum.Tukendi;

                stokRepo.Update(plan.Stok);
            }

            foreach (var hareket in stokHareketleri)
                stokHareketRepo.Remove(hareket);

            return Result.Success();
        }
    }
}
