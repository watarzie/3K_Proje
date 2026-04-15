using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciGuncelleCommandHandler : IRequestHandler<KullaniciGuncelleCommand, Result<KullaniciDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public KullaniciGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<KullaniciDto>> Handle(KullaniciGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Kullanici>();
            var kullanici = await repo.GetByIdAsync(request.Id);

            if (kullanici == null)
                return Result<KullaniciDto>.Failure("Kullanıcı bulunamadı.");

            kullanici.AdSoyad = request.AdSoyad;
            kullanici.BasHarf = request.AdSoyad.Length >= 2
                ? request.AdSoyad[..2].ToUpper()
                : request.AdSoyad.ToUpper();
            kullanici.RolId = request.RolId;

            repo.Update(kullanici);
            await _unitOfWork.SaveChangesAsync();

            // Rol navigation'ını yeniden yükle
            var rolRepo = _unitOfWork.GetRepository<Rol>();
            var rol = await rolRepo.GetByIdAsync(kullanici.RolId);

            return Result<KullaniciDto>.Success(new KullaniciDto
            {
                Id = kullanici.Id,
                AdSoyad = kullanici.AdSoyad,
                BasHarf = kullanici.BasHarf,
                RolId = kullanici.RolId,
                Rol = rol?.Ad ?? "Unknown",
                Email = kullanici.Email
            });
        }
    }
}
