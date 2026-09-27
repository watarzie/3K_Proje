using MediatR;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Commands
{
    /// <summary>Proje çeki PDF'ini üretir ve indirme hareketini kaydeder.</summary>
    public class PdfOlusturCommand : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
    }

    public sealed class PdfOlusturCommandHandler(
        IUnitOfWork unitOfWork,
        IPdfService pdfService,
        IHareketService hareketService,
        ICurrentUserService currentUser) : IRequestHandler<PdfOlusturCommand, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(PdfOlusturCommand request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await pdfService.PdfOlusturAsync(request.ProjeId);

            await hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                ReferansTipi = "Proje",
                ReferansId = request.ProjeId.ToString(),
                Islem = "PDF Oluşturuldu",
                IslemTipiId = (int)IslemTipi.PDFIndirildi,
                KullaniciId = currentUser.UserId ?? 0,
                Aciklama = "Proje çeki PDF'i oluşturuldu"
            });

            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(pdf, $"{proje.ProjeNo}_CekiRaporu.pdf"));
        }
    }
}
