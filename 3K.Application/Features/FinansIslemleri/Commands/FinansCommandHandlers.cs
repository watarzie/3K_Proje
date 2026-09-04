using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands
{
    public sealed class FinansCommandHandlers :
        IRequestHandler<FinansIsKaydiOlusturCommand, Result<FinansIsKaydiModel>>,
        IRequestHandler<FinansIsKaydiGuncelleCommand, Result<FinansIsKaydiModel>>,
        IRequestHandler<FinansIsKaydiIptalCommand, Result>,
        IRequestHandler<FinansIsKaydiGeriAlCommand, Result>,
        IRequestHandler<FinansOzelIsAylikDegerGuncelleCommand, Result>,
        IRequestHandler<FinansUretimAktarCommand, Result<FinansSenkronizasyonSonucModel>>,
        IRequestHandler<FinansSiparisOlusturCommand, Result<FinansSiparisModel>>,
        IRequestHandler<FinansSiparisGuncelleCommand, Result<FinansSiparisModel>>,
        IRequestHandler<FinansSiparisIptalCommand, Result>,
        IRequestHandler<FinansSiparisGeriAlCommand, Result>,
        IRequestHandler<FinansFaturaOlusturCommand, Result<FinansFaturaModel>>,
        IRequestHandler<FinansFaturaGuncelleCommand, Result<FinansFaturaModel>>,
        IRequestHandler<FinansFaturaIptalCommand, Result>,
        IRequestHandler<FinansFaturaGeriAlCommand, Result>,
        IRequestHandler<FinansDuzenliIsOlusturCommand, Result<FinansDuzenliIsModel>>,
        IRequestHandler<FinansDuzenliIsGuncelleCommand, Result<FinansDuzenliIsModel>>,
        IRequestHandler<FinansDuzenliIsDonemOlusturCommand, Result<FinansDonemOlusturSonucModel>>,
        IRequestHandler<FinansGiderOlusturCommand, Result<FinansGiderModel>>,
        IRequestHandler<FinansGiderGuncelleCommand, Result<FinansGiderModel>>,
        IRequestHandler<FinansGiderIptalCommand, Result>,
        IRequestHandler<FinansGiderGeriAlCommand, Result>,
        IRequestHandler<FinansGiderKategoriOlusturCommand, Result<FinansGiderKategoriModel>>,
        IRequestHandler<FinansGiderKategoriGuncelleCommand, Result<FinansGiderKategoriModel>>,
        IRequestHandler<FinansGiderKalemiOlusturCommand, Result<FinansGiderKalemiModel>>,
        IRequestHandler<FinansGiderKalemiGuncelleCommand, Result<FinansGiderKalemiModel>>,
        IRequestHandler<FinansGideriKutuphaneyeKaydetCommand, Result<FinansGiderKalemiModel>>,
        IRequestHandler<FinansUrunOlusturCommand, Result<FinansUrunModel>>,
        IRequestHandler<FinansUrunGuncelleCommand, Result<FinansUrunModel>>,
        IRequestHandler<FinansUrunPasiflestirCommand, Result>,
        IRequestHandler<FinansFiyatTarifesiOlusturCommand, Result<FinansFiyatTarifesiModel>>,
        IRequestHandler<FinansFiyatTarifesiGuncelleCommand, Result<FinansFiyatTarifesiModel>>
    {
        private readonly IFinansService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly IRolService _rolService;

        public FinansCommandHandlers(
            IFinansService service,
            ICurrentUserService currentUser,
            IRolService rolService)
        {
            _service = service;
            _currentUser = currentUser;
            _rolService = rolService;
        }

        public Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(async () =>
                FinansHassasAlanMaskeleme.IsKaydi(await _service.IsKaydiOlusturAsync(request.Model, cancellationToken)));

        public async Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var current = await _service.IsKaydiGetirAsync(request.Id, cancellationToken);
                if (current is null)
                    return FinansHandlerHelper.NotFound<FinansIsKaydiModel>("Finans iş kaydı bulunamadı.");

                var requestedPeriod = new DateTime(request.Model.FinansDonemi.Year, request.Model.FinansDonemi.Month, 1);
                var dateChanged = current.UretimTarihi != request.Model.UretimTarihi ||
                                  current.FinansDonemi.Date != requestedPeriod;
                if (dateChanged && !await HasWritePermissionAsync(FinansYetkiKodlari.TarihDegistir, cancellationToken))
                    return Result<FinansIsKaydiModel>.Failure("Üretim tarihi veya finans dönemi değiştirme yetkiniz bulunmuyor.", 403);

                var indirectPriceChange = current.FinansUrunId != request.Model.FinansUrunId ||
                                          (current.FinansDonemi.Date != requestedPeriod &&
                                           (current.FinansUrunId.HasValue || request.Model.FinansUrunId.HasValue));
                if (indirectPriceChange && !await HasWritePermissionAsync(FinansYetkiKodlari.BirimFiyatDegistir, cancellationToken))
                    return Result<FinansIsKaydiModel>.Failure("Bu değişiklik fiyat snapshot'ını etkileyebileceği için birim fiyat değiştirme yetkisi gerektirir.", 403);

                var value = await _service.IsKaydiGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null
                    ? FinansHandlerHelper.NotFound<FinansIsKaydiModel>("Finans iş kaydı bulunamadı.")
                    : Result<FinansIsKaydiModel>.Success(FinansHassasAlanMaskeleme.IsKaydi(value));
            }
            catch (UnauthorizedAccessException exception) { return Result<FinansIsKaydiModel>.Failure(exception.Message, 403); }
            catch (InvalidOperationException exception) { return Result<FinansIsKaydiModel>.Failure(exception.Message, 409); }
        }

        public Task<Result> Handle(FinansIsKaydiIptalCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.IsKaydiIptalAsync(request.Id, request.Aciklama, cancellationToken), "Finans iş kaydı bulunamadı.");

        public Task<Result> Handle(FinansIsKaydiGeriAlCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.IsKaydiGeriAlAsync(request.Id, cancellationToken), "Finans iş kaydı bulunamadı.");

        public Task<Result> Handle(FinansOzelIsAylikDegerGuncelleCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(
                () => _service.OzelIsAylikDegerGuncelleAsync(request.Id, request.Model, cancellationToken),
                "Özel iş kaydı bulunamadı.");

        public Task<Result<FinansSenkronizasyonSonucModel>> Handle(FinansUretimAktarCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.UretimKayitlariniAktarAsync(request.Kayitlar, cancellationToken));

        public Task<Result<FinansSiparisModel>> Handle(FinansSiparisOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(async () =>
                FinansHassasAlanMaskeleme.Siparis(await _service.SiparisOlusturAsync(request.Model, cancellationToken)));

        public async Task<Result<FinansSiparisModel>> Handle(FinansSiparisGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.SiparisGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null
                    ? FinansHandlerHelper.NotFound<FinansSiparisModel>("Sipariş bulunamadı.")
                    : Result<FinansSiparisModel>.Success(FinansHassasAlanMaskeleme.Siparis(value));
            }
            catch (UnauthorizedAccessException exception) { return Result<FinansSiparisModel>.Failure(exception.Message, 403); }
            catch (InvalidOperationException exception) { return Result<FinansSiparisModel>.Failure(exception.Message, 409); }
        }

        public Task<Result> Handle(FinansSiparisIptalCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.SiparisIptalAsync(request.Id, request.Aciklama, cancellationToken), "Sipariş bulunamadı.");

        public Task<Result> Handle(FinansSiparisGeriAlCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.SiparisGeriAlAsync(request.Id, cancellationToken), "Sipariş bulunamadı.");

        public Task<Result<FinansFaturaModel>> Handle(FinansFaturaOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(async () =>
                FinansHassasAlanMaskeleme.Fatura(await _service.FaturaOlusturAsync(request.Model, cancellationToken)));

        public async Task<Result<FinansFaturaModel>> Handle(FinansFaturaGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.FaturaGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null
                    ? FinansHandlerHelper.NotFound<FinansFaturaModel>("Fatura bulunamadı.")
                    : Result<FinansFaturaModel>.Success(FinansHassasAlanMaskeleme.Fatura(value));
            }
            catch (UnauthorizedAccessException exception) { return Result<FinansFaturaModel>.Failure(exception.Message, 403); }
            catch (InvalidOperationException exception) { return Result<FinansFaturaModel>.Failure(exception.Message, 409); }
        }

        public Task<Result> Handle(FinansFaturaIptalCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.FaturaIptalAsync(request.Id, request.Aciklama, cancellationToken), "Fatura bulunamadı.");

        public Task<Result> Handle(FinansFaturaGeriAlCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.FaturaGeriAlAsync(request.Id, cancellationToken), "Fatura bulunamadı.");

        public Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.DuzenliIsOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansDuzenliIsModel>> Handle(FinansDuzenliIsGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.DuzenliIsGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansDuzenliIsModel>("Düzenli iş bulunamadı.") : Result<FinansDuzenliIsModel>.Success(value);
            }
            catch (UnauthorizedAccessException exception) { return Result<FinansDuzenliIsModel>.Failure(exception.Message, 403); }
            catch (InvalidOperationException exception) { return Result<FinansDuzenliIsModel>.Failure(exception.Message, 409); }
        }

        public Task<Result<FinansDonemOlusturSonucModel>> Handle(FinansDuzenliIsDonemOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.DuzenliIsDonemiOlusturAsync(request.ReferansTarihi, cancellationToken));

        public Task<Result<FinansGiderModel>> Handle(FinansGiderOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.GiderOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansGiderModel>> Handle(FinansGiderGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.GiderGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansGiderModel>("Gider bulunamadı.") : Result<FinansGiderModel>.Success(value);
            }
            catch (UnauthorizedAccessException exception) { return Result<FinansGiderModel>.Failure(exception.Message, 403); }
            catch (InvalidOperationException exception) { return Result<FinansGiderModel>.Failure(exception.Message, 409); }
        }

        public Task<Result> Handle(FinansGiderIptalCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.GiderIptalAsync(request.Id, request.Aciklama, cancellationToken), "Gider bulunamadı.");

        public Task<Result> Handle(FinansGiderGeriAlCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.GiderGeriAlAsync(request.Id, cancellationToken), "Gider bulunamadı.");

        public Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.GiderKategoriOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansGiderKategoriModel>> Handle(FinansGiderKategoriGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.GiderKategoriGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansGiderKategoriModel>("Gider kategorisi bulunamadı.") : Result<FinansGiderKategoriModel>.Success(value);
            }
            catch (InvalidOperationException exception) { return Result<FinansGiderKategoriModel>.Failure(exception.Message, 409); }
        }

        public Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.GiderKalemiOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansGiderKalemiModel>> Handle(FinansGiderKalemiGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.GiderKalemiGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansGiderKalemiModel>("Gider kalemi bulunamadı.") : Result<FinansGiderKalemiModel>.Success(value);
            }
            catch (InvalidOperationException exception) { return Result<FinansGiderKalemiModel>.Failure(exception.Message, 409); }
        }

        public async Task<Result<FinansGiderKalemiModel>> Handle(FinansGideriKutuphaneyeKaydetCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.GideriKutuphaneyeKaydetAsync(request.GiderId, request.Model, cancellationToken);
                return value is null
                    ? FinansHandlerHelper.NotFound<FinansGiderKalemiModel>("Gider bulunamadı.")
                    : Result<FinansGiderKalemiModel>.Success(value);
            }
            catch (InvalidOperationException exception) { return Result<FinansGiderKalemiModel>.Failure(exception.Message, 409); }
        }

        public Task<Result<FinansUrunModel>> Handle(FinansUrunOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.UrunOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansUrunModel>> Handle(FinansUrunGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.UrunGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansUrunModel>("Finans ürünü bulunamadı.") : Result<FinansUrunModel>.Success(value);
            }
            catch (InvalidOperationException exception) { return Result<FinansUrunModel>.Failure(exception.Message, 409); }
        }

        public Task<Result> Handle(FinansUrunPasiflestirCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.UrunPasiflestirAsync(request.Id, cancellationToken), "Finans ürünü bulunamadı.");

        public Task<Result<FinansFiyatTarifesiModel>> Handle(FinansFiyatTarifesiOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.FiyatTarifesiOlusturAsync(request.Model, cancellationToken));

        public async Task<Result<FinansFiyatTarifesiModel>> Handle(FinansFiyatTarifesiGuncelleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.FiyatTarifesiGuncelleAsync(request.Id, request.Model, cancellationToken);
                return value is null ? FinansHandlerHelper.NotFound<FinansFiyatTarifesiModel>("Fiyat tarifesi bulunamadı.") : Result<FinansFiyatTarifesiModel>.Success(value);
            }
            catch (InvalidOperationException exception) { return Result<FinansFiyatTarifesiModel>.Failure(exception.Message, 409); }
        }

        private async Task<bool> HasWritePermissionAsync(string menuKod, CancellationToken cancellationToken)
            => _currentUser.UserId.HasValue &&
               await _rolService.HasUserPermissionAsync(
                   _currentUser.UserId.Value,
                   menuKod,
                   YetkiTipi.W,
                   cancellationToken);
    }
}
