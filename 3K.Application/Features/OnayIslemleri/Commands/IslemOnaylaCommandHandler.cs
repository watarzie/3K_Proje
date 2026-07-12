using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.OnayIslemleri.Commands
{
    public class IslemOnaylaCommandHandler : IRequestHandler<IslemOnaylaCommand, Result>
    {
        private static readonly JsonSerializerOptions PayloadSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            MaxDepth = 64
        };

        private readonly IOnayIslemRepository _onayRepository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApprovalExecutionContext _approvalExecutionContext;
        private readonly IOnayYetkiService _onayYetkiService;
        private readonly ISseNotifier _sseNotifier;
        private readonly ILogger<IslemOnaylaCommandHandler> _logger;

        public IslemOnaylaCommandHandler(
            IOnayIslemRepository onayRepository,
            IMediator mediator,
            ICurrentUserService currentUserService,
            IApprovalExecutionContext approvalExecutionContext,
            IOnayYetkiService onayYetkiService,
            ISseNotifier sseNotifier,
            ILogger<IslemOnaylaCommandHandler> logger)
        {
            _onayRepository = onayRepository;
            _mediator = mediator;
            _currentUserService = currentUserService;
            _approvalExecutionContext = approvalExecutionContext;
            _onayYetkiService = onayYetkiService;
            _sseNotifier = sseNotifier;
            _logger = logger;
        }

        public async Task<Result> Handle(IslemOnaylaCommand request, CancellationToken cancellationToken)
        {
            var originalKomutBasariylaTamamlandi = false;
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var islem = await _onayRepository.GetByIdNoTrackingAsync(
                request.OnayBekleyenIslemId,
                cancellationToken);

            if (islem == null)
                return Result.Failure("Onay kaydı bulunamadı.", 404);

            var onaylayabilir = await _onayYetkiService.KullaniciIslemOnaylayabilirMiAsync(
                kullaniciId.Value,
                islem.IslemKodu,
                islem.TalepEdenKullaniciId,
                cancellationToken);

            if (!onaylayabilir)
                return Result.Failure("Bu işlem tipi için onay yetkiniz bulunmuyor.", 403);

            if (islem.Durum != OnayDurumu.Bekliyor)
                return Result.Failure("Bu işlem başka bir kullanıcı tarafından sonuçlandırılmış.", 409);

            var kararTarihi = TurkeyTime.Now;
            var kararAlindi = await _onayRepository.OnayKarariniAlVeCalistirmayiBaslatAsync(
                islem.Id,
                kullaniciId.Value,
                kararTarihi,
                cancellationToken);

            if (!kararAlindi)
                return Result.Failure("Bu işlem başka bir kullanıcı tarafından sonuçlandırılmış.", 409);

            await OnayDegisikliginiYayinlaAsync(islem.Id);

            try
            {
                var targetType = GuvenliKomutTipiniCoz(islem.CommandType);
                if (targetType == null)
                {
                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        "Onaylanan işlem bu uygulama sürümünde çalıştırılamıyor.",
                        cancellationToken);
                }

                object? originalRequest;
                try
                {
                    originalRequest = JsonSerializer.Deserialize(
                        islem.PayloadJson,
                        targetType,
                        PayloadSerializerOptions);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(exception, "Onay kaydı {OnayId} payload verisi çözümlenemedi.", islem.Id);
                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        "Onaylanan işlemin kayıtlı verisi okunamadı.",
                        cancellationToken);
                }
                catch (NotSupportedException exception)
                {
                    _logger.LogError(exception, "Onay kaydı {OnayId} payload tipi desteklenmiyor.", islem.Id);
                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        "Onaylanan işlemin kayıtlı veri biçimi desteklenmiyor.",
                        cancellationToken);
                }

                if (originalRequest == null)
                {
                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        "Onaylanan işlemin kayıtlı verisi boş.",
                        cancellationToken);
                }

                using var approvedExecution = _approvalExecutionContext.BeginApprovedExecution();
                var response = await _mediator.Send(originalRequest, cancellationToken);

                if (response is not Result sonuc)
                {
                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        "Onaylanan işlem beklenen sonuç biçimini döndürmedi.",
                        cancellationToken);
                }

                if (!sonuc.IsSuccess)
                {
                    var guvenliHata = GuvenliMetin(
                        sonuc.Error?.Message,
                        "Onaylanan işlem iş kuralı nedeniyle tamamlanamadı.");

                    return await BasarisizTamamlaAsync(
                        islem.Id,
                        kullaniciId.Value,
                        guvenliHata,
                        cancellationToken);
                }

                originalKomutBasariylaTamamlandi = true;
                return await BasariliDurumuKaydetAsync(
                    islem.Id,
                    kullaniciId.Value,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (originalKomutBasariylaTamamlandi)
                {
                    return await BasariliDurumuKaydetAsync(
                        islem.Id,
                        kullaniciId.Value,
                        CancellationToken.None);
                }

                await BasarisizDurumuKaydetAsync(
                    islem.Id,
                    kullaniciId.Value,
                    "Onaylanan işlemin çalıştırılması iptal edildi.",
                    CancellationToken.None);

                return Result.Failure("Onay kararı kaydedildi ancak işlemin çalıştırılması iptal edildi.");
            }
            catch (Exception exception)
            {
                var takipKodu = Guid.NewGuid().ToString("N")[..8];
                _logger.LogError(
                    exception,
                    "[{TakipKodu}] Onaylanan komut çalıştırılamadı. OnayId: {OnayId}",
                    takipKodu,
                    islem.Id);

                if (originalKomutBasariylaTamamlandi)
                {
                    await OnayDegisikliginiYayinlaAsync(islem.Id);
                    return Result.Failure(
                        $"İşlem başarıyla çalıştı ancak onay sonucu kaydedilemedi. Takip kodu: {takipKodu}",
                        500);
                }

                var guvenliHata = $"İşlem çalıştırılırken beklenmeyen bir hata oluştu. Takip kodu: {takipKodu}";
                await BasarisizDurumuKaydetAsync(
                    islem.Id,
                    kullaniciId.Value,
                    guvenliHata,
                    CancellationToken.None);

                return Result.Failure($"Onay kararı kaydedildi ancak işlem çalıştırılamadı. Takip kodu: {takipKodu}", 500);
            }
        }

        private async Task<Result> BasariliDurumuKaydetAsync(
            int onayId,
            int kullaniciId,
            CancellationToken cancellationToken)
        {
            try
            {
                var tamamlandi = await _onayRepository.CalistirmayiTamamlaAsync(
                    onayId,
                    kullaniciId,
                    OnayCalistirmaDurumu.Basarili,
                    TurkeyTime.Now,
                    null,
                    cancellationToken);

                await OnayDegisikliginiYayinlaAsync(onayId);
                return tamamlandi
                    ? Result.Success()
                    : Result.Failure("İşlem çalıştı ancak onay sonucu eşzamanlı bir değişiklik nedeniyle kaydedilemedi.", 409);
            }
            catch (Exception ilkHata) when (ilkHata is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ilkHata,
                    "Başarılı onay komutunun sonucu ilk denemede kaydedilemedi. OnayId: {OnayId}",
                    onayId);

                try
                {
                    var mevcut = await _onayRepository.GetByIdNoTrackingAsync(onayId, CancellationToken.None);
                    if (mevcut?.CalistirmaDurumu == OnayCalistirmaDurumu.Basarili)
                    {
                        await OnayDegisikliginiYayinlaAsync(onayId);
                        return Result.Success();
                    }

                    var tekrarKaydedildi = await _onayRepository.CalistirmayiTamamlaAsync(
                        onayId,
                        kullaniciId,
                        OnayCalistirmaDurumu.Basarili,
                        TurkeyTime.Now,
                        null,
                        CancellationToken.None);

                    await OnayDegisikliginiYayinlaAsync(onayId);
                    return tekrarKaydedildi
                        ? Result.Success()
                        : Result.Failure("İşlem çalıştı ancak onay sonucu kaydedilemedi.", 409);
                }
                catch (Exception tekrarHatasi)
                {
                    var takipKodu = Guid.NewGuid().ToString("N")[..8];
                    _logger.LogError(
                        tekrarHatasi,
                        "[{TakipKodu}] Başarılı onay komutunun sonucu kaydedilemedi. OnayId: {OnayId}",
                        takipKodu,
                        onayId);
                    await OnayDegisikliginiYayinlaAsync(onayId);
                    return Result.Failure(
                        $"İşlem başarıyla çalıştı ancak onay sonucu kaydedilemedi. Takip kodu: {takipKodu}",
                        500);
                }
            }
        }

        private async Task<Result> BasarisizTamamlaAsync(
            int onayId,
            int kullaniciId,
            string guvenliHata,
            CancellationToken cancellationToken)
        {
            var kaydedildi = await BasarisizDurumuKaydetAsync(
                onayId,
                kullaniciId,
                guvenliHata,
                cancellationToken);

            return kaydedildi
                ? Result.Failure($"İşlem onaylandı fakat çalıştırılamadı: {guvenliHata}")
                : Result.Failure("İşlem çalıştırılamadı ve sonuç eşzamanlı bir değişiklik nedeniyle kaydedilemedi.", 409);
        }

        private async Task<bool> BasarisizDurumuKaydetAsync(
            int onayId,
            int kullaniciId,
            string guvenliHata,
            CancellationToken cancellationToken)
        {
            var kaydedildi = await _onayRepository.CalistirmayiTamamlaAsync(
                onayId,
                kullaniciId,
                OnayCalistirmaDurumu.Basarisiz,
                TurkeyTime.Now,
                GuvenliMetin(guvenliHata, "Onaylanan işlem tamamlanamadı."),
                cancellationToken);

            await OnayDegisikliginiYayinlaAsync(onayId);
            return kaydedildi;
        }

        private async Task OnayDegisikliginiYayinlaAsync(int onayId)
        {
            try
            {
                await _sseNotifier.BroadcastApprovalUpdateAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Onay kaydı {OnayId} için SSE yenileme sinyali gönderilemedi.",
                    onayId);
            }
        }

        private static Type? GuvenliKomutTipiniCoz(string commandType)
        {
            if (string.IsNullOrWhiteSpace(commandType))
                return null;

            var targetType = Type.GetType(commandType, throwOnError: false, ignoreCase: false);
            if (targetType == null || targetType.IsAbstract || targetType.IsGenericTypeDefinition)
                return null;

            if (targetType.Assembly != typeof(IslemOnaylaCommandHandler).Assembly)
                return null;

            var mediatRRequestMi = targetType.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>));

            var onayliKomutMu = typeof(IApprovalOperation).IsAssignableFrom(targetType) &&
                (typeof(IRequireApproval).IsAssignableFrom(targetType) ||
                 typeof(IAlwaysRequireApproval).IsAssignableFrom(targetType));

            return mediatRRequestMi && onayliKomutMu ? targetType : null;
        }

        private static string GuvenliMetin(string? metin, string varsayilan)
        {
            if (string.IsNullOrWhiteSpace(metin))
                return varsayilan;

            var tekSatir = string.Join(
                " ",
                metin.Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Replace('\t', ' ')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return tekSatir.Length <= 500 ? tekSatir : tekSatir[..500];
        }
    }
}
