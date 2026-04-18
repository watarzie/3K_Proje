using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciSilCommandHandler : IRequestHandler<KullaniciSilCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public KullaniciSilCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(KullaniciSilCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Kullanici>();
            var kullanici = await repo.GetByIdAsync(request.Id);

            if (kullanici == null)
                return Result<bool>.Failure("Kullanıcı bulunamadı.");

            repo.Remove(kullanici);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
