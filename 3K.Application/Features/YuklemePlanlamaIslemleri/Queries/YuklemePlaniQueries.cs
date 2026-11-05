using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;

/// <summary>Plan ekranının seçim listeleri: taşıma aileleri, yük türleri, lokasyonlar (yukleme-planlari R).</summary>
public sealed class YuklemePlaniTanimlariQuery : IRequest<Result<YuklemePlaniTanimlari>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Plana eklenebilecek projeler (proje no veya müşteriyle süzülür), sayfalı.</summary>
public sealed class PlanProjeAdaylariQuery : IRequest<Result<SayfaliSonuc<PlanProjeAdayi>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string? Arama { get; set; }
    public int? Sayfa { get; set; }
    public int? Boyut { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Yükleme planları, en son kaydedilen önce, sayfalı.</summary>
public sealed class YuklemePlanlariQuery : IRequest<Result<SayfaliSonuc<YuklemePlaniOzeti>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string? Arama { get; set; }
    public int? Sayfa { get; set; }
    public int? Boyut { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Planın son revizyonu, yük listesi, sayaçlar ve güncel kaynakla farkları.</summary>
public sealed class YuklemePlaniGetirQuery : IRequest<Result<YuklemePlaniDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    public int PlanId { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class YuklemePlaniQueryHandlers(IYuklemePlaniService service) :
    IRequestHandler<YuklemePlaniTanimlariQuery, Result<YuklemePlaniTanimlari>>,
    IRequestHandler<PlanProjeAdaylariQuery, Result<SayfaliSonuc<PlanProjeAdayi>>>,
    IRequestHandler<YuklemePlanlariQuery, Result<SayfaliSonuc<YuklemePlaniOzeti>>>,
    IRequestHandler<YuklemePlaniGetirQuery, Result<YuklemePlaniDetayi>>
{
    public const string PlanBulunamadi = "Yükleme planı bulunamadı.";

    public async Task<Result<YuklemePlaniTanimlari>> Handle(YuklemePlaniTanimlariQuery request, CancellationToken cancellationToken) =>
        Result<YuklemePlaniTanimlari>.Success(await service.TanimlarAsync(cancellationToken));

    public async Task<Result<SayfaliSonuc<PlanProjeAdayi>>> Handle(PlanProjeAdaylariQuery request, CancellationToken cancellationToken) =>
        Result<SayfaliSonuc<PlanProjeAdayi>>.Success(
            await service.ProjeAdaylariAsync(request.Arama, request.Sayfa, request.Boyut, cancellationToken));

    public async Task<Result<SayfaliSonuc<YuklemePlaniOzeti>>> Handle(YuklemePlanlariQuery request, CancellationToken cancellationToken) =>
        Result<SayfaliSonuc<YuklemePlaniOzeti>>.Success(
            await service.PlanlarAsync(request.Arama, request.Sayfa, request.Boyut, cancellationToken));

    public async Task<Result<YuklemePlaniDetayi>> Handle(YuklemePlaniGetirQuery request, CancellationToken cancellationToken) =>
        await service.PlanGetirAsync(request.PlanId, cancellationToken) is { } plan
            ? Result<YuklemePlaniDetayi>.Success(plan)
            : Result<YuklemePlaniDetayi>.Failure(PlanBulunamadi, 404);
}
