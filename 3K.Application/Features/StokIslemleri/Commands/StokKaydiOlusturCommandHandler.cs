using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.StokIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiOlusturCommandHandler : IRequestHandler<StokKaydiOlusturCommand, Result<StokKaydiDto>>
    {
        private readonly IStokService _stokService;

        public StokKaydiOlusturCommandHandler(IStokService stokService)
        {
            _stokService = stokService;
        }

        public async Task<Result<StokKaydiDto>> Handle(StokKaydiOlusturCommand request, CancellationToken cancellationToken)
        {
            var stok = await _stokService.StokKaydiOlusturAsync(new StokKaydi
            {
                MalzemeKodu = request.MalzemeKodu,
                MalzemeAdi = request.MalzemeAdi,
                Miktar = request.Miktar,
                Birim = request.Birim,
                Lokasyon = request.Lokasyon,
                KaynakProje = request.KaynakProje,
                StokGirisNedeni = request.StokGirisNedeni
            });

            return Result<StokKaydiDto>.Success(new StokKaydiDto
            {
                Id = stok.Id,
                MalzemeKodu = stok.MalzemeKodu,
                MalzemeAdi = stok.MalzemeAdi,
                Miktar = stok.Miktar,
                Birim = stok.Birim,
                Lokasyon = stok.Lokasyon,
                KaynakProje = stok.KaynakProje,
                StokGirisNedeni = stok.StokGirisNedeni,
                Durum = stok.Durum.ToString()
            });
        }
    }
}
