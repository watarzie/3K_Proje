using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Entities;

namespace _3K.Application.Features.ProjeIslemleri.Commands
{
    public class SandikKapatCommandHandler : IRequestHandler<SandikKapatCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public SandikKapatCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<bool>> Handle(SandikKapatCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Sandik>();
            var sandik = await repo.GetByIdAsync(request.SandikId);

            if (sandik == null)
                return Result<bool>.Failure("Sandık bulunamadı.", 404);
            sandik.DurumId = request.Kapali ? (int)SandikDurum.Hazir : (int)SandikDurum.Hazirlaniyor;

            repo.Update(sandik);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
    }
}
