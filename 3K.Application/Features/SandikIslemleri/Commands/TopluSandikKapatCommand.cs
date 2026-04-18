using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class TopluSandikKapatCommand : IRequest<TopluSandikKapatResult>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();

        public List<int> SandikIds { get; set; } = new();
        public bool ForceClose { get; set; } = false;
    }

    public class TopluSandikKapatResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasMissingOrDefectiveItems { get; set; }
        public List<SandikUyariDetay> UyariDetaylari { get; set; } = new();
    }

    public class SandikUyariDetay
    {
        public string SandikNo { get; set; } = string.Empty;
        public List<object> UrunHatalari { get; set; } = new();
    }
}
