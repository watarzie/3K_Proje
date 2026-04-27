using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class UcKTeslimAlCommandHandler : IRequestHandler<UcKTeslimAlCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDurumHesaplaService _durumHesaplaService;
        private readonly IHareketService _hareketService;

        public UcKTeslimAlCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IDurumHesaplaService durumHesaplaService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _durumHesaplaService = durumHesaplaService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(UcKTeslimAlCommand request, CancellationToken cancellationToken)
        {
            if (request.GelenMiktar <= 0)
                return Result.Failure("Gelen miktar 0'dan büyük olmalıdır.", 400);

            var repo = _unitOfWork.GetRepository<CekiSatiri>();
            var satir = await repo.GetByIdAsync(request.CekiSatiriId);

            if (satir == null)
                return Result.Failure("Ürün bulunamadı.", 404);

            var eskiGelenMiktar = satir.GelenMiktar;
            var eskiUcKDurum = satir.UcKDurumuId;

            // Kümülatif toplama — parça parça gelebilir
            satir.GelenMiktar += request.GelenMiktar;
            satir.TeslimTarihi = DateTime.UtcNow;
            satir.UcKAciklama = request.Aciklama;

            // 3K durumunu otomatik belirle
            if (satir.GelenMiktar >= satir.IstenenAdet)
                satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
            else
                satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;

            // Genel durumu otomatik hesapla
            satir.DurumId = _durumHesaplaService.HesaplaGenelDurum(satir.GridDurumuId, satir.UcKDurumuId);

            repo.Update(satir);
            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = satir.Id.ToString(),
                Islem = "3K Teslim Alma",
                IslemTipiId = (int)IslemTipi.UcKTeslimAlindi,
                EskiDeger = $"GelenMiktar:{eskiGelenMiktar}, UcKDurum:{eskiUcKDurum}",
                YeniDeger = $"GelenMiktar:{satir.GelenMiktar}, UcKDurum:{satir.UcKDurumuId}",
                Aciklama = $"+{request.GelenMiktar} adet teslim alındı. {request.Aciklama}"
            });

            return Result.Success();
        }
    }
}
