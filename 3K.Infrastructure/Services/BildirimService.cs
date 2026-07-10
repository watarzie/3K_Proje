using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _3K.Infrastructure.Services
{
    public class BildirimService : IBildirimService
    {
        private readonly AppDbContext _context;
        private readonly ISseNotifier _sseNotifier;
        private readonly ILogger<BildirimService> _logger;

        public BildirimService(
            AppDbContext context,
            ISseNotifier sseNotifier,
            ILogger<BildirimService> logger)
        {
            _context = context;
            _sseNotifier = sseNotifier;
            _logger = logger;
        }

        public async Task AbonelereBildirimGonderAsync(
            BildirimTipi tip,
            string baslik,
            string mesaj,
            string? hedefUrl,
            string referansTipi,
            int referansId,
            int? olusturanKullaniciId,
            CancellationToken cancellationToken = default)
        {
            var aliciIdleri = await _context.BildirimAbonelikleri
                .AsNoTracking()
                .Where(abonelik => abonelik.TipId == (int)tip)
                .Select(abonelik => abonelik.KullaniciId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (aliciIdleri.Count == 0)
                return;

            var dahaOnceOlusturuldu = await _context.Bildirimler
                .AsNoTracking()
                .AnyAsync(
                    bildirim => bildirim.TipId == (int)tip &&
                                bildirim.ReferansTipi == referansTipi &&
                                bildirim.ReferansId == referansId,
                    cancellationToken);

            if (dahaOnceOlusturuldu)
                return;

            var bildirim = new Bildirim
            {
                TipId = (int)tip,
                Baslik = baslik.Trim(),
                Mesaj = mesaj.Trim(),
                HedefUrl = string.IsNullOrWhiteSpace(hedefUrl) ? null : hedefUrl.Trim(),
                ReferansTipi = referansTipi.Trim(),
                ReferansId = referansId,
                OlusturanKullaniciId = olusturanKullaniciId,
                Alicilar = aliciIdleri
                    .Select(kullaniciId => new KullaniciBildirimi { KullaniciId = kullaniciId })
                    .ToList()
            };

            _context.Bildirimler.Add(bildirim);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _sseNotifier.NotifyUsersAsync(aliciIdleri, SseOlaylari.BildirimGuncellendi);
            }
            catch (Exception exception)
            {
                // Bildirim veritabanında kalıcıdır; geçici SSE hatası ana işlemi geri almamalıdır.
                _logger.LogWarning(exception, "Bildirim {BildirimId} için SSE sinyali gönderilemedi.", bildirim.Id);
            }
        }
    }
}
