using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    /// <summary>
    /// İş akışı 2: Excel çeki dosyasını yükle.
    /// Roller: Admin
    /// </summary>
    public class CekiYukleCommand : IRequest<Result<CekiYuklemeResultDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public Stream ExcelDosya { get; set; } = null!;
        public string DosyaAdi { get; set; } = string.Empty;
        public int KullaniciId { get; set; }
    }
}
