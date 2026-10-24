using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Barkodla ürün arama (<see cref="BarkodUrunAramaKurali"/>): bütün projelerdeki satırlar ve aynı satırların PDF/Excel
/// raporu. Durum değiştirmez.
/// </summary>
public interface IBarkodUrunAramaService
{
    /// <summary>Süzülmüş ve sıralanmış bütün satırlar (sayfalanmamış); ekran <see cref="BarkodUrunAramaKurali.Sayfala"/> kullanır.</summary>
    Task<BarkodUrunAramaSonucu> AraAsync(BarkodUrunAramaKriteri kriter, CancellationToken cancellationToken);

    /// <summary>Her satırda proje no; başlıkta barkod, açıklama, süzgeç ve özet.</summary>
    byte[] PdfOlustur(BarkodUrunAramaSonucu sonuc);

    /// <summary>PDF ile aynı satırlar ve sütunlar.</summary>
    byte[] ExcelOlustur(BarkodUrunAramaSonucu sonuc);
}
