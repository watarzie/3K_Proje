using _3K.Core.Constants;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class OnayYetkiService : IOnayYetkiService
    {
        private readonly IRolService _rolService;
        private readonly AppDbContext _context;

        public OnayYetkiService(IRolService rolService, AppDbContext context)
        {
            _rolService = rolService;
            _context = context;
        }

        public async Task<bool> KullaniciIslemOnaylayabilirMiAsync(
            int kullaniciId,
            string? islemKodu,
            int talepEdenKullaniciId,
            CancellationToken ct = default)
        {
            if (kullaniciId <= 0)
                return false;

            var adminMi = await _rolService.IsAdminAsync(kullaniciId, ct);
            if (adminMi)
                return true;

            if (kullaniciId == talepEdenKullaniciId)
                return false;

            var normalizedIslemKodu = string.IsNullOrWhiteSpace(islemKodu)
                ? OnayIslemKodlari.Genel
                : islemKodu.Trim();

            var rolId = await _context.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Id == kullaniciId)
                .Select(k => (int?)k.RolId)
                .FirstOrDefaultAsync(ct);

            if (!rolId.HasValue)
                return false;

            return await _context.OnayIslemYetkileri
                .AsNoTracking()
                .AnyAsync(y => y.RolId == rolId.Value && y.IslemKodu == normalizedIslemKodu, ct);
        }
    }
}
