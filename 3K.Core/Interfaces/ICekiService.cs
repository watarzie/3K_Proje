using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 2: Çeki yükleme akışı
    /// </summary>
    public interface ICekiService
    {
        Task<Ceki> CekiYukleAsync(Stream excelDosya, string dosyaAdi);
        Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId);
        Task<Ceki?> GetCekiByIdAsync(int cekiId);
        Task<IEnumerable<Ceki>> GetProjeCekileriAsync(int projeId);
    }
}
