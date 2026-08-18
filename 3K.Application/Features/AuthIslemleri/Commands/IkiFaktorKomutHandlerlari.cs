using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.AuthIslemleri.Commands
{
    public sealed class IkiFaktorKurulumBaslatCommandHandler
        : IRequestHandler<IkiFaktorKurulumBaslatCommand, Result<IkiFaktorKurulumDto>>
    {
        private readonly IIkiFaktorService _ikiFaktorService;

        public IkiFaktorKurulumBaslatCommandHandler(IIkiFaktorService ikiFaktorService)
        {
            _ikiFaktorService = ikiFaktorService;
        }

        public async Task<Result<IkiFaktorKurulumDto>> Handle(
            IkiFaktorKurulumBaslatCommand request,
            CancellationToken cancellationToken)
        {
            var sonuc = await _ikiFaktorService.KurulumuBaslatAsync(
                request.ChallengeToken,
                cancellationToken);

            if (!sonuc.Basarili)
                return IkiFaktorHataMapper.Failure<IkiFaktorKurulumDto>(sonuc.HataKodu);

            return Result<IkiFaktorKurulumDto>.Success(new IkiFaktorKurulumDto
            {
                ChallengeToken = sonuc.TalepTokeni!,
                ExpiresInSeconds = sonuc.GecerlilikSuresiSaniye!.Value,
                QrCodeDataUri = sonuc.QrKodDataUri!,
                ManuelAnahtar = sonuc.ManuelAnahtar!
            });
        }
    }

    public sealed class IkiFaktorKurulumDogrulaCommandHandler
        : IRequestHandler<IkiFaktorKurulumDogrulaCommand, Result<LoginResultDto>>
    {
        private readonly IIkiFaktorService _ikiFaktorService;
        private readonly IAuthService _authService;

        public IkiFaktorKurulumDogrulaCommandHandler(
            IIkiFaktorService ikiFaktorService,
            IAuthService authService)
        {
            _ikiFaktorService = ikiFaktorService;
            _authService = authService;
        }

        public async Task<Result<LoginResultDto>> Handle(
            IkiFaktorKurulumDogrulaCommand request,
            CancellationToken cancellationToken)
        {
            var sonuc = await _ikiFaktorService.KurulumuDogrulaAsync(
                request.ChallengeToken,
                request.Kod,
                cancellationToken);

            return await IkiFaktorGirisSonucuOlusturucu.OlusturAsync(
                sonuc,
                _authService,
                _ikiFaktorService,
                cancellationToken);
        }
    }

    public sealed class IkiFaktorGirisDogrulaCommandHandler
        : IRequestHandler<IkiFaktorGirisDogrulaCommand, Result<LoginResultDto>>
    {
        private readonly IIkiFaktorService _ikiFaktorService;
        private readonly IAuthService _authService;

        public IkiFaktorGirisDogrulaCommandHandler(
            IIkiFaktorService ikiFaktorService,
            IAuthService authService)
        {
            _ikiFaktorService = ikiFaktorService;
            _authService = authService;
        }

        public async Task<Result<LoginResultDto>> Handle(
            IkiFaktorGirisDogrulaCommand request,
            CancellationToken cancellationToken)
        {
            var sonuc = await _ikiFaktorService.GirisiDogrulaAsync(
                request.ChallengeToken,
                request.Kod,
                cancellationToken);

            return await IkiFaktorGirisSonucuOlusturucu.OlusturAsync(
                sonuc,
                _authService,
                _ikiFaktorService,
                cancellationToken);
        }
    }

    public sealed class IkiFaktorKurtarmaKoduDogrulaCommandHandler
        : IRequestHandler<IkiFaktorKurtarmaKoduDogrulaCommand, Result<LoginResultDto>>
    {
        private readonly IIkiFaktorService _ikiFaktorService;
        private readonly IAuthService _authService;

        public IkiFaktorKurtarmaKoduDogrulaCommandHandler(
            IIkiFaktorService ikiFaktorService,
            IAuthService authService)
        {
            _ikiFaktorService = ikiFaktorService;
            _authService = authService;
        }

        public async Task<Result<LoginResultDto>> Handle(
            IkiFaktorKurtarmaKoduDogrulaCommand request,
            CancellationToken cancellationToken)
        {
            var sonuc = await _ikiFaktorService.KurtarmaKoduylaGirisiDogrulaAsync(
                request.ChallengeToken,
                request.KurtarmaKodu,
                cancellationToken);

            return await IkiFaktorGirisSonucuOlusturucu.OlusturAsync(
                sonuc,
                _authService,
                _ikiFaktorService,
                cancellationToken);
        }
    }

    internal static class IkiFaktorGirisSonucuOlusturucu
    {
        public static async Task<Result<LoginResultDto>> OlusturAsync(
            IkiFaktorDogrulamaSonucu sonuc,
            IAuthService authService,
            IIkiFaktorService ikiFaktorService,
            CancellationToken cancellationToken)
        {
            if (!sonuc.Basarili)
            {
                return IkiFaktorHataMapper.Failure<LoginResultDto>(
                    sonuc.HataKodu,
                    sonuc.KalanDenemeSayisi);
            }

            var kullanici = sonuc.KullaniciId.HasValue
                ? await authService.GetKullaniciByIdAsync(
                    sonuc.KullaniciId.Value,
                    cancellationToken)
                : null;

            if (kullanici == null)
                return Result<LoginResultDto>.Failure("Kullanıcı bulunamadı.", 401);

            // Yönetici challenge sürerken zorunluluğu kaldırdıysa talep servis
            // katmanında reddedilir. Başarılı doğrulamada güncel enrollment
            // durumu response DTO'suna da yansıtılır.
            var ayarDurumlari = await ikiFaktorService.AyarDurumlariniGetirAsync(
                new[] { kullanici.Id },
                cancellationToken);
            ayarDurumlari.TryGetValue(kullanici.Id, out var ayarDurumu);

            var token = authService.GenerateAccessToken(
                kullanici,
                ikiFaktorDogrulandi: true);
            return Result<LoginResultDto>.Success(
                AuthDtoFactory.Authenticated(
                    kullanici,
                    token,
                    sonuc.KurtarmaKodlari,
                    ayarDurumu));
        }
    }

    internal static class IkiFaktorHataMapper
    {
        public static Result<T> Failure<T>(
            IkiFaktorHataKodu hataKodu,
            int? kalanDenemeSayisi = null)
        {
            return hataKodu switch
            {
                IkiFaktorHataKodu.SuresiDolmusTalep =>
                    Result<T>.Failure("İki faktörlü doğrulama talebinin süresi doldu. Lütfen tekrar giriş yapın.", 401),
                IkiFaktorHataKodu.DenemeLimitiAsildi =>
                    Result<T>.Failure("Çok fazla hatalı deneme yapıldı. Lütfen tekrar giriş yapın.", 429),
                IkiFaktorHataKodu.GecersizKod =>
                    Result<T>.Failure(
                        "Doğrulama kodu geçersiz.",
                        401,
                        new { kalanDenemeSayisi }),
                IkiFaktorHataKodu.TekrarKullanilanKod =>
                    Result<T>.Failure(
                        "Bu doğrulama kodu daha önce kullanıldı. Yeni kodu bekleyip tekrar deneyin.",
                        401,
                        new { kalanDenemeSayisi }),
                IkiFaktorHataKodu.KurulumZatenTamamlanmis =>
                    Result<T>.Failure("İki faktörlü doğrulama kurulumu zaten tamamlanmış.", 409),
                IkiFaktorHataKodu.KullaniciBulunamadi =>
                    Result<T>.Failure("Kullanıcı bulunamadı.", 401),
                _ => Result<T>.Failure(
                    "İki faktörlü doğrulama talebi geçersiz veya daha önce kullanılmış.",
                    401)
            };
        }
    }
}
