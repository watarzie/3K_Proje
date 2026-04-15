using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: Excel şablonunu operasyon verileriyle doldurarak indir.
    /// Roller: Admin, Yonetici
    /// </summary>
    public class ExcelOlusturCommand : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Yonetici };

        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
