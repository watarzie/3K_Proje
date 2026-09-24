using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Enums;
using _3K.Core.Constants;

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
        void Apply(IEnumerable<MenuTreeDto> nodes, int ustYetkisi)
        {
            foreach (var node in nodes)
            {
                var kendiYetkisi = node.YetkiTipiId;
                if (decisions.TryGetValue(node.Id, out var granted))
                {
                    kendiYetkisi = YetkiDegerlendirici.EtkinYetki(kendiYetkisi, granted,
                        (int)(YetkiKatalogu.Bul(node.Kod)?.GerekenYetki ?? YetkiTipi.W));
                }
                node.YetkiTipiId = YetkiDegerlendirici.UstSinirliYetki(node.Kod, kendiYetkisi, ustYetkisi);
                node.YetkiTipiMetni = ((YetkiTipi)node.YetkiTipiId).ToString();
                Apply(node.Children, node.YetkiTipiId);
            }
        }
        Apply(result.Value!.MenuAgaci, (int)YetkiTipi.W);
        return result;
    }
}
