using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetEksikUrunlerQueryHandler : IRequestHandler<GetEksikUrunlerQuery, Result<List<EksikUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetEksikUrunlerQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
        }

        public async Task<Result<List<EksikUrunDto>>> Handle(GetEksikUrunlerQuery request, CancellationToken cancellationToken)
        {
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiler = await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId);

            if (!cekiler.Any())
                return Result<List<EksikUrunDto>>.Failure("Bu projeye ait çeki bulunamadı.", 404);

            var cekiIdler = cekiler.Select(c => c.Id).ToList();

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            var tumSatirlar = await cekiSatiriRepo.FindAsync(cs => cekiIdler.Contains(cs.CekiId));
            var eksikler = tumSatirlar
                .Where(cs =>
                    (cs.GridDurumuId == (int)GridDurum.SevkEdildi || cs.GridDurumuId == (int)GridDurum.KismiSevkEdildi)
                    && cs.UcKDurumuId != (int)UcKDurum.TamGeldi
                    && cs.UcKDurumuId != (int)UcKDurum.Paketlendi
                    && cs.UcKDurumuId != (int)UcKDurum.KontrolEdildi)
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
                    GridDurumuId = cs.GridDurumuId,
                    GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(cs.GridDurumuId),
                    UcKDurumuId = cs.UcKDurumuId,
                    UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(cs.UcKDurumuId),
                    SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo
                })
                .ToList();

            return Result<List<EksikUrunDto>>.Success(eksikler);
        }
    }
}
