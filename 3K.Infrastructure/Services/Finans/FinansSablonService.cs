using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>İş şablonları: her kayıtta yeni ve değişmez bir alan sürümü açılır.</summary>
internal sealed class FinansSablonService(
    AppDbContext context,
    FinansIslemYurutucu islem,
    FinansDenetimKaydi denetim) : IFinansSablonService
{
    public async Task<IReadOnlyList<FinansSablonModel>> SablonlarAsync(CancellationToken cancellationToken)
    {
        var templates = await context.Set<FinansIsSablonu>().AsNoTracking().Include(x => x.Surumler).OrderBy(x => x.Ad).ToListAsync(cancellationToken);
        return templates.Select(MapTemplate).ToArray();
    }

    private static FinansSablonModel MapTemplate(FinansIsSablonu template)
    {
        var version = template.Surumler.OrderByDescending(x => x.Surum).First();
        return new(template.Id, template.Kod, template.Ad, template.Aktif, version.Id, version.Surum,
            JsonSerializer.Deserialize<FinansSablonAlanModel[]>(version.AlanlarJson)!);
    }

    public Task<FinansSablonModel> SablonKaydetAsync(int? id, FinansSablonKaydetModel model, CancellationToken cancellationToken)
        => islem.AtomikAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(model.Kod) || model.Kod.Length > 80 || string.IsNullOrWhiteSpace(model.Ad) || model.Ad.Length > 200 || model.Alanlar is null || model.Alanlar.Count > 100)
                throw IsKuraliException.Cakisma("Şablon kodu, adı ve en fazla 100 alan gereklidir.");
            if (model.Alanlar.Any(x => x is null || string.IsNullOrWhiteSpace(x.Kod) || !Regex.IsMatch(x.Kod, "^[A-Za-z][A-Za-z0-9_]{0,49}$") || string.IsNullOrWhiteSpace(x.Ad) || x.Ad.Length > 150 || x.VeriTuru is not ("metin" or "sayi" or "tarih" or "mantiksal")) || model.Alanlar.GroupBy(x => x.Kod).Any(x => x.Count() > 1))
                throw IsKuraliException.Cakisma("Alan kodları tekil, adları dolu ve veri türleri metin/sayi/tarih/mantiksal olmalıdır.");
            var entity = id.HasValue ? await context.Set<FinansIsSablonu>().Include(x => x.Surumler).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw IsKuraliException.Cakisma("Şablon bulunamadı.") : new FinansIsSablonu();
            entity.Kod = model.Kod.Trim().ToUpperInvariant();
            entity.Ad = model.Ad.Trim();
            entity.Aktif = model.Aktif;
            entity.Surumler.Add(new FinansIsSablonSurumu { Surum = entity.Surumler.Count == 0 ? 1 : entity.Surumler.Max(x => x.Surum) + 1, AlanlarJson = JsonSerializer.Serialize(model.Alanlar) });
            if (!id.HasValue) context.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            denetim.Ekle(nameof(FinansIsSablonu), entity.Id, "Şablon Sürümü", "Alanlar", null, JsonSerializer.Serialize(model.Alanlar));
            await context.SaveChangesAsync(cancellationToken);
            return MapTemplate(entity);
        }, cancellationToken);
}
