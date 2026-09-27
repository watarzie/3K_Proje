using Microsoft.Extensions.Logging;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

public sealed class DatabaseHealthProbe(AppDbContext context, ILogger<DatabaseHealthProbe> logger) : IDatabaseHealthProbe
{
    public async Task<DatabaseHealth> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.Database.CanConnectAsync(cancellationToken)
                ? DatabaseHealth.Connected
                : DatabaseHealth.Unreachable;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Veritabanı sağlık kontrolü hata verdi.");
            return DatabaseHealth.Error;
        }
    }
}
