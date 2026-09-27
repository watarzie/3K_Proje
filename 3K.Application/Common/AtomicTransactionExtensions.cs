using _3K.Core.Interfaces;

namespace _3K.Application.Common;

/// <summary>
/// "Ya hepsi ya hiçbiri" iş akışları için transaction yardımcısı.
/// <see cref="IUnitOfWork.ExecuteInTransactionAsync{TResult}"/> delegate normal döndüğünde
/// commit eder; bu yardımcı ise başarısız <see cref="Result"/> dönüldüğünde de bütün
/// değişiklikleri geri alır ve aynı sonucu çağırana iletir.
/// </summary>
public static class AtomicTransactionExtensions
{
    public static async Task<TResult> ExecuteAtomicAsync<TResult>(
        this IUnitOfWork unitOfWork,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        where TResult : Result
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                var result = await operation(token);
                return result.IsSuccess ? result : throw new ResultRollbackException(result);
            }, cancellationToken);
        }
        // Dış bir transaction'a katılındıysa karar onun sahibine aittir: istisna yukarı
        // taşınır ve bütün iş geri alınır. En dışta sonuç çağırana olduğu gibi döner.
        catch (ResultRollbackException rollback) when (!unitOfWork.HasActiveTransaction)
        {
            return (TResult)rollback.Result;
        }
    }
}

/// <summary>
/// Başarısız sonucu transaction sınırından taşır. Yakalanmadan pipeline'a ulaşırsa
/// UnhandledExceptionBehavior aynı hata sonucunu döndürür.
/// </summary>
public sealed class ResultRollbackException(Result result) : Exception(result.Error?.Message)
{
    public Result Result { get; } = result;
}
