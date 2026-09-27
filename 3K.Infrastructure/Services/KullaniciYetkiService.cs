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
    ICurrentUserService currentUser, IUnitOfWork unitOfWork, ISseNotifier? sseNotifier = null) : IKullaniciYetkiService
{
    public async Task<KullaniciYetkiSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
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

        var agac = await YetkiAgaciAsync(cancellationToken);
        var rolAtamalari = await RolAtamalariAsync(rolId, cancellationToken);
        var kararlar = hedefKullaniciId is int hedef
            ? await KararlarAsync(hedef, cancellationToken)
            : new Dictionary<int, bool>();
        var hedefSeviyeleri = agac.KullaniciSeviyeleri(rolAtamalari, kararlar);

        // Kullanıcı, sahip olmadığı bir izni rol üzerinden başkasına veremez.
        foreach (var dugum in agac.Dugumler)
            if (hedefSeviyeleri[dugum.Id] >= YetkiTipi.R &&
                !await rolService.HasUserPermissionAsync(actor, dugum.Kod, hedefSeviyeleri[dugum.Id], cancellationToken))
                return new(false, "Sahip olmadığınız izinler içeren bir rolü atayamazsınız.", 403);
        return new(true);
    }

    public async Task<IReadOnlyList<KullaniciYetkiModel>?> GetAsync(int kullaniciId, CancellationToken cancellationToken = default)
    {
        var user = await context.Kullanicilar.AsNoTracking().FirstOrDefaultAsync(x => x.Id == kullaniciId, cancellationToken);
        if (user == null) return null;

        var menus = await context.MenuTanimlari.AsNoTracking()
            .OrderBy(x => x.ParentId).ThenBy(x => x.Sira).ToListAsync(cancellationToken);
        var agac = new YetkiAgaci(menus.Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId)));
        var rolAtamalari = await RolAtamalariAsync(user.RolId, cancellationToken);
        var kararlar = await KararlarAsync(kullaniciId, cancellationToken);
        var etkin = agac.KullaniciSeviyeleri(rolAtamalari, kararlar);

        return menus.Select(menu => new KullaniciYetkiModel(
            menu.Id, menu.Kod, menu.LabelKey,
            rolAtamalari.GetValueOrDefault(menu.Id, (int)YetkiTipi.N),
            (int)etkin[menu.Id],
            kararlar.TryGetValue(menu.Id, out var karar) ? karar : null)).ToArray();
    }

    public async Task<KullaniciYetkiSonucu> UpdateAsync(int kullaniciId,
        IReadOnlyCollection<KullaniciYetkiKarari> kararlar, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int actor ||
            !await rolService.HasUserPermissionAsync(actor, YetkiKodlari.Kullanicilar, YetkiTipi.W, cancellationToken))
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
            var agac = await YetkiAgaciAsync(ct);
            var yeniKararlar = kararlar.Where(x => x.IzinVerildi.HasValue)
                .ToDictionary(x => x.MenuTanimiId, x => x.IzinVerildi!.Value);
            var sonrakiSeviyeler = agac.KullaniciSeviyeleri(
                mevcut.ToDictionary(row => row.MenuTanimiId, row => row.RolYetkiTipiId), yeniKararlar);
            foreach (var row in mevcut)
            {
                var sonraki = sonrakiSeviyeler[row.MenuTanimiId];
                if ((int)sonraki > row.EtkinYetkiTipiId &&
                    !await rolService.HasUserPermissionAsync(actor, row.Kod, sonraki, ct))
                    return new KullaniciYetkiSonucu(false, "Sahip olmadığınız bir yetkiyi başka kullanıcıya veremezsiniz.", 403);
            }

            var entities = await context.KullaniciYetkileri.Where(x => x.KullaniciId == kullaniciId).ToListAsync(ct);
            context.KullaniciYetkileri.RemoveRange(entities);
            await context.SaveChangesAsync(ct);
            context.KullaniciYetkileri.AddRange(yeniKararlar.Select(x => new KullaniciYetki
            {
                KullaniciId = kullaniciId, MenuTanimiId = x.Key, IzinVerildi = x.Value
            }));
            context.YetkiDegisiklikleri.Add(new YetkiDegisikligi
            {
                AktorKullaniciId = actor, HedefTuru = "Kullanici", HedefId = kullaniciId,
                OncekiDeger = JsonSerializer.Serialize(mevcut.Where(x => x.IzinVerildi.HasValue).Select(x => new { x.MenuTanimiId, x.IzinVerildi })),
                YeniDeger = JsonSerializer.Serialize(kararlar.Where(x => x.IzinVerildi.HasValue))
            });
            await context.SaveChangesAsync(ct);
            if (sseNotifier != null)
                unitOfWork.RegisterAfterCommit(_ => sseNotifier.NotifyUsersAsync(
                    [kullaniciId], SseOlaylari.YetkiGuncellendi));
            return new KullaniciYetkiSonucu(true);
        }, cancellationToken);
    }

    private async Task<YetkiAgaci> YetkiAgaciAsync(CancellationToken ct) =>
        new(await context.MenuTanimlari.AsNoTracking()
            .Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId))
            .ToListAsync(ct));

    private Task<Dictionary<int, int>> RolAtamalariAsync(int rolId, CancellationToken ct) =>
        context.RolYetkileri.AsNoTracking().Where(x => x.RolId == rolId)
            .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId, ct);

    private Task<Dictionary<int, bool>> KararlarAsync(int kullaniciId, CancellationToken ct) =>
        context.KullaniciYetkileri.AsNoTracking().Where(x => x.KullaniciId == kullaniciId)
            .ToDictionaryAsync(x => x.MenuTanimiId, x => x.IzinVerildi, ct);
}
