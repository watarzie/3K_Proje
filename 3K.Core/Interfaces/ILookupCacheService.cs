using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Startup'ta tüm lookup tablolarını IMemoryCache'e yükler.
    /// GetDeger ile O(1) label resolve sağlar.
    /// </summary>
    public interface ILookupCacheService
    {
        /// <summary>
        /// Belirtilen Lookup tipi için Id → Deger (label) döndürür.
        /// Bulunamazsa "?" döner.
        /// </summary>
        string GetDeger<TLookup>(int id) where TLookup : LookupBase;

        /// <summary>
        /// Uygulama başlangıcında çağrılarak tüm lookup verilerini belleğe yükler.
        /// </summary>
        Task WarmupAsync(CancellationToken ct = default);

        /// <summary>
        /// Belirtilen lookup tablosunu yeniden okuyup cache'e yazar.
        /// Dinamik parametre değişikliklerinden sonra kullanılır.
        /// </summary>
        Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase;
    }
}
