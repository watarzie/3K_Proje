using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public sealed class CekiRevizyonYukleCommandHandler
        : IRequestHandler<CekiRevizyonYukleCommand, Result<CekiRevizyonOnayTalebiSonuc>>
    {
        private readonly ICekiService _cekiService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ISseNotifier _sseNotifier;
        private readonly ILogger<CekiRevizyonYukleCommandHandler> _logger;

        public CekiRevizyonYukleCommandHandler(
            ICekiService cekiService,
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ISseNotifier sseNotifier,
            ILogger<CekiRevizyonYukleCommandHandler> logger)
        {
            _cekiService = cekiService;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _sseNotifier = sseNotifier;
            _logger = logger;
        }

        public async Task<Result<CekiRevizyonOnayTalebiSonuc>> Handle(
            CekiRevizyonYukleCommand request,
            CancellationToken cancellationToken)
        {
            var sonuc = await _unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
            {
                var talep = await _cekiService.CekiRevizyonOnayaSunAsync(
                    request.ExcelDosya,
                    request.DosyaAdi,
                    request.KullaniciId,
                    transactionCancellationToken);

                var onaySonucu = await _mediator.Send(
                    new CekiRevizyonOnayliUygulaCommand
                    {
                        TalepId = talep.TalepId,
                        ProjeId = talep.ProjeId,
                        ProjeNo = talep.ProjeNo
                    },
                    transactionCancellationToken);

                if (!onaySonucu.IsSuccess)
                {
                    RevizyonUygulamaHatasiniFirlat(onaySonucu);
                }

                if (onaySonucu.StatusCode == StatusConstants.ActionQueuedForApproval)
                {
                    return Result<CekiRevizyonOnayTalebiSonuc>.Success(
                        SonucuOlustur(
                            talep,
                            CekiRevizyonTalepSonucTipleri.OnayBekliyor,
                            null,
                            $"{talep.ProjeNo} revizyonu yetkili onayına sunuldu."),
                        StatusConstants.ActionQueuedForApproval);
                }

                if (onaySonucu.Value == null)
                {
                    throw new InvalidOperationException(
                        "Revizyon uygulandı ancak işlem sonucu alınamadı.");
                }

                if (onaySonucu.StatusCode != 200)
                {
                    throw new InvalidOperationException(
                        $"Revizyon işlemi beklenmeyen bir sonuç kodu döndürdü: {onaySonucu.StatusCode}.");
                }

                return Result<CekiRevizyonOnayTalebiSonuc>.Success(
                    SonucuOlustur(
                        talep,
                        CekiRevizyonTalepSonucTipleri.Uygulandi,
                        onaySonucu.Value.RevizyonCekiId,
                        onaySonucu.Value.Mesaj));
            }, cancellationToken);

            // ApprovalBehavior aynı transaction içindeyken kayıt henüz diğer
            // bağlantılara görünmeyebilir. Commit sonrasındaki bu sinyal,
            // onaycı ekranının kesin olarak yeni talebi okuyabilmesini sağlar.
            if (sonuc.StatusCode == StatusConstants.ActionQueuedForApproval)
            {
                try
                {
                    await _sseNotifier.BroadcastApprovalUpdateAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Revizyon talebi {RevizyonTalepId} için commit sonrası onay yenileme sinyali gönderilemedi.",
                        sonuc.Value?.TalepId);
                }
            }

            return sonuc;
        }

        private static void RevizyonUygulamaHatasiniFirlat(
            Result<CekiRevizyonSonuc> uygulamaSonucu)
        {
            var mesaj = uygulamaSonucu.Error?.Message ??
                "Revizyonun onay veya uygulama akışı tamamlanamadı.";
            var sorunlar = uygulamaSonucu.Error?.Issues as
                IReadOnlyCollection<CekiRevizyonSorunu> ??
                Array.Empty<CekiRevizyonSorunu>();

            // İç MediatR çağrısı bilinen revizyon hatalarını Result'a çevirir.
            // Tipli exception dış transaction'ı geri alırken merkezi behavior'ın
            // güvenli HTTP kodunu ve satır bazlı hata detaylarını korumasını sağlar.
            if (uygulamaSonucu.StatusCode == 400)
                throw new CekiRevizyonValidationException(mesaj, sorunlar);

            if (uygulamaSonucu.StatusCode == 409)
                throw new CekiRevizyonConflictException(mesaj, sorunlar);

            throw new InvalidOperationException(mesaj);
        }

        private static CekiRevizyonOnayTalebiSonuc SonucuOlustur(
            CekiRevizyonOnayTalebiSonuc talep,
            string sonucTipi,
            int? uygulananRevizyonCekiId,
            string mesaj)
        {
            return new CekiRevizyonOnayTalebiSonuc
            {
                SonucTipi = sonucTipi,
                TalepId = talep.TalepId,
                ProjeId = talep.ProjeId,
                ProjeNo = talep.ProjeNo,
                AnaCekiId = talep.AnaCekiId,
                DosyaAdi = talep.DosyaAdi,
                EklenenSatirSayisi = talep.EklenenSatirSayisi,
                GuncellenenSatirSayisi = talep.GuncellenenSatirSayisi,
                SilinenSatirSayisi = talep.SilinenSatirSayisi,
                UygulananRevizyonCekiId = uygulananRevizyonCekiId,
                Mesaj = mesaj
            };
        }
    }
}
