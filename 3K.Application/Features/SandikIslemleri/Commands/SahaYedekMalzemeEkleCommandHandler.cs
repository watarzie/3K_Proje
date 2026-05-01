using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SahaYedekMalzemeEkleCommandHandler : IRequestHandler<SahaYedekMalzemeEkleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;

        public SahaYedekMalzemeEkleCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(SahaYedekMalzemeEkleCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            // Sandık "Sevk Edildi" durumundaysa ekleme yapılamaz
            if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
                return Result.Failure("Sevk edilmiş sandığa malzeme eklenemez.");

            var icerik = new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = request.CekiSatiriId, // Projeden seçildi ise dolu, elle girildi ise null
                BarkodNo = request.BarkodNo,
                Isim = request.Isim,
                Miktar = request.Miktar,
                BirimId = request.BirimId,
                KonulanAdet = (int)request.Miktar,
                EksikAdet = 0,
                KaynakProjeNo = request.KaynakProjeNo
            };

            await icerikRepo.AddAsync(icerik);

            // Sandık durumu Boş ise Hazırlanıyor'a geçir
            if (sandik.DurumId == (int)SandikDurum.Bos)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                sandikRepo.Update(sandik);
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "SandikIcerik",
                ReferansId = icerik.Id.ToString(),
                Islem = "Saha/Yedek Malzeme Eklendi",
                IslemTipiId = (int)IslemTipi.SahaYedekMalzemeEklendi,
                Aciklama = $"'{request.Isim}' — {request.Miktar} {(request.BirimId.HasValue ? ((Birim)request.BirimId.Value).ToString() : "Adet")} — Sandık {sandik.SandikNo}"
            });

            return Result.Success();
        }
    }
}
