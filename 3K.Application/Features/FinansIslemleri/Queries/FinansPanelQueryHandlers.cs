using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>Panel, hareketler, genel arama, yaşlandırma ve denetim geçmişi sorguları.</summary>
public sealed class FinansPanelQueryHandlers(IFinansPanelService service) :
    IRequestHandler<FinansPanelQuery, Result<FinansPanelModel>>,
    IRequestHandler<FinansHareketlerQuery, Result<FinansSayfaliSonuc<FinansHareketModel>>>,
    IRequestHandler<FinansGenelAramaQuery, Result<FinansSayfaliSonuc<FinansIsKaydiModel>>>,
    IRequestHandler<FinansYaslandirmaQuery, Result<FinansSayfaliSonuc<FinansBekleyenModel>>>,
    IRequestHandler<FinansDegisiklikGecmisiQuery, Result<FinansSayfaliSonuc<FinansDegisiklikModel>>>
{
    public Task<Result<FinansPanelModel>> Handle(FinansPanelQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.PanelAsync(request.Baslangic, request.Bitis, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansHareketModel>>> Handle(FinansHareketlerQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.HareketlerAsync(request.Filtre, request.Tur, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansIsKaydiModel>>> Handle(FinansGenelAramaQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.GenelAramaAsync(request.Filtre, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansBekleyenModel>>> Handle(FinansYaslandirmaQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() =>
            service.YaslandirmaAsync(request.ReferansTarihi, request.MinimumGun, request.Asama, request.Filtre, cancellationToken));

    public Task<Result<FinansSayfaliSonuc<FinansDegisiklikModel>>> Handle(FinansDegisiklikGecmisiQuery request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => service.DegisiklikGecmisiAsync(
            request.VarlikTuru, request.VarlikId, request.PageNumber, request.PageSize, cancellationToken));
}
