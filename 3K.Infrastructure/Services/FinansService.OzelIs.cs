using Microsoft.EntityFrameworkCore;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services
{
    public sealed partial class FinansService
    {
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
            if (entity.SiparisKalemleri.Count != 0)
                throw new InvalidOperationException("PO geçmişi bulunan özel işin fiyat snapshot'ı güncellenemez.");
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
            entity.FinansMiktariManuel = true;
            AddAuditChanges(nameof(_3K.Core.Entities.FinansIsKaydi), entity, auditBefore);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
