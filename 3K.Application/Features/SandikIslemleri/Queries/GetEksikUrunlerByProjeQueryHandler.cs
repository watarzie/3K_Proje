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

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var tumSatirlar = (await cekiSatiriRepo
                .FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId))
                .ToList();

            var kaynakSatirlar = tumSatirlar
                .Where(cs => !cs.KaynakCekiSatiriId.HasValue)
                .ToList();
            var kaynakSatirIds = kaynakSatirlar.Select(cs => cs.Id).ToList();

            var tamamlamaSatirlari = kaynakSatirIds.Count == 0
                ? new List<CekiSatiri>()
                : (await cekiSatiriRepo.FindAsync(cs =>
                        cs.KaynakCekiSatiriId.HasValue &&
                        kaynakSatirIds.Contains(cs.KaynakCekiSatiriId.Value)))
                    .ToList();

            var tamamlamaPlanMap = tamamlamaSatirlari
                .GroupBy(cs => cs.KaynakCekiSatiriId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(cs => cs.IstenenAdet));

            var satirlar = kaynakSatirlar
                .Select(cs => new
                {
                    Satir = cs,
                    Planlanan = tamamlamaPlanMap.GetValueOrDefault(cs.Id),
                    PlanlanabilirKalan = Math.Max(cs.KalanMiktar - tamamlamaPlanMap.GetValueOrDefault(cs.Id), 0)
                })
                .Where(x => x.PlanlanabilirKalan > 0)
                .OrderBy(x => x.Satir.SiraNo)
                .ToList();

            var result = satirlar.Select(x => new EksikUrunForSandikDto
            {
                CekiSatiriId = x.Satir.Id,
                SiraNo = x.Satir.SiraNo,
                BarkodNo = x.Satir.BarkodNo,
                Aciklama = x.Satir.Aciklama,
                SandikNo = x.Satir.FiiliSandikNo ?? x.Satir.CekideGecenSandikNo,
                IstenenAdet = x.Satir.IstenenAdet,
                GelenMiktar = x.Satir.GelenMiktar,
                KalanMiktar = x.PlanlanabilirKalan,
                TamamlamaPlanlananAdet = x.Planlanan,
                Birim = ((Birim)x.Satir.BirimId).ToString(),
                ProjeId = proje.Id,
                ProjeNo = proje.ProjeNo
            }).ToList();

            return Result<List<EksikUrunForSandikDto>>.Success(result);
        }
    }
}
