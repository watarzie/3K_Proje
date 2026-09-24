using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.KullaniciIslemleri.Commands;

public sealed class KullaniciYetkiGuncelleCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => "kullanicilar";
    public int KullaniciId { get; set; }
    public IReadOnlyCollection<KullaniciYetkiKarari> Kararlar { get; set; } = [];
}

public sealed class KullaniciYetkiGuncelleCommandHandler(IKullaniciYetkiService service)
    : IRequestHandler<KullaniciYetkiGuncelleCommand, Result>
{
    public async Task<Result> Handle(KullaniciYetkiGuncelleCommand request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(request.KullaniciId, request.Kararlar, cancellationToken);
        return result.Basarili ? Result.Success() : Result.Failure(result.Hata!, result.DurumKodu);
    }
}
