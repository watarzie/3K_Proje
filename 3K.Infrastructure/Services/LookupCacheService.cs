using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// IMemoryCache tabanlı Lookup label resolver.
    /// Uygulama başlangıcında WarmupAsync ile tüm lookup tabloları belleğe yüklenir.
    /// GetDeger ile O(1) lookup label çözümleme yapılır.
    /// </summary>
    public class LookupCacheService : ILookupCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly AppDbContext _context;
        private const string CacheKeyPrefix = "Lookup_";

        public LookupCacheService(IMemoryCache cache, AppDbContext context)
        {
            _cache = cache;
            _context = context;
        }

        public string GetDeger<TLookup>(int id) where TLookup : LookupBase
        {
            var key = BuildCacheKey<TLookup>(id);
            return _cache.TryGetValue(key, out string? val) ? val ?? "?" : "?";
        }

        public async Task WarmupAsync(CancellationToken ct = default)
        {
            await LoadLookup<LookupProjeDurum>(ct);
            await LoadLookup<LookupSandikDurum>(ct);
            await LoadLookup<LookupSandikTipi>(ct);
            await LoadLookup<LookupDepoLokasyon>(ct);
            await LoadLookup<LookupUrunDurum>(ct);
            await LoadLookup<LookupGridDurum>(ct);
            await LoadLookup<LookupUcKDurum>(ct);
            await LoadLookup<LookupYetkiTipi>(ct);
            await LoadLookup<LookupStokDurum>(ct);
            await LoadLookup<LookupIslemTipi>(ct);
            await LoadLookup<LookupGeriGonderilmeSebebi>(ct);
            await LoadLookup<LookupGridSevkDurum>(ct);
        }

        private async Task LoadLookup<T>(CancellationToken ct) where T : LookupBase
        {
            var items = await _context.Set<T>().AsNoTracking().ToListAsync(ct);
            var options = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            };

            foreach (var item in items)
            {
                var key = BuildCacheKey<T>(item.Id);
                _cache.Set(key, item.Deger, options);
            }
        }

        private static string BuildCacheKey<T>(int id) => $"{CacheKeyPrefix}{typeof(T).Name}_{id}";
    }
}
