using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>Düzenli iş, gider kütüphanesi, ürün/tarife ve şablon tanım komutları.</summary>
public sealed class FinansTanimCommandHandlers(IFinansService service) :
    IRequestHandler<FinansDuzenliIsOlusturCommand, Result<FinansDuzenliIsModel>>,
    IRequestHandler<FinansDuzenliIsGuncelleCommand, Result<FinansDuzenliIsModel>>,
    IRequestHandler<FinansGiderKategoriOlusturCommand, Result<FinansGiderKategoriModel>>,
    IRequestHandler<FinansGiderKategoriGuncelleCommand, Result<FinansGiderKategoriModel>>,
    IRequestHandler<FinansGiderKalemiOlusturCommand, Result<FinansGiderKalemiModel>>,
    IRequestHandler<FinansGiderKalemiGuncelleCommand, Result<FinansGiderKalemiModel>>,
    IRequestHandler<FinansUrunOlusturCommand, Result<FinansUrunModel>>,
    IRequestHandler<FinansUrunGuncelleCommand, Result<FinansUrunModel>>,
    IRequestHandler<FinansUrunPasiflestirCommand, Result>,
    IRequestHandler<FinansSablonKaydetCommand, Result<FinansSablonModel>>
{
    public Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.DuzenliIsOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.DuzenliIsGuncelleAsync(request.Id, request.Model, cancellationToken), "Düzenli iş bulunamadı.");

    public Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderKategoriOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.GiderKategoriGuncelleAsync(request.Id, request.Model, cancellationToken), "Gider kategorisi bulunamadı.");

    public Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderKalemiOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.GiderKalemiGuncelleAsync(request.Id, request.Model, cancellationToken), "Gider kalemi bulunamadı.");

    public Task<Result<FinansUrunModel>> Handle(FinansUrunOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.UrunOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansUrunModel>> Handle(FinansUrunGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.UrunGuncelleAsync(request.Id, request.Model, cancellationToken), "Finans ürünü bulunamadı.");

    public Task<Result> Handle(FinansUrunPasiflestirCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.UrunPasiflestirAsync(request.Id, cancellationToken), "Finans ürünü bulunamadı.");

    public Task<Result<FinansSablonModel>> Handle(FinansSablonKaydetCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.SablonKaydetAsync(request.Id, request.Model, cancellationToken));
}
