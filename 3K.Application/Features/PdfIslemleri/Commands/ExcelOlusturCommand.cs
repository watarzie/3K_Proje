using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>Orijinal çeki şablonunu operasyon verileriyle doldurur ve indirme hareketini kaydeder.</summary>
    public class ExcelOlusturCommand : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class ExcelOlusturCommandHandler(
        IUnitOfWork unitOfWork,
        IPdfService pdfService,
        IHareketService hareketService,
        ICurrentUserService currentUser) : IRequestHandler<ExcelOlusturCommand, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(ExcelOlusturCommand request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var excel = await pdfService.ExcelOlusturAsync(request.ProjeId);

            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "Proje",
                ReferansId = request.ProjeId.ToString(),
                Islem = "Excel Oluşturuldu",
                IslemTipiId = (int)IslemTipi.ExcelIndirildi,
                KullaniciId = currentUser.UserId ?? 0,
                Aciklama = "Orijinal çeki şablonu operasyon verileriyle doldurularak Excel indirildi"
            });

            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(excel, $"{proje.ProjeNo}_Ceki.xlsx"));
        }
    }
}
