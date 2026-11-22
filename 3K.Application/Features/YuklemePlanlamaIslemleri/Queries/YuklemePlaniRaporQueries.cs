using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;

/// <summary>Araç talep ve planlama raporu (PDF, isterler 28); son revizyonun yerleşimiyle (yukleme-planlari R).</summary>
public sealed class YuklemePlaniAracTalepRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
{
    public int PlanId { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Araç bazlı yükleme planı raporu (PDF, isterler 29); aynı plan, revizyon ve araç numaralarıyla (yukleme-planlari R).</summary>
public sealed class YuklemePlaniYuklemeRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
{
    public int PlanId { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class YuklemePlaniRaporQueryHandlers(IYuklemePlaniService service, IYuklemePlaniRaporOlusturucu olusturucu) :
    IRequestHandler<YuklemePlaniAracTalepRaporuQuery, Result<DosyaIcerigi>>,
    IRequestHandler<YuklemePlaniYuklemeRaporuQuery, Result<DosyaIcerigi>>
{
    public const string YerlesimYok = "Bu revizyon için yerleşim hesaplanmadı; rapordan önce yerleşimi hesaplayın.";

    public Task<Result<DosyaIcerigi>> Handle(YuklemePlaniAracTalepRaporuQuery request, CancellationToken cancellationToken) =>
        RaporAsync(request.PlanId, "AracTalebi", olusturucu.AracTalepRaporu, cancellationToken);

    public Task<Result<DosyaIcerigi>> Handle(YuklemePlaniYuklemeRaporuQuery request, CancellationToken cancellationToken) =>
        RaporAsync(request.PlanId, "YuklemePlani", olusturucu.YuklemePlaniRaporu, cancellationToken);

    private async Task<Result<DosyaIcerigi>> RaporAsync(
        int planId, string ad, Func<YuklemePlaniDetayi, DateTime, byte[]> olustur, CancellationToken cancellationToken)
    {
        var plan = await service.PlanGetirAsync(planId, cancellationToken);
        if (plan == null)
            return Result<DosyaIcerigi>.Failure(YuklemePlaniQueryHandlers.PlanBulunamadi, 404);
        if (plan.Yerlesim == null)
            return Result<DosyaIcerigi>.Failure(YerlesimYok, 400);
        return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(
            olustur(plan, TurkeyTime.Now), $"{plan.PlanNo}_R{plan.RevizyonNo}_{ad}_{RaporDosyaAdi.TarihSaat()}.pdf"));
    }
}
