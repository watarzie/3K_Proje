using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class HareketService : IHareketService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public HareketService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task HareketKaydetAsync(HareketGecmisi hareket)
        {
            var repo = _unitOfWork.GetRepository<HareketGecmisi>();
            hareket.Tarih = DateTime.UtcNow;
            await repo.AddAsync(hareket);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId)
        {
            return await _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Where(h => h.ProjeId == projeId)
                .OrderByDescending(h => h.Tarih)
                .ToListAsync();
        }

        public async Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId)
        {
            return await _context.HareketGecmisleri
                .Include(h => h.Kullanici)
                .Where(h => h.ReferansTipi == referansTipi && h.ReferansId == referansId)
                .OrderByDescending(h => h.Tarih)
                .ToListAsync();
        }
    }
}
