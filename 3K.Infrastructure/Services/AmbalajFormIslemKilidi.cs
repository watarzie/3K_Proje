using Microsoft.EntityFrameworkCore;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

public sealed class AmbalajFormIslemKilidi(AppDbContext db) : IAmbalajFormIslemKilidi
{
    public async Task KilitleAsync(CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Form işlem kilidi transaction gerektirir.");
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(3006195001::bigint)", cancellationToken);
    }
}
