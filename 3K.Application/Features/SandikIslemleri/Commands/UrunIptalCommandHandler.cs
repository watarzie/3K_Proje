using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UrunIptalCommandHandler : IRequestHandler<UrunIptalCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public UrunIptalCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(UrunIptalCommand request, CancellationToken cancellationToken)
        {
            var urunRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var urun = await urunRepo.GetByIdAsync(request.CekiSatiriId);

            if (urun == null) return Result.Failure("Ürün bulunamadı.", 404);

            urun.Durum = StatusConstants.UrunDurum.IptalVeyaPasif;
            urun.Remarks = $"İPTAL: {request.Neden}";
            urunRepo.Update(urun);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "Ürün İptal Edildi",
                KullaniciId = request.KullaniciId,
                Aciklama = request.Neden
            });

            return Result.Success();
        }
    }
}
