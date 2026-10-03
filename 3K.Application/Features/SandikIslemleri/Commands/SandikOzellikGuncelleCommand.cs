using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommand : IRequest<Result>, ISecuredRequest
    {

        public int SandikId { get; set; }
        public string? SandikIsmi { get; set; }
        /// <summary>Sandık tipi zorunludur (G2); değişiklik Ambalaj senkronuna kaynak tipi olarak akar.</summary>
        public int? TipId { get; set; }
        public decimal? En { get; set; }
        public decimal? Boy { get; set; }
        public decimal? Yukseklik { get; set; }
        public decimal? NetKg { get; set; }
        public decimal? GrossKg { get; set; }
        public int? DepoLokasyonId { get; set; }
    }
}
