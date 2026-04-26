using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.NotIslemleri.Commands
{
    public class NotEkleCommandHandler : IRequestHandler<NotEkleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHareketService _hareketService;

        public NotEkleCommandHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _hareketService = hareketService;
        }

        public async Task<Result> Handle(NotEkleCommand request, CancellationToken cancellationToken)
        {
            // Yazan tarafı belirle (kullanıcının rolüne göre)
            var yazanTarafId = _currentUserService.Roles?.Contains(StatusConstants.KullaniciRol.PersonelGrid) == true
                ? (int)NotYazanTaraf.Grid
                : (int)NotYazanTaraf.UcK;

            var not = new Not
            {
                YazanTarafId = yazanTarafId,
                Icerik = request.Icerik,
                Tarih = DateTime.UtcNow,
                KullaniciId = _currentUserService.UserId ?? 0,
                BagliReferansTipi = request.BagliReferansTipi,
                BagliReferansId = request.BagliReferansId,
                CekiSatiriId = request.CekiSatiriId
            };

            var notRepo = _unitOfWork.GetRepository<Not>();
            await notRepo.AddAsync(not);
            await _unitOfWork.SaveChangesAsync();

            // Hareket kaydı
            var tarafMetni = yazanTarafId == (int)NotYazanTaraf.Grid ? "Grid" : "3K";
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = request.BagliReferansTipi,
                ReferansId = request.BagliReferansId.ToString(),
                Islem = $"{tarafMetni} Not Eklendi",
                IslemTipiId = (int)IslemTipi.NotEklendi,
                Aciklama = request.Icerik.Length > 100
                    ? request.Icerik[..100] + "..."
                    : request.Icerik
            });

            return Result.Success();
        }
    }
}
