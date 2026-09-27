using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansV2RaporQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule), MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))];
    public string Tur { get; init; } = "genel";
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansV2RaporQueryHandler(IFinansRaporService service) : IRequestHandler<FinansV2RaporQuery, Result<DosyaIcerigi>>
{
    public Task<Result<DosyaIcerigi>> Handle(FinansV2RaporQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(async () =>
        {
            var format = request.Format.ToLowerInvariant();
            if (format is not ("pdf" or "xlsx" or "excel")) throw new InvalidOperationException("Geçerli rapor formatı pdf veya xlsx olmalıdır.");
            var excel = format != "pdf";
            var bytes = await service.OzetRaporAsync(request.Tur, excel, request.Filtre, cancellationToken);
            return new DosyaIcerigi(bytes, excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", $"finans-{request.Tur}.{(excel ? "xlsx" : "pdf")}");
        });
}
