using MediatR;
using _3K.Application.DTOs;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    /// <summary>
    /// İş akışı 2: Excel çeki dosyası okunur, satırlar parse edilir,
    /// aynı sandık numaralı satırlar gruplanır ve sandık kayıtları oluşturulur.
    /// </summary>
    public class CekiYukleCommandHandler : IRequestHandler<CekiYukleCommand, CekiYuklemeResultDto>
    {
        private readonly ICekiService _cekiService;

        public CekiYukleCommandHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<CekiYuklemeResultDto> Handle(CekiYukleCommand request, CancellationToken cancellationToken)
        {
            var ceki = await _cekiService.CekiYukleAsync(request.ProjeId, request.ExcelDosya, request.DosyaAdi);

            var satirlar = await _cekiService.GetCekiSatirlariAsync(ceki.Id);
            var satirList = satirlar.ToList();
            var benzersizSandikSayisi = satirList.Select(s => s.CekideGecenSandikNo).Distinct().Count();

            return new CekiYuklemeResultDto
            {
                CekiId = ceki.Id,
                SatirSayisi = satirList.Count,
                SandikSayisi = benzersizSandikSayisi,
                Mesaj = $"{satirList.Count} ürün satırı okundu, {benzersizSandikSayisi} benzersiz sandık oluşturuldu."
            };
        }
    }
}
