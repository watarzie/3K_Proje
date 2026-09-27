using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>Belge yükleme ve kalıcı silme komutları.</summary>
public sealed class FinansBelgeCommandHandlers(IFinansService service) :
    IRequestHandler<FinansBelgeYukleCommand, Result<FinansBelgeModel>>,
    IRequestHandler<FinansKaliciSilCommand, Result>
{
    public Task<Result<FinansBelgeModel>> Handle(FinansBelgeYukleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.BelgeYukleAsync(request.Model, cancellationToken));

    public Task<Result> Handle(FinansKaliciSilCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.KaliciSilAsync(request.Model, cancellationToken), "Kayıt bulunamadı.");
}
