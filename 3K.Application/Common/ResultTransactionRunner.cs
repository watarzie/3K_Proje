using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    /// <summary>
    /// Result.Failure dönen bir iş akışının transaction içinde yanlışlıkla commit
    /// edilmesini engeller. Exception yalnız rollback tetiklemek için kullanılır;
    /// dışarıya özgün Result geri döner.
    /// </summary>
    internal static class ResultTransactionRunner
    {
        public static async Task<Result> ExecuteAsync(
            IUnitOfWork unitOfWork,
            Func<CancellationToken, Task<Result>> operation,
            CancellationToken cancellationToken)
        {
            var transactionSahibi = !unitOfWork.HasActiveTransaction;

            try
            {
                return await unitOfWork.ExecuteInTransactionAsync(
                    async transactionCancellationToken =>
                    {
                        var result = await operation(transactionCancellationToken);
                        if (!result.IsSuccess)
                            throw new ResultTransactionRollbackException(result);

                        return result;
                    },
                    cancellationToken);
            }
            // Dış transaction varsa sinyal yutulmaz; dış sahip de rollback yapmak zorundadır.
            catch (ResultTransactionRollbackException exception) when (transactionSahibi)
            {
                return exception.Result;
            }
        }
    }

    /// <summary>
    /// Aktif bir dış transaction varsa MediatR davranışları tarafından Result'a
    /// çevrilmeden dış transaction sahibine kadar taşınması gereken rollback sinyali.
    /// </summary>
    internal sealed class ResultTransactionRollbackException : Exception
    {
        public ResultTransactionRollbackException(Result result)
            : base(result.Error?.Message)
        {
            Result = result;
        }

        public Result Result { get; }
    }
}
