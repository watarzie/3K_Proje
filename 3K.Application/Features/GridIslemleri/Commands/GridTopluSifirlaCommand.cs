using MediatR;
using _3K.Application.Common;

namespace _3K.Application.Features.GridIslemleri.Commands
{
    /// <summary>
    /// Seçili ürünlerin Grid durumlarını toplu olarak sıfırlar (geri alır).
    /// </summary>
    public class GridTopluSifirlaCommand : IRequest<Result>, ISecuredRequest
    {

        public int ProjeId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = new();
        public string? Aciklama { get; set; }
    }
}
