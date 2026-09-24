using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.KullaniciIslemleri.Commands
{
    public class KullaniciGuncelleCommandHandler : IRequestHandler<KullaniciGuncelleCommand, Result<KullaniciDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIkiFaktorService _ikiFaktorService;
        private readonly IKullaniciYetkiService? _yetkiService;
        private readonly ICurrentUserService? _currentUser;
        private readonly ISseNotifier? _sseNotifier;
        private readonly ILogger<KullaniciGuncelleCommandHandler> _logger;

        public KullaniciGuncelleCommandHandler(
            IUnitOfWork unitOfWork,
            IIkiFaktorService ikiFaktorService,
            IKullaniciYetkiService? yetkiService = null,
            ICurrentUserService? currentUser = null,
            ISseNotifier? sseNotifier = null,
            ILogger<KullaniciGuncelleCommandHandler>? logger = null)
        {
            _unitOfWork = unitOfWork;
            _ikiFaktorService = ikiFaktorService;
            _yetkiService = yetkiService;
            _currentUser = currentUser;
            _sseNotifier = sseNotifier;
            _logger = logger ?? NullLogger<KullaniciGuncelleCommandHandler>.Instance;
        }

        public async Task<Result<KullaniciDto>> Handle(KullaniciGuncelleCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<Kullanici>();
            var kullanici = await repo.GetByIdAsync(request.Id);

            if (kullanici == null)
                return Result<KullaniciDto>.Failure("Kullanıcı bulunamadı.");

            var rolDegisti = kullanici.RolId != request.RolId;
            if (rolDegisti)
            {
                var check = _yetkiService == null ? null :
                    await _yetkiService.RolAtamayiDogrulaAsync(kullanici.Id, request.RolId, cancellationToken);
                if (check?.Basarili != true)
                    return Result<KullaniciDto>.Failure(check?.Hata ?? "Rol atama yetkisi doğrulanamadı.", check?.DurumKodu ?? 403);
                await _unitOfWork.GetRepository<YetkiDegisikligi>().AddAsync(new()
                {
                    AktorKullaniciId = _currentUser?.UserId ?? 0, HedefTuru = "KullaniciRol", HedefId = kullanici.Id,
                    OncekiDeger = kullanici.RolId.ToString(), YeniDeger = request.RolId.ToString()
                });
            }

            kullanici.AdSoyad = request.AdSoyad;
            kullanici.BasHarf = request.AdSoyad.Length >= 2
                ? request.AdSoyad[..2].ToUpper()
                : request.AdSoyad.ToUpper();
            kullanici.RolId = request.RolId;

            // GetByIdAsync tracked entity döndürür. Update çağrısı bütün kolonları
            // modified işaretleyip eşzamanlı 2FA flag değişikliğini ezebilirdi.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (rolDegisti && _sseNotifier != null)
            {
                if (_unitOfWork.HasActiveTransaction)
                    _unitOfWork.RegisterAfterCommit(_ => YetkiOlayiGonderAsync(kullanici.Id));
                else
                    await YetkiOlayiGonderAsync(kullanici.Id);
            }

            // Rol navigation'ını yeniden yükle
            var rolRepo = _unitOfWork.GetRepository<Rol>();
            var rol = await rolRepo.GetByIdAsync(kullanici.RolId);
            kullanici.Rol = rol!;
            var ayarDurumlari = await _ikiFaktorService.AyarDurumlariniGetirAsync(
                new[] { kullanici.Id },
                cancellationToken);
            ayarDurumlari.TryGetValue(kullanici.Id, out var ayarDurumu);

            return Result<KullaniciDto>.Success(
                AuthDtoFactory.Kullanici(kullanici, ayarDurumu));
        }

        private async Task YetkiOlayiGonderAsync(int kullaniciId)
        {
            try
            {
                await _sseNotifier!.NotifyUsersAsync([kullaniciId], SseOlaylari.YetkiGuncellendi);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Kullanıcı {KullaniciId} için yetki SSE sinyali gönderilemedi.", kullaniciId);
            }
        }
    }
}
