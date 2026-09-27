using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Belge ve kalıcı silme önizleme sorguları.</summary>
public sealed class FinansBelgeQueryHandlers(IFinansService service) :
    IRequestHandler<FinansBelgelerQuery, Result<IReadOnlyList<FinansBelgeModel>>>,
    IRequestHandler<FinansBelgeIndirQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansKaliciSilOnizlemeQuery, Result<FinansKaliciSilOnizlemeModel>>
{
    public Task<Result<IReadOnlyList<FinansBelgeModel>>> Handle(FinansBelgelerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.BelgelerAsync(request.HedefTuru, request.HedefId, cancellationToken));

    public Task<Result<DosyaIcerigi>> Handle(FinansBelgeIndirQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(() => service.BelgeIndirAsync(request.Id, cancellationToken), "Belge bulunamadı.");

    public Task<Result<FinansKaliciSilOnizlemeModel>> Handle(FinansKaliciSilOnizlemeQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.KaliciSilOnizlemeAsync(request.VarlikTuru, request.Id, cancellationToken), "Kayıt bulunamadı.");
}
