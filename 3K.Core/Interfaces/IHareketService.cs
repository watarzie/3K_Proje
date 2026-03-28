using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 11: Log ve hareket geçmişi
    /// UML Sequence Diagram: HareketService
    /// </summary>
    public interface IHareketService
    {
        Task HareketKaydetAsync(HareketGecmisi hareket);
        Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId);
        Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId);
    }
}
