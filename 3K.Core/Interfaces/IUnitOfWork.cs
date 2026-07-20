using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        bool HasActiveTransaction { get; }
        IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Birden fazla SaveChanges adımı içeren iş akışını tek ve atomik
        /// veritabanı transaction'ı içinde çalıştırır.
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// En dış transaction başarıyla commit edildikten sonra çalıştırılacak
        /// yan etkiyi kaydeder. Yalnızca aktif transaction içinde çağrılabilir.
        /// </summary>
        void RegisterAfterCommit(Func<CancellationToken, Task> callback);

        /// <summary>
        /// En dış transaction geri alındıktan sonra çalıştırılacak telafi
        /// işlemini kaydeder. Yalnızca aktif transaction içinde çağrılabilir.
        /// </summary>
        void RegisterAfterRollback(Func<CancellationToken, Task> callback);
    }
}
