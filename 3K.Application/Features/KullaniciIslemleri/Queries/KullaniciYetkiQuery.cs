using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.KullaniciIslemleri.Queries;

public sealed class KullaniciYetkiQuery : IRequest<Result<IReadOnlyList<KullaniciYetkiModel>>>,
    ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YetkiAtama;
    public int KullaniciId { get; set; }
}

public sealed class KullaniciYetkiQueryHandler(IKullaniciYetkiService service)
    : IRequestHandler<KullaniciYetkiQuery, Result<IReadOnlyList<KullaniciYetkiModel>>>
{
    public async Task<Result<IReadOnlyList<KullaniciYetkiModel>>> Handle(KullaniciYetkiQuery request, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(request.KullaniciId, cancellationToken);
        return result == null ? Result<IReadOnlyList<KullaniciYetkiModel>>.Failure("Kullanıcı bulunamadı.", 404)
            : Result<IReadOnlyList<KullaniciYetkiModel>>.Success(result);
    }
}
