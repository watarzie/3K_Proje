using System;
using System.Collections;
using System.Threading.Tasks;
using 3K.Core.Entities;
using 3K.Core.Interfaces;
using 3K.Infrastructure.Data;

namespace 3K.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private Hashtable _repositories;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
    {
        if (_repositories == null)
            _repositories = new Hashtable();

        var type = typeof(T).Name;

        // Eğer bu entity için daha önce repository oluşturulmadıysa oluştur ve listeye ekle
        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(GenericRepository<>);
            var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IGenericRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync()
    {
        // Burada EF Core'un kendi transaction yönetimi devreye girer.
        // İşlemlerden biri patlarsa veritabanına hiçbir şey yansımaz.
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
}