using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace _3K_API.Services;

/// <summary>
/// Kalıcı ambalaj kaynak kuyruğunun yaşam döngüsünü yönetir. PostgreSQL bildirimi
/// hızlı uyandırma, periyodik kuyruk kontrolü ise bildirim kaybına karşı güvence sağlar.
/// </summary>
public sealed class AmbalajKaynakSenkronizasyonBackgroundService : BackgroundService
{
    private readonly IOptionsMonitor<AmbalajKaynakSenkronizasyonOptions> _options;
    private readonly AmbalajKaynakSenkronizasyonKuyrukIsleyici _kuyrukIsleyici;
    private readonly AmbalajKaynakDegisikligiDinleyici _bildirimDinleyici;
    private readonly ILogger<AmbalajKaynakSenkronizasyonBackgroundService> _logger;
    private readonly Channel<bool> _uyandirmaKanali;
    private readonly IDisposable? _optionsChangeRegistration;

    public AmbalajKaynakSenkronizasyonBackgroundService(
        IOptionsMonitor<AmbalajKaynakSenkronizasyonOptions> options,
        AmbalajKaynakSenkronizasyonKuyrukIsleyici kuyrukIsleyici,
        AmbalajKaynakDegisikligiDinleyici bildirimDinleyici,
        ILogger<AmbalajKaynakSenkronizasyonBackgroundService> logger)
    {
        _options = options;
        _kuyrukIsleyici = kuyrukIsleyici;
        _bildirimDinleyici = bildirimDinleyici;
        _logger = logger;
        _uyandirmaKanali = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });
        _optionsChangeRegistration = _options.OnChange((_, _) => Uyandir());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Ambalaj kaynak senkronizasyon kuyruğu tüketicisi başlatıldı. Bildirim kanalı: {Channel}",
            AmbalajKaynakDegisikligiDinleyici.BildirimKanali);

        try
        {
            await Task.WhenAll(
                KuyruguTuketAsync(stoppingToken),
                _bildirimDinleyici.DinleAsync(Uyandir, stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanışı normal akıştır; sahiplenilmiş iş lease süresi sonunda tekrar alınabilir.
        }
    }

    public override void Dispose()
    {
        _optionsChangeRegistration?.Dispose();
        base.Dispose();
    }

    private async Task KuyruguTuketAsync(CancellationToken cancellationToken)
    {
        var hemenCalistir = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            var ayarlar = _options.CurrentValue.EtkinDegerleriAl();
            if (!hemenCalistir)
            {
                await BildirimVeyaFallbackSuresiniBekleAsync(
                    TimeSpan.FromSeconds(ayarlar.FallbackPollSeconds),
                    cancellationToken);
                ayarlar = _options.CurrentValue.EtkinDegerleriAl();
            }

            hemenCalistir = false;
            try
            {
                var sonuc = await _kuyrukIsleyici.IslemDiliminiCalistirAsync(ayarlar, cancellationToken);
                if (sonuc == AmbalajKaynakSenkronizasyonDilimiSonucu.DilimDoldu)
                {
                    // Backlog'u bekletmeden sürdürürken diğer async işlere de çalışma fırsatı ver.
                    Uyandir();
                    await Task.Yield();
                }
                else if (sonuc == AmbalajKaynakSenkronizasyonDilimiSonucu.Askida)
                {
                    await AskidaykenBekleAsync(ayarlar, cancellationToken);
                    hemenCalistir = true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ambalaj kaynak senkronizasyon kuyruğu okunamadı.");
                await Task.Delay(TimeSpan.FromSeconds(ayarlar.FallbackPollSeconds), cancellationToken);
                hemenCalistir = true;
            }
        }
    }

    private async Task BildirimVeyaFallbackSuresiniBekleAsync(
        TimeSpan fallbackSuresi,
        CancellationToken cancellationToken)
    {
        using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutTokenSource.CancelAfter(fallbackSuresi);
        try
        {
            await _uyandirmaKanali.Reader.ReadAsync(timeoutTokenSource.Token);
            BildirimTamponunuBosalt();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Bildirim gelmezse dayanıklılık için fallback süresi sonunda kuyruk kontrol edilir.
        }
    }

    private async Task AskidaykenBekleAsync(
        AmbalajKaynakSenkronizasyonCalismaAyarlari ayarlar,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(ayarlar.FallbackPollSeconds), cancellationToken);
        BildirimTamponunuBosalt();
    }

    private void BildirimTamponunuBosalt()
    {
        while (_uyandirmaKanali.Reader.TryRead(out _))
        {
        }
    }

    private void Uyandir() => _uyandirmaKanali.Writer.TryWrite(true);
}
