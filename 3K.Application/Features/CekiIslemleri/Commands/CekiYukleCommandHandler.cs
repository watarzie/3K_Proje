using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiYukleCommandHandler : IRequestHandler<CekiYukleCommand, Result<CekiYuklemeResultDto>>
    {
        private readonly ICekiService _cekiService;

        public CekiYukleCommandHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<Result<CekiYuklemeResultDto>> Handle(CekiYukleCommand request, CancellationToken cancellationToken)
        {
            var ceki = await _cekiService.CekiYukleAsync(request.ExcelDosya, request.DosyaAdi);

            var satirlar = await _cekiService.GetCekiSatirlariAsync(ceki.Id);
            var satirList = satirlar.ToList();
            var benzersizSandikSayisi = satirList.Select(s => s.CekideGecenSandikNo).Distinct().Count();

            return Result<CekiYuklemeResultDto>.Success(new CekiYuklemeResultDto
            {
                CekiId = ceki.Id,
                SatirSayisi = satirList.Count,
                SandikSayisi = benzersizSandikSayisi,
                Mesaj = $"{satirList.Count} ürün satırı okundu, {benzersizSandikSayisi} benzersiz sandık oluşturuldu."
            });
        }
    }
}
