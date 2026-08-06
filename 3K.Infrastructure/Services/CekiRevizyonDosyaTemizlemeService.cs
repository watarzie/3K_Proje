using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public sealed class CekiRevizyonDosyaTemizlemeService : ICekiRevizyonDosyaTemizlemeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CekiRevizyonDosyaTemizlemeService> _logger;

        public CekiRevizyonDosyaTemizlemeService(
            AppDbContext context,
            ILogger<CekiRevizyonDosyaTemizlemeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> BugunYuklenenUygulanmisDosyaIcerikleriniTemizleAsync(
            CancellationToken cancellationToken = default)
        {
            var temizlemeZamani = TurkeyTime.Now;
            var gunBaslangici = temizlemeZamani.Date;
            var gunBitisi = gunBaslangici.AddDays(1);

            var temizlenenKayitSayisi = await _context.CekiRevizyonTalepleri
                .Where(talep =>
                    talep.CreatedDate >= gunBaslangici &&
                    talep.CreatedDate < gunBitisi &&
                    talep.UygulananRevizyonCekiId.HasValue &&
                    talep.UygulamaTarihi.HasValue &&
                    talep.UygulamaTarihi.Value <= temizlemeZamani &&
                    talep.DosyaIcerigi != null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(talep => talep.DosyaIcerigi, (byte[]?)null)
                        .SetProperty(
                            talep => talep.DosyaIcerigiTemizlenmeTarihi,
                            (DateTime?)temizlemeZamani),
                    cancellationToken);

            _logger.LogInformation(
                "Günlük çeki revizyon dosya temizliği tamamlandı. Tarih: {Tarih:dd.MM.yyyy}, temizlenen kayıt: {KayitSayisi}",
                gunBaslangici,
                temizlenenKayitSayisi);

            return temizlenenKayitSayisi;
        }
    }
}
