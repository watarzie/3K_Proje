using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    /// <summary>Stok sekmelerinin (Aktif Stok / Tükendiler) sayaçları; arama metni listeyle aynı uygulanır.</summary>
    public class StokDurumSayilariQuery : IRequest<Result<StokDurumSayilari>>, ISecuredRequest
    {
        public string? SearchTerm { get; set; }
    }

    public sealed class StokDurumSayilariQueryHandler(IStokService stokService)
        : IRequestHandler<StokDurumSayilariQuery, Result<StokDurumSayilari>>
    {
        public async Task<Result<StokDurumSayilari>> Handle(StokDurumSayilariQuery request, CancellationToken cancellationToken) =>
            Result<StokDurumSayilari>.Success(await stokService.GetDurumSayilariAsync(request.SearchTerm, cancellationToken));
    }
}
