using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    /// <summary>Rolü siler.</summary>
    public class RolSilCommand : IRequest<Result<bool>>, ISecuredRequest
    {

        public int Id { get; set; }
    }
}
