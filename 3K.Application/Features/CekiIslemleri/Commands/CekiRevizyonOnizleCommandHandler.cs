using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiRevizyonOnizleCommandHandler : IRequestHandler<CekiRevizyonOnizleCommand, Result<CekiRevizyonOnizlemeSonuc>>
    {
        private readonly ICekiService _cekiService;

        public CekiRevizyonOnizleCommandHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<Result<CekiRevizyonOnizlemeSonuc>> Handle(CekiRevizyonOnizleCommand request, CancellationToken cancellationToken)
        {
            var sonuc = await _cekiService.CekiRevizyonOnizleAsync(request.ExcelDosya, request.DosyaAdi);
            return Result<CekiRevizyonOnizlemeSonuc>.Success(sonuc);
        }
    }
}
