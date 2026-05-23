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
            // ── Proje durum sayıları (tek sorgu) ──
            var projeDurumCounts = await _context.Projeler
                .GroupBy(p => p.DurumId)
                .Select(g => new { DurumId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            int GetDurumCount(int durumId) => projeDurumCounts.FirstOrDefault(d => d.DurumId == durumId)?.Count ?? 0;

            var toplamProje = projeDurumCounts.Sum(d => d.Count);
            var hazirlananProje = GetDurumCount((int)ProjeDurum.Hazirlaniyor);
            var beklemedeProje = GetDurumCount((int)ProjeDurum.Beklemede);
            var tamamlananProje = GetDurumCount((int)ProjeDurum.Tamamlandi);
            var sevkEdilenProje = GetDurumCount((int)ProjeDurum.SevkEdildi) + GetDurumCount((int)ProjeDurum.EksikSevkEdildi);

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

            var sahaYedekEksikUrun = await _context.SandikIcerikleri
                .Where(si => si.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Saha || si.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Yedek)
                .CountAsync(si =>
                    !(((si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar) > 0)
                      && si.KonulanAdet >= (si.CekiSatiriId != null ? si.CekiSatiri!.IstenenAdet : si.Miktar)),
                    ct);

            // ── Depo sandık sayıları (lokasyon bazlı) ──
            var depoSandikCounts = await _context.Sandiklar
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
                    LokasyonId = s.SandikIcerikleri.Any(si => si.CekiSatiri != null && si.CekiSatiri!.GridDurumuId == (int)GridDurum.GridKapandi)
                        ? (int)DepoLokasyon.Grid
                        : s.DepoLokasyonId
                })
                .Where(s => s.LokasyonId != (int)DepoLokasyon.Belirsiz)
                .GroupBy(s => s.LokasyonId)
                .Select(g => new { LokasyonId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            int GetDepoCount(int lokId) => depoSandikCounts.FirstOrDefault(d => d.LokasyonId == lokId)?.Count ?? 0;
            var toplamDepoSandik = depoSandikCounts.Sum(d => d.Count);
            var depoUcK = GetDepoCount((int)DepoLokasyon.UcK);
            var depoSeymen = GetDepoCount((int)DepoLokasyon.Seymen);
            var depoGrid = GetDepoCount((int)DepoLokasyon.Grid);

            // ── Saha/Yedek yüzdeleri ──
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
                SahaYuzde = sahaStats is { ToplamUrun: > 0 }
                    ? (int)Math.Floor((decimal)sahaStats.TamamlananUrun / sahaStats.ToplamUrun * 100) : 0,
                YedekYuzde = yedekStats is { ToplamUrun: > 0 }
                    ? (int)Math.Floor((decimal)yedekStats.TamamlananUrun / yedekStats.ToplamUrun * 100) : 0
            };
        }
    }
}
