using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;

namespace _3K.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior: ICacheableRequest implement eden istekleri
    /// IMemoryCache üzerinde cache'ler. Cache'te varsa handler'a düşmez.
    /// Pipeline sırası: Exception → Auth → Validation → Caching → Handler
    /// </summary>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Sadece ICacheableRequest implement eden istekleri cache'le
            if (request is not ICacheableRequest cacheableRequest)
                return await next();

            var cacheKey = cacheableRequest.CacheKey;

            // Cache'te varsa doğrudan döndür
            if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
            {
                _logger.LogDebug("Cache HIT — Key: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            // Cache'te yoksa handler'a devam et
            _logger.LogDebug("Cache MISS — Key: {CacheKey} — Handler'a yönlendiriliyor", cacheKey);
            var response = await next();

            // Sonucu cache'e yaz
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheableRequest.ExpirationInMinutes)
            };

            _cache.Set(cacheKey, response, cacheOptions);
            _logger.LogDebug("Cache SET — Key: {CacheKey}, TTL: {Minutes} dk", cacheKey, cacheableRequest.ExpirationInMinutes);

            return response;
        }
    }
}
