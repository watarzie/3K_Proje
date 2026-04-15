using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Commands
{
    public class RolOlusturCommandHandler : IRequestHandler<RolOlusturCommand, Result<RolDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public RolOlusturCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RolDto>> Handle(RolOlusturCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Ad))
                return Result<RolDto>.Failure("Rol adı boş olamaz.");

            var repo = _unitOfWork.GetRepository<Rol>();

            // Ad benzersizlik kontrolü
            var mevcut = (await repo.GetAllAsync()).Any(r => r.Ad.Equals(request.Ad, StringComparison.OrdinalIgnoreCase));
            if (mevcut)
                return Result<RolDto>.Failure($"'{request.Ad}' adında bir rol zaten mevcut.");

            var rol = new Rol { Ad = request.Ad };
            await repo.AddAsync(rol);
            await _unitOfWork.SaveChangesAsync();

            return Result<RolDto>.Success(new RolDto { Id = rol.Id, Ad = rol.Ad });
        }
    }
}
