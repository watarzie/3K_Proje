using System;
using System.Threading.Tasks;
using 3K.Core.Entities;

namespace 3K.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
{
    // İhtiyaç duyduğumuzda bize ilgili entity'nin repository'sini verecek jenerik metot
    IGenericRepository<T> GetRepository<T>() where T : BaseEntity;

    // Yapılan tüm değişiklikleri tek bir transaction içinde veritabanına yansıtacak metot
    Task<int> SaveChangesAsync();
}
}