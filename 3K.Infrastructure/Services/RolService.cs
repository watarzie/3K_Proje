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

        public async Task<bool> IsAdminAsync(int userId, CancellationToken ct = default)
        {
            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Include(k => k.Rol)
                .FirstOrDefaultAsync(k => k.Id == userId, ct);

            return kullanici?.RolId == 1 ||
                string.Equals(kullanici?.Rol?.Ad, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(menuKod))
                return false;

            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Id == userId)
                .Select(k => new
                {
                    k.RolId
                })
                .FirstOrDefaultAsync(ct);

            if (kullanici == null)
                return false;

            var requiredYetkiTipiId = (int)requiredYetkiTipi;

            return await _context.RolYetkileri
                .AsNoTracking()
                .AnyAsync(ry =>
                    ry.RolId == kullanici.RolId &&
                    ry.MenuTanimi.Kod == menuKod &&
                    ry.YetkiTipiId >= requiredYetkiTipiId,
                    ct);
        }
    }
}
