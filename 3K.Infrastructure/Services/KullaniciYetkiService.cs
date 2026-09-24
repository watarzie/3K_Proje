using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

public sealed class KullaniciYetkiService(AppDbContext context, IRolService rolService,
    ICurrentUserService currentUser, IUnitOfWork unitOfWork) : IKullaniciYetkiService
{
    public async Task<KullaniciYetkiSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, "kullanicilar", YetkiTipi.W, cancellationToken))
            return new(false, "Rol atama yetkiniz bulunmuyor.", 403);
        if (hedefKullaniciId == actor)
            return new(false, "Kendi rolünüzü değiştiremezsiniz.", 403);
        if (!await context.Roller.AnyAsync(x => x.Id == rolId, cancellationToken))
            return new(false, "Rol bulunamadı.", 404);
        // Onay sisteminin sabit Admin rolü istisnası ancak mevcut Admin tarafından atanabilir.
        if (rolId == 1 && !await context.Kullanicilar.AnyAsync(x => x.Id == actor && x.RolId == 1, cancellationToken))
            return new(false, "Yönetici rolünü yalnız mevcut yönetici atayabilir.", 403);
        var permissions = await context.RolYetkileri.AsNoTracking().Where(x => x.RolId == rolId)
            .Select(x => new { x.MenuTanimi.Kod, x.YetkiTipiId }).ToListAsync(cancellationToken);
        foreach (var permission in permissions.Where(x => x.YetkiTipiId >= 2))
            if (!await rolService.HasUserPermissionAsync(actor, permission.Kod, (YetkiTipi)permission.YetkiTipiId, cancellationToken))
                return new(false, "Sahip olmadığınız izinler içeren bir rolü atayamazsınız.", 403);
        return new(true);
    }

    public async Task<IReadOnlyList<KullaniciYetkiModel>?> GetAsync(int kullaniciId, CancellationToken cancellationToken = default)
    {
        var user = await context.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(x => x.Id == kullaniciId, cancellationToken);
        if (user == null) return null;
        var roles = await context.RolYetkileri.AsNoTracking().Where(x => x.RolId == user.RolId)
            .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId, cancellationToken);
        var overrides = await context.KullaniciYetkileri.AsNoTracking().Where(x => x.KullaniciId == kullaniciId)
            .ToDictionaryAsync(x => x.MenuTanimiId, x => x.IzinVerildi, cancellationToken);
        var menus = await context.MenuTanimlari.AsNoTracking().OrderBy(x => x.ParentId).ThenBy(x => x.Sira).ToListAsync(cancellationToken);
        return menus.Select(menu =>
        {
            var catalog = YetkiKatalogu.Bul(menu.Kod);
            bool? karar = overrides.TryGetValue(menu.Id, out var granted) ? granted : null;
            var rol = roles.GetValueOrDefault(menu.Id, 1);
            return new KullaniciYetkiModel(menu.Id, menu.Kod, catalog?.Ad ?? menu.LabelKey,
                rol, YetkiDegerlendirici.EtkinYetki(rol, karar, (int)(catalog?.GerekenYetki ?? YetkiTipi.W)),
                karar, catalog?.Kritik ?? false);
        }).ToArray();
    }

    public async Task<KullaniciYetkiSonucu> UpdateAsync(int kullaniciId,
        IReadOnlyCollection<KullaniciYetkiKarari> kararlar, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, "kullanicilar", YetkiTipi.W, cancellationToken))
            return new(false, "Kişisel izin atama yetkiniz bulunmuyor.", 403);
        if (actor == kullaniciId)
            return new(false, "Kendi kişisel izinlerinizi değiştiremezsiniz.", 403);
        if (kararlar.Select(x => x.MenuTanimiId).Distinct().Count() != kararlar.Count)
            return new(false, "Aynı izin birden fazla kez gönderilemez.", 400);

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var mevcut = await GetAsync(kullaniciId, ct);
            if (mevcut == null) return new KullaniciYetkiSonucu(false, "Kullanıcı bulunamadı.", 404);
            var byId = mevcut.ToDictionary(x => x.MenuTanimiId);
            if (kararlar.Any(x => !byId.ContainsKey(x.MenuTanimiId)))
                return new KullaniciYetkiSonucu(false, "Bilinmeyen izin kodu.", 400);
            // Rolden devralmak da mevcut açık reddi kaldırabilir; etkili artışlar aynı kontrolü kullanır.
            var decisions = kararlar.ToDictionary(x => x.MenuTanimiId, x => x.IzinVerildi);
            foreach (var row in mevcut)
            {
                var catalog = YetkiKatalogu.Bul(row.Kod);
                var next = YetkiDegerlendirici.EtkinYetki(row.RolYetkiTipiId,
                    decisions.GetValueOrDefault(row.MenuTanimiId), (int)(catalog?.GerekenYetki ?? YetkiTipi.W));
                if (next > row.EtkinYetkiTipiId && !await rolService.HasUserPermissionAsync(actor, row.Kod, (YetkiTipi)next, ct))
                    return new KullaniciYetkiSonucu(false, "Sahip olmadığınız bir yetkiyi başka kullanıcıya veremezsiniz.", 403);
            }
            var entities = await context.KullaniciYetkileri.Where(x => x.KullaniciId == kullaniciId).ToListAsync(ct);
            context.KullaniciYetkileri.RemoveRange(entities);
            await context.SaveChangesAsync(ct);
            context.KullaniciYetkileri.AddRange(kararlar.Where(x => x.IzinVerildi.HasValue).Select(x => new KullaniciYetki
            {
                KullaniciId = kullaniciId, MenuTanimiId = x.MenuTanimiId, IzinVerildi = x.IzinVerildi!.Value
            }));
            context.YetkiDegisiklikleri.Add(new YetkiDegisikligi
            {
                AktorKullaniciId = actor, HedefTuru = "Kullanici", HedefId = kullaniciId,
                OncekiDeger = JsonSerializer.Serialize(mevcut.Where(x => x.IzinVerildi.HasValue).Select(x => new { x.MenuTanimiId, x.IzinVerildi })),
                YeniDeger = JsonSerializer.Serialize(kararlar.Where(x => x.IzinVerildi.HasValue))
            });
            await context.SaveChangesAsync(ct);
            return new KullaniciYetkiSonucu(true);
        }, cancellationToken);
    }
}
