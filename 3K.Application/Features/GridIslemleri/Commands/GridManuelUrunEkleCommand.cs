using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    public class GridManuelUrunEkleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => new[] { StatusConstants.KullaniciRol.Admin, StatusConstants.KullaniciRol.PersonelGrid };

        public int ProjeId { get; set; }
        public string? BarkodNo { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public int IstenenAdet { get; set; }
        public int? BirimId { get; set; }
        public string? EklemeNedeni { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public string? SandikIsmi { get; set; }
    }
}

