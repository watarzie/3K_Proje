using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public class GetBekleyenSayisiQueryHandler : IRequestHandler<GetBekleyenSayisiQuery, Result<int>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetBekleyenSayisiQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(GetBekleyenSayisiQuery request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<OnayBekleyenIslem>();
            var allItems = await repo.FindAsync(o => o.Durum == Core.Enums.OnayDurumu.Bekliyor);
            var count = allItems.Count();

            return Result<int>.Success(count);
        }
    }
}
