using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Yedek proje Excel formatını okur ve proje/çeki/sandık kayıtlarını atomik oluşturur.
/// </summary>
public interface IYedekCekiImportService
{
    Task<YedekCekiImportResult> ImportAsync(
        Stream excelDosya,
        string dosyaAdi,
        int kullaniciId,
        CancellationToken cancellationToken = default);
}
