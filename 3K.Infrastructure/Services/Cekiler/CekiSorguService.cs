using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Çeki satırlarının okunması.</summary>
internal sealed class CekiSorguService(
    AppDbContext context) : ICekiSorguService
{
    public async Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId)
    {
        return await context.CekiSatirlari
            .Include(cs => cs.Paketleyen)
            .Include(cs => cs.KontrolEden)
            .Include(cs => cs.SandikIcerikleri)
            .Where(cs => cs.CekiId == cekiId)
            .OrderBy(cs => cs.SiraNo)
            .ToListAsync();
    }
}
