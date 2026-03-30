using MediatR;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class FBDenKarsilaCommandHandler : IRequestHandler<FBDenKarsilaCommand, bool>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public FBDenKarsilaCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<bool> Handle(FBDenKarsilaCommand request, CancellationToken cancellationToken)
        {
            var urunRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var fbTransferRepo = _unitOfWork.GetRepository<FBTransfer>();
            var sandikIcerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var urun = await urunRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null) return false;

            // FB Transfer kaydını at
            var fbTransfer = new FBTransfer
            {
                CekiSatiriId = urun.Id,
                KullaniciId = request.KullaniciId,
                AsilFB = request.AsilFB,
                AlinanFB = request.AlinanFB,
                Miktar = request.KarsilananAdet,
                Aciklama = request.Aciklama,
                IadeDurumu = request.IadeDurumu,
                Tarih = DateTime.UtcNow
            };
            await fbTransferRepo.AddAsync(fbTransfer);

            // Ürünü güncelle
            urun.Durum = UrunDurum.FBdenKarsilandi;
            urun.Remarks = $"FB Transfer ({request.AlinanFB}) - {request.KarsilananAdet} adet";
            urunRepo.Update(urun);

            // Sandık İçerik eksik miktarını kapat
            var icerik = (await sandikIcerikRepo.FindAsync(si => si.CekiSatiriId == urun.Id)).FirstOrDefault();
            if (icerik != null)
            {
                icerik.EksikAdet = Math.Max(0, icerik.EksikAdet - request.KarsilananAdet);
                icerik.KonulanAdet += request.KarsilananAdet;
                sandikIcerikRepo.Update(icerik);
            }

            await _unitOfWork.SaveChangesAsync();

            // Sektör/Sistem loğu
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "CekiSatiri",
                ReferansId = urun.Id.ToString(),
                Islem = "FB'den Karşılandı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"Asıl: {request.AsilFB}, Alınan: {request.AlinanFB}, Adet: {request.KarsilananAdet}"
            });

            return true;
        }
    }
}
