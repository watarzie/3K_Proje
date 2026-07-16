using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Birden fazla SaveChanges adımı içeren iş akışını tek ve atomik
        /// veritabanı transaction'ı içinde çalıştırır.
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);
    }
}
