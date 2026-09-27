using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.FinansIslemleri.Commands
{
    public sealed class FinansCommandHandlers :
        IRequestHandler<FinansIsKaydiOlusturCommand, Result<FinansIsKaydiModel>>,
        IRequestHandler<FinansIsKaydiIptalCommand, Result>,
        IRequestHandler<FinansIsKaydiGeriAlCommand, Result>,
        IRequestHandler<FinansOzelIsAylikDegerGuncelleCommand, Result>,
        IRequestHandler<FinansSiparisOlusturCommand, Result<FinansSiparisModel>>,
        IRequestHandler<FinansSiparisGuncelleCommand, Result<FinansSiparisModel>>,
        IRequestHandler<FinansSiparisIptalCommand, Result>,
        IRequestHandler<FinansFaturaOlusturCommand, Result<FinansFaturaModel>>,
        IRequestHandler<FinansFaturaGuncelleCommand, Result<FinansFaturaModel>>,
        IRequestHandler<FinansFaturaIptalCommand, Result>,
        IRequestHandler<FinansDuzenliIsOlusturCommand, Result<FinansDuzenliIsModel>>,
        IRequestHandler<FinansDuzenliIsGuncelleCommand, Result<FinansDuzenliIsModel>>,
        IRequestHandler<FinansGiderOlusturCommand, Result<FinansGiderModel>>,
        IRequestHandler<FinansGiderGuncelleCommand, Result<FinansGiderModel>>,
        IRequestHandler<FinansGiderIptalCommand, Result>,
        IRequestHandler<FinansGiderKategoriOlusturCommand, Result<FinansGiderKategoriModel>>,
        IRequestHandler<FinansGiderKategoriGuncelleCommand, Result<FinansGiderKategoriModel>>,
        IRequestHandler<FinansGiderKalemiOlusturCommand, Result<FinansGiderKalemiModel>>,
        IRequestHandler<FinansGiderKalemiGuncelleCommand, Result<FinansGiderKalemiModel>>,
        IRequestHandler<FinansUrunOlusturCommand, Result<FinansUrunModel>>,
        IRequestHandler<FinansUrunGuncelleCommand, Result<FinansUrunModel>>,
        IRequestHandler<FinansUrunPasiflestirCommand, Result>
    {
        private readonly IFinansService _service;

        public FinansCommandHandlers(IFinansService service) => _service = service;

        public Task<Result<FinansIsKaydiModel>> Handle(FinansIsKaydiOlusturCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(async () =>
                FinansHassasAlanMaskeleme.IsKaydi(await _service.IsKaydiOlusturAsync(request.Model, cancellationToken)));

        public Task<Result> Handle(FinansIsKaydiIptalCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.IsKaydiIptalAsync(request.Id, request.Aciklama, cancellationToken), "Finans iş kaydı bulunamadı.");

        public Task<Result> Handle(FinansIsKaydiGeriAlCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(() => _service.IsKaydiGeriAlAsync(request.Id, cancellationToken), "Finans iş kaydı bulunamadı.");

        public Task<Result> Handle(FinansOzelIsAylikDegerGuncelleCommand request, CancellationToken cancellationToken)
            => FinansHandlerHelper.ExecuteAsync(
                () => _service.OzelIsAylikDegerGuncelleAsync(request.Id, request.Model, cancellationToken),
                "Özel iş kaydı bulunamadı.");

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
    }
}
