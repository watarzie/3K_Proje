using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FBTransferIslemleri.DTOs;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.FBTransferIslemleri.Commands
{
    public class FBTransferOlusturCommandHandler : IRequestHandler<FBTransferOlusturCommand, Result<FBTransferResultDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public FBTransferOlusturCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result<FBTransferResultDto>> Handle(FBTransferOlusturCommand request, CancellationToken cancellationToken)
        {
            var fbTransferRepo = _unitOfWork.GetRepository<FBTransfer>();
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();

            var urun = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId);
            if (urun == null)
                return Result<FBTransferResultDto>.Failure($"Çeki satırı bulunamadı: {request.CekiSatiriId}", 404);

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

            urun.Durum = StatusConstants.UrunDurum.FBdenKarsilandi;
            var otomatikNot = $"{request.AlinanFB}'den temin edildi";
            urun.Remarks = string.IsNullOrEmpty(urun.Remarks) ? otomatikNot : $"{urun.Remarks}; {otomatikNot}";
            cekiSatiriRepo.Update(urun);

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "FBTransfer",
                ReferansId = transfer.Id.ToString(),
                Islem = "FB Transferi Yapıldı",
                KullaniciId = request.KullaniciId,
                Aciklama = $"{request.AsilFB} → {request.AlinanFB}, Miktar: {request.Miktar}"
            });

            return Result<FBTransferResultDto>.Success(new FBTransferResultDto
            {
                Id = transfer.Id,
                AsilFB = transfer.AsilFB,
                AlinanFB = transfer.AlinanFB,
                Miktar = transfer.Miktar,
                Neden = transfer.Neden,
                IadeDurumu = transfer.IadeDurumu,
                Aciklama = transfer.Aciklama,
                Tarih = transfer.Tarih
            });
        }
    }
}
