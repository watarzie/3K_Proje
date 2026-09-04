using Microsoft.EntityFrameworkCore;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Repositories;

/// <summary>
/// Salt-okuma IQueryable sorgularını EF Core'un gerçek async API'leriyle yürütür.
/// </summary>
public sealed class EntityFrameworkReadQueryExecutor : IReadQueryExecutor
{
    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> query)
        where TEntity : class => query.AsNoTracking();

    public Task<int> CountAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.CountAsync(query, cancellationToken);

    public Task<List<T>> ToListAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);
}
