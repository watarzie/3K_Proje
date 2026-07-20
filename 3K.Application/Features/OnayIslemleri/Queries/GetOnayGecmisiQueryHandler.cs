using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using _3K.Application.Common;
using _3K.Application.Features.OnayIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.OnayIslemleri.Queries
{
    public sealed class GetOnayGecmisiQueryHandler
        : IRequestHandler<GetOnayGecmisiQuery, Result<OnayGecmisiListeDto>>
    {
        private const string OnayMerkeziMenuKodu = "islem-onay-merkezi";
        private readonly IOnayIslemRepository _onayRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;
        private readonly IRolService _rolService;

        public GetOnayGecmisiQueryHandler(
            IOnayIslemRepository onayRepository,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService,
            IRolService rolService)
        {
            _onayRepository = onayRepository;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
            _rolService = rolService;
        }

        public async Task<Result<OnayGecmisiListeDto>> Handle(
            GetOnayGecmisiQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<OnayGecmisiListeDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var bekleyenleriGorebilir = await _rolService.HasUserPermissionAsync(
                kullaniciId.Value,
                OnayMerkeziMenuKodu,
                YetkiTipi.R,
                cancellationToken);
            var erisimKapsami = await _onayYetkiService.GetErisimKapsamiAsync(
                kullaniciId.Value,
                cancellationToken);

            var filtre = new OnayGecmisiFiltresi
            {
                Kapsam = KapsamiCoz(request.Kapsam),
                Durum = DurumuCoz(request.Durum),
                CalistirmaDurumu = CalistirmaDurumunuCoz(request.CalistirmaDurumu),
                BaslangicTarihi = request.BaslangicTarihi,
                BitisTarihiHaric = BitisTarihiniHaricSiniraCevir(request.BitisTarihi),
                Arama = request.Arama?.Trim(),
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu
            };

            var sonuc = await _onayRepository.GetGecmisAsync(
                kullaniciId.Value,
                bekleyenleriGorebilir,
                erisimKapsami,
                filtre,
                cancellationToken);

            var toplamSayfa = sonuc.ToplamKayit == 0
                ? 0
                : (int)Math.Ceiling(sonuc.ToplamKayit / (double)request.SayfaBoyutu);

            return Result<OnayGecmisiListeDto>.Success(new OnayGecmisiListeDto
            {
                Kayitlar = sonuc.Kayitlar.Select(kayit => kayit.ToDto()).ToList(),
                ToplamKayit = sonuc.ToplamKayit,
                Sayfa = request.Sayfa,
                SayfaBoyutu = request.SayfaBoyutu,
                ToplamSayfa = toplamSayfa
            });
        }

        internal static OnayGecmisiKapsami KapsamiCoz(string? kapsam)
        {
            return Normalize(kapsam) switch
            {
                "kararverdiklerim" => OnayGecmisiKapsami.KararVerdiklerim,
                "taleplerim" => OnayGecmisiKapsami.Taleplerim,
                "bekleyenler" => OnayGecmisiKapsami.Bekleyenler,
                _ => OnayGecmisiKapsami.Tumu
            };
        }

        internal static OnayDurumu? DurumuCoz(string? durum)
        {
            return Normalize(durum) switch
            {
                "bekliyor" => OnayDurumu.Bekliyor,
                "onaylandi" => OnayDurumu.Onaylandi,
                "reddedildi" => OnayDurumu.Reddedildi,
                _ => null
            };
        }

        internal static OnayCalistirmaDurumu? CalistirmaDurumunuCoz(string? durum)
        {
            return Normalize(durum) switch
            {
                "bilinmiyor" => OnayCalistirmaDurumu.Bilinmiyor,
                "bekliyor" => OnayCalistirmaDurumu.Bekliyor,
                "calisiyor" => OnayCalistirmaDurumu.Calisiyor,
                "basarili" => OnayCalistirmaDurumu.Basarili,
                "basarisiz" => OnayCalistirmaDurumu.Basarisiz,
                "atlandi" => OnayCalistirmaDurumu.Atlandi,
                _ => null
            };
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim()
                .ToLowerInvariant()
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("_", string.Empty, StringComparison.Ordinal);
        }

        private static DateTime? BitisTarihiniHaricSiniraCevir(DateTime? bitisTarihi)
        {
            if (!bitisTarihi.HasValue)
                return null;

            var deger = bitisTarihi.Value;
            if (deger.TimeOfDay == TimeSpan.Zero && deger.Date < DateTime.MaxValue.Date)
                return deger.Date.AddDays(1);

            return deger < DateTime.MaxValue ? deger.AddTicks(1) : deger;
        }
    }

    public sealed class GetOnayGecmisiDetayiQueryHandler
        : IRequestHandler<GetOnayGecmisiDetayiQuery, Result<OnayGecmisiKayitDto>>
    {
        private static readonly JsonSerializerOptions OnizlemeSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            MaxDepth = 64
        };

        private const string OnayMerkeziMenuKodu = "islem-onay-merkezi";
        private readonly IOnayIslemRepository _onayRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOnayYetkiService _onayYetkiService;
        private readonly IRolService _rolService;
        private readonly ILogger<GetOnayGecmisiDetayiQueryHandler> _logger;

        public GetOnayGecmisiDetayiQueryHandler(
            IOnayIslemRepository onayRepository,
            ICurrentUserService currentUserService,
            IOnayYetkiService onayYetkiService,
            IRolService rolService,
            ILogger<GetOnayGecmisiDetayiQueryHandler> logger)
        {
            _onayRepository = onayRepository;
            _currentUserService = currentUserService;
            _onayYetkiService = onayYetkiService;
            _rolService = rolService;
            _logger = logger;
        }

        public async Task<Result<OnayGecmisiKayitDto>> Handle(
            GetOnayGecmisiDetayiQuery request,
            CancellationToken cancellationToken)
        {
            var kullaniciId = _currentUserService.UserId;
            if (!kullaniciId.HasValue)
                return Result<OnayGecmisiKayitDto>.Failure("Kullanıcı bilgisi alınamadı.", 401);

            var bekleyenleriGorebilir = await _rolService.HasUserPermissionAsync(
                kullaniciId.Value,
                OnayMerkeziMenuKodu,
                YetkiTipi.R,
                cancellationToken);
            var erisimKapsami = await _onayYetkiService.GetErisimKapsamiAsync(
                kullaniciId.Value,
                cancellationToken);

            var kayit = await _onayRepository.GetGecmisDetayiAsync(
                request.Id,
                kullaniciId.Value,
                bekleyenleriGorebilir,
                erisimKapsami,
                cancellationToken);

            if (kayit == null)
                return Result<OnayGecmisiKayitDto>.Failure("Onay kaydı bulunamadı.", 404);

            var dto = kayit.ToDto();
            if (kayit.IslemKodu != OnayIslemKodlari.CekiRevizyonuUygula ||
                kayit.ReferansTipi != OnayReferansTipleri.CekiRevizyonTalebi ||
                !kayit.ReferansId.HasValue)
            {
                return Result<OnayGecmisiKayitDto>.Success(dto);
            }

            var revizyonDetayi = await RevizyonDetayiniYukleAsync(kayit, cancellationToken);
            if (revizyonDetayi == null)
            {
                return Result<OnayGecmisiKayitDto>.Failure(
                    "Revizyon talebinin ön izleme bilgisi okunamadı.",
                    500);
            }

            dto.RevizyonDetayi = revizyonDetayi;
            return Result<OnayGecmisiKayitDto>.Success(dto);
        }

        private async Task<CekiRevizyonOnayDetayiDto?> RevizyonDetayiniYukleAsync(
            OnayGecmisiKaydi yetkilendirilmisOnayKaydi,
            CancellationToken cancellationToken)
        {
            var talepId = yetkilendirilmisOnayKaydi.ReferansId!.Value;
            var snapshot = await _onayRepository.GetRevizyonOnizlemeKaydiAsync(
                talepId,
                yetkilendirilmisOnayKaydi.TalepEdenKullaniciId,
                yetkilendirilmisOnayKaydi.ProjeId,
                cancellationToken);

            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.OnizlemeJson))
                return null;

            try
            {
                var onizleme = JsonSerializer.Deserialize<CekiRevizyonOnizlemeSonuc>(
                    snapshot.OnizlemeJson,
                    OnizlemeSerializerOptions);

                if (onizleme == null ||
                    snapshot.OnizlemeSurumu != CekiRevizyonOnizlemeButunlugu.Surum ||
                    !CekiRevizyonOnizlemeButunlugu.HashDogrula(onizleme, snapshot.OnizlemeHash))
                {
                    _logger.LogError(
                        "Revizyon talebi {RevizyonTalepId} ön izleme bütünlük doğrulamasından geçemedi.",
                        talepId);
                    return null;
                }

                return new CekiRevizyonOnayDetayiDto
                {
                    TalepId = talepId,
                    Onizleme = onizleme
                };
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "Revizyon talebi {RevizyonTalepId} için güvenli ön izleme snapshot'ı okunamadı.",
                    talepId);
                return null;
            }
        }
    }
}
