using MediatR;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    public class ExcelOlusturCommandHandler : IRequestHandler<ExcelOlusturCommand, byte[]>
    {
        private readonly IPdfService _pdfService;
        private readonly IHareketService _hareketService;

        public ExcelOlusturCommandHandler(IPdfService pdfService, IHareketService hareketService)
        {
            _pdfService = pdfService;
            _hareketService = hareketService;
        }

        public async Task<byte[]> Handle(ExcelOlusturCommand request, CancellationToken cancellationToken)
        {
            var excelBytes = await _pdfService.ExcelOlusturAsync(request.ProjeId);

            // Log kaydı
            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "Proje",
                ReferansId = request.ProjeId.ToString(),
                Islem = "Excel Oluşturuldu",
                KullaniciId = request.KullaniciId,
                Aciklama = "Orijinal çeki şablonu operasyon verileriyle doldurularak Excel indirildi"
            });

            return excelBytes;
        }
    }
}
