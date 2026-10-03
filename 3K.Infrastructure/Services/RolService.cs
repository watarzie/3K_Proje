using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    public class RolService : IRolService
    {
        private readonly AppDbContext _context;

        // Scoped servis: bir istek içindeki bütün yetki kontrolleri aynı anlık görüntüyü kullanır.
        // Sonraki istek yeni örnek alır ve güncel rol/kişisel karar kayıtlarını yeniden okur.
        private readonly Dictionary<int, KullaniciYetkiSeti> _kullaniciYetkileri = new();

        public RolService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<MenuTanimi>> GetMenuAgaciAsync(CancellationToken ct = default)
        {
            var menus = await _context.MenuTanimlari.AsNoTracking()
                .OrderBy(m => m.Sira).ToListAsync(ct);
            var byId = menus.ToDictionary(m => m.Id);
            var roots = new List<MenuTanimi>();
            foreach (var menu in menus)
            {
                if (menu.ParentId == null)
                    roots.Add(menu);
                else if (byId.TryGetValue(menu.ParentId.Value, out var parent))
                    parent.Children.Add(menu);
            }
            return roots;
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
            var mevcutYetkiler = await _context.RolYetkileri
                .Where(ry => ry.RolId == rolId)
                .ToListAsync(ct);

            _context.RolYetkileri.RemoveRange(mevcutYetkiler);

            // N kaydedilmez; kaydı olmayan düğüm zaten N'dir.
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
            _kullaniciYetkileri.Clear();
        }

        public async Task<bool> HasUserPermissionAsync(int userId, string menuKod, YetkiTipi requiredYetkiTipi, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(menuKod))
                return false;

            if (!_kullaniciYetkileri.TryGetValue(userId, out var yetkiler))
            {
                yetkiler = await KullaniciYetkileriniOkuAsync(userId, ct);
                _kullaniciYetkileri[userId] = yetkiler;
            }

            return yetkiler.Saglar(menuKod, requiredYetkiTipi);
        }

        private async Task<KullaniciYetkiSeti> KullaniciYetkileriniOkuAsync(int userId, CancellationToken ct)
        {
            var rolId = await _context.Kullanicilar.AsNoTracking()
                .Where(k => k.Id == userId)
                .Select(k => (int?)k.RolId)
                .FirstOrDefaultAsync(ct);
            if (rolId == null)
                return KullaniciYetkiSeti.Bos;

            var menuler = await _context.MenuTanimlari.AsNoTracking()
                .Select(x => new YetkiDugumu(x.Id, x.Kod, x.ParentId))
                .ToListAsync(ct);
            var rolAtamalari = await _context.RolYetkileri.AsNoTracking()
                .Where(x => x.RolId == rolId)
                .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId, ct);
            var agac = new YetkiAgaci(menuler);
            var seviyeler = agac.RolSeviyeleri(rolAtamalari);
            return new KullaniciYetkiSeti(menuler.ToDictionary(x => x.Kod, x => seviyeler[x.Id]));
        }
    }
}
