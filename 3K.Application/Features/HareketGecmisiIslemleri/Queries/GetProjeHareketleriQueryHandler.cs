using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.HareketGecmisiIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.HareketGecmisiIslemleri.Queries
{
    public class GetProjeHareketleriQueryHandler : IRequestHandler<GetProjeHareketleriQuery, Result<IEnumerable<HareketGecmisiDto>>>
    {
        private readonly IHareketService _hareketService;

        public GetProjeHareketleriQueryHandler(IHareketService hareketService)
        {
            _hareketService = hareketService;
        }

        public async Task<Result<IEnumerable<HareketGecmisiDto>>> Handle(GetProjeHareketleriQuery request, CancellationToken cancellationToken)
        {
            var hareketler = await _hareketService.GetProjeHareketleriAsync(request.ProjeId);

            var result = hareketler.Select(h => new HareketGecmisiDto
            {
                Id = h.Id,
                Islem = h.Islem,
                ReferansTipi = h.ReferansTipi,
                ReferansId = h.ReferansId,
                EskiDeger = h.EskiDeger,
                YeniDeger = h.YeniDeger,
                Aciklama = h.Aciklama,
                KullaniciAdi = h.Kullanici?.AdSoyad ?? "",
                Tarih = h.Tarih
            });

            return Result<IEnumerable<HareketGecmisiDto>>.Success(result);
        }
    }
}
