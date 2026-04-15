using Microsoft.EntityFrameworkCore;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// Reflection tabanlı dinamik lookup servisi.
    /// AppDbContext üzerinden LookupBase subclass'larını dinamik olarak sorgular.
    /// </summary>
    public class LookupService : ILookupService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Build-time'da bir kez hesaplanan lookup type registry.
        /// Key: sınıf adı (case-insensitive), Value: Type
        /// </summary>
        private static readonly Dictionary<string, Type> _lookupTypeRegistry;

        static LookupService()
        {
            _lookupTypeRegistry = typeof(LookupBase).Assembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(LookupBase)))
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
        }

        public LookupService(AppDbContext context)
        {
            _context = context;
        }

        public bool IsValidLookupEntity(string entityName)
        {
            return _lookupTypeRegistry.ContainsKey(entityName);
        }

        public async Task<Dictionary<string, List<LookupBase>>> GetLookupsAsync(
            IEnumerable<string> entityNames, CancellationToken ct = default)
        {
            var result = new Dictionary<string, List<LookupBase>>();

            foreach (var entityName in entityNames)
            {
                if (!_lookupTypeRegistry.TryGetValue(entityName, out var lookupType))
                    continue; // geçersiz entity adları atlanır

                var items = await GetLookupDataAsync(lookupType, ct);
                result[entityName] = items;
            }

            return result;
        }

        /// <summary>
        /// Reflection ile generic DbSet'ten veri çeker.
        /// context.Set&lt;T&gt;() → AsNoTracking → ToListAsync
        /// </summary>
        private async Task<List<LookupBase>> GetLookupDataAsync(Type lookupType, CancellationToken ct)
        {
            // 1. context.Set<T>() çağır
            var setMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == nameof(DbContext.Set)
                            && m.IsGenericMethodDefinition
                            && m.GetParameters().Length == 0);

            var genericSetMethod = setMethod.MakeGenericMethod(lookupType);
            var dbSet = genericSetMethod.Invoke(_context, null)!;

            // 2. AsNoTracking
            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking))!
                .MakeGenericMethod(lookupType);

            var query = asNoTrackingMethod.Invoke(null, new[] { dbSet })!;

            // 3. ToListAsync
            var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync)
                            && m.GetParameters().Length == 2)
                .MakeGenericMethod(lookupType);

            var task = (Task)toListAsyncMethod.Invoke(null, new[] { query, ct })!;
            await task;

            // 4. Result → List<LookupBase>
            var resultProp = task.GetType().GetProperty("Result")!;
            var list = (System.Collections.IList)resultProp.GetValue(task)!;

            var items = new List<LookupBase>(list.Count);
            foreach (var item in list)
            {
                items.Add((LookupBase)item);
            }

            items.Sort((a, b) => a.Anahtar.CompareTo(b.Anahtar));
            return items;
        }
    }
}
