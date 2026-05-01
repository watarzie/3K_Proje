using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Boş sandığı siler (içinde ürün olmamalı).
    /// </summary>
    public class SandikSilCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.Personel3K };

        public int SandikId { get; set; }
        public int ProjeId { get; set; }
    }
}
