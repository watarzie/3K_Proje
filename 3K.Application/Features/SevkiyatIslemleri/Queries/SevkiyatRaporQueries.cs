using MediatR;
using _3K.Application.Common;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>
/// Sevkiyat dönem raporu (sevkiyatlar R): Sevkiyatlar sayfasının süzgeçleriyle (tarih, plaka, proje no, proje tipi)
/// seçilen seferler. <see cref="Format"/> "pdf" özet, "excel" bütün döküm (seferler, araçlar, projeler, sandıklar, ürünler).
/// </summary>
public sealed class SevkiyatDonemRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
{
    public string Format { get; init; } = SevkiyatRaporBicimleri.Pdf;
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
    public string? Plaka { get; init; }
    public string? ProjeNo { get; init; }
    public int? ProjeTipiId { get; init; }
}

/// <summary>
/// Tek tır seferinin yazdırılabilir PDF raporu (sevkiyatlar R). Gerçek sefer kimlikle; sefere bağlı olmayan eski
/// sevkiyatlar plaka + sevk günüyle (<see cref="EskiPlaka"/>, <see cref="EskiTarih"/>) bulunur.
/// </summary>
public sealed class SevkSeferiRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
{
    public int? SeferId { get; init; }
    public string? EskiPlaka { get; init; }
    public DateTime? EskiTarih { get; init; }
}

/// <summary>Dönem raporu biçimleri.</summary>
public static class SevkiyatRaporBicimleri
{
    public const string Pdf = "pdf";
    public const string Excel = "excel";

    /// <summary>"pdf", "excel" veya "xlsx" (büyük/küçük harf duyarsız); diğerleri geçersiz.</summary>
    public static string? Coz(string? format) => format?.Trim().ToLowerInvariant() switch
    {
        "pdf" => Pdf,
        "excel" or "xlsx" => Excel,
        _ => null
    };
}
