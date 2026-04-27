using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeOlusturCommandHandler : IRequestHandler<ProjeOlusturCommand, Result<ProjeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ILookupCacheService _lookupCache;
        private readonly ICurrentUserService _currentUserService;

        public ProjeOlusturCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ILookupCacheService lookupCache, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
            _currentUserService = currentUserService;
        }

        public async Task<Result<ProjeDto>> Handle(ProjeOlusturCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            var proje = new Proje
            {
                ProjeNo = request.ProjeNo,
                Musteri = request.Musteri,
                DurumId = (int)ProjeDurum.Hazirlaniyor,
                ProjeTipiId = request.ProjeTipiId,
                PlanlananSevkTarihi = request.PlanlananSevkTarihi,
                SorumluKisi = request.SorumluKisi,
                Lokasyon = request.Lokasyon
            };

            await projeRepo.AddAsync(proje);
            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = proje.Id,
                ReferansTipi = "Proje",
                ReferansId = proje.Id.ToString(),
                Islem = "Proje Oluşturuldu",
                IslemTipiId = (int)IslemTipi.ProjeOlusturuldu,
                KullaniciId = _currentUserService.UserId ?? 0,
                Aciklama = $"Proje {request.ProjeNo} oluşturuldu (Tip: {_lookupCache.GetDeger<LookupProjeTipi>(request.ProjeTipiId)})"
            });

            return Result<ProjeDto>.Success(new ProjeDto
            {
                Id = proje.Id,
                ProjeNo = proje.ProjeNo,
                Musteri = proje.Musteri,
                DurumId = proje.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupProjeDurum>(proje.DurumId),
                ProjeTipiId = proje.ProjeTipiId,
                ProjeTipiMetni = _lookupCache.GetDeger<LookupProjeTipi>(proje.ProjeTipiId),
                PlanlananSevkTarihi = proje.PlanlananSevkTarihi,
                SorumluKisi = proje.SorumluKisi
            });
        }
    }
}
