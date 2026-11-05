using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;

/// <summary>Kural seti sürümleri (mevzuat-kurallari R).</summary>
public sealed class MevzuatKuralSetleriQuery : IRequest<Result<IReadOnlyList<MevzuatKuralSetiOzeti>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.MevzuatKurallari;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Kural seti ayrıntısı: kurallar, parametreleri, madde atıfları ve kaynakları.</summary>
public sealed class MevzuatKuralSetiGetirQuery : IRequest<Result<MevzuatKuralSetiDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    public int Id { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.MevzuatKurallari;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

public sealed class MevzuatKuralQueryHandlers(IMevzuatKuralService service) :
    IRequestHandler<MevzuatKuralSetleriQuery, Result<IReadOnlyList<MevzuatKuralSetiOzeti>>>,
    IRequestHandler<MevzuatKuralSetiGetirQuery, Result<MevzuatKuralSetiDetayi>>
{
    public async Task<Result<IReadOnlyList<MevzuatKuralSetiOzeti>>> Handle(MevzuatKuralSetleriQuery request, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<MevzuatKuralSetiOzeti>>.Success(await service.KuralSetleriAsync(cancellationToken));

    public async Task<Result<MevzuatKuralSetiDetayi>> Handle(MevzuatKuralSetiGetirQuery request, CancellationToken cancellationToken) =>
        await service.KuralSetiGetirAsync(request.Id, cancellationToken) is { } detay
            ? Result<MevzuatKuralSetiDetayi>.Success(detay)
            : Result<MevzuatKuralSetiDetayi>.Failure("Kural seti bulunamadı.", 404);
}
