using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.Events;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public sealed class CekiRevizyonOnayliUygulaCommandHandler
        : IRequestHandler<CekiRevizyonOnayliUygulaCommand, Result<CekiRevizyonSonuc>>
    {
        private readonly ICekiService _cekiService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublisher _publisher;
        private readonly ILogger<CekiRevizyonOnayliUygulaCommandHandler> _logger;

        public CekiRevizyonOnayliUygulaCommandHandler(
            ICekiService cekiService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            IPublisher publisher,
            ILogger<CekiRevizyonOnayliUygulaCommandHandler> logger)
        {
            _cekiService = cekiService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<CekiRevizyonSonuc>> Handle(
            CekiRevizyonOnayliUygulaCommand request,
            CancellationToken cancellationToken)
        {
            var onaylayanKullaniciId = _currentUserService.UserId;
            if (!onaylayanKullaniciId.HasValue)
                return Result<CekiRevizyonSonuc>.Failure("Onaylayan kullanıcı bilgisi alınamadı.", 401);

            var talep = await _unitOfWork
                .GetRepository<CekiRevizyonTalebi>()
                .GetByIdAsync(request.TalepId);

            if (talep == null)
                return Result<CekiRevizyonSonuc>.Failure("Revizyon talebi bulunamadı.", 404);

            // Bildirim meta verisini veri değişikliğinden önce yükle. Böylece
            // revizyon commit edildikten sonra yapılacak ek bir veritabanı
            // sorgusu, başarılı işlemin hatalı görünmesine neden olamaz.
            var sonuc = await _cekiService.OnayliCekiRevizyonunuUygulaAsync(
                request.TalepId,
                onaylayanKullaniciId.Value,
                cancellationToken);

            var bildirimOlayi = new CekiDosyasiYuklendiEvent(
                sonuc.RevizyonCekiId,
                sonuc.ProjeId,
                sonuc.ProjeNo,
                talep.DosyaAdi,
                talep.TalepEdenKullaniciId,
                RevizyonMu: true,
                SatirSayisi: sonuc.IslenenRevizyonSatiriSayisi,
                SandikSayisi: 0,
                EklenenSatirSayisi: sonuc.EklenenSatirSayisi,
                GuncellenenSatirSayisi: sonuc.GuncellenenSatirSayisi,
                SilinenSatirSayisi: sonuc.SilinenSatirSayisi);

            if (_unitOfWork.HasActiveTransaction)
            {
                _unitOfWork.RegisterAfterCommit(_ => BildirimiYayinlaAsync(bildirimOlayi));
            }
            else
            {
                await BildirimiYayinlaAsync(bildirimOlayi);
            }

            return Result<CekiRevizyonSonuc>.Success(sonuc);
        }

        private async Task BildirimiYayinlaAsync(CekiDosyasiYuklendiEvent bildirimOlayi)
        {
            try
            {
                await _publisher.Publish(bildirimOlayi, CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Revizyon transaction'ı commit edildikten sonraki bildirim
                // hatası, başarıyla uygulanan işlemi başarısız göstermemelidir.
                _logger.LogError(
                    exception,
                    "Uygulanan revizyon {RevizyonCekiId} için bildirim olayı yayımlanamadı.",
                    bildirimOlayi.CekiId);
            }
        }
    }
}
