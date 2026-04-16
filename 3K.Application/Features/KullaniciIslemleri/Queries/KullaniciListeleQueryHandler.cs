using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    public class KullaniciListeleQueryHandler : IRequestHandler<KullaniciListeleQuery, Result<IEnumerable<KullaniciDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public KullaniciListeleQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IEnumerable<KullaniciDto>>> Handle(KullaniciListeleQuery request, CancellationToken cancellationToken)
        {
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var kullanicilar = await kullaniciRepo.GetAllWithIncludeAsync(k => k.Rol);

            var result = kullanicilar.Select(k => new KullaniciDto
            {
                Id = k.Id,
                AdSoyad = k.AdSoyad,
                BasHarf = k.BasHarf,
                RolId = k.RolId,
                Rol = k.Rol?.Ad ?? "Belirtilmemiş",
                Email = k.Email
            });

            return Result<IEnumerable<KullaniciDto>>.Success(result);
        }
    }
}
