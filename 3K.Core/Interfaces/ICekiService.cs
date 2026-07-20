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
        Task<CekiRevizyonOnizlemeSonuc> CekiRevizyonOnizleAsync(Stream excelDosya, string dosyaAdi);
        Task<CekiRevizyonOnayTalebiSonuc> CekiRevizyonOnayaSunAsync(
            Stream excelDosya,
            string dosyaAdi,
            int kullaniciId,
            CancellationToken cancellationToken = default);
        Task<CekiRevizyonSonuc> OnayliCekiRevizyonunuUygulaAsync(
            int talepId,
            int uygulayanKullaniciId,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<CekiSatiri>> GetCekiSatirlariAsync(int cekiId);
        Task<Ceki?> GetCekiByIdAsync(int cekiId);
        Task<IEnumerable<Ceki>> GetProjeCekileriAsync(int projeId);
    }
}
