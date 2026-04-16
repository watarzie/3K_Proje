using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciSifreDegistirCommandHandler
        : IRequestHandler<KullaniciSifreDegistirCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public KullaniciSifreDegistirCommandHandler(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        public async Task<Result<bool>> Handle(KullaniciSifreDegistirCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.YeniSifre) || request.YeniSifre.Length < 6)
                return Result<bool>.Failure("Şifre en az 6 karakter olmalıdır.", 400);

            var repo = _unitOfWork.GetRepository<Kullanici>();
            var kullanici = await repo.GetByIdAsync(request.KullaniciId);

            if (kullanici == null)
                return Result<bool>.Failure("Kullanıcı bulunamadı.", 404);

            kullanici.SifreHash = _authService.HashPassword(request.YeniSifre);
            repo.Update(kullanici);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
