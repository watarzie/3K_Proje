using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public sealed class FinansIsRaporuDosyaQuery : FinansQuery<FinansDosyaDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule),
            FinansYetkiKodlari.Write(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderRaporuDosyaQuery : FinansQuery<FinansDosyaDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GiderGoruntule),
            FinansYetkiKodlari.Write(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansAylikRaporDosyaQuery : FinansQuery<FinansDosyaDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GiderGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.KarlilikGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule),
            FinansYetkiKodlari.Write(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public int Yil { get; init; }
        public int Ay { get; init; }
        public IReadOnlyCollection<string> Gruplar { get; init; } = Array.Empty<string>();
    }

    public sealed class FinansSiparisDurumRaporuDosyaQuery : FinansQuery<FinansDosyaDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule),
            FinansYetkiKodlari.Write(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansRaporQueryHandlers :
        IRequestHandler<FinansIsRaporuDosyaQuery, Result<FinansDosyaDto>>,
        IRequestHandler<FinansGiderRaporuDosyaQuery, Result<FinansDosyaDto>>,
        IRequestHandler<FinansSiparisDurumRaporuDosyaQuery, Result<FinansDosyaDto>>,
        IRequestHandler<FinansAylikRaporDosyaQuery, Result<FinansDosyaDto>>
    {
        private readonly IFinansRaporService _service;

        public FinansRaporQueryHandlers(IFinansRaporService service) => _service = service;

        public async Task<Result<FinansDosyaDto>> Handle(FinansIsRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return InvalidFormat();
            var bytes = excel
                ? await _service.IslerExcelAsync(request.Filtre, cancellationToken)
                : await _service.IslerPdfAsync(request.Filtre, cancellationToken);
            return Result<FinansDosyaDto>.Success(NewFile(bytes, excel, "finans-isleri"));
        }

        public async Task<Result<FinansDosyaDto>> Handle(FinansGiderRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return InvalidFormat();
            var bytes = excel
                ? await _service.GiderlerExcelAsync(request.Filtre, cancellationToken)
                : await _service.GiderlerPdfAsync(request.Filtre, cancellationToken);
            return Result<FinansDosyaDto>.Success(NewFile(bytes, excel, "finans-giderleri"));
        }

        public async Task<Result<FinansDosyaDto>> Handle(FinansAylikRaporDosyaQuery request, CancellationToken cancellationToken)
        {
            if (request.Yil is < 2000 or > 2200 || request.Ay is < 1 or > 12)
                return Result<FinansDosyaDto>.Failure("Geçerli bir yıl ve ay seçilmelidir.");
            var format = request.Format.Trim().ToLowerInvariant();
            if (format is not ("pdf" or "xlsx" or "excel" or "ayri"))
                return Result<FinansDosyaDto>.Failure("Rapor formatı pdf, xlsx veya ayri olmalıdır.", 400);

            if (format == "ayri")
            {
                var zip = await _service.AylikZipAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken);
                return Result<FinansDosyaDto>.Success(new FinansDosyaDto(
                    zip,
                    "application/zip",
                    $"finans-aylik-raporlar-{request.Yil}-{request.Ay:00}-{TurkeyTime.Now:yyyyMMddHHmmss}.zip"));
            }

            var excel = format is "xlsx" or "excel";
            var bytes = excel
                ? await _service.AylikExcelAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken)
                : await _service.AylikPdfAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken);
            return Result<FinansDosyaDto>.Success(NewFile(bytes, excel, $"finans-aylik-{request.Yil}-{request.Ay:00}"));
        }

        public async Task<Result<FinansDosyaDto>> Handle(FinansSiparisDurumRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return InvalidFormat();
            var bytes = excel
                ? await _service.SiparisDurumExcelAsync(request.Filtre, cancellationToken)
                : await _service.SiparisDurumPdfAsync(request.Filtre, cancellationToken);
            return Result<FinansDosyaDto>.Success(NewFile(bytes, excel, "finans-siparis-durumu"));
        }

        private static bool TryResolveFormat(string? format, out bool excel)
        {
            excel = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase);
            return excel || string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static Result<FinansDosyaDto> InvalidFormat()
            => Result<FinansDosyaDto>.Failure("Rapor formatı pdf veya xlsx olmalıdır.", 400);

        private static FinansDosyaDto NewFile(byte[] bytes, bool excel, string prefix)
            => new(
                bytes,
                excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf",
                $"{prefix}-{TurkeyTime.Now:yyyyMMddHHmmss}.{(excel ? "xlsx" : "pdf")}");
    }

    internal static class FinansRaporPermissionRules
    {
        public static string ExportPermission(string? format)
            => string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase)
                ? FinansYetkiKodlari.ExcelAktar
                : FinansYetkiKodlari.PdfAktar;
    }
}
