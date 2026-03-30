using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class SandikService : ISandikService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public SandikService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<IEnumerable<Sandik>> GetProjeSandiklariAsync(int projeId)
        {
            return await _context.Sandiklar
                .Include(s => s.SandikIcerikleri)
                .Where(s => s.ProjeId == projeId)
                .OrderBy(s => s.SandikNo)
                .ToListAsync();
        }

        public async Task<Sandik?> GetSandikDetayAsync(int sandikId)
        {
            return await _context.Sandiklar
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.Paketleyen)
                .Include(s => s.SandikIcerikleri)
                    .ThenInclude(si => si.CekiSatiri)
                        .ThenInclude(cs => cs.KontrolEden)
                .FirstOrDefaultAsync(s => s.Id == sandikId);
        }

        public async Task<Sandik?> GetSandikByNoAsync(int projeId, string sandikNo)
        {
            return await _context.Sandiklar
                .FirstOrDefaultAsync(s => s.ProjeId == projeId && s.SandikNo == sandikNo);
        }

        public async Task<Sandik> SandikOlusturAsync(int projeId, string sandikNo, DepoLokasyon depoLokasyonu = DepoLokasyon.Belirsiz)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandik = new Sandik
            {
                ProjeId = projeId,
                SandikNo = sandikNo,
                Durum = SandikDurum.Hazirlaniyor,
                DepoLokasyonu = depoLokasyonu
            };
            await sandikRepo.AddAsync(sandik);
            await _unitOfWork.SaveChangesAsync();
            return sandik;
        }

        public async Task<bool> SandikDegistirAsync(int cekiSatiriId, int yeniSandikId, int kullaniciId, int projeId)
        {
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            var eskiIcerikler = await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == cekiSatiriId);
            var eskiIcerik = eskiIcerikler.FirstOrDefault();

            if (eskiIcerik != null)
            {
                sandikIcerikRepo.Remove(eskiIcerik);
            }

            var yeniIcerik = new SandikIcerik
            {
                SandikId = yeniSandikId,
                CekiSatiriId = cekiSatiriId,
                KonulanAdet = eskiIcerik?.KonulanAdet ?? 0,
                EksikAdet = eskiIcerik?.EksikAdet ?? 0
            };
            await sandikIcerikRepo.AddAsync(yeniIcerik);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<SandikIcerik>> GetSandikIcerikAsync(int sandikId)
        {
            return await _context.SandikIcerikleri
                .Include(si => si.CekiSatiri)
                    .ThenInclude(cs => cs.Paketleyen)
                .Include(si => si.CekiSatiri)
                    .ThenInclude(cs => cs.KontrolEden)
                .Where(si => si.SandikId == sandikId)
                .ToListAsync();
        }
    }
}
