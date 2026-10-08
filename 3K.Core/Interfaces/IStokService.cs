using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Stok kaydı oluşturma ve sayfalı listeleme.
    /// </summary>
    public interface IStokService
    {
        Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi);
        
        /// <summary>Sayfalı stok listesi; <paramref name="durumId"/> verilirse yalnız o durumdaki kayıtlar döner.</summary>
        Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(
            string? searchTerm, int pageNumber, int pageSize, int? durumId = null);

        /// <summary>Aktif ve tükenmiş stok kaydı sayıları (sekme sayaçları).</summary>
        Task<StokDurumSayilari> GetDurumSayilariAsync(string? searchTerm, CancellationToken cancellationToken = default);
    }
}
