using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Queries
{
    public class StokListeleQueryHandler : IRequestHandler<StokListeleQuery, Result<IEnumerable<StokKaydiDto>>>
    {
        private readonly IStokService _stokService;

        public StokListeleQueryHandler(IStokService stokService)
        {
            _stokService = stokService;
        }

        public async Task<Result<IEnumerable<StokKaydiDto>>> Handle(StokListeleQuery request, CancellationToken cancellationToken)
        {
            var stoklar = string.IsNullOrEmpty(request.MalzemeKodu)
                ? await _stokService.GetTumStoklarAsync()
                : await _stokService.GetUygunStoklarAsync(request.MalzemeKodu);

            var result = stoklar.Select(s => new StokKaydiDto
            {
                Id = s.Id,
                MalzemeKodu = s.MalzemeKodu,
                MalzemeAdi = s.MalzemeAdi,
                Miktar = s.Miktar,
                Birim = s.Birim,
                Lokasyon = s.Lokasyon,
                KaynakProje = s.KaynakProje,
                Durum = s.Durum.ToString()
            });

            return Result<IEnumerable<StokKaydiDto>>.Success(result);
        }
    }
}
