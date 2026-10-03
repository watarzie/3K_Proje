using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

/// <summary>Rol atama denetimi: atayan kişi, sahip olmadığı bir izni rol üzerinden başkasına veremez.</summary>
public sealed class RolAtamaService(AppDbContext context, IRolService rolService, ICurrentUserService currentUser)
    : IRolAtamaService
{
    public async Task<RolAtamaSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, YetkiKodlari.Kullanicilar, YetkiTipi.W, cancellationToken))
            return new(false, "Rol atama yetkiniz bulunmuyor.", 403);
        if (hedefKullaniciId == actor)
            return new(false, "Kendi rolünüzü değiştiremezsiniz.", 403);
        if (!await context.Roller.AnyAsync(x => x.Id == rolId, cancellationToken))
            return new(false, "Rol bulunamadı.", 404);
        // Onay sisteminin sabit Admin rolü istisnası ancak mevcut Admin tarafından atanabilir.
        if (SistemRolleri.AdminMi(rolId) &&
            !await context.Kullanicilar.AnyAsync(x => x.Id == actor && x.RolId == SistemRolleri.AdminId, cancellationToken))
            return new(false, "Yönetici rolünü yalnız mevcut yönetici atayabilir.", 403);

        var agac = new YetkiAgaci(await context.MenuTanimlari.AsNoTracking()
            .Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId))
            .ToListAsync(cancellationToken));
        var rolAtamalari = await context.RolYetkileri.AsNoTracking().Where(x => x.RolId == rolId)
            .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId, cancellationToken);
        var hedefSeviyeleri = agac.RolSeviyeleri(rolAtamalari);

        // Kullanıcı, sahip olmadığı bir izni rol üzerinden başkasına veremez.
        foreach (var dugum in agac.Dugumler)
            if (hedefSeviyeleri[dugum.Id] >= YetkiTipi.R &&
                !await rolService.HasUserPermissionAsync(actor, dugum.Kod, hedefSeviyeleri[dugum.Id], cancellationToken))
                return new(false, "Sahip olmadığınız izinler içeren bir rolü atayamazsınız.", 403);
        return new(true);
    }
}
