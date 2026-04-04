using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>
    /// İş akışı 9: PDF oluşturma.
    /// Roller: Admin, Yonetici
    /// </summary>
    public class PdfOlusturCommand : IRequest<Result<byte[]>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { nameof(KullaniciRol.Admin), nameof(KullaniciRol.Yonetici) };

        public int ProjeId { get; set; }
        public int KullaniciId { get; set; }
    }
}
