using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>Sipariş (PO) ve fatura komutları.</summary>
public sealed class FinansSiparisFaturaCommandHandlers(IFinansSiparisFaturaService service) :
    IRequestHandler<FinansSiparisOlusturCommand, Result<FinansSiparisModel>>,
    IRequestHandler<FinansSiparisKalemEkleCommand, Result<FinansSiparisModel>>,
    IRequestHandler<FinansSiparisGuncelleCommand, Result<FinansSiparisModel>>,
    IRequestHandler<FinansSiparisIptalCommand, Result>,
    IRequestHandler<FinansFaturaOlusturCommand, Result<FinansFaturaModel>>,
    IRequestHandler<FinansFaturaGuncelleCommand, Result<FinansFaturaModel>>,
    IRequestHandler<FinansFaturaIptalCommand, Result>,
    IRequestHandler<FinansFaturaOdemeCommand, Result<FinansFaturaModel>>
{
    public Task<Result<FinansFaturaModel>> Handle(FinansFaturaOdemeCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.OdemeKaydetAsync(request.Id, request.Model, cancellationToken), "Fatura bulunamadı.");

    public Task<Result<FinansSiparisModel>> Handle(FinansSiparisOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.SiparisOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansSiparisModel>> Handle(FinansSiparisKalemEkleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.SiparisKalemEkleAsync(request.Id, request.Model, cancellationToken), "Sipariş bulunamadı.");

    public Task<Result<FinansSiparisModel>> Handle(FinansSiparisGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.SiparisGuncelleAsync(request.Id, request.Model, cancellationToken), "Sipariş bulunamadı.");

    public Task<Result> Handle(FinansSiparisIptalCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.SiparisIptalAsync(request.Id, request.Aciklama, cancellationToken), "Sipariş bulunamadı.");

    public Task<Result<FinansFaturaModel>> Handle(FinansFaturaOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.FaturaOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansFaturaModel>> Handle(FinansFaturaGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.FaturaGuncelleAsync(request.Id, request.Model, cancellationToken), "Fatura bulunamadı.");

    public Task<Result> Handle(FinansFaturaIptalCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.FaturaIptalAsync(request.Id, request.Aciklama, cancellationToken), "Fatura bulunamadı.");
}
