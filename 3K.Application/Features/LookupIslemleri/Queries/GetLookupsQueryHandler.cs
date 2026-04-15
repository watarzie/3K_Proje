using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.LookupIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.LookupIslemleri.Queries
{
    /// <summary>
    /// Lookup query handler — ILookupService üzerinden dinamik veri çeker.
    /// Clean Architecture: Application katmanı Infrastructure'a bağımlı değildir.
    /// </summary>
    public class GetLookupsQueryHandler : IRequestHandler<GetLookupsQuery, Result<Dictionary<string, List<LookupItemDto>>>>
    {
        private readonly ILookupService _lookupService;

        public GetLookupsQueryHandler(ILookupService lookupService)
        {
            _lookupService = lookupService;
        }

        public async Task<Result<Dictionary<string, List<LookupItemDto>>>> Handle(
            GetLookupsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.Entities == null || request.Entities.Count == 0)
            {
                return Result<Dictionary<string, List<LookupItemDto>>>.Failure(
                    "En az bir entity adı belirtilmelidir.");
            }

            // Güvenlik: Tüm entity adlarının geçerli olup olmadığını kontrol et
            foreach (var entityName in request.Entities)
            {
                if (!_lookupService.IsValidLookupEntity(entityName))
                {
                    return Result<Dictionary<string, List<LookupItemDto>>>.Failure(
                        $"Geçersiz lookup entity: '{entityName}'. Yalnızca LookupBase türevleri kabul edilir.");
                }
            }

            var lookups = await _lookupService.GetLookupsAsync(request.Entities, cancellationToken);

            // LookupBase → LookupItemDto dönüşümü
            var result = new Dictionary<string, List<LookupItemDto>>();
            foreach (var kvp in lookups)
            {
                result[kvp.Key] = kvp.Value.Select(item => new LookupItemDto
                {
                    Id = item.Id,
                    Anahtar = item.Anahtar,
                    Deger = item.Deger
                }).ToList();
            }

            return Result<Dictionary<string, List<LookupItemDto>>>.Success(result);
        }
    }
}
