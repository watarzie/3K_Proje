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

    /// <summary>Projenin sevkiyat kayıtlarındaki sandıkların son sevk tarihi (sandık kimliğine göre).</summary>
    internal static async Task<Dictionary<int, DateTime>> GetSandikSevkTarihleriAsync(
        this AppDbContext context,
        int projeId,
        CancellationToken cancellationToken = default) =>
        await context.SevkiyatSandiklari
            .AsNoTracking()
            .Where(ss => ss.Sevkiyat.ProjeId == projeId)
            .GroupBy(ss => ss.SandikId)
            .Select(g => new { SandikId = g.Key, Tarih = g.Max(ss => ss.Sevkiyat.SevkTarihi) })
            .ToDictionaryAsync(x => x.SandikId, x => x.Tarih, cancellationToken);

    /// <summary>
    /// Hiç sandık içeriği olmayan eski (tahsissiz) satırların sandığı; satır kimliğine göre. Sistemin etkin içerik
    /// kuralıyla (SandikService) aynıdır: fiili, yoksa çekideki sandık numarası projede tek bir sandıkla eşleşiyorsa o
    /// sandık. Ekranlar bu satırı o sandıkta gösterdiği için SEVK de o sandığın sevkinden okunur (R-07).
    /// </summary>
    internal static async Task<Dictionary<int, Sandik>> GetTahsissizSatirSandiklariAsync(
        this AppDbContext context,
        int projeId,
        IEnumerable<CekiSatiri> satirlar,
        CancellationToken cancellationToken = default)
    {
        var tahsissizSatirlar = satirlar.Where(s => s.Id > 0 && s.SandikIcerikleri.Count == 0).ToList();
        if (tahsissizSatirlar.Count == 0)
            return new Dictionary<int, Sandik>();

        var benzersizSandiklar = (await context.Sandiklar
                .AsNoTracking()
                .Where(s => s.ProjeId == projeId)
                .ToListAsync(cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s.SandikNo))
            .GroupBy(s => s.SandikNo.Trim().ToUpperInvariant())
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single());

        var sonuc = new Dictionary<int, Sandik>();
        foreach (var satir in tahsissizSatirlar)
        {
            var sandikNo = string.IsNullOrWhiteSpace(satir.FiiliSandikNo) ? satir.CekideGecenSandikNo : satir.FiiliSandikNo;
            if (!string.IsNullOrWhiteSpace(sandikNo) &&
                benzersizSandiklar.TryGetValue(sandikNo.Trim().ToUpperInvariant(), out var sandik))
                sonuc[satir.Id] = sandik;
        }

        return sonuc;
    }

    internal static decimal GetEksikRaporKalan(CekiSatiri satir, IReadOnlyDictionary<int, decimal> tamamlamaPlanMap)
    {
        if (satir.KaynakCekiSatiriId.HasValue)
            return satir.KalanMiktar;

        var sevkEdilenSahaTamamlama = tamamlamaPlanMap.TryGetValue(satir.Id, out var value) ? value : 0;
        return Math.Max(satir.KalanMiktar - sevkEdilenSahaTamamlama, 0);
    }
}
