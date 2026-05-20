using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    /// <summary>
    /// Proje ve tüm alt verilerini siler (sandıklar, ürünler, vb.)
    /// </summary>
    public class ProjeSilCommand : IRequest<Result<bool>>, ISecuredRequest
    {

        public int ProjeId { get; set; }
    }
}
