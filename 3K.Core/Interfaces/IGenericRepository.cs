using System.Linq.Expressions;
using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// İlişkili entity'leri Include ederek tüm kayıtları getirir.
        /// Kullanım: GetAllWithIncludeAsync(k => k.Rol)
        /// </summary>
        Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> Queryable();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
