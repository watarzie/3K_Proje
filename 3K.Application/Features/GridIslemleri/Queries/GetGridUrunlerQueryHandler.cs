using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridUrunlerQueryHandler : IRequestHandler<GetGridUrunlerQuery, Result<List<GridUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly GridUrunListesiOkuyucu _okuyucu;

        public GetGridUrunlerQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _okuyucu = new GridUrunListesiOkuyucu(unitOfWork, lookupCache, sahaTamamlamaService);
        }

        public async Task<Result<List<GridUrunDto>>> Handle(GetGridUrunlerQuery request, CancellationToken cancellationToken)
        {
            var proje = await _unitOfWork.GetRepository<Proje>().GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<List<GridUrunDto>>.Failure("Proje bulunamadı.", 404);

            var urunler = await _okuyucu.OkuAsync(proje, request.SandikId, request.SandikNo, cancellationToken);
            return urunler == null
                ? Result<List<GridUrunDto>>.Failure("Bu projeye ait ürün bulunamadı.", 404)
                : Result<List<GridUrunDto>>.Success(urunler);
        }
    }
}
