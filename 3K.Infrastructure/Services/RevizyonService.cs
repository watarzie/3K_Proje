using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace _3K.Infrastructure.Services
{
    public class RevizyonService : IRevizyonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;

        public RevizyonService(IUnitOfWork unitOfWork, AppDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task RevizyonKaydetAsync(Revizyon revizyon)
        {
            var repo = _unitOfWork.GetRepository<Revizyon>();
            revizyon.Tarih = DateTime.UtcNow;
            await repo.AddAsync(revizyon);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<Revizyon>> GetProjeRevizyonlariAsync(int projeId)
        {
            return await _context.Revizyonlar
                .Include(r => r.Kullanici)
                .Where(r => r.ProjeId == projeId)
                .OrderByDescending(r => r.Tarih)
                .ToListAsync();
        }
    }
}
