using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Sipariş (PO) ve fatura sorguları.</summary>
public sealed class FinansSiparisFaturaQueryHandlers(IFinansService service) :
    IRequestHandler<FinansSiparislerQuery, Result<FinansSayfaliSonuc<FinansSiparisModel>>>,
    IRequestHandler<FinansSiparisGetirQuery, Result<FinansSiparisModel>>,
    IRequestHandler<FinansFaturalarQuery, Result<FinansSayfaliSonuc<FinansFaturaModel>>>,
    IRequestHandler<FinansFaturaGetirQuery, Result<FinansFaturaModel>>
{
    public Task<Result<FinansSayfaliSonuc<FinansSiparisModel>>> Handle(FinansSiparislerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.SiparislerAsync(request.Filtre, cancellationToken));

    public Task<Result<FinansSiparisModel>> Handle(FinansSiparisGetirQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.SiparisGetirAsync(request.Id, cancellationToken), "Sipariş bulunamadı.");

    public Task<Result<FinansSayfaliSonuc<FinansFaturaModel>>> Handle(FinansFaturalarQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.FaturalarAsync(request.Filtre, cancellationToken));

    public Task<Result<FinansFaturaModel>> Handle(FinansFaturaGetirQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.FaturaGetirAsync(request.Id, cancellationToken), "Fatura bulunamadı.");
}
