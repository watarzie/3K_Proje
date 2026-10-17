using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>
/// PO öncesi iş onayına gönderme. Talepler ve onay kuyruğu kayıtları tek transaction'da yazılır: bir talebin iç komutu
/// reddedilirse hiçbir talep oluşmaz.
/// </summary>
public sealed class FinansPoOnayGonderCommandHandler(
    IFinansPoOnayService service,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ISseNotifier sseNotifier,
    ILogger<FinansPoOnayGonderCommandHandler> logger)
    : IRequestHandler<FinansPoOnayGonderCommand, Result<FinansPoOnayGonderSonucu>>
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<Result<FinansPoOnayGonderSonucu>> Handle(
        FinansPoOnayGonderCommand request,
        CancellationToken cancellationToken)
    {
        var sonuc = await unitOfWork.ExecuteAtomicAsync(async token =>
        {
            var hazirlik = await service.TalepleriOlusturAsync(request.Model, token);
            var onayaAlinan = 0;
            foreach (var talep in hazirlik.Talepler)
            {
                var uygulama = await mediator.Send(
                    new FinansPoOnayUygulaCommand
                    {
                        TalepId = talep.TalepId,
                        ProjeId = talep.ProjeId,
                        ProjeNo = talep.ProjeNo,
                        Ozet = $"{talep.Ad} · {talep.KalemSayisi} kalem · {talep.NetTutar.ToString("N2", Tr)} {talep.ParaBirimi}",
                        OnizlemeHash = talep.OnizlemeHash
                    },
                    token);

                // İç komutun reddi bütün talepleri geri alır; kullanıcı mesajı ve durum kodu korunur.
                if (!uygulama.IsSuccess)
                {
                    throw new IsKuraliException(
                        uygulama.Error?.Message ?? "PO onay talebi oluşturulamadı.",
                        uygulama.StatusCode);
                }

                if (uygulama.StatusCode == StatusConstants.ActionQueuedForApproval)
                    onayaAlinan++;
            }

            var deger = new FinansPoOnayGonderSonucu
            {
                Talepler = hazirlik.Talepler,
                KalemSayisi = hazirlik.KalemSayisi,
                AtlananKalemSayisi = hazirlik.AtlananKalemSayisi,
                OnayaAlinanTalepSayisi = onayaAlinan,
                DogrudanOnaylananTalepSayisi = hazirlik.Talepler.Count - onayaAlinan
            };
            return onayaAlinan > 0
                ? Result<FinansPoOnayGonderSonucu>.Success(deger, StatusConstants.ActionQueuedForApproval)
                : Result<FinansPoOnayGonderSonucu>.Success(deger);
        }, cancellationToken);

        // ApprovalBehavior'ın sinyali commit'ten önce gider; onaycı ekranı yeni talebi kesin okuyabilsin diye
        // commit sonrası bir kez daha yenileme sinyali gönderilir.
        if (sonuc.StatusCode == StatusConstants.ActionQueuedForApproval)
        {
            try
            {
                await sseNotifier.BroadcastApprovalUpdateAsync();
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "PO onay talepleri için commit sonrası onay yenileme sinyali gönderilemedi.");
            }
        }

        return sonuc;
    }
}

/// <summary>Onaylanan (veya kural kapalıyken doğrudan çalışan) PO öncesi iş onayı talebini kalemlere uygular.</summary>
public sealed class FinansPoOnayUygulaCommandHandler(IFinansPoOnayService service)
    : IRequestHandler<FinansPoOnayUygulaCommand, Result<FinansPoOnayUygulamaSonucu>>
{
    public Task<Result<FinansPoOnayUygulamaSonucu>> Handle(
        FinansPoOnayUygulaCommand request,
        CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.OnayiUygulaAsync(request.TalepId, request.OnizlemeHash, cancellationToken));
}
