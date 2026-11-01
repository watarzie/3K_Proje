using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>
    /// Saha/Yedek projesinin sevke hazır sandık raporu (beyanname için; <see cref="Format"/> "pdf" / "excel"): yalnız
    /// kapalı ve ölçü/ağırlığı girilmiş sandıklar ve içerikleri, başta listelenmeyen sandıkların nedeni. İzin mevcut
    /// Saha/Yedek sandık raporuyla aynıdır (projenin gerçek tipinin sandık raporu okuması). Durum değiştirmez.
    /// </summary>
    public class GetSevkeHazirSandikRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public string Format { get; set; } = SevkiyatRaporBicimleri.Pdf;
    }

    public sealed class GetSevkeHazirSandikRaporuQueryHandler(IUnitOfWork unitOfWork, ISahaSandikRaporService sahaSandikRaporu)
        : IRequestHandler<GetSevkeHazirSandikRaporuQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<DosyaIcerigi>> Handle(GetSevkeHazirSandikRaporuQuery request, CancellationToken cancellationToken)
        {
            var format = SevkiyatRaporBicimleri.Coz(request.Format);
            if (format is null)
                return Result<DosyaIcerigi>.Failure("Rapor biçimi pdf veya excel olmalıdır.");
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;
            if (proje.ProjeTipiId is not ((int)ProjeTipi.Saha or (int)ProjeTipi.Yedek))
                return Result<DosyaIcerigi>.Failure("Sevke hazır sandık raporu yalnız Saha ve Yedek projelerinde alınır.");

            var dosyaAdi = $"{proje.ProjeNo}_{RaporDosyaAdi.SahaRaporAdi(proje)}_SevkeHazir";
            return Result<DosyaIcerigi>.Success(format == SevkiyatRaporBicimleri.Excel
                ? DosyaIcerigi.Excel(await sahaSandikRaporu.SevkeHazirSandiklarExcelOlusturAsync(request.ProjeId), dosyaAdi + ".xlsx")
                : DosyaIcerigi.Pdf(await sahaSandikRaporu.SevkeHazirSandiklarPdfOlusturAsync(request.ProjeId), dosyaAdi + ".pdf"));
        }
    }
}
