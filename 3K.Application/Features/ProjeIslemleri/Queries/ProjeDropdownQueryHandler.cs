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
            var hasFilter =
                request.ProjeTipiId.HasValue ||
                !string.IsNullOrWhiteSpace(request.SearchTerm) ||
                request.IsSevkEdilen.HasValue ||
                request.IncludeIds.Count > 0;

            var projeler = hasFilter
                ? await _projeRepository.GetLightFilteredAsync(
                    request.ProjeTipiId,
                    request.SearchTerm,
                    request.IsSevkEdilen,
                    request.Take,
                    request.IncludeIds,
                    cancellationToken)
                : await _projeRepository.GetAllLightAsync(cancellationToken);

            var result = projeler.Select(p => new ProjeDropdownDto
            {
                Id = p.Id,
                ProjeNo = p.ProjeNo,
                Musteri = p.Musteri,
                ProjeTipiId = p.ProjeTipiId,
                DurumId = p.DurumId,
                Lokasyon = p.Lokasyon
            });

            return Result<IEnumerable<ProjeDropdownDto>>.Success(result);
        }
    }
}
