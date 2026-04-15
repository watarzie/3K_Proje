using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FBTransferIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.FBTransferIslemleri.Commands
{
    /// <summary>
    /// İş akışı 5: FB arası malzeme transfer.
    /// Roller: Admin, Personel3K
    /// </summary>
    public class FBTransferOlusturCommand : IRequest<Result<FBTransferResultDto>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int CekiSatiriId { get; set; }
        public string AsilFB { get; set; } = string.Empty;
        public string AlinanFB { get; set; } = string.Empty;
        public int Miktar { get; set; }
        public string? Neden { get; set; }
        public string? IadeDurumu { get; set; }
        public string? Aciklama { get; set; }
        public int KullaniciId { get; set; }
        public int ProjeId { get; set; }
    }
}
