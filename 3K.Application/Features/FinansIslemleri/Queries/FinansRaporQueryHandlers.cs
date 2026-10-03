using MediatR;
using _3K.Application.Common;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansRaporQueryHandlers(IFinansRaporService service) :
    IRequestHandler<FinansIsRaporuDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansGiderRaporuDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansSiparisDurumRaporuDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansAylikRaporDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansOzetRaporuDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansAlacakRaporuDosyaQuery, Result<DosyaIcerigi>>,
    IRequestHandler<FinansRaporOnizlemeQuery, Result<FinansRaporOnizlemeModel>>
{
    public Task<Result<FinansRaporOnizlemeModel>> Handle(FinansRaporOnizlemeQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.OnizlemeAsync(request.Tur.Trim().ToLowerInvariant(), request.Filtre, cancellationToken));

    public Task<Result<DosyaIcerigi>> Handle(FinansAlacakRaporuDosyaQuery request, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(request.Format, out var excel))
            return Task.FromResult(InvalidFormat());
        return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
            ? await service.AlacakExcelAsync(request.Filtre, cancellationToken)
            : await service.AlacakPdfAsync(request.Filtre, cancellationToken), excel, "finans-alacaklar"));
    }

    public Task<Result<DosyaIcerigi>> Handle(FinansIsRaporuDosyaQuery request, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(request.Format, out var excel))
            return Task.FromResult(InvalidFormat());
        return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
            ? await service.IslerExcelAsync(request.Filtre, cancellationToken)
            : await service.IslerPdfAsync(request.Filtre, cancellationToken), excel, "finans-isleri"));
    }

    public Task<Result<DosyaIcerigi>> Handle(FinansGiderRaporuDosyaQuery request, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(request.Format, out var excel))
            return Task.FromResult(InvalidFormat());
        return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
            ? await service.GiderlerExcelAsync(request.Filtre, cancellationToken)
            : await service.GiderlerPdfAsync(request.Filtre, cancellationToken), excel, "finans-giderleri"));
    }

    public Task<Result<DosyaIcerigi>> Handle(FinansAylikRaporDosyaQuery request, CancellationToken cancellationToken)
    {
        var format = request.Format.Trim().ToLowerInvariant();
        if (format is not ("pdf" or "xlsx" or "excel" or "ayri"))
            return Task.FromResult(Result<DosyaIcerigi>.Failure("Rapor formatı pdf, xlsx veya ayri olmalıdır.", 400));

        if (format == "ayri")
            return FinansHandlerHelper.ExecuteAsync(async () => new DosyaIcerigi(
                await service.AylikZipAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken),
                "application/zip",
                $"finans-aylik-raporlar-{request.Yil}-{request.Ay:00}-{TurkeyTime.Now:yyyyMMddHHmmss}.zip"));

        var excel = format is "xlsx" or "excel";
        return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
            ? await service.AylikExcelAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken)
            : await service.AylikPdfAsync(request.Yil, request.Ay, request.Gruplar, cancellationToken),
            excel, $"finans-aylik-{request.Yil}-{request.Ay:00}"));
    }

    public Task<Result<DosyaIcerigi>> Handle(FinansSiparisDurumRaporuDosyaQuery request, CancellationToken cancellationToken)
    {
        if (!TryResolveFormat(request.Format, out var excel))
            return Task.FromResult(InvalidFormat());
        return FinansHandlerHelper.ExecuteAsync(async () => NewFile(excel
            ? await service.SiparisDurumExcelAsync(request.Filtre, cancellationToken)
            : await service.SiparisDurumPdfAsync(request.Filtre, cancellationToken), excel, "finans-siparis-durumu"));
    }


    public Task<Result<DosyaIcerigi>> Handle(FinansOzetRaporuDosyaQuery request, CancellationToken cancellationToken)
    {
        var format = request.Format.ToLowerInvariant();
        if (format is not ("pdf" or "xlsx" or "excel"))
            return Task.FromResult(Result<DosyaIcerigi>.Failure("Geçerli rapor formatı pdf veya xlsx olmalıdır.", 400));
        var excel = format != "pdf";
        return FinansHandlerHelper.ExecuteAsync(async () => new DosyaIcerigi(
            await service.OzetRaporAsync(request.Tur, excel, request.Filtre, cancellationToken),
            excel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf",
            $"finans-{request.Tur}.{(excel ? "xlsx" : "pdf")}"));
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
