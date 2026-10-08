using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    /// <summary>
    /// Stok listesi (stok okuma izni). <see cref="DurumId"/> verilirse yalnız o durumdaki kayıtlar döner
    /// (Aktif Stok / Tükendiler sekmeleri); verilmezse bütün kayıtlar.
    /// </summary>
    public class StokListeleQuery : PaginatedQuery<Result<PaginatedList<StokKaydiDto>>>, ISecuredRequest
    {
        public string? SearchTerm { get; set; }
        public int? DurumId { get; set; }
    }
}
