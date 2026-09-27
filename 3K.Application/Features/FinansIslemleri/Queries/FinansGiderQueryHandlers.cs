using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Gider sorguları.</summary>
public sealed class FinansGiderQueryHandlers(IFinansGiderService service) :
    IRequestHandler<FinansGiderlerQuery, Result<FinansSayfaliSonuc<FinansGiderModel>>>
{
    public Task<Result<FinansSayfaliSonuc<FinansGiderModel>>> Handle(FinansGiderlerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderlerAsync(request.Filtre, cancellationToken));
}
