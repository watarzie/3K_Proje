using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class StokService : IStokService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public StokService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(string? searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.StokKayitlari.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(s => s.MalzemeKodu.ToLower().Contains(lowerSearch) || 
                                         s.MalzemeAdi.ToLower().Contains(lowerSearch) ||
                                         (s.KaynakProje != null && s.KaynakProje.ToLower().Contains(lowerSearch)));
            }

            query = query.OrderByDescending(x => x.Id);

            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, count);
        }

        public async Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi)
        {
            var repo = _unitOfWork.GetRepository<StokKaydi>();
            stokKaydi.DurumId = (int)StokDurum.Aktif;
            await repo.AddAsync(stokKaydi);
            await _unitOfWork.SaveChangesAsync();
            return stokKaydi;
        }
    }
}
