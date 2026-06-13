using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiRevizyonYukleCommandHandler : IRequestHandler<CekiRevizyonYukleCommand, Result<CekiRevizyonSonuc>>
    {
        private readonly ICekiService _cekiService;

        public CekiRevizyonYukleCommandHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<Result<CekiRevizyonSonuc>> Handle(CekiRevizyonYukleCommand request, CancellationToken cancellationToken)
        {
            var sonuc = await _cekiService.CekiRevizyonYukleAsync(request.ExcelDosya, request.DosyaAdi, request.KullaniciId);
            return Result<CekiRevizyonSonuc>.Success(sonuc);
        }
    }
}
