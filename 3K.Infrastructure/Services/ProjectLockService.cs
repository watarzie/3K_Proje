using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace _3K.Infrastructure.Services
{
    public class ProjectLockService : IProjectLockService
    {
        private readonly AppDbContext _context;

        public ProjectLockService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CheckLockAsync(int projeId)
        {
            var durumId = await _context.Projeler
                .Where(p => p.Id == projeId)
                .Select(p => p.DurumId)
                .FirstOrDefaultAsync();

            if (durumId == (int)ProjeDurum.SevkEdildi)
            {
                throw new System.Exception("Sevk edilen proje kilitlidir, üzerinde işlem yapılamaz!");
            }
        }

        public async Task CheckLockByCekiSatiriAsync(int cekiSatiriId)
        {
            var projeId = await _context.CekiSatirlari
                .Where(cs => cs.Id == cekiSatiriId)
                .Select(cs => cs.Ceki.ProjeId)
                .FirstOrDefaultAsync();

            if (projeId > 0)
            {
                await CheckLockAsync(projeId);
            }
        }

        public async Task CheckLockBySandikAsync(int sandikId)
        {
            var projeId = await _context.Sandiklar
                .Where(s => s.Id == sandikId)
                .Select(s => s.ProjeId)
                .FirstOrDefaultAsync();

            if (projeId > 0)
            {
                await CheckLockAsync(projeId);
            }
        }

        public async Task CheckLockByCekiAsync(int cekiId)
        {
            var projeId = await _context.Cekiler
                .Where(c => c.Id == cekiId)
                .Select(c => c.ProjeId)
                .FirstOrDefaultAsync();

            if (projeId > 0)
            {
                await CheckLockAsync(projeId);
            }
        }
    }
}
