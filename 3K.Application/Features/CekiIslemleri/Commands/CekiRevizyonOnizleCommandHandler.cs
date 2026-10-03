using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiRevizyonOnizleCommandHandler : IRequestHandler<CekiRevizyonOnizleCommand, Result<CekiRevizyonOnizlemeSonuc>>
    {
        private readonly ICekiRevizyonService _cekiRevizyon;

        public CekiRevizyonOnizleCommandHandler(ICekiRevizyonService cekiRevizyon)
        {
            _cekiRevizyon = cekiRevizyon;
        }

        public async Task<Result<CekiRevizyonOnizlemeSonuc>> Handle(CekiRevizyonOnizleCommand request, CancellationToken cancellationToken)
        {
            var sonuc = await _cekiRevizyon.CekiRevizyonOnizleAsync(request.ExcelDosya, request.DosyaAdi);
            return Result<CekiRevizyonOnizlemeSonuc>.Success(sonuc);
        }
    }
}
