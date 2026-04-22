using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SandikOzellikGuncelleCommandHandler : IRequestHandler<SandikOzellikGuncelleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SandikOzellikGuncelleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(SandikOzellikGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result.Failure("Sandık bulunamadı", 404);

            sandik.En = request.En;
            sandik.Boy = request.Boy;
            sandik.Yukseklik = request.Yukseklik;
            sandik.NetKg = request.NetKg;
            sandik.GrossKg = request.GrossKg;

            repo.Update(sandik);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
    }
}
