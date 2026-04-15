using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Rolü siler.</summary>
    public class RolSilCommand : IRequest<Result<bool>>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin };

        public int Id { get; set; }
    }
}
