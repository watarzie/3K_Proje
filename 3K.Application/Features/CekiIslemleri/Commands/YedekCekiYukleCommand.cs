using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;

namespace _3K.Application.Features.CekiIslemleri.Commands;

/// <summary>
/// Yedek proje formatındaki Excel çekisini yükler.
/// </summary>
public sealed class YedekCekiYukleCommand
    : IRequest<Result<CekiYuklemeResultDto>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => "yedek-ceki-yukle";

    public Stream ExcelDosya { get; set; } = null!;
    public string DosyaAdi { get; set; } = string.Empty;
    public int KullaniciId { get; set; }
}
