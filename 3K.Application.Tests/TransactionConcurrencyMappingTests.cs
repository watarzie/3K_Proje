using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

public sealed class TransactionConcurrencyMappingTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.SerializationFailure, 0)]
    [InlineData(PostgresErrorCodes.SerializationFailure, 1)]
    [InlineData(PostgresErrorCodes.SerializationFailure, 2)]
    [InlineData(PostgresErrorCodes.DeadlockDetected, 0)]
    [InlineData(PostgresErrorCodes.DeadlockDetected, 1)]
    [InlineData(PostgresErrorCodes.DeadlockDetected, 2)]
    public void ProviderSarmalanmisSerializationVeDeadlock_TransactionConflictOlarakTaninir(string state, int depth)
    {
        Assert.True(UnitOfWork.IsTransactionConcurrencyConflict(Wrap(state, depth)));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    [InlineData(PostgresErrorCodes.ConnectionFailure)]
    public void DigerPostgresHatalari_SarmalansaBileConcurrency409aCevrilmez(string state)
    {
        Assert.False(UnitOfWork.IsTransactionConcurrencyConflict(Wrap(state, 2)));
    }

    [Fact]
    public void ProviderOlmayanHataVeyaMetindekiSqlState_ConcurrencySayilmaz()
    {
        Assert.False(UnitOfWork.IsTransactionConcurrencyConflict(new InvalidOperationException("40001")));
        Assert.False(UnitOfWork.IsTransactionConcurrencyConflict(new TimeoutException("40P01")));
    }

    private static Exception Wrap(string state, int depth)
    {
        Exception result = new PostgresException("Sentetik test hatası", "ERROR", "ERROR", state);
        if (depth >= 1) result = new DbUpdateException("Veri güncelleme hatası", result);
        if (depth >= 2) result = new InvalidOperationException("Provider execution strategy sarmalayıcısı", result);
        return result;
    }
}
