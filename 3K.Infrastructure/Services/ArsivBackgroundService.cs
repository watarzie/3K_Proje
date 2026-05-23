using _3K.Core.Interfaces;
using _3K.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _3K.Infrastructure.Services
{
    public class ArsivBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ArsivBackgroundService> _logger;

        public ArsivBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ArsivBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Arşiv arka plan servisi başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Her gece saat 02:00'de çalış
                    var now = TurkeyTime.Now;
                    var nextRun = now.Date.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
                    var delay = nextRun - now;

                    _logger.LogInformation("Sonraki arşivleme: {NextRun:dd.MM.yyyy HH:mm}", nextRun);

                    await Task.Delay(delay, stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var arsivService = scope.ServiceProvider.GetRequiredService<IArsivService>();
                    var count = await arsivService.ProjeleriArsivleAsync();

                    if (count > 0)
                        _logger.LogInformation("Otomatik arşivleme tamamlandı: {Count} kayıt taşındı", count);
                    else
                        _logger.LogInformation("Arşivlenecek kayıt bulunamadı.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Arşivleme sırasında hata oluştu");
                    // Hata durumunda 1 saat bekle ve tekrar dene
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}
