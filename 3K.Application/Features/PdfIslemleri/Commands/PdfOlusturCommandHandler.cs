using MediatR;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    public class PdfOlusturCommandHandler : IRequestHandler<PdfOlusturCommand, byte[]>
    {
        private readonly IPdfService _pdfService;
        private readonly IHareketService _hareketService;

        public PdfOlusturCommandHandler(IPdfService pdfService, IHareketService hareketService)
        {
            _pdfService = pdfService;
            _hareketService = hareketService;
        }

        public async Task<byte[]> Handle(PdfOlusturCommand request, CancellationToken cancellationToken)
        {
            var pdfBytes = await _pdfService.PdfOlusturAsync(request.ProjeId);

            // Log kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "Proje",
                ReferansId = request.ProjeId.ToString(),
                Islem = "PDF Oluşturuldu",
                KullaniciId = request.KullaniciId,
                Aciklama = "Proje çeki PDF'i oluşturuldu"
            });

            return pdfBytes;
        }
    }
}
