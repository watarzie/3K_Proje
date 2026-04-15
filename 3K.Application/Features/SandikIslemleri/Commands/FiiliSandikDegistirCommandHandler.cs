using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class FiiliSandikDegistirCommandHandler : IRequestHandler<FiiliSandikDegistirCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public FiiliSandikDegistirCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(FiiliSandikDegistirCommand request, CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var revizyonRepo = _unitOfWork.GetRepository<Revizyon>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            string eskiSandikNo = urun.FiiliSandikNo ?? urun.CekideGecenSandikNo;

            var hedefSandiklar = await sandikRepo.FindAsync(s =>
                s.ProjeId == request.ProjeId && s.SandikNo == request.YeniFiiliSandikNo);
            var hedefSandik = hedefSandiklar.FirstOrDefault();

            if (hedefSandik == null)
            {
                hedefSandik = new Sandik
                {
                    ProjeId = request.ProjeId,
                    SandikNo = request.YeniFiiliSandikNo,
                    Durum = StatusConstants.ProjeDurum.Hazirlaniyor
                };
                await sandikRepo.AddAsync(hedefSandik);
                await _unitOfWork.SaveChangesAsync();
            }

            var eskiIcerikler = await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == request.CekiSatiriId);
            var eskiIcerik = eskiIcerikler.FirstOrDefault();

            int konulanAdet = eskiIcerik?.KonulanAdet ?? urun.IstenenAdet;
            int eksikAdet = eskiIcerik?.EksikAdet ?? 0;

            if (eskiIcerik != null) sandikIcerikRepo.Remove(eskiIcerik);

            await sandikIcerikRepo.AddAsync(new SandikIcerik
            {
                SandikId = hedefSandik.Id,
                CekiSatiriId = request.CekiSatiriId,
                KonulanAdet = konulanAdet,
                EksikAdet = eksikAdet
            });

            urun.FiiliSandikNo = request.YeniFiiliSandikNo;
            var otomatikNot = $"{eskiSandikNo} → {request.YeniFiiliSandikNo}";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks) ? otomatikNot : $"{urun.Remarks}; {otomatikNot}";
            cekiSatiriRepo.Update(urun);

            await revizyonRepo.AddAsync(new Revizyon
            {
                ProjeId = request.ProjeId,
                KullaniciId = request.KullaniciId,
                Tip = "Sandık Değişikliği",
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = otomatikNot
            });

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Fiili Sandık Değiştirildi",
                KullaniciId = request.KullaniciId,
                EskiDeger = eskiSandikNo,
                YeniDeger = request.YeniFiiliSandikNo,
                Aciklama = otomatikNot
            });

            return Result.Success();
        }
    }
}