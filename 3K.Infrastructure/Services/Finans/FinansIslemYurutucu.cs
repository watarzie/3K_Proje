using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using _3K.Core.Exceptions;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services.Finans;

/// <summary>
/// Finans yazma işlemlerinin tek transaction modeli. İşlem ve denetim kayıtları aynı transaction'da
/// yazılır; dışarıda açılmış bir transaction sahiplenilmez, böylece üretim aktarımı ve dönem üretimi
/// gibi üst akışlarla birlikte çalışır. Benzersizlik ve serileştirme çakışmaları 409 iş kuralı
/// hatasına çevrilir.
/// </summary>
internal sealed class FinansIslemYurutucu(AppDbContext context)
{
    /// <summary>
    /// İşlemi Serializable bir transaction'da çalıştırır; transaction zaten açıksa ona katılır.
    /// </summary>
    public async Task<T> AtomikAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        IDbContextTransaction? ownedTransaction = null;
        if (context.Database.CurrentTransaction is null)
            ownedTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var result = await operation();
            if (ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            if (ownedTransaction is not null)
                await RollbackQuietlyAsync(ownedTransaction);

            ThrowIfPersistenceConflict(exception);
            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    /// <summary>
    /// Başarısız commit sonrası transaction zaten kapanmış olabilir; geri alma hatası asıl hatanın
    /// (409'a çevrilecek çakışma dahil) yerine geçmemelidir. İptal edilmiş istekte de geri alınır.
    /// </summary>
    private static async Task RollbackQuietlyAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Bağlantı veya transaction kapanmışsa PostgreSQL işlemi zaten geri almıştır.
        }
    }

    private static bool IsUniqueViolation(Exception exception)
        => FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation;

    private static bool IsSerializationFailure(Exception exception)
        => FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure;

    private static void ThrowIfPersistenceConflict(Exception exception)
    {
        if (IsUniqueViolation(exception))
            throw IsKuraliException.Cakisma(
                "Aynı benzersiz değer başka bir işlem tarafından daha önce kaydedildi. Verileri yenileyip tekrar deneyin.",
                exception);
        if (IsSerializationFailure(exception))
            throw IsKuraliException.Cakisma(
                "Kayıt aynı anda başka bir kullanıcı tarafından değiştirildi. Verileri yenileyip tekrar deneyin.",
                exception);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgresException)
                return postgresException;
        return null;
    }
}
