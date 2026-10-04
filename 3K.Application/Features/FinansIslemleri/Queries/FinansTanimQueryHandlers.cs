using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Düzenli iş, gider kütüphanesi, ürün/tarife, para birimi ve finans parametresi tanım sorguları.</summary>
public sealed class FinansTanimQueryHandlers(
    IFinansDuzenliIsService duzenliIsler,
    IFinansGiderTanimService giderTanimlari,
    IFinansUrunService urunler,
    IFinansParametreService parametreler) :
    IRequestHandler<FinansDuzenliIslerQuery, Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>>,
    IRequestHandler<FinansGiderKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
    IRequestHandler<FinansGiderKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
    IRequestHandler<FinansGiderKutuphaneKategorileriQuery, Result<IReadOnlyList<FinansGiderKategoriModel>>>,
    IRequestHandler<FinansGiderKutuphaneKalemleriQuery, Result<IReadOnlyList<FinansGiderKalemiModel>>>,
    IRequestHandler<FinansUrunlerQuery, Result<FinansSayfaliSonuc<FinansUrunModel>>>,
    IRequestHandler<FinansTarifeDuzeltmeOnizlemeQuery, Result<FinansTarifeDuzeltmeOnizlemeModel>>,
    IRequestHandler<FinansParaBirimleriQuery, Result<IReadOnlyList<FinansParaBirimiModel>>>,
    IRequestHandler<FinansParametrelerQuery, Result<FinansParametrelerModel>>
{
    public Task<Result<FinansSayfaliSonuc<FinansDuzenliIsModel>>> Handle(FinansDuzenliIslerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => duzenliIsler.DuzenliIslerSayfaliAsync(
            request.SadeceAktif, request.Arama, request.PageNumber, request.PageSize, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKategorileriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => giderTanimlari.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKalemleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            giderTanimlari.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKategoriModel>>> Handle(FinansGiderKutuphaneKategorileriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => giderTanimlari.GiderKategorileriAsync(request.SadeceAktif, cancellationToken));

    public Task<Result<IReadOnlyList<FinansGiderKalemiModel>>> Handle(FinansGiderKutuphaneKalemleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            giderTanimlari.GiderKalemleriAsync(request.KategoriId, request.SadeceAktif, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansUrunModel>>> Handle(FinansUrunlerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => urunler.UrunlerSayfaliAsync(
            request.SadeceAktif, request.TarifeTarihi, request.Arama, request.PageNumber, request.PageSize, cancellationToken));

    public Task<Result<FinansTarifeDuzeltmeOnizlemeModel>> Handle(FinansTarifeDuzeltmeOnizlemeQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => urunler.TarifeDuzeltmeOnizleAsync(request.UrunId, request.TarifeId, request.BirimFiyat, request.KdvOrani, cancellationToken),
            "Finans ürünü veya tarife sürümü bulunamadı.");

    public Task<Result<IReadOnlyList<FinansParaBirimiModel>>> Handle(FinansParaBirimleriQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => parametreler.ParaBirimleriAsync(request.SadeceAktif, cancellationToken));

    public Task<Result<FinansParametrelerModel>> Handle(FinansParametrelerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => parametreler.ParametrelerAsync(cancellationToken));
}
