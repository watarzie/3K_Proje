using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.StokIslemleri.Commands
{
    public class StokKaydiOlusturCommandHandler : IRequestHandler<StokKaydiOlusturCommand, StokKaydiDto>
    {
        private readonly IStokService _stokService;

        public StokKaydiOlusturCommandHandler(IStokService stokService)
        {
            _stokService = stokService;
        }

        public async Task<StokKaydiDto> Handle(StokKaydiOlusturCommand request, CancellationToken cancellationToken)
        {
            var stok = await _stokService.StokKaydiOlusturAsync(new StokKaydi
            {
                MalzemeKodu = request.MalzemeKodu,
                MalzemeAdi = request.MalzemeAdi,
                Miktar = request.Miktar,
                Birim = request.Birim,
                Lokasyon = request.Lokasyon,
                KaynakProje = request.KaynakProje
            });

            return new StokKaydiDto
            {
                Id = stok.Id,
                MalzemeKodu = stok.MalzemeKodu,
                MalzemeAdi = stok.MalzemeAdi,
                Miktar = stok.Miktar,
                Birim = stok.Birim,
                Lokasyon = stok.Lokasyon,
                KaynakProje = stok.KaynakProje,
                Durum = stok.Durum.ToString()
            };
        }
    }
}
