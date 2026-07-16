using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public sealed class SahaAktarimSilmeKorumaService : ISahaAktarimSilmeKorumaService
    {
        private readonly AppDbContext _context;

        public SahaAktarimSilmeKorumaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HashSet<int>> GetAktifAktarimBagliSandikIdsAsync(
            IEnumerable<int> sandikIds,
            CancellationToken cancellationToken = default)
        {
            var ids = NormalizeIds(sandikIds);
            if (ids.Count == 0)
                return new HashSet<int>();

            var dogrudanBagliKalemler = await AktifKalemler()
                .Where(k =>
                    (k.KaynakSandikId.HasValue && ids.Contains(k.KaynakSandikId.Value)) ||
                    (k.SahaSandikId.HasValue && ids.Contains(k.SahaSandikId.Value)))
                .Select(k => new
                {
                    k.KaynakSandikId,
                    k.SahaSandikId
                })
                .ToListAsync(cancellationToken);

            var bagliSandikIds = dogrudanBagliKalemler
                .SelectMany(k => new[] { k.KaynakSandikId, k.SahaSandikId })
                .Where(id => id.HasValue && ids.Contains(id.Value))
                .Select(id => id!.Value)
                .ToHashSet();

            // Ürün başka bir sandığa taşınmışsa defterdeki SahaSandikId eski kalabilir.
            // Bu nedenle mevcut içerik -> çeki satırı ilişkisi de esas kaynak olarak taranır.
            var icerikUzerindenBagliIds = await _context.SandikIcerikleri
                .AsNoTracking()
                .Where(i =>
                    ids.Contains(i.SandikId) &&
                    i.CekiSatiriId.HasValue &&
                    (
                        _context.SahaAktarimKalemleri.Any(k =>
                            k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                            k.DurumId != (int)SahaAktarimDurum.Iptal &&
                            (
                                k.KaynakCekiSatiriId == i.CekiSatiriId.Value ||
                                k.SahaCekiSatiriId == i.CekiSatiriId.Value
                            )) ||
                        (
                            i.CekiSatiri != null &&
                            i.CekiSatiri.KaynakCekiSatiriId.HasValue &&
                            i.CekiSatiri.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                            !_context.SahaAktarimKalemleri.Any(k =>
                                k.SahaCekiSatiriId == i.CekiSatiriId.Value)
                        ) ||
                        _context.CekiSatirlari.Any(hedef =>
                            hedef.KaynakCekiSatiriId == i.CekiSatiriId.Value &&
                            hedef.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                            !_context.SahaAktarimKalemleri.Any(k =>
                                k.SahaCekiSatiriId == hedef.Id)
                        )
                    ))
                .Select(i => i.SandikId)
                .Distinct()
                .ToListAsync(cancellationToken);

            bagliSandikIds.UnionWith(icerikUzerindenBagliIds);
            return bagliSandikIds;
        }

        public async Task<HashSet<int>> GetAktifAktarimBagliCekiSatiriIdsAsync(
            IEnumerable<int> cekiSatiriIds,
            CancellationToken cancellationToken = default)
        {
            var ids = NormalizeIds(cekiSatiriIds);
            if (ids.Count == 0)
                return new HashSet<int>();

            var aktifKalemler = AktifKalemler();
            var defterUzerindenBagliIds = await aktifKalemler
                .Where(k =>
                    ids.Contains(k.KaynakCekiSatiriId) ||
                    (k.SahaCekiSatiriId.HasValue && ids.Contains(k.SahaCekiSatiriId.Value)))
                .Select(k => new
                {
                    k.KaynakCekiSatiriId,
                    k.SahaCekiSatiriId
                })
                .ToListAsync(cancellationToken);

            var bagliSatirIds = defterUzerindenBagliIds
                .SelectMany(k => new int?[] { k.KaynakCekiSatiriId, k.SahaCekiSatiriId })
                .Where(id => id.HasValue && ids.Contains(id.Value))
                .Select(id => id!.Value)
                .ToHashSet();

            // Defter altyapısından önce üretilmiş saha aktarım satırlarını da koru.
            // Defterde temsil edilen satırlar burada tekrar legacy sayılmaz.
            var legacyHedefIds = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(cs =>
                    ids.Contains(cs.Id) &&
                    cs.KaynakCekiSatiriId.HasValue &&
                    cs.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                    !_context.SahaAktarimKalemleri.Any(k => k.SahaCekiSatiriId == cs.Id))
                .Select(cs => cs.Id)
                .ToListAsync(cancellationToken);

            bagliSatirIds.UnionWith(legacyHedefIds);

            var legacyKaynakIds = await _context.CekiSatirlari
                .AsNoTracking()
                .Where(hedef =>
                    hedef.KaynakCekiSatiriId.HasValue &&
                    ids.Contains(hedef.KaynakCekiSatiriId.Value) &&
                    hedef.Ceki.Proje.ProjeTipiId == (int)ProjeTipi.Saha &&
                    !_context.SahaAktarimKalemleri.Any(k => k.SahaCekiSatiriId == hedef.Id))
                .Select(hedef => hedef.KaynakCekiSatiriId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            bagliSatirIds.UnionWith(legacyKaynakIds);
            return bagliSatirIds;
        }

        private IQueryable<SahaAktarimKalemi> AktifKalemler()
        {
            return _context.SahaAktarimKalemleri
                .AsNoTracking()
                .Where(k =>
                    k.DurumId != (int)SahaAktarimDurum.GeriAlindi &&
                    k.DurumId != (int)SahaAktarimDurum.Iptal);
        }

        private static List<int> NormalizeIds(IEnumerable<int> ids)
        {
            return ids
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
    }
}
