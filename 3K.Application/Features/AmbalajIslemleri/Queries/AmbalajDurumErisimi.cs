using System.Text.Json.Serialization;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

/// <summary>HTTP girdisi değildir; pipeline her çalıştırmada güncel izinlerden doldurur.</summary>
public interface IAmbalajDurumKapsamli
{
    [JsonIgnore] int[]? IzinliDurumlar { get; set; }
}

internal static class AmbalajDurumErisimi
{
    public static IQueryable<AmbalajUretimKaydi> Filtrele(IQueryable<AmbalajUretimKaydi> query, int[]? allowed) =>
        allowed == null ? query : query.Where(k => allowed.Contains((int)k.UretimDurumu));

    public static IQueryable<Proje> ProjeleriFiltrele(IQueryable<Proje> query,
        IQueryable<AmbalajUretimKaydi> records, int[]? allowed)
    {
        if (allowed == null) return query;
        var required = records.Where(k => !k.IptalMi && k.AmbalajaDahil);
        return query.Where(p => allowed.Contains(
            required.Any(k => k.ProjeId == p.Id) && !required.Any(k => k.ProjeId == p.Id && k.UretimDurumu != AmbalajUretimDurumu.Tamamlandi)
                ? 3 : required.Any(k => k.ProjeId == p.Id && k.UretimDurumu != AmbalajUretimDurumu.Planlandi) ? 2 : 1));
    }
}
