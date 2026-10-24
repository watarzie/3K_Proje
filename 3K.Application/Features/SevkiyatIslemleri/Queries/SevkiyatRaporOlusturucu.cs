using _3K.Application.Common;
using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>
/// Sefer satırlarından (<see cref="SevkSeferiOkuyucu"/>) rapor modelini kurar: sevkiyat, proje, sandık ve sandık
/// içeriği üç sorguda okunur; seferler en yeniden eskiye, projeler proje no, sandıklar doğal sandık no sırasıyla.
/// Sandık miktarı sandık içeriği ekranıyla aynı kuraldır (<see cref="SandikTahsisHelper.HesaplaSandikMiktari"/>).
/// </summary>
internal static class SevkiyatRaporOlusturucu
{
    public static async Task<IReadOnlyList<SevkiyatRaporSeferi>> SeferlerAsync(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        ILookupCacheService lookup,
        IReadOnlyCollection<SevkiyatSatiri> satirlar,
        CancellationToken cancellationToken)
    {
        if (satirlar.Count == 0)
            return [];

        var sevkiyatIdleri = satirlar.Select(s => s.SevkiyatId).Distinct().ToArray();
        var sevkiyatlar = (await readQueries.ToListAsync(unitOfWork.GetRepository<Sevkiyat>().Queryable()
            .Where(s => sevkiyatIdleri.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Proje.ProjeNo,
                s.Proje.Musteri,
                s.Proje.ProjeTipiId,
                s.Proje.Lokasyon,
                s.SevkiyatNo,
                s.Aciklama,
                KaydedenAdi = s.Kullanici.AdSoyad,
                SeferKaydedenAdi = s.SevkSeferi != null ? s.SevkSeferi.Kullanici.AdSoyad : null,
                SeferAciklamasi = s.SevkSeferi != null ? s.SevkSeferi.Aciklama : null
            }), cancellationToken)).ToDictionary(s => s.Id);

        var sandiklar = await readQueries.ToListAsync(unitOfWork.GetRepository<SevkiyatSandik>().Queryable()
            .Where(ss => sevkiyatIdleri.Contains(ss.SevkiyatId))
            .Select(ss => new
            {
                ss.SevkiyatId,
                ss.SandikId,
                ss.Sandik.SandikNo,
                ss.Sandik.Ad,
                ss.Sandik.AdIngilizce,
                ss.Sandik.TipId,
                ss.Sandik.Boy,
                ss.Sandik.En,
                ss.Sandik.Yukseklik,
                ss.Sandik.NetKg,
                ss.Sandik.GrossKg,
                ss.Sandik.DepoLokasyonId,
                ss.Sandik.SevkiyatDuzeltmeAcikMi
            }), cancellationToken);

        var urunler = await UrunlerAsync(unitOfWork, readQueries, lookup,
            sandiklar.Select(s => s.SandikId).Distinct().ToArray(), cancellationToken);

