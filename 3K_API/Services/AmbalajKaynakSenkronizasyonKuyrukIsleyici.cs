using Microsoft.EntityFrameworkCore;
using _3K.Application.Common;
using _3K.Application.Features.AmbalajIslemleri;
using _3K.Application.Features.AmbalajIslemleri.Commands;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K_API.Services;

internal enum AmbalajKaynakSenkronizasyonDilimiSonucu
{
    Tamamlandi,
    DilimDoldu,
    Askida
}

/// <summary>
/// Bir tüketim dilimindeki kuyruk sahiplenme, senkronizasyon ve ack/retry akışını yürütür.
/// Her iş işlemden hemen önce tek başına claim edilir; böylece sırada beklerken lease tüketmez.
/// </summary>
public sealed class AmbalajKaynakSenkronizasyonKuyrukIsleyici
{
    private static readonly TimeSpan AzamiYenidenDenemeGecikmesi = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AmbalajKaynakSenkronizasyonKuyrukIsleyici> _logger;
    private string? _sonEngelKodu;

    public AmbalajKaynakSenkronizasyonKuyrukIsleyici(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<AmbalajKaynakSenkronizasyonKuyrukIsleyici> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    internal async Task<AmbalajKaynakSenkronizasyonDilimiSonucu> IslemDiliminiCalistirAsync(
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        if (!ayarlar.Enabled)
        {
            EngeliBirKezLogla(
                "disabled",
                LogLevel.Information,
                "Ambalaj otomatik kaynak senkronizasyonu yapılandırma ile devre dışı.");
            return AmbalajKaynakSenkronizasyonDilimiSonucu.Askida;
        }

        if (!await SahiplenmeKosullariSaglandiMiAsync(ayarlar, cancellationToken))
            return AmbalajKaynakSenkronizasyonDilimiSonucu.Askida;

        EngelKalktiysaLogla();

        var islenenIsSayisi = 0;
        while (islenenIsSayisi < ayarlar.BatchSize && !cancellationToken.IsCancellationRequested)
        {
            var isKaydi = await SiradakiIsiSahiplenAsync(ayarlar, cancellationToken);
            if (isKaydi is null)
                return AmbalajKaynakSenkronizasyonDilimiSonucu.Tamamlandi;

            await IsiIsleAsync(isKaydi, ayarlar, cancellationToken);
            islenenIsSayisi++;
        }

        return AmbalajKaynakSenkronizasyonDilimiSonucu.DilimDoldu;
    }

    private async Task<bool> SahiplenmeKosullariSaglandiMiAsync(
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        if (!ayarlar.SystemUserId.HasValue || ayarlar.SystemUserId <= 0)
        {
            EngeliBirKezLogla(
                "missing-user-id",
                LogLevel.Warning,
                "Ambalaj otomatik kaynak senkronizasyonu bekliyor: SystemUserId tanımlı değil veya geçersiz. Kuyruktan iş sahiplenilmeyecek.");
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var rolService = scope.ServiceProvider.GetRequiredService<IRolService>();
        var duzenlemeyeYetkiliMi = await rolService.HasUserPermissionAsync(
            ayarlar.SystemUserId.Value,
            AmbalajMenuKodlari.KayitDuzenle,
            YetkiTipi.W,
            cancellationToken);
        if (duzenlemeyeYetkiliMi)
            return true;

        // Yetki sorgusu kullanıcı yokken de false döner. Yalnız başarısız yolda
        // doğru operasyon mesajını seçebilmek için ek varlık kontrolü yapılır.
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var userExists = await unitOfWork.GetRepository<Kullanici>()
            .Queryable()
            .AnyAsync(x => x.Id == ayarlar.SystemUserId.Value, cancellationToken);
        if (!userExists)
        {
            EngeliBirKezLogla(
                $"unknown-user:{ayarlar.SystemUserId.Value}",
                LogLevel.Error,
                "Ambalaj otomatik kaynak senkronizasyonu bekliyor: servis kullanıcısı bulunamadı. KullaniciId: {UserId}. Kuyruktan iş sahiplenilmeyecek.",
                ayarlar.SystemUserId.Value);
            return false;
        }

        EngeliBirKezLogla(
            $"unauthorized-user:{ayarlar.SystemUserId.Value}",
            LogLevel.Error,
            "Ambalaj otomatik kaynak senkronizasyonu bekliyor: servis kullanıcısında üretim düzenleme (W) yetkisi yok. KullaniciId: {UserId}. Kuyruktan iş sahiplenilmeyecek.",
            ayarlar.SystemUserId.Value);
        return false;
    }

    private async Task<AmbalajKaynakSenkronizasyonKuyrukIsi?> SiradakiIsiSahiplenAsync(
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var kuyruk = scope.ServiceProvider.GetRequiredService<IAmbalajKaynakSenkronizasyonKuyrugu>();
        var isler = await kuyruk.IsleriSahiplenAsync(
            1,
            TimeSpan.FromSeconds(ayarlar.LeaseSeconds),
            cancellationToken);
        return isler.Count == 0 ? null : isler[0];
    }

    private async Task IsiIsleAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        Result<AmbalajSenkronizasyonSonucuDto> result;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var synchronizationService = scope.ServiceProvider
                .GetRequiredService<IAmbalajKaynakSenkronizasyonService>();
            result = await synchronizationService.SenkronizeEtAsync(
                isKaydi.ProjeId,
                new FixedCurrentUserService(ayarlar.SystemUserId!.Value),
                cancellationToken,
                sonucKayitlariniOlustur: false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation hata/deneme olarak yazılmaz. Lease dolduğunda iş tekrar sahiplenilir.
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Ambalaj otomatik kaynak senkronizasyonu hata verdi. ProjeId: {ProjectId}, Sürüm: {Revision}",
                isKaydi.ProjeId,
                isKaydi.Surum);
            await BasarisizTamamlaAsync(
                isKaydi,
                exception.Message,
                null,
                ayarlar,
                cancellationToken);
            return;
        }

        if (result.IsSuccess || result.StatusCode == 404)
        {
            await BasariyiKaydetAsync(isKaydi, cancellationToken);
            return;
        }

        var hata = result.Error?.Message
            ?? $"Senkronizasyon başarısız oldu (HTTP {result.StatusCode}).";
        await BasarisizTamamlaAsync(
            isKaydi,
            hata,
            result.StatusCode,
            ayarlar,
            cancellationToken);
    }

