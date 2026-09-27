using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>Gereken menü izni projenin veritabanındaki gerçek tipinden çözülür.</summary>
    public class GetEksikUrunlerPdfQuery
        : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresProjectMenuPermission
    {
        public int ProjeId { get; set; }

        int IRequiresProjectMenuPermission.PermissionProjectId => ProjeId;
        ProjectMenuOperation IRequiresProjectMenuPermission.PermissionOperation => ProjectMenuOperation.MissingItemsReport;
        YetkiTipi IRequiresProjectMenuPermission.RequiredYetkiTipi => YetkiTipi.R;
    }

    public sealed class GetEksikUrunlerPdfQueryHandler(IUnitOfWork unitOfWork, IPdfService pdfService)
        : IRequestHandler<GetEksikUrunlerPdfQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetEksikUrunlerPdfQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var pdf = await pdfService.EksikUrunlerRaporuPdfOlusturAsync(request.ProjeId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(pdf,
                $"{proje.ProjeNo}_{RaporDosyaAdi.EksikRaporAdi(proje.ProjeTipiId)}.pdf"));
        }
    }
}
