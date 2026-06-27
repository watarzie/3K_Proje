using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    public class GetEksikUrunlerQueryHandler : IRequestHandler<GetEksikUrunlerQuery, Result<List<EksikUrunDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILookupCacheService _lookupCache;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public GetEksikUrunlerQueryHandler(
            IUnitOfWork unitOfWork,
            ILookupCacheService lookupCache,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _lookupCache = lookupCache;
            _sahaTamamlamaService = sahaTamamlamaService;
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
            var kaynakSatirIds = tumSatirlar
                .Where(cs => !cs.KaynakCekiSatiriId.HasValue)
                .Select(cs => cs.Id)
                .ToList();
            var sahaTamamlamaMap = await _sahaTamamlamaService.GetAktifGerceklesenTamamlamaMapAsync(kaynakSatirIds, cancellationToken);

            var eksikler = tumSatirlar
                .Select(cs => new
                {
                    Satir = cs,
                    Kalan = CekiSatiriKalanHelper.HesaplaEtkinKalan(cs, sahaTamamlamaMap)
                })
                .Where(x => x.Kalan > 0)
                .OrderBy(x => x.Satir.SiraNo)
                .Select(x => new EksikUrunDto
                {
                    CekiSatiriId = x.Satir.Id,
                    SiraNo = x.Satir.SiraNo,
                    BarkodNo = x.Satir.BarkodNo,
                    Aciklama = x.Satir.Aciklama,
                    IstenenAdet = x.Satir.IstenenAdet,
                    GelenMiktar = x.Satir.GelenMiktar,
                    EksikMiktar = x.Kalan,
                    GridDurumuId = x.Satir.GridDurumuId,
                    GridDurumuMetni = _lookupCache.GetDeger<LookupGridDurum>(x.Satir.GridDurumuId),
                    UcKDurumuId = x.Satir.UcKDurumuId,
                    UcKDurumuMetni = _lookupCache.GetDeger<LookupUcKDurum>(x.Satir.UcKDurumuId),
                    SandikNo = x.Satir.FiiliSandikNo ?? x.Satir.CekideGecenSandikNo
                })
                .ToList();

            return Result<List<EksikUrunDto>>.Success(eksikler);
        }
    }
}
