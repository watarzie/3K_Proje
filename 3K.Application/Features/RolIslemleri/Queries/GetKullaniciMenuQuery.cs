using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Queries;

/// <summary>Yalnız oturum sahibinin güncel DB rolü; istemciden rol/kullanıcı kimliği alınmaz.</summary>
public sealed class GetKullaniciMenuQuery : IRequest<Result<RolDetayDto>>;

public sealed class GetKullaniciMenuQueryHandler(
    ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IRolService rolService)
    : IRequestHandler<GetKullaniciMenuQuery, Result<RolDetayDto>>
{
    public async Task<Result<RolDetayDto>> Handle(GetKullaniciMenuQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not int userId)
            return Result<RolDetayDto>.Failure("Oturum açmanız gerekiyor.", 401);
        var user = await unitOfWork.GetRepository<Kullanici>().GetByIdAsync(userId);
        if (user == null)
            return Result<RolDetayDto>.Failure("Kullanıcı bulunamadı.", 401);

        // Bu iç okuma yalnız doğrulanmış kullanıcının kendi rolüyle sınırlıdır.
        var result = await GetRolDetayQueryHandler.OkuAsync(unitOfWork, rolService, user.RolId, cancellationToken);
        if (!result.IsSuccess) return result;
        var decisions = (await unitOfWork.GetRepository<KullaniciYetki>().FindAsync(x => x.KullaniciId == userId))
            .ToDictionary(x => x.MenuTanimiId, x => x.IzinVerildi);
        void Apply(IEnumerable<MenuTreeDto> nodes)
        {
            foreach (var node in nodes)
            {
                if (decisions.TryGetValue(node.Id, out var granted))
                {
                    node.YetkiTipiId = _3K.Core.Models.YetkiDegerlendirici.EtkinYetki(node.YetkiTipiId, granted,
                        (int)(_3K.Core.Constants.YetkiKatalogu.Bul(node.Kod)?.GerekenYetki ?? _3K.Core.Enums.YetkiTipi.W));
                    node.YetkiTipiMetni = ((_3K.Core.Enums.YetkiTipi)node.YetkiTipiId).ToString();
                }
                Apply(node.Children);
            }
        }
        Apply(result.Value!.MenuAgaci);
        return result;
    }
}
