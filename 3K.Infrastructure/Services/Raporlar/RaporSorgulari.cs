using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Eksik ve gerçekleşen raporların paylaştığı saha tamamlama planı ve kalan miktar hesabı.</summary>
internal static class RaporSorgulari
{
    /// <summary>Kaynak satırların saha projelerinde tamamlanan (istenen) miktarı; kaynak satır kimliğine göre.</summary>
    internal static async Task<Dictionary<int, decimal>> GetTamamlamaPlanMapAsync(
        this AppDbContext context,
        IReadOnlyCollection<CekiSatiri> satirlar,
        bool sadeceSevkEdilenSandiklar,
        CancellationToken cancellationToken = default)
    {
        var kaynakSatirIds = satirlar
            .Where(s => !s.KaynakCekiSatiriId.HasValue)
            .Select(s => s.Id)
            .Distinct()
            .ToList();

        if (!kaynakSatirIds.Any())
            return new Dictionary<int, decimal>();

        var query = context.CekiSatirlari
            .AsNoTracking()
            .Where(cs => cs.KaynakCekiSatiriId.HasValue &&
                kaynakSatirIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha);

        if (sadeceSevkEdilenSandiklar)
            query = query.Where(cs => cs.SandikIcerikleri.Any(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi));

        return await query
            .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
            .Select(g => new { CekiSatiriId = g.Key, TamamlananAdet = g.Sum(cs => cs.IstenenAdet) })
            .ToDictionaryAsync(x => x.CekiSatiriId, x => x.TamamlananAdet, cancellationToken);
    }

    internal static decimal GetEksikRaporKalan(CekiSatiri satir, IReadOnlyDictionary<int, decimal> tamamlamaPlanMap)
    {
        if (satir.KaynakCekiSatiriId.HasValue)
            return satir.KalanMiktar;

        var sevkEdilenSahaTamamlama = tamamlamaPlanMap.TryGetValue(satir.Id, out var value) ? value : 0;
        return Math.Max(satir.KalanMiktar - sevkEdilenSahaTamamlama, 0);
    }
}
