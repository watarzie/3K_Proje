namespace _3K.Core.Interfaces;

/// <summary>
/// Uygulama katmanını belirli bir ORM'e bağlamadan sorguların asenkron çalıştırılmasını sağlar.
/// <see cref="ToListAsync{T}"/> ve <see cref="CountAsync{T}"/> sorguyu olduğu gibi yürütür: izlenen
/// <c>Queryable()</c> sorgusu izlenen örnek döndürür; salt-okuma için sorgu önce <see cref="AsNoTracking{TEntity}"/> ile sarılır.
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