        var sandikMap = sandiklar.ToLookup(s => s.SevkiyatId);
        return satirlar
            .GroupBy(s => s.SeferAnahtari)
            .Select(g =>
            {
                var ilk = g.OrderBy(s => s.SevkiyatId).First();
                var ilkSevkiyat = sevkiyatlar[ilk.SevkiyatId];
                var projeler = g
                    .OrderBy(s => s.ProjeNo, StringComparer.Ordinal).ThenBy(s => s.SevkiyatNo)
                    .Select(s =>
                    {
                        var sevkiyat = sevkiyatlar[s.SevkiyatId];
                        return new SevkiyatRaporProjesi(
                            sevkiyat.ProjeNo,
                            sevkiyat.Musteri,
                            Etiket(lookup.GetDeger<LookupProjeTipi>(sevkiyat.ProjeTipiId)),
                            sevkiyat.SevkiyatNo,
                            Bos(sevkiyat.Lokasyon),
                            sandikMap[s.SevkiyatId]
                                .OrderBy(x => x.SandikNo, SandikNumarasiComparer.Instance).ThenBy(x => x.SandikId)
                                .Select(x => new SevkiyatRaporSandigi(
                                    x.SandikNo,
                                    Bos(x.Ad) ?? string.Empty,
                                    Bos(x.AdIngilizce),
                                    Etiket(lookup.GetDeger<LookupSandikTipi>(x.TipId)),
                                    x.Boy,
                                    x.En,
                                    x.Yukseklik,
                                    x.NetKg,
                                    x.GrossKg,
                                    Etiket(lookup.GetDeger<LookupDepoLokasyon>(x.DepoLokasyonId)),
                                    x.SevkiyatDuzeltmeAcikMi,
                                    urunler.TryGetValue(x.SandikId, out var liste) ? liste : []))
                                .ToArray());
                    })
                    .ToArray();
                return new SevkiyatRaporSeferi(
                    ilk.SeferNo ?? string.Empty,
                    !ilk.SeferId.HasValue,
                    AracPlakaKurali.Bicimle(ilk.Plaka),
                    AracPlakaKurali.Bicimle(ilk.DorsePlaka),
                    Bos(ilk.Sofor),
                    g.Min(s => s.SevkTarihi),
                    Bos(ilkSevkiyat.SeferKaydedenAdi) ?? Bos(ilkSevkiyat.KaydedenAdi),
                    Bos(ilkSevkiyat.SeferAciklamasi) ?? g.OrderBy(s => s.SevkiyatId)
                        .Select(s => Bos(sevkiyatlar[s.SevkiyatId].Aciklama)).FirstOrDefault(a => a != null),
                    projeler);
            })
            .OrderByDescending(s => s.SevkTarihi)
            .ThenBy(s => s.EskiKayit)
            .ThenByDescending(s => s.SeferNo, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Sandık içerikleri: çeki satırından gelen ürünlerde sandığa düşen tahsis (Grid kapanmışsa tamamı konulmuş sayılır),
    /// saha/yedek elle eklenen malzemelerde girilen miktar.
    /// </summary>
    private static async Task<Dictionary<int, SevkiyatRaporUrunu[]>> UrunlerAsync(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        ILookupCacheService lookup,
        int[] sandikIdleri,
        CancellationToken cancellationToken)
    {
        if (sandikIdleri.Length == 0)
            return [];

        var icerikler = await readQueries.ToListAsync(unitOfWork.GetRepository<SandikIcerik>().Queryable()
            .Where(i => sandikIdleri.Contains(i.SandikId))
            .Select(i => new
            {
                i.Id,
                i.SandikId,
                i.CekiSatiriId,
                i.TahsisMiktari,
                i.KonulanAdet,
                i.Miktar,
                i.BarkodNo,
                i.Isim,
                i.BirimId,
                SatirSiraNo = i.CekiSatiri != null ? (int?)i.CekiSatiri.SiraNo : null,
                SatirPozNo = i.CekiSatiri != null ? i.CekiSatiri.OlcuResmiPozNo : null,
                SatirBarkodNo = i.CekiSatiri != null ? i.CekiSatiri.BarkodNo : null,
                SatirAciklama = i.CekiSatiri != null ? i.CekiSatiri.Aciklama : null,
                SatirIstenenAdet = i.CekiSatiri != null ? (decimal?)i.CekiSatiri.IstenenAdet : null,
                SatirBirimId = i.CekiSatiri != null ? (int?)i.CekiSatiri.BirimId : null,
                SatirGridDurumuId = i.CekiSatiri != null ? (int?)i.CekiSatiri.GridDurumuId : null
            }), cancellationToken);

        var satirIdleri = icerikler.Where(i => i.CekiSatiriId.HasValue).Select(i => i.CekiSatiriId!.Value).Distinct().ToArray();
        var tahsisSayilari = satirIdleri.Length == 0
            ? new Dictionary<int, int>()
            : (await readQueries.ToListAsync(unitOfWork.GetRepository<SandikIcerik>().Queryable()
                    .Where(i => i.CekiSatiriId.HasValue && satirIdleri.Contains(i.CekiSatiriId.Value))
                    .GroupBy(i => i.CekiSatiriId!.Value)
                    .Select(g => new { CekiSatiriId = g.Key, Sayi = g.Count() }), cancellationToken))
                .ToDictionary(x => x.CekiSatiriId, x => x.Sayi);

        return icerikler
            .GroupBy(i => i.SandikId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(i => i.SatirSiraNo ?? int.MaxValue).ThenBy(i => i.Id)
                .Select(i =>
                {
                    decimal miktar, konulan;
                    if (i.CekiSatiriId is int satirId && i.SatirIstenenAdet is decimal istenen)
                    {
                        miktar = SandikTahsisHelper.HesaplaSandikMiktari(
                            new CekiSatiri { IstenenAdet = istenen },
                            new SandikIcerik { TahsisMiktari = i.TahsisMiktari, KonulanAdet = i.KonulanAdet },
                            tahsisSayilari.GetValueOrDefault(satirId, 1));
                        konulan = i.SatirGridDurumuId == (int)GridDurum.GridKapandi ? miktar : i.KonulanAdet;
                    }
                    else
                    {
                        (miktar, konulan) = ElleEklenenMalzemeKurali.Miktarlar(i.TahsisMiktari, i.Miktar, i.KonulanAdet);
                    }
                    var birimId = i.SatirBirimId ?? i.BirimId;
                    return new SevkiyatRaporUrunu(
                        Bos(i.SatirPozNo),
                        Bos(i.SatirBarkodNo) ?? Bos(i.BarkodNo) ?? string.Empty,
                        Bos(i.SatirAciklama) ?? Bos(i.Isim) ?? string.Empty,
                        miktar,
                        konulan,
                        Math.Max(miktar - konulan, 0),
                        birimId.HasValue ? Etiket(lookup.GetDeger<LookupBirim>(birimId.Value)) : null);
                })
                .ToArray());
    }

    /// <summary>Lookup önbelleğinde olmayan değer ("?") rapora boş yazılır.</summary>
    private static string? Etiket(string? deger) => string.IsNullOrWhiteSpace(deger) || deger == "?" ? null : deger.Trim();

    private static string? Bos(string? deger) => string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();
}
