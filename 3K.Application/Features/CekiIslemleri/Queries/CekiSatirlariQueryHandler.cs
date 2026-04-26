using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Queries
{
    public class CekiSatirlariQueryHandler : IRequestHandler<CekiSatirlariQuery, Result<IEnumerable<CekiSatiriDto>>>
    {
        private readonly ICekiService _cekiService;

        public CekiSatirlariQueryHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<Result<IEnumerable<CekiSatiriDto>>> Handle(CekiSatirlariQuery request, CancellationToken cancellationToken)
        {
            var satirlar = await _cekiService.GetCekiSatirlariAsync(request.CekiId);

            var result = satirlar.Select(s => new CekiSatiriDto
            {
                Id = s.Id,
                SiraNo = s.SiraNo,
                OlcuResmiPozNo = s.OlcuResmiPozNo,
                BarkodNo = s.BarkodNo,
                Aciklama = s.Aciklama,
                IstenenAdet = s.IstenenAdet,
                Birim = ((Birim)s.BirimId).ToString(),
                CekideGecenSandikNo = s.CekideGecenSandikNo,
                FiiliSandikNo = s.FiiliSandikNo,
                Remarks = s.Remarks,
                Durum = s.DurumId.ToString(),
                PaketleyenBasHarf = s.Paketleyen?.BasHarf,
                KontrolEdenBasHarf = s.KontrolEden?.BasHarf
            });

            return Result<IEnumerable<CekiSatiriDto>>.Success(result);
        }
    }
}
