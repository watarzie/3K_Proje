using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri.Queries;

/// <summary>
/// Proje listesini üretim tamamlanmasına göre ayırır (A3). SQL karşılığı
/// <see cref="AmbalajUretimPolitikasi.ProjeDurumu"/> kuralıdır: ambalaja dahil aktif sandığı olan ve
/// bunların hepsi tamamlanan proje tamamlanmıştır.
/// </summary>
internal static class AmbalajProjeTamamlanmaFiltresi
{
    public static IQueryable<Proje> Uygula(
        IQueryable<Proje> projeler,
        IQueryable<AmbalajUretimKaydi> kayitlar,
        bool? tamamlanan)
    {
        if (!tamamlanan.HasValue) return projeler;
        var gerekli = kayitlar.Where(k => !k.IptalMi && k.AmbalajaDahil);
        return tamamlanan.Value
            ? projeler.Where(p => gerekli.Any(k => k.ProjeId == p.Id) &&
                                  !gerekli.Any(k => k.ProjeId == p.Id && k.UretimDurumu != AmbalajUretimDurumu.Tamamlandi))
            : projeler.Where(p => !gerekli.Any(k => k.ProjeId == p.Id) ||
                                  gerekli.Any(k => k.ProjeId == p.Id && k.UretimDurumu != AmbalajUretimDurumu.Tamamlandi));
    }
}
