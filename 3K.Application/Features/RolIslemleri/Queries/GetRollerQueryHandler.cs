using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.RolIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.RolIslemleri.Queries
{
    public class GetRollerQueryHandler : IRequestHandler<GetRollerQuery, Result<List<RolDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRollerQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<RolDto>>> Handle(GetRollerQuery request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Rol>();
            var roller = await repo.GetAllAsync();

            var result = roller
                .OrderBy(r => r.Ad)
                .Select(r => new RolDto { Id = r.Id, Ad = r.Ad })
                .ToList();

            return Result<List<RolDto>>.Success(result);
        }
    }
}
