using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class ManuelUrunEkleCommandHandler : IRequestHandler<ManuelUrunEkleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public ManuelUrunEkleCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(ManuelUrunEkleCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            var sonCeki = (await cekiRepo.FindAsync(c => c.ProjeId == request.ProjeId))
                          .OrderByDescending(c => c.YuklemeTarihi)
                          .FirstOrDefault();
            if (sonCeki == null) return Result.Failure("Projeye ait çeki bulunamadı.", 404);

            var yeniUrun = new CekiSatiri
            {
                CekiId = sonCeki.Id,
                SiraNo = 9999,
                BarkodNo = request.BarkodNo,
                Aciklama = request.Aciklama,
                IstenenAdet = request.IstenenAdet,
                Birim = request.Birim,
                CekideGecenSandikNo = sandik.SandikNo,
                FiiliSandikNo = sandik.SandikNo,
                DurumId = (int)UrunDurum.Bekliyor,
                IsManuelEklenen = true,
                EklemeNedeni = request.EklemeNedeni,
                GridDurumuId = (int)GridDurum.Bekliyor,
                UcKDurumuId = (int)UcKDurum.Bekliyor
            };

            await cekiSatiriRepo.AddAsync(yeniUrun);
            await _unitOfWork.SaveChangesAsync();

            await sandikIcerikRepo.AddAsync(new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = yeniUrun.Id,
                KonulanAdet = request.IstenenAdet,
                EksikAdet = 0
            });
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = yeniUrun.Id.ToString(),
                Islem = "Manuel Ürün Eklendi",
                IslemTipiId = (int)IslemTipi.ManuelUrunEklendi,
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = $"Neden: {request.EklemeNedeni}, Miktar: {request.IstenenAdet} {request.Birim}"
            });

            return Result.Success();
        }
    }
}
