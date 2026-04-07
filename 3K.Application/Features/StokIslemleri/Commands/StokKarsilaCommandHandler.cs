using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKarsilaCommandHandler : IRequestHandler<StokKarsilaCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStokService _stokService;
        private readonly IHareketService _hareketService;

        public StokKarsilaCommandHandler(IUnitOfWork unitOfWork, IStokService stokService, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _stokService = stokService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(StokKarsilaCommand request, CancellationToken cancellationToken)
        {
            var yeterli = await _stokService.StokYeterliMi(request.StokKaydiId, request.Miktar);
            if (!yeterli)
                return Result.Failure("Stok miktarı yetersiz.", 400);

            await _stokService.StokDusAsync(request.StokKaydiId, request.Miktar);

            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            var icerikler = await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == request.CekiSatiriId);
            var icerik = icerikler.FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.Miktar);
                icerik.KonulanAdet += request.Miktar;
                sandikIcerikRepo.Update(icerik);
            }

            urun.Durum = "StoktanKarsilandi";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks)
                ? "Kalan stoktan karşılandı"
                : $"{urun.Remarks}; Kalan stoktan karşılandı";
            cekiSatiriRepo.Update(urun);

            var stokHareketRepo = _unitOfWork.GetRepository<StokHareketi>();
            var stokHareketi = new StokHareketi
            {
                StokKaydiId = request.StokKaydiId,
                CekiSatiriId = request.CekiSatiriId,
                ProjeId = request.ProjeId,
                KullaniciId = request.KullaniciId,
                Miktar = request.Miktar,
                IslemTipi = "StokKullanimi",
                Aciklama = $"Proje {request.ProjeId} için stoktan {request.Miktar} adet kullanıldı"
            };
            await stokHareketRepo.AddAsync(stokHareketi);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "StokHareketi",
                ReferansId = stokHareketi.Id.ToString(),
                Islem = "Stoktan Karşılandı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"StokKaydı: {request.StokKaydiId}, Miktar: {request.Miktar}"
            });

            return Result.Success();
        }
    }
}
