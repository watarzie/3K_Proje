using _3K.Core.Constants;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class OnayYetkiService : IOnayYetkiService
    {
        private readonly AppDbContext _context;

        public OnayYetkiService(AppDbContext context)
        {
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

            var kapsam = await GetErisimKapsamiAsync(kullaniciId, ct);
            if (kapsam.TumIslemler)
                return true;

            if (!kapsam.KendiTalepleriniOnaylayabilir && kullaniciId == talepEdenKullaniciId)
                return false;

            var normalizedIslemKodu = string.IsNullOrWhiteSpace(islemKodu)
                ? OnayIslemKodlari.Genel
                : islemKodu.Trim();

            return kapsam.IslemKodlari.Contains(normalizedIslemKodu, StringComparer.Ordinal);
        }

        public async Task<OnayErisimKapsami> GetErisimKapsamiAsync(
            int kullaniciId,
            CancellationToken ct = default)
        {
            if (kullaniciId <= 0)
                return new OnayErisimKapsami();

            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .Where(k => k.Id == kullaniciId)
                .Select(k => new
                {
                    k.RolId,
                    RolAdi = k.Rol != null ? k.Rol.Ad : null
                })
                .FirstOrDefaultAsync(ct);

            if (kullanici == null)
                return new OnayErisimKapsami();

            var adminMi = kullanici.RolId == 1 ||
                          string.Equals(kullanici.RolAdi, "Admin", StringComparison.OrdinalIgnoreCase);

            if (adminMi)
            {
                return new OnayErisimKapsami
                {
                    TumIslemler = true,
                    KendiTalepleriniOnaylayabilir = true
                };
            }

            var islemKodlari = await _context.OnayIslemYetkileri
                .AsNoTracking()
                .Where(y => y.RolId == kullanici.RolId)
                .Select(y => y.IslemKodu)
                .Distinct()
                .ToListAsync(ct);

            return new OnayErisimKapsami
            {
                TumIslemler = false,
                KendiTalepleriniOnaylayabilir = false,
                IslemKodlari = islemKodlari
            };
        }
    }
}
