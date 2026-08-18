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
        private readonly IIkiFaktorService _ikiFaktorService;

        public KullaniciGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IIkiFaktorService ikiFaktorService)
        {
            _unitOfWork = unitOfWork;
            _ikiFaktorService = ikiFaktorService;
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

            // GetByIdAsync tracked entity döndürür. Update çağrısı bütün kolonları
            // modified işaretleyip eşzamanlı 2FA flag değişikliğini ezebilirdi.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Rol navigation'ını yeniden yükle
            var rolRepo = _unitOfWork.GetRepository<Rol>();
            var rol = await rolRepo.GetByIdAsync(kullanici.RolId);
            kullanici.Rol = rol!;
            var ayarDurumlari = await _ikiFaktorService.AyarDurumlariniGetirAsync(
                new[] { kullanici.Id },
                cancellationToken);
            ayarDurumlari.TryGetValue(kullanici.Id, out var ayarDurumu);

            return Result<KullaniciDto>.Success(
                AuthDtoFactory.Kullanici(kullanici, ayarDurumu));
        }
    }
}
