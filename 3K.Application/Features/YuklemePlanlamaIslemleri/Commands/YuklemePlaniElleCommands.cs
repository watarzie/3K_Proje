using System.Text.Json.Serialization;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;

/// <summary>Elle yerleşimde araç listesi (denetleme ve kaydetme istekleri için ortak doğrulama).</summary>
public interface IElleYerlesimIstegi
{
    List<ElleAracDto> Araclar { get; }
}

/// <summary>
/// Elle yerleşimi kaydetmeden denetler (sürükle-bırak sonrası anlık kontrol, isterler 22). Durum değiştirmez; düzenleme
/// ekranına ait olduğu için yukleme-planlari W ister.
/// </summary>
public sealed class YuklemePlaniElleDenetleQuery : IRequest<Result<PlanYerlesimi>>, ISecuredRequest, IRequiresMenuPermission, IElleYerlesimIstegi
{
    [JsonIgnore]
    public int PlanId { get; set; }

    public List<ElleAracDto> Araclar { get; set; } = [];

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>Elle düzenlenen yerleşimi yeni revizyon olarak kaydeder (isterler 20–21); arada kaydedilmişse 409.</summary>
public sealed class YuklemePlaniElleKaydetCommand : IRequest<Result<YuklemePlaniDetayi>>, ISecuredRequest, IRequiresMenuPermission, IElleYerlesimIstegi
{
    [JsonIgnore]
    public int PlanId { get; set; }

    public int BeklenenRevizyonNo { get; set; }
    public List<ElleAracDto> Araclar { get; set; } = [];

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>
/// "Kalanı otomatik diz": araçlardaki yükler sabit kalır, konmamış yükler yerleştirilir; sonuç denetlenip döner,
/// kaydedilmez (durum değiştirmez). Düzenleme ekranına ait olduğu için yukleme-planlari W ister.
/// </summary>
public sealed class YuklemePlaniElleTamamlaQuery : IRequest<Result<PlanYerlesimi>>, ISecuredRequest, IRequiresMenuPermission, IElleYerlesimIstegi
{
    [JsonIgnore]
    public int PlanId { get; set; }

    public List<ElleAracDto> Araclar { get; set; } = [];

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>Elle yerleşimde eklenebilecek ekipmanlar (araç ekleme seçicisi); düzenleme ekranına ait, W ister.</summary>
public sealed class YuklemePlaniElleEkipmanlariQuery : IRequest<Result<IReadOnlyList<ElleEkipmanSecenegi>>>, ISecuredRequest, IRequiresMenuPermission
{
    public int PlanId { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>Elle yerleşim işleyicileri; denetim, tamamlama ve kayıt servistedir (istemci ölçü/ağırlık göndermez).</summary>
public sealed class YuklemePlaniElleHandlers(IYuklemePlaniService service, ICurrentUserService currentUser) :
    IRequestHandler<YuklemePlaniElleDenetleQuery, Result<PlanYerlesimi>>,
    IRequestHandler<YuklemePlaniElleKaydetCommand, Result<YuklemePlaniDetayi>>,
    IRequestHandler<YuklemePlaniElleTamamlaQuery, Result<PlanYerlesimi>>,
    IRequestHandler<YuklemePlaniElleEkipmanlariQuery, Result<IReadOnlyList<ElleEkipmanSecenegi>>>
{
    public async Task<Result<PlanYerlesimi>> Handle(YuklemePlaniElleTamamlaQuery request, CancellationToken cancellationToken) =>
        Result<PlanYerlesimi>.Success(await service.ElleTamamlaAsync(request.PlanId, request.Araclar.Select(a => a.Model()).ToList(), cancellationToken));

    public async Task<Result<IReadOnlyList<ElleEkipmanSecenegi>>> Handle(YuklemePlaniElleEkipmanlariQuery request, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<ElleEkipmanSecenegi>>.Success(await service.ElleEkipmanlariAsync(request.PlanId, cancellationToken));

    public async Task<Result<PlanYerlesimi>> Handle(YuklemePlaniElleDenetleQuery request, CancellationToken cancellationToken) =>
        Result<PlanYerlesimi>.Success(await service.ElleDenetleAsync(request.PlanId, request.Araclar.Select(a => a.Model()).ToList(), cancellationToken));

    public async Task<Result<YuklemePlaniDetayi>> Handle(YuklemePlaniElleKaydetCommand request, CancellationToken cancellationToken) =>
        Result<YuklemePlaniDetayi>.Success(await service.ElleKaydetAsync(
            new ElleYerlesimModeli(request.PlanId, request.BeklenenRevizyonNo, request.Araclar.Select(a => a.Model()).ToList()),
            currentUser.UserId, cancellationToken));
}
