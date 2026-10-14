using _3K.Application.Features.SevkiyatIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>Sefer istatistiğinin kaynak satırı: sandıklı bir proje sevkiyatı ve (varsa) seferi.</summary>
internal sealed class SevkiyatSatiri
{
    public int SevkiyatId { get; init; }
    public int? SeferId { get; init; }
    public string? SeferNo { get; init; }
    public string? SeferPlaka { get; init; }
    public string? Sofor { get; init; }
    public string? DorsePlaka { get; init; }
    public string? SevkiyatPlaka { get; init; }
    public DateTime SevkTarihi { get; init; }
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public int ProjeTipiId { get; init; }
    public int SevkiyatNo { get; init; }
    public int SandikSayisi { get; init; }
    public decimal? BrutKg { get; init; }
    public bool DuzeltmeAcik { get; init; }

    /// <summary>Seferin plakası; eski kayıtta sevkiyatın plakası (normalize).</summary>
    public string? Plaka => SeferId.HasValue ? SeferPlaka : AracPlakaKurali.Normalize(SevkiyatPlaka);

    /// <summary>Gerçek sefer "S:{id}"; eski kayıt plaka + sevk günü "E:{plaka}|{gün}".</summary>
    public string SeferAnahtari => SeferId.HasValue
        ? $"S:{SeferId.Value}"
        : $"E:{Plaka}|{SevkTarihi:yyyy-MM-dd}";
}

/// <summary>
/// Sevkiyat ve sefer kayıtlarından (hareket geçmişi değil) sefer satırlarını kurar. Sandığı kalmamış sevkiyatlar ve
/// boş seferler sayılmaz; sefere bağlı olmayan eski sevkiyatlar plaka + sevk günüyle sanal sefer olur.
/// </summary>
internal static class SevkSeferiOkuyucu
{
    public static Task<List<SevkiyatSatiri>> SatirlarAsync(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        DateTime? baslangic,
        DateTime? bitis,
        int? projeTipiId,
        CancellationToken cancellationToken,
        IReadOnlyCollection<int>? sevkiyatIdleri = null)
    {
        var query = unitOfWork.GetRepository<Sevkiyat>().Queryable().Where(s => s.Sandiklar.Any());
        if (sevkiyatIdleri != null)
            query = query.Where(s => sevkiyatIdleri.Contains(s.Id));
        if (baslangic.HasValue)
            query = query.Where(s => s.SevkTarihi >= baslangic.Value.Date);
        if (bitis.HasValue)
        {
            var bitisSonrasi = bitis.Value.Date.AddDays(1);
            query = query.Where(s => s.SevkTarihi < bitisSonrasi);
        }
        if (projeTipiId.HasValue)
            query = query.Where(s => s.Proje.ProjeTipiId == projeTipiId.Value);

        return readQueries.ToListAsync(query.Select(s => new SevkiyatSatiri
        {
            SevkiyatId = s.Id,
            SeferId = s.SevkSeferiId,
            SeferNo = s.SevkSeferi != null ? s.SevkSeferi.SeferNo : null,
            SeferPlaka = s.SevkSeferi != null ? s.SevkSeferi.AracPlaka : null,
            Sofor = s.SevkSeferi != null ? s.SevkSeferi.Sofor : null,
            DorsePlaka = s.SevkSeferi != null ? s.SevkSeferi.DorsePlaka : null,
            SevkiyatPlaka = s.AracPlaka,
            SevkTarihi = s.SevkTarihi,
            ProjeId = s.ProjeId,
            ProjeNo = s.Proje.ProjeNo,
            ProjeTipiId = s.Proje.ProjeTipiId,
            SevkiyatNo = s.SevkiyatNo,
            SandikSayisi = s.Sandiklar.Count,
            BrutKg = s.Sandiklar.Sum(ss => ss.Sandik.GrossKg),
            DuzeltmeAcik = s.Sandiklar.Any(ss => ss.Sandik.SevkiyatDuzeltmeAcikMi)
        }), cancellationToken);
    }

    /// <summary>Satırları sefer başına toplar; en yeni sefer önce.</summary>
    public static List<SevkSeferiOzetDto> Seferler(IEnumerable<SevkiyatSatiri> satirlar) => satirlar
        .GroupBy(x => x.SeferAnahtari)
        .Select(g =>
        {
            var ilk = g.OrderBy(x => x.SevkiyatId).First();
            var kilolar = g.Where(x => x.BrutKg.HasValue).Select(x => x.BrutKg!.Value).ToList();
            return new SevkSeferiOzetDto
            {
                Anahtar = g.Key,
                SeferId = ilk.SeferId,
                SeferNo = ilk.SeferNo ?? string.Empty,
                EskiKayit = !ilk.SeferId.HasValue,
                AracPlaka = AracPlakaKurali.Bicimle(ilk.Plaka),
                Sofor = ilk.Sofor,
                DorsePlaka = AracPlakaKurali.Bicimle(ilk.DorsePlaka),
                SevkTarihi = g.Min(x => x.SevkTarihi),
                ProjeSayisi = g.Select(x => x.ProjeId).Distinct().Count(),
                SandikSayisi = g.Sum(x => x.SandikSayisi),
                BrutKg = kilolar.Count > 0 ? kilolar.Sum() : null,
                DuzeltmesiAcikSandikVar = g.Any(x => x.DuzeltmeAcik),
                ProjeNolari = g.Select(x => x.ProjeNo).Distinct().Order(StringComparer.Ordinal).ToArray()
            };
        })
        .OrderByDescending(x => x.SevkTarihi)
        .ThenByDescending(x => x.SeferId ?? 0)
        .ThenBy(x => x.Anahtar, StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// Sevkiyatlar sayfasının metin süzgeci: plaka normalize içerir (bkz. <see cref="PlakaEslesir"/>), proje no
    /// seferdeki projelerden birinde büyük/küçük harf duyarsız geçer (sefer bütün projeleriyle kalır).
    /// </summary>
    public static IEnumerable<SevkSeferiOzetDto> Suz(IEnumerable<SevkSeferiOzetDto> seferler, string? plaka, string? projeNo)
    {
        var arananProje = projeNo?.Trim();
        return seferler
            .Where(x => PlakaEslesir(x.AracPlaka, plaka))
            .Where(x => string.IsNullOrEmpty(arananProje) ||
                        x.ProjeNolari.Any(p => p.Contains(arananProje, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Normalize plaka süzgeci: "34 abc" ile "34ABC123" eşleşir.</summary>
    public static bool PlakaEslesir(string? plaka, string? aranan)
    {
        var arananNormal = AracPlakaKurali.Normalize(aranan);
        return arananNormal is null ||
               (AracPlakaKurali.Normalize(plaka) ?? string.Empty).Contains(arananNormal, StringComparison.Ordinal);
    }
}
