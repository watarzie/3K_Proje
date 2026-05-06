using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Proje ve tüm alt verilerini siler (sandıklar, ürünler, vb.)
    /// Roller: Sadece Admin
    /// </summary>
    public class ProjeSilCommand : IRequest<Result<bool>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };
        public string? RequiredMenuKod => "aktif-projeler";

        public int ProjeId { get; set; }
    }
}
