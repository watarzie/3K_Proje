using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public sealed class FinansIsRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
            MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule),
            MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansAylikRaporDosyaQuery : FinansQuery<DosyaIcerigi>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KarlilikGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
            MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public int Yil { get; init; }
        public int Ay { get; init; }
        public IReadOnlyCollection<string> Gruplar { get; init; } = Array.Empty<string>();
    }

    public sealed class FinansSiparisDurumRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
            MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
        ];
        public string Format { get; init; } = "pdf";
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansRaporQueryHandlers :
        IRequestHandler<FinansIsRaporuDosyaQuery, Result<DosyaIcerigi>>,
        IRequestHandler<FinansGiderRaporuDosyaQuery, Result<DosyaIcerigi>>,
        IRequestHandler<FinansSiparisDurumRaporuDosyaQuery, Result<DosyaIcerigi>>,
        IRequestHandler<FinansAylikRaporDosyaQuery, Result<DosyaIcerigi>>
    {
        private readonly IFinansRaporService _service;

        public FinansRaporQueryHandlers(IFinansRaporService service) => _service = service;

        public Task<Result<DosyaIcerigi>> Handle(FinansIsRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return Task.FromResult(InvalidFormat());
            return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
                ? await _service.IslerExcelAsync(request.Filtre, cancellationToken)
                : await _service.IslerPdfAsync(request.Filtre, cancellationToken), excel, "finans-isleri"));
        }

        public Task<Result<DosyaIcerigi>> Handle(FinansGiderRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return Task.FromResult(InvalidFormat());
            return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
                ? await _service.GiderlerExcelAsync(request.Filtre, cancellationToken)
                : await _service.GiderlerPdfAsync(request.Filtre, cancellationToken), excel, "finans-giderleri"));
        }

        public Task<Result<DosyaIcerigi>> Handle(FinansAylikRaporDosyaQuery request, CancellationToken cancellationToken)
        {
            var format = request.Format.Trim().ToLowerInvariant();
            if (format is not ("pdf" or "xlsx" or "excel" or "ayri"))
                return Task.FromResult(Result<DosyaIcerigi>.Failure("Rapor formatı pdf, xlsx veya ayri olmalıdır.", 400));

            if (format == "ayri")
                return FinansHandlerHelper.ExecuteAsync(async () => new DosyaIcerigi(
                    await _service.AylikZipAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken),
                    "application/zip",
                    $"finans-aylik-raporlar-{request.Yil}-{request.Ay:00}-{TurkeyTime.Now:yyyyMMddHHmmss}.zip"));

            var excel = format is "xlsx" or "excel";
            return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
                ? await _service.AylikExcelAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken)
                : await _service.AylikPdfAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken),
                excel, $"finans-aylik-{request.Yil}-{request.Ay:00}"));
        }

        public Task<Result<DosyaIcerigi>> Handle(FinansSiparisDurumRaporuDosyaQuery request, CancellationToken cancellationToken)
        {
            if (!TryResolveFormat(request.Format, out var excel))
                return Task.FromResult(InvalidFormat());
            return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
                ? await _service.SiparisDurumExcelAsync(request.Filtre, cancellationToken)
                : await _service.SiparisDurumPdfAsync(request.Filtre, cancellationToken), excel, "finans-siparis-durumu"));
        }

        private static bool TryResolveFormat(string? format, out bool excel)
        {
            excel = string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase);
            return excel || string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);
        }

        private static Result<DosyaIcerigi> InvalidFormat()
            => Result<DosyaIcerigi>.Failure("Rapor formatı pdf veya xlsx olmalıdır.", 400);

        private static DosyaIcerigi NewFile(byte[] bytes, bool excel, string prefix)
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
                ? YetkiKodlari.Finans.ExcelAktar
                : YetkiKodlari.Finans.PdfAktar;
    }
}
