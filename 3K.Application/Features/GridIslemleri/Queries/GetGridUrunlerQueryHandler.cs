using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.GridIslemleri.Queries
{
    public class GetGridUrunlerQueryHandler : IRequestHandler<GetGridUrunlerQuery, Result<List<GridUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;

        public GetGridUrunlerQueryHandler(IUnitOfWork unitOfWork, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
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
                    Birim = ((Birim)cs.BirimId).ToString(),
                    SandikNo = cs.FiiliSandikNo ?? cs.CekideGecenSandikNo,
                    GridDurumuId = cs.GridDurumuId,
                    GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(cs.GridDurumuId),
                    GridGelenAdet = cs.GridGelenAdet,
                    TrafoSevkAdet = cs.TrafoSevkAdet,
                    GridSevkDurumuId = cs.GridSevkDurumuId,
                    GridSevkDurumuMetni = _lookupCache.GetDeger<LookupGridSevkDurum>(cs.GridSevkDurumuId),
                    GridSevkMiktari = cs.GridSevkMiktari,
                    GridSevkTarihi = cs.GridSevkTarihi,
                    GridAciklama = cs.GridAciklama,
                    GridEksikMiktar = cs.GridEksikMiktar,
                    UcKDurumuId = cs.UcKDurumuId,
                    UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(cs.UcKDurumuId),
                    GelenMiktar = cs.GelenMiktar,
                    KaynakHedefProjeNo = cs.KaynakHedefProjeNo,
                    UcKAciklama = cs.UcKAciklama,
                    GenelDurumId = cs.DurumId,
                    GenelDurumMetni = _lookupCache.GetDeger<LookupUrunDurum>(cs.DurumId),
                    // Madde 2: Parçalı karşılama
                    StokKarsilanan = cs.StokKarsilanan,
                    ProjeKarsilanan = cs.ProjeKarsilanan,
                    TedarikciKarsilanan = cs.TedarikciKarsilanan,
                    EksikMiktar = cs.EksikMiktar,
                    KalanMiktar = cs.KalanMiktar
                })
                .ToList();

            return Result<List<GridUrunDto>>.Success(result);
        }
    }
}
