using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Constants;
using _3K.Core.Models;
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

            var node = await _context.MenuTanimlari.AsNoTracking()
                .Where(x => x.Kod == menuKod)
                .Select(x => new { x.Id, x.Kod, x.ParentId }).SingleOrDefaultAsync(ct);
            if (node == null) return false;

            var path = new List<(int Id, string Kod)>();
            var visited = new HashSet<int>();
            while (true)
            {
                if (!visited.Add(node.Id)) return false;
                path.Add((node.Id, node.Kod));
                if (!node.ParentId.HasValue) break;
                node = await _context.MenuTanimlari.AsNoTracking()
                    .Where(x => x.Id == node.ParentId.Value)
                    .Select(x => new { x.Id, x.Kod, x.ParentId }).SingleOrDefaultAsync(ct);
                if (node == null) return false;
            }

            var pathIds = path.Select(x => x.Id).ToArray();
            var roleLevels = await _context.RolYetkileri.AsNoTracking()
                .Where(x => x.RolId == kullanici.RolId && pathIds.Contains(x.MenuTanimiId))
                .ToDictionaryAsync(x => x.MenuTanimiId, x => x.YetkiTipiId, ct);
            var decisions = await _context.KullaniciYetkileri.AsNoTracking()
                .Where(x => x.KullaniciId == userId && pathIds.Contains(x.MenuTanimiId))
                .ToDictionaryAsync(x => x.MenuTanimiId, x => x.IzinVerildi, ct);
            var roleEffective = new Dictionary<int, int>();
            var inherited = (int)YetkiTipi.W;
            foreach (var menu in path.AsEnumerable().Reverse())
            {
                inherited = YetkiDegerlendirici.UstSinirliYetki(menu.Kod,
                    roleLevels.GetValueOrDefault(menu.Id, (int)YetkiTipi.N), inherited);
                roleEffective[menu.Id] = inherited;
            }
            inherited = (int)YetkiTipi.W;
            foreach (var menu in path.AsEnumerable().Reverse())
            {
                var local = YetkiDegerlendirici.EtkinYetki(roleEffective[menu.Id],
                    decisions.TryGetValue(menu.Id, out var granted) ? granted : null,
                    (int)(YetkiKatalogu.Bul(menu.Kod)?.GerekenYetki ?? YetkiTipi.W));
                inherited = YetkiDegerlendirici.UstSinirliYetki(menu.Kod, local, inherited);
            }
            return inherited >= (int)requiredYetkiTipi;
        }
    }
}
