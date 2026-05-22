using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.LookupIslemleri.Queries
{
    /// <summary>
    /// Birden fazla lookup tablosunu tek seferde çeker.
    /// Entity adları LookupBase subclass adlarıdır (örn: "LookupProjeDurum").
    /// Lookup verileri rol/depo yönetimi gibi ekranlardan değişebildiği için
    /// bu sorgu uygulama cache'ine alınmaz.
    /// </summary>
    public class GetLookupsQuery : IRequest<Result<Dictionary<string, List<DTOs.LookupItemDto>>>>
    {
        /// <summary>
        /// İstenen lookup sınıf adları listesi.
        /// Örn: ["LookupProjeDurum", "LookupSandikDurum"]
        /// </summary>
        public List<string> Entities { get; set; } = new();

    }
}
