using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>Gereken menü izni projenin veritabanındaki gerçek tipinden çözülür.</summary>
    public class GetEksikUrunlerExcelQuery
        : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresProjectMenuPermission
    {
        public int ProjeId { get; set; }

        int IRequiresProjectMenuPermission.PermissionProjectId => ProjeId;
        ProjectMenuOperation IRequiresProjectMenuPermission.PermissionOperation => ProjectMenuOperation.MissingItemsReport;
        YetkiTipi IRequiresProjectMenuPermission.RequiredYetkiTipi => YetkiTipi.R;
    }

    public sealed class GetEksikUrunlerExcelQueryHandler(IUnitOfWork unitOfWork, IEksikUrunlerRaporService eksikUrunlerRaporu)
        : IRequestHandler<GetEksikUrunlerExcelQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetEksikUrunlerExcelQuery request, CancellationToken cancellationToken)
        {
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            var excel = await eksikUrunlerRaporu.EksikUrunlerRaporuExcelOlusturAsync(request.ProjeId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(excel,
                $"{proje.ProjeNo}_{RaporDosyaAdi.EksikRaporAdi(proje.ProjeTipiId)}.xlsx"));
        }
    }
}
