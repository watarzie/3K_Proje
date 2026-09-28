using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Çeki revizyonu: ön izleme, onaya sunma ve onaylanan revizyonun uygulanması.</summary>
public interface ICekiRevizyonService
{
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
}
