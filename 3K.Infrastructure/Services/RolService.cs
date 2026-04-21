using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class RolService : IRolService
    {
        private readonly AppDbContext _context;

        public RolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default)
        {
            return await _context.MenuTanimlari
                .AsNoTracking()
                .Include(m => m.Children)
                .Where(m => m.ParentId == null)
                .OrderBy(m => m.Sira)
                .ToListAsync(ct);
        }

        public async Task<List<RolYetki>> GetRolYetkileriAsync(int rolId, CancellationToken ct = default)
        {
            return await _context.RolYetkileri
                .AsNoTracking()
                .Where(ry => ry.RolId == rolId)
                .ToListAsync(ct);
        }

        public async Task YetkileriGuncelleAsync(int rolId, List<RolYetki> yeniYetkiler, CancellationToken ct = default)
        {
            // Mevcut yetkileri sil
            var mevcutYetkiler = await _context.RolYetkileri
                .Where(ry => ry.RolId == rolId)
                .ToListAsync(ct);

            _context.RolYetkileri.RemoveRange(mevcutYetkiler);

            // Yeni yetkileri ekle (sadece W ve R olanlar — N olanlar kayıt edilmez)
            var kaydedilecek = yeniYetkiler
                .Where(y => y.YetkiTipiId == (int)YetkiTipi.W || y.YetkiTipiId == (int)YetkiTipi.R)
                .Select(y => new RolYetki
                {
                    RolId = rolId,
                    MenuTanimiId = y.MenuTanimiId,
                    YetkiTipiId = y.YetkiTipiId
                })
                .ToList();

            await _context.RolYetkileri.AddRangeAsync(kaydedilecek, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> HasPermissionAsync(string[] userRoles, string menuKod, string yetkiTipi = "W", CancellationToken ct = default)
        {
            if (userRoles == null || userRoles.Length == 0) return false;

            if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase)) return true;

            var roles = await _context.Roller
                .Where(r => userRoles.Contains(r.Ad))
                .ToListAsync(ct);

            if (!roles.Any()) return false;

            var roleIds = roles.Select(r => r.Id).ToList();

            // Map string yetkiTipi to int for backward compatibility
            int yetkiTipiId = yetkiTipi switch
            {
                "W" => (int)YetkiTipi.W,
                "R" => (int)YetkiTipi.R,
                _ => (int)YetkiTipi.N
            };

            var hasPermission = await _context.RolYetkileri
                .Include(ry => ry.MenuTanimi)
                .AnyAsync(ry => roleIds.Contains(ry.RolId) &&
                                ry.MenuTanimi.Kod == menuKod &&
                                ry.YetkiTipiId >= yetkiTipiId, ct);

            return hasPermission;
        }
    }
}
