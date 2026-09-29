using System.Globalization;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Para birimi tablosu (F1) ve vade parametreleri (D3). Para birimi kodu finans kayıtlarında snapshot olarak
/// saklandığı için değiştirilmez; pasifleştirilen para birimi yalnız yeni seçimlerden çıkar.
/// </summary>
internal sealed class FinansParametreService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansParametreService
{
    public async Task<IReadOnlyList<FinansParaBirimiModel>> ParaBirimleriAsync(bool sadeceAktif, CancellationToken cancellationToken)
    {
        var query = context.Set<FinansParaBirimi>().AsNoTracking();
        if (sadeceAktif) query = query.Where(x => x.Aktif);
        return await query.OrderBy(x => x.Sira).ThenBy(x => x.Kod)
            .Select(x => new FinansParaBirimiModel(x.Id, x.Kod, x.Ad, x.Sembol, x.Aktif, x.Sira))
            .ToListAsync(cancellationToken);
    }

    public Task<FinansParaBirimiModel> ParaBirimiOlusturAsync(FinansParaBirimiKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            var kod = KodDogrula(model.Kod);
            if (await context.Set<FinansParaBirimi>().AnyAsync(x => x.Kod == kod, cancellationToken))
                throw IsKuraliException.Cakisma($"{kod} para birimi zaten tanımlı.");
            var entity = new FinansParaBirimi { Kod = kod, Ad = model.Ad.Trim(), Sembol = Temizle(model.Sembol), Aktif = model.Aktif, Sira = model.Sira };
            context.Set<FinansParaBirimi>().Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            denetim.Ekle(nameof(FinansParaBirimi), entity.Id, "Oluşturma", "*", null, $"{entity.Kod} · {entity.Ad}");
            await context.SaveChangesAsync(cancellationToken);
            return Map(entity);
        }, cancellationToken);

    public Task<FinansParaBirimiModel?> ParaBirimiGuncelleAsync(int id, FinansParaBirimiKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            var entity = await context.Set<FinansParaBirimi>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return null;
            if (KodDogrula(model.Kod) != entity.Kod)
                throw IsKuraliException.Cakisma("Para birimi kodu değiştirilemez; farklı kod için yeni para birimi ekleyin.");
            var before = FinansDenetimKaydi.AnlikGoruntu(entity);
            entity.Ad = model.Ad.Trim();
            entity.Sembol = Temizle(model.Sembol);
            entity.Aktif = model.Aktif;
            entity.Sira = model.Sira;
            denetim.DegisiklikleriEkle(nameof(FinansParaBirimi), entity, before);
            await context.SaveChangesAsync(cancellationToken);
            return (FinansParaBirimiModel?)Map(entity);
        }, cancellationToken);

    public async Task<FinansParametrelerModel> ParametrelerAsync(CancellationToken cancellationToken)
        => new(await FinansSorgulari.VarsayilanVadeGunuAsync(context, cancellationToken),
            await FinansSorgulari.VadeUyariGunuAsync(context, cancellationToken));

    public Task<FinansParametrelerModel> ParametreleriKaydetAsync(FinansParametrelerModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            FinansVadeKurallari.VadeGunuDogrula(model.VarsayilanVadeGunu);
            FinansVadeKurallari.UyariGunuDogrula(model.VadeUyariGunu);
            await ParametreKaydetAsync(FinansParametreKodlari.VarsayilanVadeGunu, model.VarsayilanVadeGunu, cancellationToken);
            await ParametreKaydetAsync(FinansParametreKodlari.VadeUyariGunu, model.VadeUyariGunu, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return model;
        }, cancellationToken);

    private async Task ParametreKaydetAsync(string kod, int deger, CancellationToken cancellationToken)
    {
        var yeniDeger = deger.ToString(CultureInfo.InvariantCulture);
        var entity = await context.Set<FinansParametre>().FirstOrDefaultAsync(x => x.Kod == kod, cancellationToken);
        if (entity is null)
        {
            entity = new FinansParametre { Kod = kod, Deger = yeniDeger };
            context.Set<FinansParametre>().Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            denetim.Ekle(nameof(FinansParametre), entity.Id, "Oluşturma", kod, null, yeniDeger);
            return;
        }
        if (entity.Deger == yeniDeger) return;
        denetim.DegisiklikEkle(nameof(FinansParametre), entity.Id, kod, entity.Deger, yeniDeger);
        entity.Deger = yeniDeger;
    }

    private static string KodDogrula(string kod)
    {
        var normalized = FinansParaBirimiKurali.Normalize(kod);
        if (!normalized.All(char.IsAsciiLetterUpper))
            throw IsKuraliException.Cakisma("Para birimi kodu üç harften oluşmalıdır (ISO-4217, ör. USD).");
        return normalized;
    }

    private static string? Temizle(string? deger) => string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();

    private static FinansParaBirimiModel Map(FinansParaBirimi x) => new(x.Id, x.Kod, x.Ad, x.Sembol, x.Aktif, x.Sira);
}
