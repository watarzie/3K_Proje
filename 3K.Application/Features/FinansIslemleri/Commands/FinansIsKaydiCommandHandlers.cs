using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>İş kaydı, özel iş ve fiyatlandırma komutları.</summary>
public sealed class FinansIsKaydiCommandHandlers(IFinansService service) :
    IRequestHandler<FinansIsKaydiOlusturCommand, Result<FinansIsKaydiModel>>,
    IRequestHandler<FinansIsKaydiIptalCommand, Result>,
    IRequestHandler<FinansIsKaydiGeriAlCommand, Result>,
    IRequestHandler<FinansOzelIsAylikDegerGuncelleCommand, Result>,
    IRequestHandler<FinansTarihiDegistirCommand, Result<FinansIsKaydiModel>>,
    IRequestHandler<FinansFiyatlandirCommand, Result<FinansIsKaydiModel>>
{
    public Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.IsKaydiOlusturAsync(request.Model, cancellationToken));

    public Task<Result> Handle(FinansIsKaydiIptalCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.IsKaydiIptalAsync(request.Id, request.Aciklama, cancellationToken), "Finans iş kaydı bulunamadı.");

    public Task<Result> Handle(FinansIsKaydiGeriAlCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.IsKaydiGeriAlAsync(request.Id, cancellationToken), "Finans iş kaydı bulunamadı.");

    public Task<Result> Handle(FinansOzelIsAylikDegerGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.OzelIsAylikDegerGuncelleAsync(request.Id, request.Model, cancellationToken), "Özel iş kaydı bulunamadı.");

    public Task<Result<FinansIsKaydiModel>> Handle(FinansTarihiDegistirCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.FinansTarihiDegistirAsync(request.Id, request.Model, cancellationToken), "Finans iş kaydı bulunamadı.");

    public Task<Result<FinansIsKaydiModel>> Handle(FinansFiyatlandirCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.FiyatlandirAsync(request.Id, request.Model, cancellationToken), "Finans iş kaydı bulunamadı.");
}
