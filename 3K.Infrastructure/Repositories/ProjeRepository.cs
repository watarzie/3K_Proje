using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Repositories
{
    public class ProjeRepository : IProjeRepository
    {
        private readonly AppDbContext _context;

        public ProjeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Proje>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Projeler
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.CekiSatiri)
                .Include(p => p.Cekiler)
                    .ThenInclude(c => c.CekiSatirlari)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Projeler.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<Proje>> GetPagedWithDetailsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var safePage = Math.Max(page, 1);
            var safePageSize = Math.Clamp(pageSize, 1, 100);

            return await _context.Projeler
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.CekiSatiri)
                .Include(p => p.Cekiler)
                    .ThenInclude(c => c.CekiSatirlari)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Proje> Items, int TotalCount)> GetFilteredPagedAsync(
            int? projeTipiId, string? searchTerm, bool? isSevkEdilen,
            int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Projeler.AsQueryable();

            if (projeTipiId.HasValue)
                query = query.Where(p => p.ProjeTipiId == projeTipiId.Value);

            if (isSevkEdilen.HasValue)
            {
                if (isSevkEdilen.Value)
                    query = query.Where(p =>
                        p.DurumId == (int)ProjeDurum.SevkEdildi ||
                        p.DurumId == (int)ProjeDurum.EksikSevkEdildi ||
                        p.Sandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi));
                else
                    query = query.Where(p => p.DurumId != (int)ProjeDurum.SevkEdildi); // EksikSevkEdildi aktif iş olarak kalır
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(p =>
                    p.ProjeNo.ToLower().Contains(lower) ||
                    p.Musteri.ToLower().Contains(lower));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var safePage = Math.Max(pageNumber, 1);
            var safePageSize = Math.Clamp(pageSize, 1, 100);

            var items = await query
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Include(p => p.Sandiklar)
                    .ThenInclude(s => s.SandikIcerikleri)
                        .ThenInclude(si => si.CekiSatiri)
                .Include(p => p.Cekiler)
                    .ThenInclude(c => c.CekiSatirlari)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Proje tablosu.
        /// </summary>
        public async Task<IEnumerable<Proje>> GetAllLightAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Projeler
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Proje>> GetLightFilteredAsync(
            int? projeTipiId,
            string? searchTerm,
            bool? isSevkEdilen,
            int take,
            IReadOnlyCollection<int>? includeIds,
            CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 100);
            var query = _context.Projeler.AsNoTracking().AsQueryable();

            if (projeTipiId.HasValue)
                query = query.Where(p => p.ProjeTipiId == projeTipiId.Value);

            if (isSevkEdilen.HasValue)
            {
                if (isSevkEdilen.Value)
                    query = query.Where(p =>
                        p.DurumId == (int)ProjeDurum.SevkEdildi ||
                        p.DurumId == (int)ProjeDurum.EksikSevkEdildi ||
                        p.Sandiklar.Any(s => s.DurumId == (int)SandikDurum.Sevkedildi));
                else
                    query = query.Where(p => p.DurumId != (int)ProjeDurum.SevkEdildi);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.ProjeNo.ToLower().Contains(lower) ||
                    p.Musteri.ToLower().Contains(lower) ||
                    (p.Lokasyon != null && p.Lokasyon.ToLower().Contains(lower)));
            }

            var filtered = await query
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .Take(safeTake)
                .ToListAsync(cancellationToken);

            var selectedIds = includeIds?
                .Where(id => id > 0)
                .Distinct()
                .ToArray() ?? [];

            if (selectedIds.Length == 0)
                return filtered;

            var mevcutIds = filtered.Select(p => p.Id).ToHashSet();
            var eksikSeciliIds = selectedIds.Where(id => !mevcutIds.Contains(id)).ToArray();
            if (eksikSeciliIds.Length == 0)
                return filtered;

            var included = await _context.Projeler
                .AsNoTracking()
                .Where(p => eksikSeciliIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            return filtered
                .Concat(included)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderByDescending(p => p.CreatedDate)
                .ThenByDescending(p => p.Id)
                .ToList();
        }
    }
}
