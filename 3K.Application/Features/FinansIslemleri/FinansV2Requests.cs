using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri;

public sealed class FinansTarihiDegistirCommand : FinansCommand<FinansIsKaydiModel?>
{ public override string RequiredMenuKod => FinansYetkiKodlari.FinansTarihiDegistir; public int Id { get; init; } public FinansTarihiDegistirModel Model { get; init; } = null!; }
public sealed class FinansFiyatlandirCommand : FinansCommand<FinansIsKaydiModel?>
{ public override string RequiredMenuKod => FinansYetkiKodlari.FiyatlandirmaDegistir; public int Id { get; init; } public FinansFiyatlandirmaModel Model { get; init; } = null!; }
public sealed class FinansPanelQuery : FinansQuery<FinansPanelModel>
{ public override string RequiredMenuKod => FinansYetkiKodlari.Modul; public DateTime Baslangic { get; init; } public DateTime Bitis { get; init; } }
public sealed class FinansHareketlerQuery : FinansQuery<FinansSayfaliSonuc<FinansHareketModel>>
{ public override string RequiredMenuKod => FinansYetkiKodlari.KayitGoruntule; public FinansListeFiltre Filtre { get; init; } = new(); public string? Tur { get; init; } }
public sealed class FinansGenelAramaQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
{ public override string RequiredMenuKod => FinansYetkiKodlari.KayitGoruntule; public FinansListeFiltre Filtre { get; init; } = new(); }
public sealed class FinansYaslandirmaQuery : FinansQuery<FinansSayfaliSonuc<FinansBekleyenModel>>
{ public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule; public FinansListeFiltre Filtre { get; init; } = new(); public DateTime ReferansTarihi { get; init; } public int MinimumGun { get; init; } }
public sealed class FinansSablonlarQuery : FinansQuery<IReadOnlyList<FinansSablonModel>>
{ public override string RequiredMenuKod => FinansYetkiKodlari.KayitGoruntule; }
public sealed class FinansSablonKaydetCommand : FinansCommand<FinansSablonModel>
{ public override string RequiredMenuKod => FinansYetkiKodlari.SablonYonet; public int? Id { get; init; } public FinansSablonKaydetModel Model { get; init; } = null!; }
public sealed class FinansKaliciSilOnizlemeQuery : FinansQuery<FinansKaliciSilOnizlemeModel?>
{ public override string RequiredMenuKod => FinansYetkiKodlari.KaliciSil; public string VarlikTuru { get; init; } = ""; public int Id { get; init; } }
public sealed class FinansKaliciSilCommand : FinansCommand
{ public override string RequiredMenuKod => FinansYetkiKodlari.KaliciSil; public FinansKaliciSilModel Model { get; init; } = null!; }
public sealed class FinansBelgelerQuery : FinansQuery<IReadOnlyList<FinansBelgeModel>>
{ public override string RequiredMenuKod => FinansYetkiKodlari.BelgeIndir; public string HedefTuru { get; init; } = ""; public int HedefId { get; init; } }
public sealed class FinansBelgeYukleCommand : FinansCommand<FinansBelgeModel>
{ public override string RequiredMenuKod => FinansYetkiKodlari.BelgeYukle; public FinansBelgeYukleModel Model { get; init; } = null!; }
public sealed class FinansBelgeIndirQuery : FinansQuery<FinansBelgeIcerikModel?>
{ public override string RequiredMenuKod => FinansYetkiKodlari.BelgeIndir; public int Id { get; init; } }

public sealed class FinansV2Handlers(IFinansService service) :
    IRequestHandler<FinansTarihiDegistirCommand, Result<FinansIsKaydiModel?>>,
    IRequestHandler<FinansFiyatlandirCommand, Result<FinansIsKaydiModel?>>,
    IRequestHandler<FinansPanelQuery, Result<FinansPanelModel>>,
    IRequestHandler<FinansHareketlerQuery, Result<FinansSayfaliSonuc<FinansHareketModel>>>,
    IRequestHandler<FinansGenelAramaQuery, Result<FinansSayfaliSonuc<FinansIsKaydiModel>>>,
    IRequestHandler<FinansYaslandirmaQuery, Result<FinansSayfaliSonuc<FinansBekleyenModel>>>,
    IRequestHandler<FinansSablonlarQuery, Result<IReadOnlyList<FinansSablonModel>>>,
    IRequestHandler<FinansSablonKaydetCommand, Result<FinansSablonModel>>,
    IRequestHandler<FinansKaliciSilOnizlemeQuery, Result<FinansKaliciSilOnizlemeModel?>>,
    IRequestHandler<FinansKaliciSilCommand, Result>,
    IRequestHandler<FinansBelgelerQuery, Result<IReadOnlyList<FinansBelgeModel>>>,
    IRequestHandler<FinansBelgeYukleCommand, Result<FinansBelgeModel>>,
    IRequestHandler<FinansBelgeIndirQuery, Result<FinansBelgeIcerikModel?>>
{
    public Task<Result<FinansIsKaydiModel?>> Handle(FinansTarihiDegistirCommand r, CancellationToken ct) => FinansHandlerHelper.ExecuteOptionalAsync(() => service.FinansTarihiDegistirAsync(r.Id, r.Model, ct));
    public Task<Result<FinansIsKaydiModel?>> Handle(FinansFiyatlandirCommand r, CancellationToken ct) => FinansHandlerHelper.ExecuteOptionalAsync(() => service.FiyatlandirAsync(r.Id, r.Model, ct));
    public Task<Result<FinansPanelModel>> Handle(FinansPanelQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.PanelAsync(r.Baslangic, r.Bitis, ct));
    public Task<Result<FinansSayfaliSonuc<FinansHareketModel>>> Handle(FinansHareketlerQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.HareketlerAsync(r.Filtre, r.Tur, ct));
    public Task<Result<FinansSayfaliSonuc<FinansIsKaydiModel>>> Handle(FinansGenelAramaQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.GenelAramaAsync(r.Filtre, ct));
    public Task<Result<FinansSayfaliSonuc<FinansBekleyenModel>>> Handle(FinansYaslandirmaQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.YaslandirmaAsync(r.ReferansTarihi, r.MinimumGun, r.Filtre, ct));
    public Task<Result<IReadOnlyList<FinansSablonModel>>> Handle(FinansSablonlarQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.SablonlarAsync(ct));
    public Task<Result<FinansSablonModel>> Handle(FinansSablonKaydetCommand r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.SablonKaydetAsync(r.Id, r.Model, ct));
    public Task<Result<FinansKaliciSilOnizlemeModel?>> Handle(FinansKaliciSilOnizlemeQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteOptionalAsync(() => service.KaliciSilOnizlemeAsync(r.VarlikTuru, r.Id, ct));
    public Task<Result> Handle(FinansKaliciSilCommand r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.KaliciSilAsync(r.Model, ct), "Kayıt bulunamadı.");
    public Task<Result<IReadOnlyList<FinansBelgeModel>>> Handle(FinansBelgelerQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.BelgelerAsync(r.HedefTuru, r.HedefId, ct));
    public Task<Result<FinansBelgeModel>> Handle(FinansBelgeYukleCommand r, CancellationToken ct) => FinansHandlerHelper.ExecuteAsync(() => service.BelgeYukleAsync(r.Model, ct));
    public Task<Result<FinansBelgeIcerikModel?>> Handle(FinansBelgeIndirQuery r, CancellationToken ct) => FinansHandlerHelper.ExecuteOptionalAsync(() => service.BelgeIndirAsync(r.Id, ct));
}
