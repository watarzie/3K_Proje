using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetEksikUrunlerByProjeQueryHandler : IRequestHandler<GetEksikUrunlerByProjeQuery, Result<List<EksikUrunForSandikDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEksikUrunlerByProjeQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<EksikUrunForSandikDto>>> Handle(GetEksikUrunlerByProjeQuery request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result<List<EksikUrunForSandikDto>>.Failure("Proje bulunamadı.", 404);

            // Sadece normal projeler
            if (proje.ProjeTipiId != (int)ProjeTipi.Normal)
                return Result<List<EksikUrunForSandikDto>>.Failure("Sadece normal projelerden ürün seçilebilir.");

            var satirlar = (await _unitOfWork.GetRepository<CekiSatiri>()
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .Where(cs => cs.KalanMiktar > 0)
                .OrderBy(cs => cs.SiraNo)
                .ToList();

            var result = satirlar.Select(cs => new EksikUrunForSandikDto
            {
                CekiSatiriId = cs.Id,
                SiraNo = cs.SiraNo,
                BarkodNo = cs.BarkodNo,
                Aciklama = cs.Aciklama,
                SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                IstenenAdet = cs.IstenenAdet,
                GelenMiktar = cs.GelenMiktar,
                KalanMiktar = cs.KalanMiktar,
                Birim = ((Birim)cs.BirimId).ToString(),
                ProjeNo = proje.ProjeNo
            }).ToList();

            return Result<List<EksikUrunForSandikDto>>.Success(result);
        }
    }
}
