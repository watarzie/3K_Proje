using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
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

        /// <summary>
        /// Dropdown'lar için hafif proje listesi — Include yok, sadece Proje tablosu.
        /// </summary>
        public async Task<IEnumerable<Proje>> GetAllLightAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Projeler
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
