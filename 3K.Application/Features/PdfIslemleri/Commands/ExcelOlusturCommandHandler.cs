using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    public class ExcelOlusturCommandHandler : IRequestHandler<ExcelOlusturCommand, Result<byte[]>>
    {
        private readonly IPdfService _pdfService;
        private readonly IHareketService _hareketService;

        public ExcelOlusturCommandHandler(IPdfService pdfService, IHareketService hareketService)
        {
            _pdfService = pdfService;
            _hareketService = hareketService;
        }

        public async Task<Result<byte[]>> Handle(ExcelOlusturCommand request, CancellationToken cancellationToken)
        {
            var excelBytes = await _pdfService.ExcelOlusturAsync(request.ProjeId);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "Proje",
                ReferansId = request.ProjeId.ToString(),
                Islem = "Excel Oluşturuldu",
                IslemTipiId = (int)IslemTipi.ExcelIndirildi,
                KullaniciId = request.KullaniciId,
                Aciklama = "Orijinal çeki şablonu operasyon verileriyle doldurularak Excel indirildi"
            });

            return Result<byte[]>.Success(excelBytes);
        }
    }
}
