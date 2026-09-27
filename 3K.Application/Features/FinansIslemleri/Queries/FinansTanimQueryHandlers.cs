using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Düzenli iş, gider kütüphanesi, ürün/tarife ve şablon tanım sorguları.</summary>
public sealed class FinansTanimQueryHandlers(IFinansService service) :
    IRequestHandler<FinansDuzenliIslerQuery, Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>>,
    IRequestHandler<FinansGiderKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
    IRequestHandler<FinansGiderKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
    IRequestHandler<FinansGiderKutuphaneKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
    IRequestHandler<FinansGiderKutuphaneKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
    IRequestHandler<FinansUrunlerQuery, Result<FinansSayfaliSonuc<FinansUrunModel>>>,
    IRequestHandler<FinansSablonlarQuery, Result<IReadOnlyList<FinansSablonModel>>>
{
    public Task<Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>> Handle(FinansDuzenliIslerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.DuzenliIslerSayfaliAsync(
            request.SadeceAktif, request.Arama, request.PageNumber, request.PageSize, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKategorileriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKalemleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            service.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKutuphaneKategorileriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKutuphaneKalemleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            service.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansUrunModel>>> Handle(FinansUrunlerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.UrunlerSayfaliAsync(
            request.SadeceAktif, request.TarifeTarihi, request.Arama, request.PageNumber, request.PageSize, cancellationToken));

    public Task<Result<IReadOnlyList<FinansSablonModel>>> Handle(FinansSablonlarQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.SablonlarAsync(cancellationToken));
}
