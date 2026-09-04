using Microsoft.EntityFrameworkCore;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
        public async Task<FinansSayfaliSonuc<FinansOzelIsModel>> OzelIslerAsync(
            FinansListeFiltre filtre,
            CancellationToken cancellationToken)
        {
            var pageNumber = Math.Max(1, filtre.PageNumber);
            var pageSize = Math.Clamp(filtre.PageSize, 1, 250);
            var query = ApplyFilter(
                IsKaydiDetayQuery(),
                filtre with { IsTuru = FinansIsTuru.OzelIs, IptalEdilenleriDahilEt = true });
            var count = await query.CountAsync(cancellationToken);
            var entities = await query
                .OrderByDescending(x => x.UretimTarihi)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new FinansSayfaliSonuc<FinansOzelIsModel>
            {
                Items = entities.Select(entity =>
                {
                    var siparisKalemleri = entity.SiparisKalemleri
                        .Where(x => !x.FinansSiparis.IptalEdildi)
                        .ToList();
                    var faturaKalemleri = siparisKalemleri
                        .SelectMany(x => x.FaturaKalemleri)
                        .Where(x => !x.FinansFatura.IptalEdildi)
                        .ToList();
                    var faturaBekleyenSiparisId = siparisKalemleri
                        .Where(x => x.Adet - x.FaturaKalemleri
                            .Where(fatura => !fatura.FinansFatura.IptalEdildi)
                            .Sum(fatura => fatura.Adet) > Tolerance)
                        .Select(x => (int?)x.FinansSiparisId)
                        .FirstOrDefault();

                    return new FinansOzelIsModel(
                        entity.Id,
                        entity.SandikNo ?? $"OZL-{entity.Id:D6}",
                        entity.OzelIsTuru ?? "Özel İş",
                        entity.Musteri,
                        entity.IsAdi,
                        entity.Adet,
                        entity.Birim,
                        entity.BirimFiyatSnapshot,
                        entity.ParaBirimiSnapshot,
                        entity.KdvOraniSnapshot,
                        entity.Id,
                        entity.UretimTarihi,
                        entity.DuzenliIsId,
                        siparisKalemleri.Select(x => x.FinansSiparis.PoNumarasi).Distinct().Order().ToArray(),
                        faturaKalemleri.Select(x => x.FinansFatura.FaturaNumarasi).Distinct().Order().ToArray(),
                        faturaBekleyenSiparisId);
                }).ToArray(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = count
            };
        }

        public async Task<bool> OzelIsAylikDegerGuncelleAsync(
            int id,
            FinansAylikDegerModel model,
            CancellationToken cancellationToken)
        {
            var entity = await IsKaydiDetayQuery(true)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsTuru == FinansIsTuru.OzelIs, cancellationToken);
            if (entity is null) return false;
            if (entity.IptalEdildi)
                throw new InvalidOperationException("İptal edilmiş özel iş güncellenemez.");
            if (entity.SiparisKalemleri.Any(x => !x.FinansSiparis.IptalEdildi))
                throw new InvalidOperationException("Aktif PO bulunan özel iş güncellenemez.");
            if (!model.Miktar.HasValue && !model.NetBirimFiyat.HasValue)
                throw new InvalidOperationException("Miktar veya net birim fiyat gönderilmelidir.");
            if (model.Miktar is <= 0 || model.NetBirimFiyat is < 0)
                throw new InvalidOperationException("Miktar pozitif, net birim fiyat sıfır veya daha büyük olmalıdır.");
            if (model.Miktar.HasValue && entity.HesaplamaYontemi != FinansHesaplamaYontemi.DegiskenAdet)
                throw new InvalidOperationException("Bu işin miktarı düzenlenemez.");
            if (model.NetBirimFiyat.HasValue && entity.HesaplamaYontemi != FinansHesaplamaYontemi.DegiskenTutar)
                throw new InvalidOperationException("Bu işin tutarı düzenlenemez.");

            var auditBefore = CaptureAuditState(entity);
            if (model.Miktar.HasValue)
            {
                entity.Adet = model.Miktar.Value;
                entity.ToplamM3 = decimal.Round(entity.Adet * entity.BirimM3, 6, MidpointRounding.AwayFromZero);
            }
            if (model.NetBirimFiyat.HasValue)
                entity.BirimFiyatSnapshot = model.NetBirimFiyat.Value;
            AddAuditChanges(nameof(_3K.Core.Entities.FinansIsKaydi), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
