using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Barkod etiketi (<see cref="BarkodEtiketKurali"/>): bir projenin sandık ve ürün listeleri (sunucuda sayfalı) ve seçilen
/// kayıtların Grid etiketleri (PDF veya Godex EZPL). Durum değiştirmez. Proje yoksa ya da seçilen kayıt bu projede
/// yoksa <see cref="Exceptions.KayitBulunamadiException"/> (404); seçilen ürünün barkodu basılamıyorsa
/// <see cref="Exceptions.IsKuraliException"/> (400).
/// </summary>
public interface IBarkodEtiketService
{
    /// <summary>Etiket basılacak proje seçimi: Normal/Saha/Yedek bütün projeler, en yeni önce; arama proje no veya müşteride.</summary>
    Task<SayfaliSonuc<BarkodEtiketProjesi>> ProjeleriListeleAsync(string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    Task<BarkodEtiketProjesi> ProjeAsync(int projeId, CancellationToken cancellationToken);

    /// <summary>Projenin sandıkları; arama sandık no veya adında. Doğal sandık no sırasıyla.</summary>
    Task<SayfaliSonuc<BarkodEtiketSandikSatiri>> SandiklariListeleAsync(
        int projeId, string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    /// <summary>
    /// Projenin çeki satırları ve sandıklarına elle eklenen malzemeler; <paramref name="sandikId"/> verilirse yalnız o
    /// sandıktakiler ve satırın o sandıktaki tahsis miktarı (<see cref="BarkodEtiketUrunSatiri.SandiktakiMiktar"/>).
    /// Arama barkod veya açıklamada.
    /// </summary>
    Task<SayfaliSonuc<BarkodEtiketUrunSatiri>> UrunleriListeleAsync(
        int projeId, int? sandikId, string? arama, int? sayfa, int? boyut, CancellationToken cancellationToken);

    /// <summary>Seçilen sandıkların etiketleri (seçim sırasıyla, her biri adedi kadar); tarih AA/YY olarak basılır.</summary>
    Task<DosyaIcerigi> SandikEtiketleriAsync(
        int projeId, IReadOnlyList<BarkodEtiketSandikSecimi> secimler, int yil, int ay, BarkodEtiketBicimi bicim,
        CancellationToken cancellationToken);

    /// <summary>Seçilen ürünlerin etiketleri (seçim sırasıyla, her biri adedi kadar).</summary>
    Task<DosyaIcerigi> UrunEtiketleriAsync(
        int projeId, IReadOnlyList<BarkodEtiketUrunSecimi> secimler, BarkodEtiketBicimi bicim, CancellationToken cancellationToken);
}
