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
                .Include(p => p.Cekiler)
                    .ThenInclude(c => c.CekiSatirlari)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}
