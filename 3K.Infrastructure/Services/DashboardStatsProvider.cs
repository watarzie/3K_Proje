using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class DashboardStatsProvider : IDashboardStatsProvider
    {
        private readonly AppDbContext _context;

        public DashboardStatsProvider(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOzetRawStats> GetOzetStatsAsync(CancellationToken ct = default)
        {
            // ── Proje durum sayıları (proje tipi + durum bazlı tek sorgu) ──
            var projeDurumRows = await _context.Projeler
                .Select(p => new
                {
                    p.ProjeTipiId,
                    p.DurumId,
                    ToplamSandik = p.Sandiklar.Count(),
                    HazirSandik = p.Sandiklar.Count(s =>
                        s.DurumId == (int)SandikDurum.Kapandi ||
                        s.DurumId == (int)SandikDurum.Sevkedildi)
                })
                .ToListAsync(ct);

            var projeTipiDurumCounts = projeDurumRows
                .Select(p => new
                {
                    p.ProjeTipiId,
                    DurumId = HesaplaDashboardDurumId(p.DurumId, p.ToplamSandik, p.HazirSandik)
                })
                .GroupBy(p => new { p.ProjeTipiId, p.DurumId })
                .Select(g => new { g.Key.ProjeTipiId, g.Key.DurumId, Count = g.Count() })
                .ToList();

            var projeDurumCounts = projeTipiDurumCounts
                .GroupBy(d => d.DurumId)
                .Select(g => new { DurumId = g.Key, Count = g.Sum(x => x.Count) })
                .ToList();

            int GetDurumCount(int durumId) => projeDurumCounts.FirstOrDefault(d => d.DurumId == durumId)?.Count ?? 0;
            int GetTipDurumCount(int tipId, int durumId) => projeTipiDurumCounts.FirstOrDefault(d => d.ProjeTipiId == tipId && d.DurumId == durumId)?.Count ?? 0;
            int GetTipToplamProje(int tipId) => projeTipiDurumCounts.Where(d => d.ProjeTipiId == tipId).Sum(d => d.Count);

            var toplamProje = projeDurumCounts.Sum(d => d.Count);
            var hazirlananProje = GetDurumCount((int)ProjeDurum.Hazirlaniyor);
            var beklemedeProje = GetDurumCount((int)ProjeDurum.Beklemede);
            var tamamlananProje = GetDurumCount((int)ProjeDurum.Tamamlandi);
            var sevkEdilenProje = GetDurumCount((int)ProjeDurum.SevkEdildi);
            var eksikSevkEdilenProje = GetDurumCount((int)ProjeDurum.EksikSevkEdildi);

            var projeTipiAdlari = await _context.LookupProjeTipleri
                .Select(l => new { l.Id, l.Deger })
                .ToDictionaryAsync(l => l.Id, l => l.Deger, ct);

            // ── Toplam sandık ──
            var toplamSandik = await _context.Sandiklar.CountAsync(ct);

            // ── Sandık sayıları proje tipine göre (tek sorgu) ──
            var sandikProjeTipiCounts = await _context.Sandiklar
                .GroupBy(s => s.Proje.ProjeTipiId)
                .Select(g => new { ProjeTipiId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            int GetSandikByTip(int tipId) => sandikProjeTipiCounts.FirstOrDefault(d => d.ProjeTipiId == tipId)?.Count ?? 0;

            // ── Eksik ürün sayısı ──
            var normalSatirStatsRows = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal)
                .Where(cs => !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => new NormalSatirStatsRow
                {
                    Id = cs.Id,
                    IstenenAdet = cs.IstenenAdet,
                    GelenMiktar = cs.GelenMiktar,
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    ProjeGonderilen = cs.ProjeGonderilen,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    HataliMiktar = cs.HataliMiktar,
                    DurumId = cs.DurumId,
                    GridDurumuId = cs.GridDurumuId
                })
                .ToListAsync(ct);

            var normalSahaTamamlamaMap = await GetAktifSahaTamamlamaMapAsync(
                normalSatirStatsRows.Select(r => r.Id),
                ct);
            var normalEksikUrun = normalSatirStatsRows.Count(r => HesaplaEtkinKalan(r, normalSahaTamamlamaMap) > 0);
            var normalTamamlanmaStats = new NormalTamamlanmaStats
            {
                ToplamUrun = normalSatirStatsRows.Count,
                TamamlananUrun = normalSatirStatsRows.Count(r => HesaplaEtkinKalan(r, normalSahaTamamlamaMap) <= 0)
            };

            var sahaYedekEksikUrunCounts = await _context.SandikIcerikleri
                .Where(si => si.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Saha || si.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Yedek)
                .Where(si =>
                    !(((si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar) > 0)
                      && si.KonulanAdet >= (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar)))
                .GroupBy(si => si.Sandik.Proje.ProjeTipiId)
                .Select(g => new { ProjeTipiId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            int GetEksikUrunByTip(int tipId) => tipId == (int)ProjeTipi.Normal
                ? normalEksikUrun
                : sahaYedekEksikUrunCounts.FirstOrDefault(d => d.ProjeTipiId == tipId)?.Count ?? 0;

            var sahaYedekEksikUrun = sahaYedekEksikUrunCounts.Sum(d => d.Count);

            // ── Depo sandık sayıları (lokasyon bazlı) ──
            var depoSandikCountsByTip = await _context.Sandiklar
                .Where(s => s.DurumId != (int)SandikDurum.Sevkedildi)
                .Where(s => s.SandikIcerikleri.Any(si =>
                    (si.CekiSatiriId != null && (si.CekiSatiri!.GelenMiktar > 0
                        || si.CekiSatiri!.ProjeKarsilanan > 0
                        || si.CekiSatiri!.StokKarsilanan > 0
                        || si.CekiSatiri!.TedarikciKarsilanan > 0))
                    ||
                    (si.CekiSatiriId == null && (si.Miktar > 0 || si.KonulanAdet > 0 || si.StokKarsilanan > 0 || si.ProjeKarsilanan > 0 || si.TedarikciKarsilanan > 0))
                    ||
                    (si.CekiSatiri != null && si.CekiSatiri!.GridDurumuId == (int)GridDurum.GridKapandi)
                ))
                .Select(s => new
                {
                    ProjeTipiId = s.Proje.ProjeTipiId,
                    LokasyonId = s.DepoLokasyonId
                })
                .Where(s => s.LokasyonId != (int)DepoLokasyon.Belirsiz)
                .GroupBy(s => new { s.ProjeTipiId, s.LokasyonId })
                .Select(g => new { g.Key.ProjeTipiId, g.Key.LokasyonId, Count = g.Count() })
                .ToListAsync(ct);

            var depoSandikCounts = depoSandikCountsByTip
                .GroupBy(d => d.LokasyonId)
                .Select(g => new { LokasyonId = g.Key, Count = g.Sum(x => x.Count) })
                .ToList();

            int GetDepoCount(int lokId) => depoSandikCounts.FirstOrDefault(d => d.LokasyonId == lokId)?.Count ?? 0;
            var toplamDepoSandik = depoSandikCounts.Sum(d => d.Count);
            var depoUcK = GetDepoCount((int)DepoLokasyon.UcK);
            var depoSeymen = GetDepoCount((int)DepoLokasyon.Seymen);
            var depoGrid = GetDepoCount((int)DepoLokasyon.Grid);
            var depoLokasyonIds = depoSandikCounts
                .Select(d => d.LokasyonId)
                .Concat(depoSandikCountsByTip.Select(d => d.LokasyonId))
                .Distinct()
                .ToList();
            var depoLokasyonlar = await _context.LookupDepoLokasyonlari
                .Where(l => depoLokasyonIds.Contains(l.Id))
                .Select(l => new { l.Id, l.Anahtar, l.Deger })
                .ToListAsync(ct);
            var depoLokasyonLookup = depoLokasyonlar.ToDictionary(l => l.Id);

            List<DashboardDepoDagilimRawStats> BuildDepoDagilimlari(IEnumerable<(int LokasyonId, int Count)> counts)
            {
                return counts
                    .Select(d =>
                    {
                        depoLokasyonLookup.TryGetValue(d.LokasyonId, out var lokasyon);
                        return new DashboardDepoDagilimRawStats
                        {
                            DepoLokasyonId = d.LokasyonId,
                            DepoLokasyonMetni = lokasyon?.Deger ?? $"Depo {d.LokasyonId}",
                            SandikSayisi = d.Count
                        };
                    })
                    .OrderBy(d => depoLokasyonLookup.TryGetValue(d.DepoLokasyonId, out var lokasyon) ? lokasyon.Anahtar : d.DepoLokasyonId)
                    .ThenBy(d => d.DepoLokasyonMetni)
                    .ToList();
            }

            var depoDagilimlari = BuildDepoDagilimlari(depoSandikCounts.Select(d => (d.LokasyonId, d.Count)));
            var normalDepoDagilimlari = BuildDepoDagilimlari(depoSandikCountsByTip
                .Where(d => d.ProjeTipiId == (int)ProjeTipi.Normal)
                .Select(d => (d.LokasyonId, d.Count)));
            var sahaDepoDagilimlari = BuildDepoDagilimlari(depoSandikCountsByTip
                .Where(d => d.ProjeTipiId == (int)ProjeTipi.Saha)
                .Select(d => (d.LokasyonId, d.Count)));
            var yedekDepoDagilimlari = BuildDepoDagilimlari(depoSandikCountsByTip
                .Where(d => d.ProjeTipiId == (int)ProjeTipi.Yedek)
                .Select(d => (d.LokasyonId, d.Count)));

            // ── Proje tipi bazlı ürün tamamlanma yüzdeleri ──
            var sahaYedekStats = await _context.Projeler
                .Where(p => p.ProjeTipiId == (int)ProjeTipi.Saha || p.ProjeTipiId == (int)ProjeTipi.Yedek)
                .GroupBy(p => p.ProjeTipiId)
                .Select(g => new
                {
                    ProjeTipiId = g.Key,
                    ToplamUrun = g.Sum(p => p.Sandiklar.SelectMany(s => s.SandikIcerikleri).Count()),
                    TamamlananUrun = g.Sum(p => p.Sandiklar.SelectMany(s => s.SandikIcerikleri)
                        .Count(si =>
                            ((si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar) > 0)
                            && si.KonulanAdet >= (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar)))
                })
                .ToListAsync(ct);

            var sahaStats = sahaYedekStats.FirstOrDefault(s => s.ProjeTipiId == (int)ProjeTipi.Saha);
            var yedekStats = sahaYedekStats.FirstOrDefault(s => s.ProjeTipiId == (int)ProjeTipi.Yedek);

            int GetTamamlanmaYuzdesi(int tipId)
            {
                if (tipId == (int)ProjeTipi.Normal)
                {
                    return normalTamamlanmaStats is { ToplamUrun: > 0 }
                        ? (int)Math.Floor((decimal)normalTamamlanmaStats.TamamlananUrun / normalTamamlanmaStats.ToplamUrun * 100)
                        : 0;
                }

                var stats = sahaYedekStats.FirstOrDefault(s => s.ProjeTipiId == tipId);
                return stats is { ToplamUrun: > 0 }
                    ? (int)Math.Floor((decimal)stats.TamamlananUrun / stats.ToplamUrun * 100)
                    : 0;
            }

            DashboardProjeTipiOzetRawStats BuildProjeTipiOzet(int tipId, List<DashboardDepoDagilimRawStats> depoDagilimlari)
            {
                projeTipiAdlari.TryGetValue(tipId, out var projeTipiMetni);

                return new DashboardProjeTipiOzetRawStats
                {
                    ProjeTipiId = tipId,
                    ProjeTipiMetni = string.IsNullOrWhiteSpace(projeTipiMetni) ? ((ProjeTipi)tipId).ToString() : projeTipiMetni,
                    ToplamProje = GetTipToplamProje(tipId),
                    HazirlananProje = GetTipDurumCount(tipId, (int)ProjeDurum.Hazirlaniyor),
                    SevkEdilenProje = GetTipDurumCount(tipId, (int)ProjeDurum.SevkEdildi),
                    EksikSevkEdilenProje = GetTipDurumCount(tipId, (int)ProjeDurum.EksikSevkEdildi),
                    TamamlananProje = GetTipDurumCount(tipId, (int)ProjeDurum.Tamamlandi),
                    ToplamSandik = GetSandikByTip(tipId),
                    EksikUrunSayisi = GetEksikUrunByTip(tipId),
                    ToplamDepoSandik = depoDagilimlari.Sum(d => d.SandikSayisi),
                    TamamlanmaYuzdesi = GetTamamlanmaYuzdesi(tipId),
                    DepoDagilimlari = depoDagilimlari
                };
            }

            var projeTipiOzetleri = new List<DashboardProjeTipiOzetRawStats>
            {
                BuildProjeTipiOzet((int)ProjeTipi.Normal, normalDepoDagilimlari),
                BuildProjeTipiOzet((int)ProjeTipi.Saha, sahaDepoDagilimlari),
                BuildProjeTipiOzet((int)ProjeTipi.Yedek, yedekDepoDagilimlari)
            };

            return new DashboardOzetRawStats
            {
                ToplamProje = toplamProje,
                HazirlananProje = hazirlananProje,
                BeklemedeProje = beklemedeProje,
                TamamlananProje = tamamlananProje,
                SevkEdilenProje = sevkEdilenProje,
                EksikSevkEdilenProje = eksikSevkEdilenProje,
                ToplamSandik = toplamSandik,
                NormalSandik = GetSandikByTip((int)ProjeTipi.Normal),
                SahaSandik = GetSandikByTip((int)ProjeTipi.Saha),
                YedekSandik = GetSandikByTip((int)ProjeTipi.Yedek),
                EksikUrunSayisi = normalEksikUrun + sahaYedekEksikUrun,
                ToplamDepoSandik = toplamDepoSandik,
                DepoUcKSandik = depoUcK,
                DepoSeymenSandik = depoSeymen,
                DepoGridSandik = depoGrid,
                DepoDigerSandik = Math.Max(toplamDepoSandik - depoUcK - depoSeymen - depoGrid, 0),
                DepoDagilimlari = depoDagilimlari,
                NormalDepoDagilimlari = normalDepoDagilimlari,
                SahaDepoDagilimlari = sahaDepoDagilimlari,
                YedekDepoDagilimlari = yedekDepoDagilimlari,
                SahaYuzde = sahaStats is { ToplamUrun: > 0 }
                    ? (int)Math.Floor((decimal)sahaStats.TamamlananUrun / sahaStats.ToplamUrun * 100) : 0,
                YedekYuzde = yedekStats is { ToplamUrun: > 0 }
                    ? (int)Math.Floor((decimal)yedekStats.TamamlananUrun / yedekStats.ToplamUrun * 100) : 0,
                ProjeTipiOzetleri = projeTipiOzetleri
            };
        }

        private static int HesaplaDashboardDurumId(int mevcutDurumId, int toplamSandik, int hazirSandik)
        {
            if (mevcutDurumId == (int)ProjeDurum.SevkEdildi || mevcutDurumId == (int)ProjeDurum.EksikSevkEdildi)
                return mevcutDurumId;

            if (toplamSandik > 0 && hazirSandik == toplamSandik)
                return (int)ProjeDurum.Tamamlandi;

            return (int)ProjeDurum.Hazirlaniyor;
        }

        private async Task<Dictionary<int, decimal>> GetAktifSahaTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken)
        {
            var kaynakIds = kaynakCekiSatiriIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (kaynakIds.Count == 0)
                return new Dictionary<int, decimal>();

            var satirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kaynakIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha)
                .Select(cs => new SahaTamamlamaStatsRow
                {
                    KaynakCekiSatiriId = cs.KaynakCekiSatiriId!.Value,
                    IstenenAdet = cs.IstenenAdet,
                    KonulanAdet = cs.SandikIcerikleri.Sum(si => (decimal?)si.KonulanAdet) ?? 0
                })
                .ToListAsync(cancellationToken);

            return satirlar
                .GroupBy(s => s.KaynakCekiSatiriId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(HesaplaGerceklesenSahaTamamlama));
        }

        private static decimal HesaplaEtkinKalan(
            NormalSatirStatsRow row,
            IReadOnlyDictionary<int, decimal> sevkEdilenSahaTamamlamaMap)
        {
            var hamKalan = CekiSatiriKalanHelper.HesaplaHamKalan(
                row.IstenenAdet,
                row.GelenMiktar,
                row.StokKarsilanan,
                row.ProjeKarsilanan,
                row.TedarikciKarsilanan,
                row.ProjeGonderilen,
                row.TrafoSevkAdet,
                row.HataliMiktar,
                row.DurumId,
                row.GridDurumuId);

            var sahaTamamlanan = sevkEdilenSahaTamamlamaMap.TryGetValue(row.Id, out var value) ? value : 0;
            return Math.Max(hamKalan - sahaTamamlanan, 0);
        }

        private static decimal HesaplaGerceklesenSahaTamamlama(SahaTamamlamaStatsRow row)
        {
            if (row.IstenenAdet <= 0)
                return 0;

            return Math.Min(Math.Max(row.KonulanAdet, 0), row.IstenenAdet);
        }

        private sealed class NormalTamamlanmaStats
        {
            public int ToplamUrun { get; set; }
            public int TamamlananUrun { get; set; }
        }

        private sealed class NormalSatirStatsRow
        {
            public int Id { get; set; }
            public decimal IstenenAdet { get; set; }
            public decimal GelenMiktar { get; set; }
            public decimal StokKarsilanan { get; set; }
            public decimal ProjeKarsilanan { get; set; }
            public decimal TedarikciKarsilanan { get; set; }
            public decimal ProjeGonderilen { get; set; }
            public decimal TrafoSevkAdet { get; set; }
            public decimal HataliMiktar { get; set; }
            public int DurumId { get; set; }
            public int GridDurumuId { get; set; }
        }

        private sealed class SahaTamamlamaStatsRow
        {
            public int KaynakCekiSatiriId { get; set; }
            public decimal IstenenAdet { get; set; }
            public decimal KonulanAdet { get; set; }
        }
    }
}
