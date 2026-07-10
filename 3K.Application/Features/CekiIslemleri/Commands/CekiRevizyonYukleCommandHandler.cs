using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.BildirimIslemleri.Events;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiRevizyonYukleCommandHandler : IRequestHandler<CekiRevizyonYukleCommand, Result<CekiRevizyonSonuc>>
    {
        private readonly ICekiService _cekiService;
        private readonly IPublisher _publisher;

        public CekiRevizyonYukleCommandHandler(ICekiService cekiService, IPublisher publisher)
        {
            _cekiService = cekiService;
            _publisher = publisher;
        }

        public async Task<Result<CekiRevizyonSonuc>> Handle(CekiRevizyonYukleCommand request, CancellationToken cancellationToken)
        {
            var sonuc = await _cekiService.CekiRevizyonYukleAsync(request.ExcelDosya, request.DosyaAdi, request.KullaniciId);

            await _publisher.Publish(new CekiDosyasiYuklendiEvent(
                sonuc.RevizyonCekiId,
                sonuc.ProjeId,
                sonuc.ProjeNo,
                request.DosyaAdi,
                request.KullaniciId,
                RevizyonMu: true,
                SatirSayisi: sonuc.IslenenRevizyonSatiriSayisi,
                SandikSayisi: 0,
                EklenenSatirSayisi: sonuc.EklenenSatirSayisi,
                GuncellenenSatirSayisi: sonuc.GuncellenenSatirSayisi,
                SilinenSatirSayisi: sonuc.SilinenSatirSayisi), CancellationToken.None);

            return Result<CekiRevizyonSonuc>.Success(sonuc);
        }
    }
}
