using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    public class RolSilCommandHandler : IRequestHandler<RolSilCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RolSilCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(RolSilCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Rol>();
            var rol = (await repo.GetAllAsync()).FirstOrDefault(r => r.Id == request.Id);

            if (rol == null)
                return Result<bool>.Failure("Rol bulunamadı.", 404);

            // Sistem yöneticisi rolü kimliğiyle korunur; ad değişse de silinemez.
            if (SistemRolleri.AdminMi(rol.Id))
                return Result<bool>.Failure("Admin rolü silinemez.");

            // Bu role atanmış kullanıcı var mı kontrol et
            var kullaniciRepo = _unitOfWork.GetRepository<Kullanici>();
            var atanmisKullanici = (await kullaniciRepo.GetAllAsync()).Any(k => k.RolId == request.Id);
            if (atanmisKullanici)
                return Result<bool>.Failure("Bu role atanmış kullanıcılar var. Önce kullanıcıların rolleri değiştirilmeli.");

            repo.Remove(rol);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
