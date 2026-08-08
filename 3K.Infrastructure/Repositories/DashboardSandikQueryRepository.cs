using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Enums;
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

            var sahaUzerindenSevkEdilenSandikIds = filtre.SahaUzerindenSevkEdilenSandikIds
                .Distinct()
                .ToList();
            var query = _context.Sandiklar
                .AsNoTracking()
                .Where(sandik => sandik.ProjeId == filtre.ProjeId);

            if (sahaUzerindenSevkEdilenSandikIds.Count == 0)
            {
                query = query.Where(sandik => sandik.DurumId == filtre.DurumId);
            }
            else if (filtre.DurumId == (int)SandikDurum.Sevkedildi)
            {
                query = query.Where(sandik =>
                    sandik.DurumId == (int)SandikDurum.Sevkedildi ||
                    sahaUzerindenSevkEdilenSandikIds.Contains(sandik.Id));
            }
            else
            {
                query = query.Where(sandik =>
                    sandik.DurumId == filtre.DurumId &&
                    !sahaUzerindenSevkEdilenSandikIds.Contains(sandik.Id));
            }

            if (!string.IsNullOrWhiteSpace(filtre.SearchTerm))
            {
                var searchPattern = $"%{EscapeIlikePattern(filtre.SearchTerm.Trim())}%";
                query = query.Where(sandik =>
                    EF.Functions.ILike(sandik.SandikNo, searchPattern, "\\"));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var etkinDurumMetni = await _context.LookupSandikDurumlari
                .AsNoTracking()
                .Where(durum => durum.Id == filtre.DurumId)
                .Select(durum => durum.Deger)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
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
                    DurumId = filtre.DurumId,
                    DurumMetni = etkinDurumMetni,
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
