using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 2: Çeki yükleme akışı
    /// </summary>
    public interface ICekiService
    {
        Task<Ceki> CekiYukleAsync(Stream excelDosya, string dosyaAdi);
        Task<Ceki> CekiManuelOlusturAsync(ManuelCekiOlusturModel model);
        Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId);
        Task<Ceki?> GetCekiByIdAsync(int cekiId);
        Task<IEnumerable<Ceki>> GetProjeCekileriAsync(int projeId);
    }
}
