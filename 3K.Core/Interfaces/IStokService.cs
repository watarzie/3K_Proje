using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 6: Kalan stoktan eksik tamamlama
    /// UML Sequence Diagram: StokService
    /// </summary>
    public interface IStokService
    {
        Task<IEnumerable<StokKaydi>> GetUygunStoklarAsync(string? malzemeKodu = null);
        Task<StokKaydi?> GetStokByIdAsync(int stokKaydiId);
        Task<bool> StokYeterliMi(int stokKaydiId, int miktar);
        Task<bool> StokDusAsync(int stokKaydiId, int miktar);
        Task<StokKaydi> StokKaydiOlusturAsync(StokKaydi stokKaydi);
        Task<IEnumerable<StokKaydi>> GetTumStoklarAsync();
    }
}
