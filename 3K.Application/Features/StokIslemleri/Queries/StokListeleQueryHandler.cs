using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    public class StokListeleQueryHandler : IRequestHandler<StokListeleQuery, Result<PaginatedList<StokKaydiDto>>>
    {
        private readonly IStokService _stokService;

        public StokListeleQueryHandler(IStokService stokService)
        {
            _stokService = stokService;
        }

        public async Task<Result<PaginatedList<StokKaydiDto>>> Handle(StokListeleQuery request, CancellationToken cancellationToken)
        {
            var pagedData = await _stokService.GetPaginatedStoklarAsync(request.SearchTerm, request.PageNumber, request.PageSize);

            var dtos = pagedData.Items.Select(s => new StokKaydiDto
            {
                Id = s.Id,
                MalzemeKodu = s.MalzemeKodu,
                MalzemeAdi = s.MalzemeAdi,
                Miktar = s.Miktar,
                Birim = s.Birim,
                Lokasyon = s.Lokasyon,
                KaynakProje = s.KaynakProje,
                StokGirisNedeni = s.StokGirisNedeni,
                Durum = s.Durum.ToString()
            }).ToList();

            var paginatedList = new PaginatedList<StokKaydiDto>(dtos, pagedData.TotalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedList<StokKaydiDto>>.Success(paginatedList);
        }
    }
}
