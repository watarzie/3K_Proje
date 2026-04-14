using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridUrunlerQueryHandler : IRequestHandler<GetGridUrunlerQuery, Result<List<GridUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetGridUrunlerQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<GridUrunDto>>> Handle(GetGridUrunlerQuery request, CancellationToken cancellationToken)
        {
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiler = await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId);

            if (!cekiler.Any())
                return Result<List<GridUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var cekiIdler = cekiler.Select(c => c.Id).ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var satirlar = await cekiSatiriRepo.FindAsync(cs => cekiIdler.Contains(cs.CekiId));

            var result = satirlar
                .OrderBy(cs => cs.SiraNo)
                .Select(cs => new GridUrunDto
                {
                    CekiSatiriId = cs.Id,
                    SiraNo = cs.SiraNo,
                    BarkodNo = cs.BarkodNo,
                    Aciklama = cs.Aciklama,
                    IstenenAdet = cs.IstenenAdet,
                    Birim = cs.Birim,
                    SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                    GridDurumu = cs.GridDurumu,
                    GridGelenAdet = cs.GridGelenAdet,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    GridSevkDurumu = cs.GridSevkDurumu,
                    GridSevkMiktari = cs.GridSevkMiktari,
                    GridSevkTarihi = cs.GridSevkTarihi,
                    GridNotu = cs.GridNotu,
                    GridEksikMiktar = cs.GridEksikMiktar,
                    UcKDurumu = cs.UcKDurumu,
                    GelenMiktar = cs.GelenMiktar,
                    GenelDurum = cs.Durum
                })
                .ToList();

            return Result<List<GridUrunDto>>.Success(result);
        }
    }
}
