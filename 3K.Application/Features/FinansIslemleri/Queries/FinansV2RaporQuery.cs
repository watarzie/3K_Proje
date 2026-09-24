using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansV2RaporQuery : FinansQuery<FinansDosyaDto>
{
    public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule), FinansYetkiKodlari.Read(FinansRaporPermissionRules.ExportPermission(Format))];
    public string Tur { get; init; } = "genel";
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansV2RaporQueryHandler(IFinansRaporService service) : IRequestHandler<FinansV2RaporQuery, Result<FinansDosyaDto>>
{
    public Task<Result<FinansDosyaDto>> Handle(FinansV2RaporQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(async () =>
        {
            var format = request.Format.ToLowerInvariant();
            if (format is not ("pdf" or "xlsx" or "excel")) throw new InvalidOperationException("Geçerli rapor formatı pdf veya xlsx olmalıdır.");
            var excel = format != "pdf";
            var bytes = await service.OzetRaporAsync(request.Tur, excel, request.Filtre, cancellationToken);
            return new FinansDosyaDto(bytes, excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf", $"finans-{request.Tur}.{(excel ? "xlsx" : "pdf")}");
        });
}
