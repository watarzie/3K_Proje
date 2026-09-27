using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>İş kaydı, aylık iş listesi ve proje seçeneği sorguları.</summary>
public sealed class FinansIsKaydiQueryHandlers(IFinansService service) :
    IRequestHandler<FinansIsKaydiGetirQuery, Result<FinansIsKaydiModel>>,
    IRequestHandler<FinansIsKayitlariSecimQuery, Result<IReadOnlyList<FinansIsKaydiModel>>>,
    IRequestHandler<FinansAylikIslerQuery, Result<FinansAylikSayfaliSonuc>>,
    IRequestHandler<FinansProjeSecenekleriQuery, Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>>
{
    public Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiGetirQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.IsKaydiGetirAsync(request.Id, cancellationToken), "Finans iş kaydı bulunamadı.");

    public Task<Result<IReadOnlyList<FinansIsKaydiModel>>> Handle(FinansIsKayitlariSecimQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.IsKayitlariSecimAsync(request.Ids, cancellationToken));

    public Task<Result<FinansAylikSayfaliSonuc>> Handle(FinansAylikIslerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            service.AylikOzetSayfaliAsync(request.Yil, request.Ay, request.Filtre, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansProjeSecenekModel>>> Handle(FinansProjeSecenekleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            service.ProjeSecenekleriAsync(request.Arama, request.PageNumber, request.PageSize, cancellationToken));
}
