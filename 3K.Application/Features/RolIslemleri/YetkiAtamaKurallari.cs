using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri;

internal static class YetkiAtamaKurallari
{
    public static async Task<Result> DogrulaAsync(IUnitOfWork unitOfWork, IRolService rolService,
        ICurrentUserService currentUser, IReadOnlyCollection<RolYetkiItemDto> permissions,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, "rol-yonetimi", YetkiTipi.W, ct))
            return Result.Failure("Rol izinlerini değiştirme yetkiniz bulunmuyor.", 403);
        if (permissions.Select(x => x.MenuTanimiId).Distinct().Count() != permissions.Count ||
            permissions.Any(x => x.YetkiTipiId is < 1 or > 3))
            return Result.Failure("Yinelenen veya geçersiz izin.", 400);
        var menus = (await unitOfWork.GetRepository<MenuTanimi>().GetAllAsync()).ToDictionary(x => x.Id);
        if (permissions.Any(x => !menus.ContainsKey(x.MenuTanimiId)))
            return Result.Failure("Bilinmeyen izin.", 400);
        foreach (var permission in permissions)
        {
            var menu = menus[permission.MenuTanimiId];
            var definition = YetkiKatalogu.Bul(menu.Kod);
            if (definition != null && permission.YetkiTipiId != 1 &&
                permission.YetkiTipiId != (int)definition.GerekenYetki)
                return Result.Failure("İşlem ve alan izinlerinde yalnız tanımlı izin seviyesi kullanılabilir.", 400);
        }
        return Result.Success();
    }
}
