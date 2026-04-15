using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetEksikUrunlerQueryHandler : IRequestHandler<GetEksikUrunlerQuery, Result<List<EksikUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetEksikUrunlerQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<EksikUrunDto>>> Handle(GetEksikUrunlerQuery request, CancellationToken cancellationToken)
        {
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiler = await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId);

            if (!cekiler.Any())
                return Result<List<EksikUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var cekiIdler = cekiler.Select(c => c.Id).ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            // Grid sevk etmiş ama 3K tarafında tam gelmemiş ürünler
            var tumSatirlar = await cekiSatiriRepo.FindAsync(cs => cekiIdler.Contains(cs.CekiId));
            var eksikler = tumSatirlar
                .Where(cs =>
                    (cs.GridDurumu == StatusConstants.GridDurum.SevkEdildi || cs.GridDurumu == StatusConstants.GridDurum.KismiSevkEdildi)
                    && cs.UcKDurumu != StatusConstants.UcKDurum.TamGeldi
                    && cs.UcKDurumu != StatusConstants.UcKDurum.Paketlendi
                    && cs.UcKDurumu != StatusConstants.UcKDurum.KontrolEdildi)
                .OrderBy(cs => cs.SiraNo)
                .Select(cs => new EksikUrunDto
                {
                    CekiSatiriId = cs.Id,
                    SiraNo = cs.SiraNo,
                    BarkodNo = cs.BarkodNo,
                    Aciklama = cs.Aciklama,
                    IstenenAdet = cs.IstenenAdet,
                    GelenMiktar = cs.GelenMiktar,
                    EksikMiktar = cs.IstenenAdet - cs.GelenMiktar,
                    GridDurumu = cs.GridDurumu,
                    UcKDurumu = cs.UcKDurumu,
                    SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo
                })
                .ToList();

            return Result<List<EksikUrunDto>>.Success(eksikler);
        }
    }
}
