using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Queries
{
    public class CekiSatirlariQueryHandler : IRequestHandler<CekiSatirlariQuery, IEnumerable<CekiSatiriDto>>
    {
        private readonly ICekiService _cekiService;

        public CekiSatirlariQueryHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<IEnumerable<CekiSatiriDto>> Handle(CekiSatirlariQuery request, CancellationToken cancellationToken)
        {
            var satirlar = await _cekiService.GetCekiSatirlariAsync(request.CekiId);

            return satirlar.Select(s => new CekiSatiriDto
            {
                Id = s.Id,
                SiraNo = s.SiraNo,
                BarkodNo = s.BarkodNo,
                Aciklama = s.Aciklama,
                IstenenAdet = s.IstenenAdet,
                Birim = s.Birim,
                CekideGecenSandikNo = s.CekideGecenSandikNo,
                FiiliSandikNo = s.FiiliSandikNo,
                Remarks = s.Remarks,
                Durum = s.Durum.ToString(),
                PaketleyenBasHarf = s.Paketleyen?.BasHarf,
                KontrolEdenBasHarf = s.KontrolEden?.BasHarf
            });
        }
    }
}