    private async Task BasariyiKaydetAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var kuyruk = scope.ServiceProvider.GetRequiredService<IAmbalajKaynakSenkronizasyonKuyrugu>();
            var sonuc = await kuyruk.BasariliTamamlaAsync(isKaydi, cancellationToken);

            if (sonuc.Durum == AmbalajKaynakSenkronizasyonSonlandirmaDurumu.YenidenKuyrugaAlindi)
            {
                _logger.LogDebug(
                    "Ambalaj senkronizasyonu sırasında yeni kaynak değişikliği oluştu; proje yeniden kuyruğa alındı. ProjeId: {ProjectId}",
                    isKaydi.ProjeId);
            }
            else if (sonuc.Durum == AmbalajKaynakSenkronizasyonSonlandirmaDurumu.SahiplikKaybedildi)
            {
                _logger.LogWarning(
                    "Ambalaj senkronizasyon işinin lease sahipliği tamamlamadan önce kaybedildi. ProjeId: {ProjectId}, Sürüm: {Revision}",
                    isKaydi.ProjeId,
                    isKaydi.Surum);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Başarılı ambalaj senkronizasyon işi kuyrukta tamamlanamadı; lease sonunda idempotent olarak tekrar işlenecek. ProjeId: {ProjectId}",
                isKaydi.ProjeId);
        }
    }

    private async Task BasarisizTamamlaAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        string hata,
        int? durumKodu,
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        var yenidenDenemeTarihiUtc = _timeProvider.GetUtcNow().UtcDateTime
            .Add(YenidenDenemeGecikmesiniHesapla(isKaydi.DenemeSayisi));
        var guvenliHata = HataMetniniSinirla(hata);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var kuyruk = scope.ServiceProvider.GetRequiredService<IAmbalajKaynakSenkronizasyonKuyrugu>();
            var sonuc = await kuyruk.BasarisizTamamlaAsync(
                isKaydi,
                guvenliHata,
                yenidenDenemeTarihiUtc,
                ayarlar.MaxAttempts,
                cancellationToken);

            if (sonuc.Durum == AmbalajKaynakSenkronizasyonSonlandirmaDurumu.HataKuyrugunaAlindi)
            {
                _logger.LogError(
                    "Ambalaj kaynak senkronizasyon işi azami deneme sayısına ulaştı. ProjeId: {ProjectId}, Deneme: {Attempt}, Hata: {Error}",
                    isKaydi.ProjeId,
                    sonuc.DenemeSayisi,
                    guvenliHata);
                return;
            }

            if (sonuc.Durum == AmbalajKaynakSenkronizasyonSonlandirmaDurumu.SahiplikKaybedildi)
            {
                _logger.LogWarning(
                    "Başarısız ambalaj senkronizasyon işinin lease sahipliği kaybedildi. ProjeId: {ProjectId}, Sürüm: {Revision}",
                    isKaydi.ProjeId,
                    isKaydi.Surum);
                return;
            }

            _logger.LogWarning(
                "Ambalaj kaynak senkronizasyon işi yeniden denenecek. ProjeId: {ProjectId}, Durum: {Status}, Deneme: {Attempt}, SonrakiDenemeUtc: {RetryAtUtc}, Hata: {Error}",
                isKaydi.ProjeId,
                durumKodu,
                sonuc.DenemeSayisi,
                yenidenDenemeTarihiUtc,
                guvenliHata);
        }
        catch (OperationCanceledException)
        {
            // Cancellation hata/deneme olarak yazılmaz; lease mekanizması işi geri kazandırır.
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Başarısız ambalaj senkronizasyon işi kuyruğa geri yazılamadı; lease sonunda tekrar alınacak. ProjeId: {ProjectId}",
                isKaydi.ProjeId);
        }
    }

    private void EngeliBirKezLogla(
        string kod,
        LogLevel level,
        string message,
        params object?[] args)
    {
        if (string.Equals(_sonEngelKodu, kod, StringComparison.Ordinal))
            return;

        _sonEngelKodu = kod;
        _logger.Log(level, message, args);
    }

    private void EngelKalktiysaLogla()
    {
        if (_sonEngelKodu is null)
            return;

        _sonEngelKodu = null;
        _logger.LogInformation("Ambalaj otomatik kaynak senkronizasyonunun bekleme nedeni giderildi; kuyruk tüketimi devam ediyor.");
    }

    private static TimeSpan YenidenDenemeGecikmesiniHesapla(int denemeSayisi)
    {
        var carpan = Math.Pow(2, Math.Clamp(denemeSayisi, 0, 8));
        var gecikme = TimeSpan.FromSeconds(5 * carpan);
        return gecikme <= AzamiYenidenDenemeGecikmesi
            ? gecikme
            : AzamiYenidenDenemeGecikmesi;
    }

    private static string HataMetniniSinirla(string? hata)
    {
        const int maxLength = 2000;
        var sonuc = string.IsNullOrWhiteSpace(hata) ? "Bilinmeyen senkronizasyon hatası." : hata.Trim();
        return sonuc.Length <= maxLength ? sonuc : sonuc[..maxLength];
    }

    private sealed class FixedCurrentUserService(int userId) : ICurrentUserService
    {
        public int? UserId => userId;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }
}
