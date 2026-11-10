using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;

/// <summary>Yüklenebilecek planlar: yerleşimi olan ya da yüklemesi süren (arac-yukleme R), sayfalı.</summary>
public sealed class AracYuklemePlanlariQuery : IRequest<Result<SayfaliSonuc<AracYuklemePlanOzeti>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string? Arama { get; set; }
    public int? Sayfa { get; set; }
    public int? Boyut { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Planın yükleme durumu: süren (yoksa en son) oturum ve araçların ilerlemesi.</summary>
public sealed class AracYuklemeDurumuQuery : IRequest<Result<AracYuklemeDurumu>>, ISecuredRequest, IRequiresMenuPermission
{
    public int PlanId { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Oturumdaki aracın yerleşimi, yükleme işaretleri ve sıradaki adım.</summary>
public sealed class AracYuklemeAracQuery : IRequest<Result<AracYuklemeAraci>>, ISecuredRequest, IRequiresMenuPermission
{
    public int OturumId { get; set; }
    public int AracNo { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>
/// Okutulan sandık barkodunu çözer: yük hangi araçta, sırada mı, yüklenmiş mi. Durum değiştirmez (işaretleme ayrı
/// komuttur); izleyen kullanıcı da sorabilsin diye R yeter.
/// </summary>
public sealed class AracYuklemeCozumleQuery : IRequest<Result<YukCozumu>>, ISecuredRequest, IRequiresMenuPermission
{
    public int OturumId { get; set; }
    public string? Barkod { get; set; }

    /// <summary>Ekranda açık olan araç.</summary>
    public int AracNo { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class AracYuklemeQueryHandlers(IYuklemeOturumuService service) :
    IRequestHandler<AracYuklemePlanlariQuery, Result<SayfaliSonuc<AracYuklemePlanOzeti>>>,
    IRequestHandler<AracYuklemeDurumuQuery, Result<AracYuklemeDurumu>>,
    IRequestHandler<AracYuklemeAracQuery, Result<AracYuklemeAraci>>,
    IRequestHandler<AracYuklemeCozumleQuery, Result<YukCozumu>>
{
    public const string AracBulunamadi = "Yükleme oturumu ya da araç bulunamadı.";

    public async Task<Result<SayfaliSonuc<AracYuklemePlanOzeti>>> Handle(AracYuklemePlanlariQuery request, CancellationToken cancellationToken) =>
        Result<SayfaliSonuc<AracYuklemePlanOzeti>>.Success(
            await service.YuklenecekPlanlarAsync(request.Arama, request.Sayfa, request.Boyut, cancellationToken));

    public async Task<Result<AracYuklemeDurumu>> Handle(AracYuklemeDurumuQuery request, CancellationToken cancellationToken) =>
        await service.DurumAsync(request.PlanId, cancellationToken) is { } durum
            ? Result<AracYuklemeDurumu>.Success(durum)
            : Result<AracYuklemeDurumu>.Failure(YuklemePlaniQueryHandlers.PlanBulunamadi, 404);

    public async Task<Result<AracYuklemeAraci>> Handle(AracYuklemeAracQuery request, CancellationToken cancellationToken) =>
        await service.AracAsync(request.OturumId, request.AracNo, cancellationToken) is { } arac
            ? Result<AracYuklemeAraci>.Success(arac)
            : Result<AracYuklemeAraci>.Failure(AracBulunamadi, 404);

    public async Task<Result<YukCozumu>> Handle(AracYuklemeCozumleQuery request, CancellationToken cancellationToken) =>
        Result<YukCozumu>.Success(await service.CozumleAsync(request.OturumId, request.Barkod ?? string.Empty, request.AracNo, cancellationToken));
}
