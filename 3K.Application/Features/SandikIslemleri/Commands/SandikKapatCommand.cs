using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikKapatCommand : IRequest<SandikKapatResult>, ISecuredRequest
    {
        public string[] RequiredRoles => Array.Empty<string>();

        public int SandikId { get; set; }
        public bool ForceClose { get; set; } = false;
    }

    public class SandikKapatResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasMissingOrDefectiveItems { get; set; }
        public List<object> MissingItemDetails { get; set; } = new();
    }
}
