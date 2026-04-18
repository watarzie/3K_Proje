namespace _3K.Application.Common
{
    /// <summary>
    /// Bu interface'i implement eden MediatR istekleri otomatik olarak
    /// In-Memory Cache'e alınır. CachingBehavior pipeline tarafından yönetilir.
    /// </summary>
    public interface ICacheableRequest
    {
        /// <summary>Cache anahtarı — her sorgu için benzersiz olmalı.</summary>
        string CacheKey { get; }

        /// <summary>Cache süresi (dakika). Varsayılan 10 dk.</summary>
        int ExpirationInMinutes => 10;
    }
}
