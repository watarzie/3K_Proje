using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>Gider komutları.</summary>
public sealed class FinansGiderCommandHandlers(IFinansGiderService service) :
    IRequestHandler<FinansGiderOlusturCommand, Result<FinansGiderModel>>,
    IRequestHandler<FinansGiderGuncelleCommand, Result<FinansGiderModel>>,
    IRequestHandler<FinansGiderIptalCommand, Result>
{
    public Task<Result<FinansGiderModel>> Handle(FinansGiderOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GiderOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansGiderModel>> Handle(FinansGiderGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => service.GiderGuncelleAsync(request.Id, request.Model, cancellationToken), "Gider bulunamadı.");

    public Task<Result> Handle(FinansGiderIptalCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => service.GiderIptalAsync(request.Id, request.Aciklama, cancellationToken), "Gider bulunamadı.");
}
