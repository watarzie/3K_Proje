using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Boş sandığı veya sadece işlem görmemiş manuel ürün içeren manuel sandığı siler.
    /// </summary>
    public class SandikSilCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };
        public string? RequiredMenuKod => "sandik-yonetimi";

        public int SandikId { get; set; }
        public int ProjeId { get; set; }
    }
}
