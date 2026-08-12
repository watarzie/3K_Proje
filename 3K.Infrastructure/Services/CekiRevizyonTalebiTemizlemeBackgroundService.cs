using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _3K.Core.Helpers;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

/// <summary>
/// Başarıyla uygulanmış revizyon taleplerindeki büyük Excel içeriğini,
/// günlük yedek alınmadan önce temizler. Talep ve ön izleme geçmişi korunur.
/// </summary>
public sealed class CekiRevizyonTalebiTemizlemeBackgroundService : BackgroundService
{
    private static readonly TimeSpan CalismaSaati = new(22, 45, 0);
    private static readonly TimeSpan HataSonrasiBekleme = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CekiRevizyonTalebiTemizlemeBackgroundService> _logger;

    public CekiRevizyonTalebiTemizlemeBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CekiRevizyonTalebiTemizlemeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Çeki revizyon talebi dosya temizleme servisi başlatıldı.");

        // Servis 22:45 çalışmasını kaçırmış veya yeni deploy edilmişse geçmişte
        // birikmiş uygulanmış talepleri ertesi günü beklemeden idempotent temizle.
        try
        {
            await TemizleyeneKadarDeneAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = TurkeyTime.Now;
            var nextRun = now.Date.Add(CalismaSaati);
            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            _logger.LogInformation(
                "Sonraki çeki revizyon talebi dosya temizliği: {NextRun:dd.MM.yyyy HH:mm}",
                nextRun);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
                await TemizleyeneKadarDeneAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task TemizleyeneKadarDeneAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var temizlenenTalepSayisi = await context.CekiRevizyonTalepleri
                    .Where(talep =>
                        talep.UygulananRevizyonCekiId.HasValue &&
                        talep.UygulamaTarihi.HasValue &&
                        talep.DosyaIcerigi != null)
                    .ExecuteUpdateAsync(
                        guncelleme => guncelleme.SetProperty(
                            talep => talep.DosyaIcerigi,
                            (byte[]?)null),
                        stoppingToken);

                _logger.LogInformation(
                    "Çeki revizyon talebi dosya temizliği tamamlandı. Temizlenen talep: {Count}",
                    temizlenenTalepSayisi);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Çeki revizyon talebi dosya temizliği başarısız oldu; {RetryMinutes} dakika sonra tekrar denenecek.",
                    HataSonrasiBekleme.TotalMinutes);
                await Task.Delay(HataSonrasiBekleme, stoppingToken);
            }
        }
    }
}
