using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using 3K.Core.Entities;
using 3K.Core.Interfaces;

namespace 3K.Application.Features.SandikIslemleri.Queries
{
    public class GetProjeSandiklariQueryHandler : IRequestHandler<GetProjeSandiklariQuery, IEnumerable<Sandik>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProjeSandiklariQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Sandik>> Handle(GetProjeSandiklariQuery request, CancellationToken cancellationToken)
    {
        var sandikRepo = _unitOfWork.GetRepository<Sandik>();
        // Sadece ilgili projeye ait sandıkları getiriyoruz
        return await sandikRepo.FindAsync(s => s.ProjeId == request.ProjeId);
    }
}
}