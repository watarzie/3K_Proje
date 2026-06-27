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

        public async Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            return await GetGerceklesenTamamlamaMapAsync(
                kaynakCekiSatiriIds,
                sadeceSevkEdilenSandiklar: false,
                cancellationToken);
        }

        public async Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(
            IEnumerable<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            return await GetGerceklesenTamamlamaMapAsync(
                kaynakCekiSatiriIds,
                sadeceSevkEdilenSandiklar: true,
                cancellationToken);
        }

        public async Task<bool> AktifTamamlamaVarMiAsync(
            int kaynakCekiSatiriId,
            CancellationToken cancellationToken = default)
        {
            if (kaynakCekiSatiriId <= 0)
                return false;

            var yeniKayitVar = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .AnyAsync(k =>
                    k.KaynakCekiSatiriId == kaynakCekiSatiriId &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal,
                    cancellationToken);

            if (yeniKayitVar)
                return true;

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

            var yeniSatirIds = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    kaynakIds.Contains(k.KaynakCekiSatiriId) &&
                    k.AktarimTipiId == (int)SahaAktarimTipi.SandikBazli &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal)
                .Select(k => k.KaynakCekiSatiriId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var legacySatirIds = await _context.CekiSatirlari
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

            return yeniSatirIds
                .Concat(legacySatirIds)
                .Distinct()
                .ToHashSet();
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

            var yeniMap = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    kaynakIds.Contains(k.KaynakCekiSatiriId) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal &&
                    (!sadeceSevkEdilenSandiklar || k.SevkiyatKapsamindaMi))
                .GroupBy(k => k.KaynakCekiSatiriId)
                .Select(g => new
                {
                    CekiSatiriId = g.Key,
                    TamamlananAdet = g.Sum(k => k.Miktar)
                })
                .ToDictionaryAsync(x => x.CekiSatiriId, x => x.TamamlananAdet, cancellationToken);

            var temsilEdilenSahaSatirIds = await GetYeniDefterdekiSahaSatirIdsAsync(kaynakIds, cancellationToken);

            var legacyQuery = _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kaynakIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                    !temsilEdilenSahaSatirIds.Contains(cs.Id) &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha);

            if (sadeceSevkEdilenSandiklar)
                legacyQuery = legacyQuery.Where(cs => cs.SandikIcerikleri.Any(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi));

            var legacyMap = await legacyQuery
                .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
                .Select(g => new
                {
                    CekiSatiriId = g.Key,
                    TamamlananAdet = g.Sum(cs => cs.IstenenAdet)
                })
                .ToDictionaryAsync(x => x.CekiSatiriId, x => x.TamamlananAdet, cancellationToken);

            return MergeMaps(yeniMap, legacyMap);
        }

        private async Task<Dictionary<int, decimal>> GetGerceklesenTamamlamaMapAsync(
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

            var yeniSatirlar = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    kaynakIds.Contains(k.KaynakCekiSatiriId) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal &&
                    (!sadeceSevkEdilenSandiklar || k.SevkiyatKapsamindaMi))
                .Select(k => new SahaTamamlamaSatirRow
                {
                    KaynakCekiSatiriId = k.KaynakCekiSatiriId,
                    IstenenAdet = k.Miktar,
                    KonulanAdet = k.SahaCekiSatiri == null
                        ? 0
                        : (k.SahaCekiSatiri.SandikIcerikleri.Sum(si => (decimal?)si.KonulanAdet) ?? 0)
                })
                .ToListAsync(cancellationToken);

            var yeniMap = yeniSatirlar
                .GroupBy(s => s.KaynakCekiSatiriId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(HesaplaGerceklesenTamamlama));

            var temsilEdilenSahaSatirIds = await GetYeniDefterdekiSahaSatirIdsAsync(kaynakIds, cancellationToken);

            var legacyQuery = _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    cs.KaynakCekiSatiriId.HasValue &&
                    kaynakIds.Contains(cs.KaynakCekiSatiriId.Value) &&
                    !temsilEdilenSahaSatirIds.Contains(cs.Id) &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha);

            if (sadeceSevkEdilenSandiklar)
                legacyQuery = legacyQuery.Where(cs => cs.SandikIcerikleri.Any(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi));

            var legacySatirlar = await legacyQuery
                .Select(cs => new SahaTamamlamaSatirRow
                {
                    KaynakCekiSatiriId = cs.KaynakCekiSatiriId!.Value,
                    IstenenAdet = cs.IstenenAdet,
                    KonulanAdet = sadeceSevkEdilenSandiklar
                        ? (cs.SandikIcerikleri
                            .Where(si => si.Sandik.DurumId == (int)SandikDurum.Sevkedildi)
                            .Sum(si => (decimal?)si.KonulanAdet) ?? 0)
                        : (cs.SandikIcerikleri.Sum(si => (decimal?)si.KonulanAdet) ?? 0)
                })
                .ToListAsync(cancellationToken);

            var legacyMap = legacySatirlar
                .GroupBy(s => s.KaynakCekiSatiriId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(HesaplaGerceklesenTamamlama));

            return MergeMaps(yeniMap, legacyMap);
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

            await SenkronizeSahaAktarimKalemleriBySahaSandikIdsInternalAsync(sandikIds, cancellationToken);

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

            var defterKaynakSatirIds = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    k.SahaSandikId.HasValue &&
                    sandikIds.Contains(k.SahaSandikId.Value) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal)
                .Select(k => k.KaynakCekiSatiriId)
                .Distinct()
                .ToListAsync(cancellationToken);

            await SenkronizeKaynakProjelerAsync(kaynakSatirIds.Concat(defterKaynakSatirIds), cancellationToken);
        }

        private async Task<HashSet<int>> GetYeniDefterdekiSahaSatirIdsAsync(
            IReadOnlyCollection<int> kaynakIds,
            CancellationToken cancellationToken)
        {
            if (kaynakIds.Count == 0)
                return new HashSet<int>();

            var ids = await _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    kaynakIds.Contains(k.KaynakCekiSatiriId) &&
                    k.SahaCekiSatiriId.HasValue)
                .Select(k => k.SahaCekiSatiriId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            return ids.ToHashSet();
        }

        private async Task SenkronizeSahaAktarimKalemleriBySahaSandikIdsInternalAsync(
            IReadOnlyCollection<int> sahaSandikIds,
            CancellationToken cancellationToken)
        {
            if (sahaSandikIds.Count == 0)
                return;

            var kalemler = await _context.SahaAktarimKalemleri
                .Include(k => k.SahaSandik)
                .Include(k => k.SahaCekiSatiri)
                    .ThenInclude(cs => cs!.SandikIcerikleri)
                        .ThenInclude(si => si.Sandik)
                .Where(k =>
                    k.SahaSandikId.HasValue &&
                    sahaSandikIds.Contains(k.SahaSandikId.Value) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal)
                .ToListAsync(cancellationToken);

            var degisimVar = false;
            foreach (var kalem in kalemler)
            {
                var yeniDurum = HesaplaSahaAktarimDurumu(kalem);
                var sevkiyatKapsaminda = yeniDurum == (int)SahaAktarimDurum.SevkEdildi ||
                    yeniDurum == (int)SahaAktarimDurum.SevkiyatDuzeltmede;
                var duzeltmede = yeniDurum == (int)SahaAktarimDurum.SevkiyatDuzeltmede;

                if (kalem.DurumId != yeniDurum ||
                    kalem.SevkiyatKapsamindaMi != sevkiyatKapsaminda ||
                    kalem.DuzeltmeyeAcikMi != duzeltmede)
                {
                    kalem.DurumId = yeniDurum;
                    kalem.SevkiyatKapsamindaMi = sevkiyatKapsaminda;
                    kalem.DuzeltmeyeAcikMi = duzeltmede;
                    degisimVar = true;
                }

                if (sevkiyatKapsaminda && !kalem.SevkTarihi.HasValue)
                {
                    kalem.SevkTarihi = TurkeyTime.Now;
                    degisimVar = true;
                }

                if (!sevkiyatKapsaminda && kalem.SevkTarihi.HasValue)
                {
                    kalem.SevkTarihi = null;
                    degisimVar = true;
                }
            }

            if (degisimVar)
                await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SenkronizeSahaAktarimKalemleriByKaynakSatirIdsInternalAsync(
            IReadOnlyCollection<int> kaynakCekiSatiriIds,
            CancellationToken cancellationToken)
        {
            if (kaynakCekiSatiriIds.Count == 0)
                return;

            var kalemler = await _context.SahaAktarimKalemleri
                .Include(k => k.SahaSandik)
                .Include(k => k.SahaCekiSatiri)
                    .ThenInclude(cs => cs!.SandikIcerikleri)
                        .ThenInclude(si => si.Sandik)
                .Where(k =>
                    kaynakCekiSatiriIds.Contains(k.KaynakCekiSatiriId) &&
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal)
                .ToListAsync(cancellationToken);

            var degisimVar = false;
            foreach (var kalem in kalemler)
            {
                var yeniDurum = HesaplaSahaAktarimDurumu(kalem);
                var sevkiyatKapsaminda = yeniDurum == (int)SahaAktarimDurum.SevkEdildi ||
                    yeniDurum == (int)SahaAktarimDurum.SevkiyatDuzeltmede;
                var duzeltmede = yeniDurum == (int)SahaAktarimDurum.SevkiyatDuzeltmede;

                if (kalem.DurumId != yeniDurum ||
                    kalem.SevkiyatKapsamindaMi != sevkiyatKapsaminda ||
                    kalem.DuzeltmeyeAcikMi != duzeltmede)
                {
                    kalem.DurumId = yeniDurum;
                    kalem.SevkiyatKapsamindaMi = sevkiyatKapsaminda;
                    kalem.DuzeltmeyeAcikMi = duzeltmede;
                    degisimVar = true;
                }

                if (sevkiyatKapsaminda && !kalem.SevkTarihi.HasValue)
                {
                    kalem.SevkTarihi = TurkeyTime.Now;
                    degisimVar = true;
                }

                if (!sevkiyatKapsaminda && kalem.SevkTarihi.HasValue)
                {
                    kalem.SevkTarihi = null;
                    degisimVar = true;
                }
            }

            if (degisimVar)
                await _context.SaveChangesAsync(cancellationToken);
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

            await SenkronizeSahaAktarimKalemleriByKaynakSatirIdsInternalAsync(kaynakIds, cancellationToken);

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
            var aktifGerceklesenTamamlamaMap = await GetAktifGerceklesenTamamlamaMapAsync(tumKaynakSatirIds, cancellationToken);
            var sevkEdilenGerceklesenTamamlamaMap = await GetSevkEdilenGerceklesenTamamlamaMapAsync(tumKaynakSatirIds, cancellationToken);

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
                var sahaSevkiyleTamamlamaVar = projeSatirlari.Any(s => sevkEdilenGerceklesenTamamlamaMap.GetValueOrDefault(s.Id) > 0);
                var tumUrunlerTamamlandi = projeSatirlari.All(s => HesaplaEtkinKalan(s, aktifGerceklesenTamamlamaMap) <= 0);
                var tumUrunlerSevkKapsamindaTamamlandi = projeSatirlari.All(s => HesaplaEtkinKalan(s, sevkEdilenGerceklesenTamamlamaMap) <= 0);
                var normalSandikSevkiVar = (stats?.SevkEdilenSandik ?? 0) > 0;
                var sevkKaydiVar = normalSandikSevkiVar || sahaSevkiyleTamamlamaVar;
                var eskiDurum = proje.DurumId;

                if (sevkKaydiVar && tumUrunlerSevkKapsamindaTamamlandi)
                {
                    proje.DurumId = (int)ProjeDurum.SevkEdildi;
                }
                else if (sevkKaydiVar)
                {
                    proje.DurumId = (int)ProjeDurum.EksikSevkEdildi;
                }
                else if (tumUrunlerTamamlandi &&
                         stats is { ToplamSandik: > 0 } &&
                         stats.HazirSandik == stats.ToplamSandik)
                {
                    proje.DurumId = (int)ProjeDurum.Tamamlandi;
                }
                else if (proje.DurumId == (int)ProjeDurum.SevkEdildi ||
                         proje.DurumId == (int)ProjeDurum.EksikSevkEdildi ||
                         proje.DurumId == (int)ProjeDurum.Tamamlandi)
                {
                    proje.DurumId = (int)ProjeDurum.Hazirlaniyor;
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

        private static decimal HesaplaGerceklesenTamamlama(SahaTamamlamaSatirRow satir)
        {
            if (satir.IstenenAdet <= 0)
                return 0;

            return Math.Min(Math.Max(satir.KonulanAdet, 0), satir.IstenenAdet);
        }

        private static Dictionary<int, decimal> MergeMaps(
            IReadOnlyDictionary<int, decimal> primary,
            IReadOnlyDictionary<int, decimal> fallback)
        {
            var result = primary.ToDictionary(k => k.Key, v => v.Value);
            foreach (var item in fallback)
            {
                result[item.Key] = result.GetValueOrDefault(item.Key) + item.Value;
            }

            return result;
        }

        private static int HesaplaSahaAktarimDurumu(SahaAktarimKalemi kalem)
        {
            if (kalem.SahaSandik?.DurumId == (int)SandikDurum.Sevkedildi)
            {
                return kalem.SahaSandik.SevkiyatDuzeltmeAcikMi
                    ? (int)SahaAktarimDurum.SevkiyatDuzeltmede
                    : (int)SahaAktarimDurum.SevkEdildi;
            }

            var sahaSatir = kalem.SahaCekiSatiri;
            var icerikler = sahaSatir?.SandikIcerikleri ?? new List<SandikIcerik>();
            var konulanAdet = icerikler.Sum(i => i.KonulanAdet);

            if (kalem.Miktar > 0 && konulanAdet >= kalem.Miktar)
                return (int)SahaAktarimDurum.Tamamlandi;

            if (SahaSatiriIslemBasladiMi(sahaSatir, icerikler))
                return (int)SahaAktarimDurum.Hazirlaniyor;

            return (int)SahaAktarimDurum.Planlandi;
        }

        private static bool SahaSatiriIslemBasladiMi(
            CekiSatiri? satir,
            IEnumerable<SandikIcerik> icerikler)
        {
            if (satir == null)
                return icerikler.Any(i => i.KonulanAdet > 0 || i.EksikAdet > 0);

            return satir.GridDurumuId != (int)GridDurum.Gelmedi ||
                satir.GridGelenAdet > 0 ||
                satir.TrafoSevkAdet > 0 ||
                satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdilmedi ||
                (satir.GridSevkMiktari ?? 0) > 0 ||
                satir.YenidenSevkGerekliAdet > 0 ||
                satir.GelenMiktar > 0 ||
                satir.StokKarsilanan > 0 ||
                satir.ProjeKarsilanan > 0 ||
                satir.ProjeGonderilen > 0 ||
                satir.TedarikciKarsilanan > 0 ||
                satir.HataliMiktar > 0 ||
                satir.GeriGonderilenMiktar > 0 ||
                satir.UcKDurumuId != (int)UcKDurum.Bekliyor ||
                satir.UcKKarsilamaTipiId != (int)UcKDurum.Bekliyor ||
                icerikler.Any(i =>
                    i.KonulanAdet > 0 ||
                    i.EksikAdet > 0 ||
                    i.StokKarsilanan > 0 ||
                    i.ProjeKarsilanan > 0 ||
                    i.TedarikciKarsilanan > 0);
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

        private sealed class SahaTamamlamaSatirRow
        {
            public int KaynakCekiSatiriId { get; set; }
            public decimal IstenenAdet { get; set; }
            public decimal KonulanAdet { get; set; }
        }
    }
}
