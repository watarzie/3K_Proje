using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.RolIslemleri;

/// <summary>
/// Rol izin listesinin doğrulanması ve ağaca göre normalleştirilmesi. Her kod aynı N/R/W
/// modelini kullanır; kaydedilen seviye üst düğümün seviyesiyle sınırlanmış etkin seviyedir.
/// </summary>
internal static class YetkiAtamaKurallari
{
    public static async Task<Result<List<RolYetkiItemDto>>> DogrulaVeSinirlaAsync(IUnitOfWork unitOfWork, IRolService rolService,
        ICurrentUserService currentUser, IReadOnlyCollection<RolYetkiItemDto> permissions,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, YetkiKodlari.RolYonetimi, YetkiTipi.W, ct))
            return Result<List<RolYetkiItemDto>>.Failure("Rol izinlerini değiştirme yetkiniz bulunmuyor.", 403);
        if (permissions.Select(x => x.MenuTanimiId).Distinct().Count() != permissions.Count ||
            permissions.Any(x => !Enum.IsDefined((YetkiTipi)x.YetkiTipiId)))
            return Result<List<RolYetkiItemDto>>.Failure("Yinelenen veya geçersiz izin.", 400);

        var menus = (await unitOfWork.GetRepository<MenuTanimi>().GetAllAsync()).ToDictionary(x => x.Id);
        if (permissions.Any(x => !menus.ContainsKey(x.MenuTanimiId)))
            return Result<List<RolYetkiItemDto>>.Failure("Bilinmeyen izin.", 400);
        if (permissions.Any(x => HiyerarsiBozukMu(menus, x.MenuTanimiId)))
            return Result<List<RolYetkiItemDto>>.Failure("Geçersiz menü hiyerarşisi.", 400);

        var agac = new YetkiAgaci(menus.Values.Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId)));
        var etkin = agac.RolSeviyeleri(permissions.ToDictionary(x => x.MenuTanimiId, x => x.YetkiTipiId));
        return Result<List<RolYetkiItemDto>>.Success(permissions.Select(permission => new RolYetkiItemDto
        {
            MenuTanimiId = permission.MenuTanimiId,
            YetkiTipiId = (int)etkin[permission.MenuTanimiId]
        }).ToList());
    }

    /// <summary>İzin verilen düğümün atalarında kayıp üst düğüm veya döngü varsa seviye anlamsızdır.</summary>
    private static bool HiyerarsiBozukMu(IReadOnlyDictionary<int, MenuTanimi> menus, int menuId)
    {
        var ziyaret = new HashSet<int>();
        for (var current = menus[menuId]; current.ParentId is int ustId; current = menus[ustId])
        {
            if (!menus.ContainsKey(ustId) || !ziyaret.Add(current.Id))
                return true;
        }
        return false;
    }
}
