using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
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

        public async Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(
            string? searchTerm, int pageNumber, int pageSize, int? durumId = null)
        {
            var query = StokListeFiltresi.Uygula(_context.StokKayitlari.AsQueryable(), searchTerm, durumId)
                .OrderByDescending(x => x.Id);

            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, count);
        }

        public async Task<StokDurumSayilari> GetDurumSayilariAsync(string? searchTerm, CancellationToken cancellationToken = default)
        {
            var sayilar = await StokListeFiltresi.Uygula(_context.StokKayitlari.AsNoTracking(), searchTerm, null)
                .GroupBy(s => s.DurumId)
                .Select(g => new { DurumId = g.Key, Sayi = g.Count() })
                .ToListAsync(cancellationToken);
            return new StokDurumSayilari(
                sayilar.Where(x => x.DurumId == (int)StokDurum.Aktif).Sum(x => x.Sayi),
                sayilar.Where(x => x.DurumId == (int)StokDurum.Tukendi).Sum(x => x.Sayi));
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
