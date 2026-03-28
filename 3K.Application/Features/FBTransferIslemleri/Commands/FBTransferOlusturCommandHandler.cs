using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.FBTransferIslemleri.Commands
{
    public class FBTransferOlusturCommandHandler : IRequestHandler<FBTransferOlusturCommand, FBTransferResultDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public FBTransferOlusturCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<FBTransferResultDto> Handle(FBTransferOlusturCommand request, CancellationToken cancellationToken)
        {
            var fbTransferRepo = _unitOfWork.GetRepository<FBTransfer>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            // Ürünü getir
            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null)
                throw new KeyNotFoundException($"Çeki satırı bulunamadı: {request.CekiSatiriId}");

            // FB Transfer kaydı oluştur
            var transfer = new FBTransfer
            {
                CekiSatiriId = request.CekiSatiriId,
                KullaniciId = request.KullaniciId,
                AsilFB = request.AsilFB,
                AlinanFB = request.AlinanFB,
                Miktar = request.Miktar,
                Neden = request.Neden,
                IadeDurumu = request.IadeDurumu,
                Aciklama = request.Aciklama
            };

            await fbTransferRepo.AddAsync(transfer);

            // Ürün durumunu güncelle
            urun.Durum = UrunDurum.FBdenKarsilandi;
            // Opsiyonel: Açıklama alanına otomatik not
            var otomatikNot = $"{request.AlinanFB}'den temin edildi";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks) ? otomatikNot : $"{urun.Remarks}; {otomatikNot}";
            cekiSatiriRepo.Update(urun);

            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "FBTransfer",
                ReferansId = transfer.Id.ToString(),
                Islem = "FB Transferi Yapıldı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"{request.AsilFB} → {request.AlinanFB}, Miktar: {request.Miktar}"
            });

            return new FBTransferResultDto
            {
                Id = transfer.Id,
                AsilFB = transfer.AsilFB,
                AlinanFB = transfer.AlinanFB,
                Miktar = transfer.Miktar,
                Neden = transfer.Neden,
                IadeDurumu = transfer.IadeDurumu,
                Aciklama = transfer.Aciklama,
                Tarih = transfer.Tarih
            };
        }
    }
}
