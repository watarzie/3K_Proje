using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.RolIslemleri;

internal static class YetkiAtamaKurallari
{
    public static async Task<Result<List<RolYetkiItemDto>>> DogrulaVeSinirlaAsync(IUnitOfWork unitOfWork, IRolService rolService,
        ICurrentUserService currentUser, IReadOnlyCollection<RolYetkiItemDto> permissions,
        CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, "rol-yonetimi", YetkiTipi.W, ct))
            return Result<List<RolYetkiItemDto>>.Failure("Rol izinlerini değiştirme yetkiniz bulunmuyor.", 403);
        if (permissions.Select(x => x.MenuTanimiId).Distinct().Count() != permissions.Count ||
            permissions.Any(x => x.YetkiTipiId is < 1 or > 3))
            return Result<List<RolYetkiItemDto>>.Failure("Yinelenen veya geçersiz izin.", 400);
        var menus = (await unitOfWork.GetRepository<MenuTanimi>().GetAllAsync()).ToDictionary(x => x.Id);
        if (permissions.Any(x => !menus.ContainsKey(x.MenuTanimiId)))
            return Result<List<RolYetkiItemDto>>.Failure("Bilinmeyen izin.", 400);

        var requested = permissions.ToDictionary(x => x.MenuTanimiId, x => x.YetkiTipiId);
        var effective = new Dictionary<int, int>();
        var visiting = new HashSet<int>();
        var invalidHierarchy = false;
        int Calculate(int id)
        {
            if (effective.TryGetValue(id, out var value)) return value;
            if (!menus.TryGetValue(id, out var menu) || !visiting.Add(id))
            {
                invalidHierarchy = true;
                return (int)YetkiTipi.N;
            }
            var parent = menu.ParentId is int parentId ? Calculate(parentId) : (int)YetkiTipi.W;
            visiting.Remove(id);
            return effective[id] = YetkiDegerlendirici.UstSinirliYetki(menu.Kod,
                requested.GetValueOrDefault(id, (int)YetkiTipi.N), parent);
        }
        var normalized = new List<RolYetkiItemDto>(permissions.Count);
        foreach (var permission in permissions)
        {
            var menu = menus[permission.MenuTanimiId];
            var definition = YetkiKatalogu.Bul(menu.Kod);
            if (definition != null && permission.YetkiTipiId != 1 &&
                permission.YetkiTipiId != (int)definition.GerekenYetki)
                return Result<List<RolYetkiItemDto>>.Failure("İşlem ve alan izinlerinde yalnız tanımlı izin seviyesi kullanılabilir.", 400);
            normalized.Add(new RolYetkiItemDto
            {
                MenuTanimiId = permission.MenuTanimiId,
                YetkiTipiId = Calculate(permission.MenuTanimiId)
            });
        }
        if (invalidHierarchy)
            return Result<List<RolYetkiItemDto>>.Failure("Geçersiz menü hiyerarşisi.", 400);
        return Result<List<RolYetkiItemDto>>.Success(normalized);
    }
}
