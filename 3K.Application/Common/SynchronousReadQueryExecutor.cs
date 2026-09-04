using _3K.Core.Interfaces;

namespace _3K.Application.Common;

/// <summary>
/// LINQ-to-Objects kullanan birim testleri için EF bağımsız geri dönüş uygulamasıdır.
/// Canlı uygulamada DI üzerinden EntityFrameworkReadQueryExecutor kullanılır.
/// </summary>
internal sealed class SynchronousReadQueryExecutor : IReadQueryExecutor
{
    public static SynchronousReadQueryExecutor Instance { get; } = new();

    private SynchronousReadQueryExecutor()
    {
    }

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query;

    public Task<int> CountAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(query.Count());
    }

    public Task<List<T>> ToListAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(query.ToList());
    }
}
