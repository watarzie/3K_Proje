namespace _3K.Core.Interfaces;

/// <summary>
/// Uygulama katmanını belirli bir ORM'e bağlamadan salt-okuma sorgularının
/// izlenmeden ve asenkron çalıştırılmasını sağlar.
/// </summary>
public interface IReadQueryExecutor
{
    IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class;

    Task<int> CountAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default);

    Task<List<T>> ToListAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default);
}
