using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Dinamik lookup veri erişim servisi.
    /// Reflection ile LookupBase subclass'larından veri çeker.
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Verilen entity adlarına karşılık gelen lookup verilerini döner.
        /// </summary>
        /// <param name="entityNames">Lookup sınıf adları (örn: "LookupProjeDurum")</param>
        /// <returns>Key: entity adı, Value: lookup kayıt listesi</returns>
        Task<Dictionary<string, List<LookupBase>>> GetLookupsAsync(IEnumerable<string> entityNames, CancellationToken ct = default);

        /// <summary>
        /// Geçerli bir lookup entity adı mı kontrol eder.
        /// </summary>
        bool IsValidLookupEntity(string entityName);
    }
}
