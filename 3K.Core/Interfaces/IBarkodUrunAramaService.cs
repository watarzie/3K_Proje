using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Barkodla ürün arama (<see cref="BarkodUrunAramaKurali"/>): bütün projelerdeki satırlar ve aynı satırların PDF/Excel
/// raporu. Durum değiştirmez.
/// </summary>
public interface IBarkodUrunAramaService
{
    Task<BarkodUrunAramaSonucu> AraAsync(string barkod, bool yalnizEksikler, int? projeTipiId, CancellationToken cancellationToken);

    /// <summary>Her satırda proje no; başlıkta barkod, açıklama, süzgeç ve özet.</summary>
    byte[] PdfOlustur(BarkodUrunAramaSonucu sonuc);

    /// <summary>PDF ile aynı satırlar ve sütunlar.</summary>
    byte[] ExcelOlustur(BarkodUrunAramaSonucu sonuc);
}
