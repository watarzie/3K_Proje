using Microsoft.EntityFrameworkCore;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class SahaTamamlamaService : ISahaTamamlamaService
    {
        private readonly AppDbContext _context;

        public SahaTamamlamaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            return await GetTamamlamaMapAsync(
                kaynakCekiSatiriIds,
                sadeceSevkEdilenSandiklar: true,
                cancellationToken);
        }

        public async Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            return await GetTamamlamaMapAsync(
                kaynakCekiSatiriIds,
                sadeceSevkEdilenSandiklar: false,
                cancellationToken);
        }

        public async Task<bool> AktifTamamlamaVarMiAsync(
            int kaynakCekiSatiriId,
            CancellationToken cancellationToken = default)
        {
            if (kaynakCekiSatiriId <= 0)
                return false;

            return await _context.CekiSatirlari
                .AsNoTracking()
                .AnyAsync(cs =>
                    cs.KaynakCekiSatiriId == kaynakCekiSatiriId &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha,
                    cancellationToken);
        }

        public async Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            var kaynakIds = kaynakCekiSatiriIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (kaynakIds.Count == 0)
                return new HashSet<int>();

            var satirIds = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kaynakIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                    cs.UcKAciklama != null &&
                    cs.UcKAciklama.StartsWith(SahaAktarimConstants.SandikBazliAktarimAciklamaPrefix))
                .Select(cs => cs.KaynakCekiSatiriId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return satirIds.ToHashSet();
        }

        private async Task<Dictionary<int, decimal>> GetTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            bool sadeceSevkEdilenSandiklar,
            CancellationToken cancellationToken)
        {
            var kaynakIds = kaynakCekiSatiriIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (kaynakIds.Count == 0)
                return new Dictionary<int, decimal>();

            var query = _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kaynakIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha);

            if (sadeceSevkEdilenSandiklar)
                query = query.Where(cs => cs.SandikIcerikleri.Any(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi));

            return await query
                .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
                .Select(g => new
                {
                    CekiSatiriId = g.Key,
                    TamamlananAdet = g.Sum(cs => cs.IstenenAdet)
                })
                .ToDictionaryAsync(x => x.CekiSatiriId, x => x.TamamlananAdet, cancellationToken);
        }

        public async Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(
            IEnumerable<int> sahaSandikIds,
            CancellationToken cancellationToken = default)
        {
            var sandikIds = sahaSandikIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (sandikIds.Count == 0)
                return;

            var kaynakSatirIds = await _context.SandikIcerikleri
                .AsNoTracking()
                .Where(si =>
                    sandikIds.Contains(si.SandikId) &&
                    si.Sandik.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                    si.CekiSatiri != null &&
                    si.CekiSatiri.KaynakCekiSatiriId.HasValue)
                .Select(si => si.CekiSatiri!.KaynakCekiSatiriId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            await SenkronizeKaynakProjelerAsync(kaynakSatirIds, cancellationToken);
        }

        public async Task SenkronizeKaynakProjelerAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            var kaynakIds = kaynakCekiSatiriIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (kaynakIds.Count == 0)
                return;

            var kaynakProjeIds = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs => kaynakIds.Contains(cs.Id))
                .Select(cs => cs.Ceki.ProjeId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (kaynakProjeIds.Count == 0)
                return;

            var kaynakSatirlar = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    kaynakProjeIds.Contains(cs.Ceki.ProjeId) &&
                    !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => new KaynakSatirDurumRow
                {
                    Id = cs.Id,
                    ProjeId = cs.Ceki.ProjeId,
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
                .ToListAsync(cancellationToken);

            if (kaynakSatirlar.Count == 0)
                return;

            var tumKaynakSatirIds = kaynakSatirlar.Select(s => s.Id).ToList();
            var sevkEdilenTamamlamaMap = await GetSevkEdilenTamamlamaMapAsync(tumKaynakSatirIds, cancellationToken);

            var sandikStats = await _context.Sandiklar
                .AsNoTracking()
                .Where(s => kaynakProjeIds.Contains(s.ProjeId))
                .GroupBy(s => s.ProjeId)
                .Select(g => new KaynakProjeSandikStats
                {
                    ProjeId = g.Key,
                    ToplamSandik = g.Count(),
                    SevkEdilenSandik = g.Count(s => s.DurumId == (int)SandikDurum.Sevkedildi),
                    HazirSandik = g.Count(s =>
                        s.DurumId == (int)SandikDurum.Kapandi ||
                        s.DurumId == (int)SandikDurum.Sevkedildi)
                })
                .ToDictionaryAsync(x => x.ProjeId, cancellationToken);

            var projeler = await _context.Projeler
                .Where(p => kaynakProjeIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var proje in projeler)
            {
                var projeSatirlari = kaynakSatirlar.Where(s => s.ProjeId == proje.Id).ToList();
                if (projeSatirlari.Count == 0)
                    continue;

                sandikStats.TryGetValue(proje.Id, out var stats);
                var sahaSevkiyleTamamlamaVar = projeSatirlari.Any(s => sevkEdilenTamamlamaMap.GetValueOrDefault(s.Id) > 0);
                var tumUrunlerTamamlandi = projeSatirlari.All(s => HesaplaEtkinKalan(s, sevkEdilenTamamlamaMap) <= 0);
                var eskiDurum = proje.DurumId;

                if (tumUrunlerTamamlandi)
                {
                    if ((stats?.SevkEdilenSandik ?? 0) > 0 ||
                        sahaSevkiyleTamamlamaVar ||
                        proje.DurumId == (int)ProjeDurum.SevkEdildi ||
                        proje.DurumId == (int)ProjeDurum.EksikSevkEdildi)
                    {
                        proje.DurumId = (int)ProjeDurum.SevkEdildi;
                    }
                    else if (stats is { ToplamSandik: > 0 } && stats.HazirSandik == stats.ToplamSandik)
                    {
                        proje.DurumId = (int)ProjeDurum.Tamamlandi;
                    }
                }
                else if (proje.DurumId == (int)ProjeDurum.SevkEdildi ||
                         proje.DurumId == (int)ProjeDurum.EksikSevkEdildi)
                {
                    proje.DurumId = (stats?.SevkEdilenSandik ?? 0) > 0 || sahaSevkiyleTamamlamaVar
                        ? (int)ProjeDurum.EksikSevkEdildi
                        : (int)ProjeDurum.Hazirlaniyor;
                }

                if (proje.DurumId != eskiDurum)
                    _context.Projeler.Update(proje);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static decimal HesaplaEtkinKalan(
            KaynakSatirDurumRow satir,
            IReadOnlyDictionary<int, decimal> sevkEdilenTamamlamaMap)
        {
            var hamKalan = CekiSatiriKalanHelper.HesaplaHamKalan(
                satir.IstenenAdet,
                satir.GelenMiktar,
                satir.StokKarsilanan,
                satir.ProjeKarsilanan,
                satir.TedarikciKarsilanan,
                satir.ProjeGonderilen,
                satir.TrafoSevkAdet,
                satir.HataliMiktar,
                satir.DurumId,
                satir.GridDurumuId);

            var sahaTamamlanan = sevkEdilenTamamlamaMap.TryGetValue(satir.Id, out var value) ? value : 0;
            return Math.Max(hamKalan - sahaTamamlanan, 0);
        }

        private sealed class KaynakSatirDurumRow
        {
            public int Id { get; set; }
            public int ProjeId { get; set; }
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

        private sealed class KaynakProjeSandikStats
        {
            public int ProjeId { get; set; }
            public int ToplamSandik { get; set; }
            public int SevkEdilenSandik { get; set; }
            public int HazirSandik { get; set; }
        }
    }
}
