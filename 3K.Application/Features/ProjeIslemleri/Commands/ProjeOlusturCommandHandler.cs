using MediatR;
using _3K.Application.Common;
using _3K.Application.DTOs;
using _3K.Core.Entities;

using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class ProjeOlusturCommandHandler : IRequestHandler<ProjeOlusturCommand, Result<ProjeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;

        public ProjeOlusturCommandHandler(IUnitOfWork unitOfWork, IHareketService hareketService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
        }

        public async Task<Result<ProjeDto>> Handle(ProjeOlusturCommand request, CancellationToken cancellationToken)
        {
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            var proje = new Proje
            {
                ProjeNo = request.ProjeNo,
                Musteri = request.Musteri,
                Durum = "Hazirlaniyor",
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
                KullaniciId = request.KullaniciId,
                Aciklama = $"Proje {request.ProjeNo} oluşturuldu"
            });

            return Result<ProjeDto>.Success(new ProjeDto
            {
                Id = proje.Id,
                ProjeNo = proje.ProjeNo,
                Musteri = proje.Musteri,
                Durum = proje.Durum.ToString(),
                PlanlananSevkTarihi = proje.PlanlananSevkTarihi,
                SorumluKisi = proje.SorumluKisi
            });
        }
    }
}
