using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.DTOs;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>Tır seferleri listesi (sevkiyatlar R): tarih aralığı, plaka, proje no ve proje tipi süzgeci; sayfalı.</summary>
public sealed class SevkSeferleriQuery : IRequest<Result<PaginatedList<SevkSeferiOzetDto>>>, ISecuredRequest
{
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
    public string? Plaka { get; init; }
    public string? ProjeNo { get; init; }
    public int? ProjeTipiId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>
/// Seferin projeleri ve sandıkları. Gerçek sefer kimlikle; sefere bağlı olmayan eski sevkiyatlar plaka + sevk günüyle
/// (<see cref="EskiPlaka"/>, <see cref="EskiTarih"/>) bulunur.
/// </summary>
public sealed class SevkSeferiDetayQuery : IRequest<Result<SevkSeferiDetayDto>>, ISecuredRequest
{
    public int? SeferId { get; init; }
    public string? EskiPlaka { get; init; }
    public DateTime? EskiTarih { get; init; }
}

/// <summary>Sevkiyat istatistikleri (sevkiyatlar R): plaka, proje ve ay bazında sefer ve sandık sayıları.</summary>
public sealed class SevkiyatIstatistikQuery : IRequest<Result<SevkiyatIstatistikDto>>, ISecuredRequest
{
    public DateTime? Baslangic { get; init; }
    public DateTime? Bitis { get; init; }
}
