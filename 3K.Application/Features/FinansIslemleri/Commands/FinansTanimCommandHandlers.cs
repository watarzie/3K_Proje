using MediatR;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>Düzenli iş, gider kütüphanesi, ürün/tarife, para birimi ve finans parametresi tanım komutları.</summary>
public sealed class FinansTanimCommandHandlers(
    IFinansDuzenliIsService duzenliIsler,
    IFinansGiderTanimService giderTanimlari,
    IFinansUrunService urunler,
    IFinansParametreService parametreler) :
    IRequestHandler<FinansDuzenliIsOlusturCommand, Result<FinansDuzenliIsModel>>,
    IRequestHandler<FinansDuzenliIsGuncelleCommand, Result<FinansDuzenliIsModel>>,
    IRequestHandler<FinansDuzenliIsDonemUretCommand, Result<FinansDonemOlusturSonucModel>>,
    IRequestHandler<FinansGiderKategoriOlusturCommand, Result<FinansGiderKategoriModel>>,
    IRequestHandler<FinansGiderKategoriGuncelleCommand, Result<FinansGiderKategoriModel>>,
    IRequestHandler<FinansGiderKalemiOlusturCommand, Result<FinansGiderKalemiModel>>,
    IRequestHandler<FinansGiderKalemiGuncelleCommand, Result<FinansGiderKalemiModel>>,
    IRequestHandler<FinansUrunOlusturCommand, Result<FinansUrunModel>>,
    IRequestHandler<FinansUrunGuncelleCommand, Result<FinansUrunModel>>,
    IRequestHandler<FinansUrunPasiflestirCommand, Result>,
    IRequestHandler<FinansUrunYenidenFiyatlaCommand, Result<FinansYenidenFiyatlamaSonucModel>>,
    IRequestHandler<FinansTarifeDuzeltCommand, Result<FinansTarifeDuzeltmeSonucModel>>,
    IRequestHandler<FinansParaBirimiOlusturCommand, Result<FinansParaBirimiModel>>,
    IRequestHandler<FinansParaBirimiGuncelleCommand, Result<FinansParaBirimiModel>>,
    IRequestHandler<FinansParametreleriKaydetCommand, Result<FinansParametrelerModel>>
{
    public Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => duzenliIsler.DuzenliIsOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => duzenliIsler.DuzenliIsGuncelleAsync(request.Id, request.Model, cancellationToken), "Düzenli iş bulunamadı.");

    public Task<Result<FinansDonemOlusturSonucModel>> Handle(FinansDuzenliIsDonemUretCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => duzenliIsler.DuzenliIsDonemleriniUretAsync(request.Id, request.BitisTarihi, cancellationToken), "Düzenli iş bulunamadı.");

    public Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => giderTanimlari.GiderKategoriOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => giderTanimlari.GiderKategoriGuncelleAsync(request.Id, request.Model, cancellationToken), "Gider kategorisi bulunamadı.");

    public Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => giderTanimlari.GiderKalemiOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => giderTanimlari.GiderKalemiGuncelleAsync(request.Id, request.Model, cancellationToken), "Gider kalemi bulunamadı.");

    public Task<Result<FinansUrunModel>> Handle(FinansUrunOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => urunler.UrunOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansUrunModel>> Handle(FinansUrunGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => urunler.UrunGuncelleAsync(request.Id, request.Model, cancellationToken), "Finans ürünü bulunamadı.");

    public Task<Result> Handle(FinansUrunPasiflestirCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(
            () => urunler.UrunPasiflestirAsync(request.Id, cancellationToken), "Finans ürünü bulunamadı.");

    public Task<Result<FinansYenidenFiyatlamaSonucModel>> Handle(FinansUrunYenidenFiyatlaCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => urunler.YenidenFiyatlaAsync(request.Id, request.Aciklama, cancellationToken), "Finans ürünü bulunamadı.");

    public Task<Result<FinansTarifeDuzeltmeSonucModel>> Handle(FinansTarifeDuzeltCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => urunler.TarifeDuzeltAsync(request.UrunId, request.TarifeId, request.Model, cancellationToken),
            "Finans ürünü veya tarife sürümü bulunamadı.");

    public Task<Result<FinansParaBirimiModel>> Handle(FinansParaBirimiOlusturCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => parametreler.ParaBirimiOlusturAsync(request.Model, cancellationToken));

    public Task<Result<FinansParaBirimiModel>> Handle(FinansParaBirimiGuncelleCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteOptionalAsync(
            () => parametreler.ParaBirimiGuncelleAsync(request.Id, request.Model, cancellationToken), "Para birimi bulunamadı.");

    public Task<Result<FinansParametrelerModel>> Handle(FinansParametreleriKaydetCommand request, CancellationToken cancellationToken)
        => FinansHandlerHelper.ExecuteAsync(() => parametreler.ParametreleriKaydetAsync(request.Model, cancellationToken));
}
