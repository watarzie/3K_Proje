using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace _3K.Application.Features.BildirimIslemleri.Events
{
    public class CekiDosyasiYuklendiEventHandler : INotificationHandler<CekiDosyasiYuklendiEvent>
    {
        private readonly IBildirimService _bildirimService;
        private readonly ILogger<CekiDosyasiYuklendiEventHandler> _logger;

        public CekiDosyasiYuklendiEventHandler(
            IBildirimService bildirimService,
            ILogger<CekiDosyasiYuklendiEventHandler> logger)
        {
            _bildirimService = bildirimService;
            _logger = logger;
        }

        public async Task Handle(CekiDosyasiYuklendiEvent notification, CancellationToken cancellationToken)
        {
            var tip = notification.RevizyonMu
                ? BildirimTipi.CekiRevizyonuYuklendi
                : BildirimTipi.CekiYuklendi;
            var baslik = notification.RevizyonMu
                ? "Revizyon çekisi yüklendi"
                : "Yeni çeki yüklendi";
            var mesaj = notification.RevizyonMu
                ? $"{notification.ProjeNo} projesinin revizyon çekisi yüklendi. Eklenen: {notification.EklenenSatirSayisi}, güncellenen: {notification.GuncellenenSatirSayisi}, silinen: {notification.SilinenSatirSayisi}."
                : $"{notification.ProjeNo} projesinin çekisi yüklendi. {notification.SatirSayisi} ürün satırı ve {notification.SandikSayisi} sandık işlendi.";
            var hedefUrl = notification.ProjeId > 0
                ? $"/sandik-yonetimi/{notification.ProjeId}"
                : "/projeler";

            try
            {
                await _bildirimService.AbonelereBildirimGonderAsync(
                    tip,
                    baslik,
                    mesaj,
                    hedefUrl,
                    BildirimReferansTipleri.Ceki,
                    notification.CekiId,
                    notification.YukleyenKullaniciId > 0 ? notification.YukleyenKullaniciId : null,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                // Çeki işlemi tamamlandıktan sonra oluşan bildirim hatası ana işlemi başarısız göstermemelidir.
                _logger.LogError(
                    exception,
                    "Çeki {CekiId} için kullanıcı bildirimleri oluşturulamadı.",
                    notification.CekiId);
            }
        }
    }
}
