using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Stok kaydı oluşturma ve sayfalı listeleme.
    /// </summary>
    public interface IStokService
    {
        Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi);
        
        Task<(IEnumerable<StokKaydi> Items, int TotalCount)> GetPaginatedStoklarAsync(string? searchTerm, int pageNumber, int pageSize);
    }
}
