using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Queries;

/// <summary>
/// Oturum sahibinin erişebildiği (R/W) menü ağacı; yetkisiz düğümler hiç gönderilmez.
/// Yalnız güncel DB rolü ve kişisel kararlar kullanılır; istemciden rol/kullanıcı kimliği alınmaz.
/// </summary>
public sealed class GetKullaniciMenuQuery : IRequest<Result<List<MenuTreeDto>>>;

public sealed class GetKullaniciMenuQueryHandler(
    ICurrentUserService currentUserService, IUnitOfWork unitOfWork, IRolService rolService)
    : IRequestHandler<GetKullaniciMenuQuery, Result<List<MenuTreeDto>>>
{
    public async Task<Result<List<MenuTreeDto>>> Handle(GetKullaniciMenuQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is not int userId)
            return Result<List<MenuTreeDto>>.Failure("Oturum açmanız gerekiyor.", 401);
        var user = await unitOfWork.GetRepository<Kullanici>().GetByIdAsync(userId);
        if (user == null)
            return Result<List<MenuTreeDto>>.Failure("Kullanıcı bulunamadı.", 401);

        var kararlar = (await unitOfWork.GetRepository<KullaniciYetki>().FindAsync(x => x.KullaniciId == userId))
            .ToDictionary(x => x.MenuTanimiId, x => x.IzinVerildi);
        var detay = await GetRolDetayQueryHandler.OkuAsync(unitOfWork, rolService, user.RolId, kararlar, cancellationToken);
        return detay.IsSuccess
            ? Result<List<MenuTreeDto>>.Success(ErisilebilenDugumler(detay.Value!.MenuAgaci))
            : Result<List<MenuTreeDto>>.Failure(detay.Error!.Message, detay.StatusCode);
    }

    /// <summary>Üst düğüm N ise alt düğümler de gösterilmez (üst tavan kuralı).</summary>
    private static List<MenuTreeDto> ErisilebilenDugumler(IEnumerable<MenuTreeDto> dugumler) =>
        dugumler.Where(d => d.YetkiTipiId >= (int)YetkiTipi.R)
            .Select(d =>
            {
                d.Children = ErisilebilenDugumler(d.Children);
                return d;
            })
            .ToList();
}
