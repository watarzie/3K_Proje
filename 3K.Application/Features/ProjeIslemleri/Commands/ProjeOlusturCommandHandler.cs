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

        public ProjeOlusturCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService, ILookupCacheService lookupCache)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _lookupCache = lookupCache;
        }

        public async Task<Result<ProjeDto>> Handle(ProjeOlusturCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            var proje = new Proje
            {
                ProjeNo = request.ProjeNo,
                Musteri = request.Musteri,
                DurumId = (int)ProjeDurum.Hazirlaniyor,
                PlanlananSevkTarihi = request.PlanlananSevkTarihi,
                SorumluKisi = request.SorumluKisi
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
                KullaniciId = request.KullaniciId,
                Aciklama = $"Proje {request.ProjeNo} oluşturuldu"
            });

            return Result<ProjeDto>.Success(new ProjeDto
            {
                Id = proje.Id,
                ProjeNo = proje.ProjeNo,
                Musteri = proje.Musteri,
                DurumId = proje.DurumId,
                DurumMetni = _lookupCache.GetDeger<LookupProjeDurum>(proje.DurumId),
                PlanlananSevkTarihi = proje.PlanlananSevkTarihi,
                SorumluKisi = proje.SorumluKisi
            });
        }
    }
}
