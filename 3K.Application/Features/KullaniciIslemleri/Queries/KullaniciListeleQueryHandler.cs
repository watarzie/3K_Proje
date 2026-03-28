using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Queries
{
    public class KullaniciListeleQueryHandler : IRequestHandler<KullaniciListeleQuery, IEnumerable<KullaniciDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public KullaniciListeleQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<KullaniciDto>> Handle(KullaniciListeleQuery request, CancellationToken cancellationToken)
        {
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var kullanicilar = await kullaniciRepo.GetAllAsync();

            return kullanicilar.Select(k => new KullaniciDto
            {
                Id = k.Id,
                AdSoyad = k.AdSoyad,
                BasHarf = k.BasHarf,
                Rol = k.Rol.ToString(),
                Email = k.Email
            });
        }
    }
}
