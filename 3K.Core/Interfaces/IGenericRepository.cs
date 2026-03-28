using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace 3K.Core.Interfaces
{
    // T, BaseEntity'den miras alan herhangi bir sınıf (Proje, Ceki vs.) olabilir.
    public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    // Belirli bir şarta göre veri getirmek için (Örn: Sadece durumu "Aktif" olanlar)
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
}
