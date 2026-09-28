using _3K.Core.Entities;

namespace _3K.Core.Interfaces;

/// <summary>Projenin ana çekisini Excel dosyasından yükler.</summary>
public interface ICekiYuklemeService
{
    Task<Ceki> CekiYukleAsync(Stream excelDosya, string dosyaAdi);
}
