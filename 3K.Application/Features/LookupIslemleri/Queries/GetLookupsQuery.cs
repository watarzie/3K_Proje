using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.LookupIslemleri.Queries
{
    /// <summary>
    /// Birden fazla lookup tablosunu tek seferde çeker.
    /// Entity adları LookupBase subclass adlarıdır (örn: "LookupProjeDurum").
    /// ICacheableRequest ile cache'lenir — Lookup verileri nadiren değişir.
    /// </summary>
    public class GetLookupsQuery : IRequest<Result<Dictionary<string, List<DTOs.LookupItemDto>>>>, ICacheableRequest
    {
        /// <summary>
        /// İstenen lookup sınıf adları listesi.
        /// Örn: ["LookupProjeDurum", "LookupSandikDurum"]
        /// </summary>
        public List<string> Entities { get; set; } = new();

        // ===== ICacheableRequest =====
        public string CacheKey => $"Lookups_{string.Join("_", Entities.OrderBy(e => e))}";
        public int ExpirationInMinutes => 30; // Lookup verileri 30 dk cache'lenir
    }
}
