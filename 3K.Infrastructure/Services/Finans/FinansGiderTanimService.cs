using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>Gider kütüphanesi: gider kategorileri ve varsayılan değerli gider kalemleri.</summary>
internal sealed class FinansGiderTanimService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansGiderTanimService
{
    public async Task<IReadOnlyList<FinansGiderKategoriModel>> GiderKategorileriAsync(bool sadeceAktif, CancellationToken cancellationToken)
    {
        var query = context.Set<FinansGiderKategori>().AsNoTracking();
        if (sadeceAktif) query = query.Where(x => x.Aktif);
        return await query.OrderBy(x => x.Ad).Select(x => new FinansGiderKategoriModel(x.Id, x.Ad, x.Aktif)).ToListAsync(cancellationToken);
    }

    public Task<FinansGiderKategoriModel> GiderKategoriOlusturAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderKategoriOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansGiderKategoriModel> GiderKategoriOlusturCoreAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
    {
        if (await context.Set<FinansGiderKategori>().AnyAsync(x => x.Ad.ToLower() == model.Ad.Trim().ToLower(), cancellationToken))
            throw IsKuraliException.Cakisma("Aynı isimde gider kategorisi zaten var.");
        var entity = new FinansGiderKategori { Ad = model.Ad.Trim(), Aktif = model.Aktif };
        context.Set<FinansGiderKategori>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansGiderKategori), entity.Id, "Oluşturma", "*", null, entity.Ad);
        await context.SaveChangesAsync(cancellationToken);
        return new FinansGiderKategoriModel(entity.Id, entity.Ad, entity.Aktif);
    }

    public Task<FinansGiderKategoriModel?> GiderKategoriGuncelleAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderKategoriGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansGiderKategoriModel?> GiderKategoriGuncelleCoreAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansGiderKategori>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (await context.Set<FinansGiderKategori>().AnyAsync(x => x.Id != id && x.Ad.ToLower() == model.Ad.Trim().ToLower(), cancellationToken))
            throw IsKuraliException.Cakisma("Aynı isimde gider kategorisi zaten var.");
        denetim.DegisiklikEkle(nameof(FinansGiderKategori), id, nameof(entity.Ad), entity.Ad, model.Ad.Trim());
        denetim.DegisiklikEkle(nameof(FinansGiderKategori), id, nameof(entity.Aktif), entity.Aktif, model.Aktif);
        entity.Ad = model.Ad.Trim();
        entity.Aktif = model.Aktif;
        await context.SaveChangesAsync(cancellationToken);
        return new FinansGiderKategoriModel(entity.Id, entity.Ad, entity.Aktif);
    }

    public async Task<IReadOnlyList<FinansGiderKalemiModel>> GiderKalemleriAsync(int? kategoriId, bool sadeceAktif, CancellationToken cancellationToken)
    {
        var query = context.Set<FinansGiderKalemi>().AsNoTracking();
        if (kategoriId.HasValue) query = query.Where(x => x.FinansGiderKategoriId == kategoriId);
        if (sadeceAktif) query = query.Where(x => x.Aktif);
        return await query.OrderBy(x => x.Ad)
            .Select(x => new FinansGiderKalemiModel(
                x.Id, x.FinansGiderKategoriId, x.Kod, x.Ad, x.Aktif,
                x.VarsayilanFirmaVeyaKisi, x.VarsayilanMiktar, x.VarsayilanBirim,
                x.VarsayilanBirimFiyat, x.VarsayilanParaBirimi,
                x.VarsayilanKdvDahil, x.VarsayilanKdvOrani))
            .ToListAsync(cancellationToken);
    }

    public Task<FinansGiderKalemiModel> GiderKalemiOlusturAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderKalemiOlusturCoreAsync(model, cancellationToken), cancellationToken);

    private async Task<FinansGiderKalemiModel> GiderKalemiOlusturCoreAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
    {
        if (!await context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
        var code = model.Kod.Trim().ToUpperInvariant();
        if (await context.Set<FinansGiderKalemi>().AnyAsync(x => x.Kod == code, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kalemi kodu benzersiz olmalıdır.");
        var entity = new FinansGiderKalemi
        {
            FinansGiderKategoriId = model.KategoriId,
            Kod = code,
            Ad = model.Ad.Trim(),
            Aktif = model.Aktif
        };
        ApplyExpenseItemDefaults(entity, model);
        context.Set<FinansGiderKalemi>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        denetim.Ekle(nameof(FinansGiderKalemi), entity.Id, "Oluşturma", "*", null, entity.Kod);
        await context.SaveChangesAsync(cancellationToken);
        return MapExpenseItem(entity);
    }

    public Task<FinansGiderKalemiModel?> GiderKalemiGuncelleAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(() => GiderKalemiGuncelleCoreAsync(id, model, cancellationToken), cancellationToken);

    private async Task<FinansGiderKalemiModel?> GiderKalemiGuncelleCoreAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken)
    {
        var entity = await context.Set<FinansGiderKalemi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        if (!await context.Set<FinansGiderKategori>().AnyAsync(x => x.Id == model.KategoriId && x.Aktif, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kategorisi bulunamadı veya pasif.");
        var code = model.Kod.Trim().ToUpperInvariant();
        if (await context.Set<FinansGiderKalemi>().AnyAsync(x => x.Id != id && x.Kod == code, cancellationToken))
            throw IsKuraliException.Cakisma("Gider kalemi kodu benzersiz olmalıdır.");
        denetim.DegisiklikEkle(nameof(FinansGiderKalemi), id, nameof(entity.FinansGiderKategoriId), entity.FinansGiderKategoriId, model.KategoriId);
        denetim.DegisiklikEkle(nameof(FinansGiderKalemi), id, nameof(entity.Kod), entity.Kod, code);
        denetim.DegisiklikEkle(nameof(FinansGiderKalemi), id, nameof(entity.Ad), entity.Ad, model.Ad.Trim());
        denetim.DegisiklikEkle(nameof(FinansGiderKalemi), id, nameof(entity.Aktif), entity.Aktif, model.Aktif);
        entity.FinansGiderKategoriId = model.KategoriId;
        entity.Kod = code;
        entity.Ad = model.Ad.Trim();
        entity.Aktif = model.Aktif;
        var defaultsBefore = FinansDenetimKaydi.AnlikGoruntu(entity);
        ApplyExpenseItemDefaults(entity, model);
        denetim.DegisiklikleriEkle(nameof(FinansGiderKalemi), entity, defaultsBefore);
        await context.SaveChangesAsync(cancellationToken);
        return MapExpenseItem(entity);
    }

    private static void ApplyExpenseItemDefaults(FinansGiderKalemi entity, FinansGiderKalemiKaydetModel model)
    {
        entity.VarsayilanFirmaVeyaKisi = model.VarsayilanFirmaVeyaKisi?.Trim();
        entity.VarsayilanMiktar = model.VarsayilanMiktar;
        entity.VarsayilanBirim = model.VarsayilanBirim?.Trim();
        entity.VarsayilanBirimFiyat = model.VarsayilanBirimFiyat;
        entity.VarsayilanParaBirimi = string.IsNullOrWhiteSpace(model.VarsayilanParaBirimi)
            ? null
            : FinansParaBirimiKurali.Normalize(model.VarsayilanParaBirimi);
        entity.VarsayilanKdvDahil = model.VarsayilanKdvDahil;
        entity.VarsayilanKdvOrani = model.VarsayilanKdvOrani;
    }

    private static FinansGiderKalemiModel MapExpenseItem(FinansGiderKalemi entity)
        => new(
            entity.Id, entity.FinansGiderKategoriId, entity.Kod, entity.Ad, entity.Aktif,
            entity.VarsayilanFirmaVeyaKisi, entity.VarsayilanMiktar, entity.VarsayilanBirim,
            entity.VarsayilanBirimFiyat, entity.VarsayilanParaBirimi,
            entity.VarsayilanKdvDahil, entity.VarsayilanKdvOrani);
}
