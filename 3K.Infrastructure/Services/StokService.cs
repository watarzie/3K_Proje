using _3K.Core.Entities;
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
                                         s.KaynakProje.ToLower().Contains(lowerSearch));
            }

            query = query.OrderByDescending(x => x.Id);

            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, count);
        }

        public async Task<IEnumerable<StokKaydi>> GetUygunStoklarAsync(string? searchTerm = null)
        {
            var query = _context.StokKayitlari
                .Where(s => s.Durum == "Aktif" && s.Miktar > 0);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(s => s.MalzemeKodu.ToLower().Contains(lowerSearch) || 
                                         s.MalzemeAdi.ToLower().Contains(lowerSearch));
            }

            return await query.OrderBy(s => s.MalzemeKodu).ToListAsync();
        }

        public async Task<StokKaydi?> GetStokByIdAsync(int stokKaydiId)
        {
            return await _context.StokKayitlari.FindAsync(stokKaydiId);
        }

        public async Task<bool> StokYeterliMi(int stokKaydiId, int miktar)
        {
            var stok = await _context.StokKayitlari.FindAsync(stokKaydiId);
            return stok != null && stok.Miktar >= miktar;
        }

        public async Task<bool> StokDusAsync(int stokKaydiId, int miktar)
        {
            var repo = _unitOfWork.GetRepository<StokKaydi>();
            var stok = await repo.GetByIdAsync(stokKaydiId);
            if (stok == null || stok.Miktar < miktar) return false;

            stok.Miktar -= miktar;
            if (stok.Miktar == 0)
                stok.Durum = "Tukendi";

            repo.Update(stok);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi)
        {
            var repo = _unitOfWork.GetRepository<StokKaydi>();
            stokKaydi.Durum = "Aktif";
            await repo.AddAsync(stokKaydi);
            await _unitOfWork.SaveChangesAsync();
            return stokKaydi;
        }

        public async Task<IEnumerable<StokKaydi>> GetTumStoklarAsync()
        {
            return await _context.StokKayitlari.OrderBy(s => s.MalzemeKodu).ToListAsync();
        }
    }
}
