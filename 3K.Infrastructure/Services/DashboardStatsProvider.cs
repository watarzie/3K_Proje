using _3K.Core.Entities;
using _3K.Core.Enums;
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
            var projeTipiDurumCounts = await _context.Projeler
                .GroupBy(p => new { p.ProjeTipiId, p.DurumId })
                .Select(g => new { g.Key.ProjeTipiId, g.Key.DurumId, Count = g.Count() })
                .ToListAsync(ct);

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
            var sevkEdilenProje = GetDurumCount((int)ProjeDurum.SevkEdildi) + GetDurumCount((int)ProjeDurum.EksikSevkEdildi);

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
            var normalEksikUrun = await _context.CekiSatirlari
                .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal)
                .Where(cs => cs.GridDurumuId != (int)GridDurum.GridKapandi && cs.GridDurumuId != (int)GridDurum.Iptal)
                .CountAsync(cs =>
                    (cs.IstenenAdet - cs.GelenMiktar - cs.StokKarsilanan - cs.ProjeKarsilanan - cs.TedarikciKarsilanan + cs.ProjeGonderilen - cs.TrafoSevkAdet > 0)
                    ||
                    ((cs.HataliMiktar > 0 || cs.DurumId == (int)UrunDurum.HataliUyumsuzGonderim)
                        && cs.IstenenAdet - cs.GelenMiktar - cs.StokKarsilanan - cs.ProjeKarsilanan - cs.TedarikciKarsilanan + cs.ProjeGonderilen - cs.TrafoSevkAdet <= 0),
                    ct);

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
                    LokasyonId = s.SandikIcerikleri.Any(si => si.CekiSatiri != null && si.CekiSatiri!.GridDurumuId == (int)GridDurum.GridKapandi)
                        ? (int)DepoLokasyon.Grid
                        : s.DepoLokasyonId
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
            var normalTamamlanmaStats = await _context.CekiSatirlari
                .Where(cs => cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Normal)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    ToplamUrun = g.Count(),
                    TamamlananUrun = g.Count(cs =>
                        cs.GridDurumuId == (int)GridDurum.GridKapandi ||
                        cs.GridDurumuId == (int)GridDurum.Iptal ||
                        (cs.HataliMiktar <= 0 &&
                         cs.DurumId != (int)UrunDurum.HataliUyumsuzGonderim &&
                         cs.IstenenAdet - cs.GelenMiktar - cs.StokKarsilanan - cs.ProjeKarsilanan - cs.TedarikciKarsilanan + cs.ProjeGonderilen - cs.TrafoSevkAdet <= 0))
                })
                .FirstOrDefaultAsync(ct);

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
                    SevkEdilenProje = GetTipDurumCount(tipId, (int)ProjeDurum.SevkEdildi) + GetTipDurumCount(tipId, (int)ProjeDurum.EksikSevkEdildi),
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
    }
}
