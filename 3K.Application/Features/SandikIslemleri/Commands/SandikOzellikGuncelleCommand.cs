using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommand : IRequest<Result>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();

        public int SandikId { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
    }
}
