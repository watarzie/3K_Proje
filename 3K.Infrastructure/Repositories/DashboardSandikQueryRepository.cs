using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Repositories
{
    public class DashboardSandikQueryRepository : IDashboardSandikQueryRepository
    {
        private readonly AppDbContext _context;

        public DashboardSandikQueryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSandikDrillDownSonucu> GetProjeSandiklariAsync(
            DashboardSandikDrillDownFiltresi filtre,
            CancellationToken cancellationToken = default)
        {
            var projeBulundu = await _context.Projeler
                .AsNoTracking()
                .AnyAsync(proje => proje.Id == filtre.ProjeId, cancellationToken);

            if (!projeBulundu)
            {
                return new DashboardSandikDrillDownSonucu
                {
                    ProjeBulundu = false
                };
            }

            var query = _context.Sandiklar
                .AsNoTracking()
                .Where(sandik =>
                    sandik.ProjeId == filtre.ProjeId &&
                    sandik.DurumId == filtre.DurumId);

            if (!string.IsNullOrWhiteSpace(filtre.SearchTerm))
            {
                var searchPattern = $"%{EscapeIlikePattern(filtre.SearchTerm.Trim())}%";
                query = query.Where(sandik =>
                    EF.Functions.ILike(sandik.SandikNo, searchPattern, "\\"));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(sandik => sandik.SandikNo)
                .ThenBy(sandik => sandik.Id)
                .Skip((filtre.Page - 1) * filtre.PageSize)
                .Take(filtre.PageSize)
                .Select(sandik => new DashboardSandikDrillDownSatiri
                {
                    SandikId = sandik.Id,
                    SandikNo = sandik.SandikNo,
                    SandikAdi = sandik.Ad,
                    DurumId = sandik.DurumId,
                    DurumMetni = sandik.DurumLookup != null
                        ? sandik.DurumLookup.Deger
                        : string.Empty,
                    DepoLokasyonId = sandik.DepoLokasyonId,
                    DepoLokasyonMetni = sandik.DepoLokasyonLookup != null
                        ? sandik.DepoLokasyonLookup.Deger
                        : string.Empty
                })
                .ToListAsync(cancellationToken);

            return new DashboardSandikDrillDownSonucu
            {
                ProjeBulundu = true,
                Items = items,
                TotalCount = totalCount
            };
        }

        private static string EscapeIlikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }
    }
}
