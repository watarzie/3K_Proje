using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

// Yalnız bellek: gerçek EF servislerinin LINQ predicate/projection'ları çalışır.
// SQL çevirisi/ilişkisel constraint testi değildir; hiçbir bağlantı veya veri yazımı yoktur.
internal sealed class OrtakMemorySet<T>(IEnumerable<T> rows) : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    private readonly IQueryable<T> _query = new OrtakAsyncQuery<T>(rows);
    public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => throw new NotSupportedException();
    Type IQueryable.ElementType => typeof(T);
    Expression IQueryable.Expression => _query.Expression;
    IQueryProvider IQueryable.Provider => _query.Provider;
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _query.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _query.GetEnumerator();
    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default) =>
        new OrtakAsyncEnumerator<T>(_query.GetEnumerator());
}

internal sealed class OrtakAsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public OrtakAsyncQuery(IEnumerable<T> rows) : base(rows) { }
    public OrtakAsyncQuery(Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new OrtakAsyncProvider(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = default) =>
        new OrtakAsyncEnumerator<T>(AsEnumerable().GetEnumerator());
    private IEnumerable<T> AsEnumerable() => ((IQueryable<T>)this).AsEnumerable();
}

internal sealed class OrtakAsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression)
    {
        var itemType = expression.Type.GetGenericArguments()[0];
        return (IQueryable)Activator.CreateInstance(typeof(OrtakAsyncQuery<>).MakeGenericType(itemType), expression)!;
    }
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new OrtakAsyncQuery<TElement>(expression);
    public object? Execute(Expression expression) => inner.Execute(expression);
    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var resultType = typeof(TResult).GetGenericArguments().Single();
        var value = inner.Execute(expression);
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType)
            .Invoke(null, [value])!;
    }
}

internal sealed class OrtakAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}

internal sealed class OrtakMemoryUow : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repos = [];
    private readonly List<Func<CancellationToken, Task>> _afterCommit = [];
    private readonly List<Func<CancellationToken, Task>> _afterRollback = [];
    public int SaveCount { get; private set; }
    public bool HasActiveTransaction { get; private set; }
    public OrtakMemoryRepo<T> Repo<T>() where T : BaseEntity
    {
        if (!_repos.TryGetValue(typeof(T), out var repo)) _repos[typeof(T)] = repo = new OrtakMemoryRepo<T>();
        return (OrtakMemoryRepo<T>)repo;
    }
    public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
    public Task<int> SaveChangesAsync(CancellationToken token = default) { SaveCount++; return Task.FromResult(1); }
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken token = default)
    {
        HasActiveTransaction = true;
        TResult result;
        try
        {
            result = await operation(token);
        }
        catch
        {
            HasActiveTransaction = false;
            try
            {
                foreach (var callback in _afterRollback.ToArray()) await callback(CancellationToken.None);
            }
            finally
            {
                _afterCommit.Clear();
                _afterRollback.Clear();
            }
            throw;
        }
        finally
        {
            HasActiveTransaction = false;
        }
        try
        {
            foreach (var callback in _afterCommit.ToArray()) await callback(CancellationToken.None);
            return result;
        }
        finally
        {
            _afterCommit.Clear();
            _afterRollback.Clear();
        }
    }
    public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => _afterCommit.Add(callback);
    public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => _afterRollback.Add(callback);
    public void Dispose() { }
}

internal sealed class OrtakMemoryRepo<T> : IGenericRepository<T> where T : BaseEntity
{
    public List<T> Rows { get; } = [];
    public int FindCount { get; private set; }
    public int UpdateCount { get; private set; }
    public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(x => x.Id == id));
    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
    public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => GetAllAsync();
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    { FindCount++; return Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList()); }
    public IQueryable<T> Queryable() => new OrtakAsyncQuery<T>(Rows);
    public Task AddAsync(T entity)
    { if (entity.Id == 0) entity.Id = Rows.Select(x => x.Id).DefaultIfEmpty().Max() + 1; Rows.Add(entity); return Task.CompletedTask; }
    public void Update(T entity) { UpdateCount++; }
    public void Remove(T entity) => Rows.Remove(entity);
}

internal sealed record OrtakUser(int? UserId = 7, bool IsAuthenticated = true, string? MenuKod = "grid-modulu") : ICurrentUserService;

internal sealed class OrtakHareket : IHareketService
{
    public List<HareketGecmisi> Rows { get; } = [];
    public Task HareketKaydetAsync(HareketGecmisi hareket) { Rows.Add(hareket); return Task.CompletedTask; }
    public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
    public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
    public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize) => throw new NotSupportedException();
}

internal sealed class OrtakSaha : ISahaTamamlamaService
{
    public HashSet<int> AktifKaynaklar { get; } = [];
    public List<int> SenkronizeEdilen { get; } = [];
    private Task<Dictionary<int, decimal>> Map(IEnumerable<int> ids) =>
        Task.FromResult(ids.Where(AktifKaynaklar.Contains).ToDictionary(x => x, _ => 1m));
    public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken token = default) => Task.FromResult(AktifKaynaklar.Contains(id));
    public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Map(ids);
    public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Map(ids);
    public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Map(ids);
    public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Map(ids);
    public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => Map(ids);
    public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(ids.Where(AktifKaynaklar.Contains).ToHashSet());
    public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(new KaynakSandikSahaAktarimDurumu());
    public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken token = default) { SenkronizeEdilen.AddRange(ids); return Task.CompletedTask; }
    public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => Task.CompletedTask;
}
