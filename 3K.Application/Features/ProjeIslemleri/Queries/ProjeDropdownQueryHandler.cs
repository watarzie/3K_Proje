using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeDropdownQueryHandler : IRequestHandler<ProjeDropdownQuery, Result<IEnumerable<ProjeDropdownDto>>>
    {
        private readonly IProjeRepository _projeRepository;

        public ProjeDropdownQueryHandler(IProjeRepository projeRepository)
        {
            _projeRepository = projeRepository;
        }

        public async Task<Result<IEnumerable<ProjeDropdownDto>>> Handle(ProjeDropdownQuery request, CancellationToken cancellationToken)
        {
            var projeler = await _projeRepository.GetAllLightAsync(cancellationToken);

            var result = projeler.Select(p => new ProjeDropdownDto
            {
                Id = p.Id,
                ProjeNo = p.ProjeNo,
                Musteri = p.Musteri
            });

            return Result<IEnumerable<ProjeDropdownDto>>.Success(result);
        }
    }
}
