using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using MediatR;

namespace _3K.Application.Features.BildirimIslemleri.Queries
{
    public class GetBildirimAbonelikAyarlariQueryHandler
        : IRequestHandler<GetBildirimAbonelikAyarlariQuery, Result<List<BildirimAbonelikAyariDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBildirimAbonelikAyarlariQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<BildirimAbonelikAyariDto>>> Handle(
            GetBildirimAbonelikAyarlariQuery request,
            CancellationToken cancellationToken)
        {
            var kullanicilar = await _unitOfWork
                .GetRepository<Kullanici>()
                .GetAllWithIncludeAsync(kullanici => kullanici.Rol);
            var abonelikler = await _unitOfWork.GetRepository<BildirimAboneligi>().GetAllAsync();
            var abonelikSeti = abonelikler
                .Select(abonelik => (abonelik.KullaniciId, abonelik.TipId))
                .ToHashSet();

            var sonuc = kullanicilar
                .OrderBy(kullanici => kullanici.AdSoyad)
                .Select(kullanici => new BildirimAbonelikAyariDto
                {
                    KullaniciId = kullanici.Id,
                    AdSoyad = kullanici.AdSoyad,
                    Email = kullanici.Email,
                    Rol = kullanici.Rol?.Ad ?? "Belirtilmemiş",
                    CekiYuklendiBildirimi = abonelikSeti.Contains((kullanici.Id, (int)BildirimTipi.CekiYuklendi)),
                    CekiRevizyonuBildirimi = abonelikSeti.Contains((kullanici.Id, (int)BildirimTipi.CekiRevizyonuYuklendi))
                })
                .ToList();

            return Result<List<BildirimAbonelikAyariDto>>.Success(sonuc);
        }
    }
}
