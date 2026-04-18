using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// BackgroundService — Kuyruktan işleri sırayla alır ve çalıştırır.
    /// Her iş kendi DI scope'unda çalışır (DbContext vb. doğru yaşam döngüsü).
    /// Uygulama ayakta olduğu sürece çalışır, graceful shutdown destekler.
    /// </summary>
    public class BackgroundTaskProcessor : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackgroundTaskProcessor> _logger;

        public BackgroundTaskProcessor(
            IBackgroundTaskQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<BackgroundTaskProcessor> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Task Processor başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await _queue.DequeueAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown — normal
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background görev işlenirken hata oluştu.");
                }
            }

            _logger.LogInformation("Background Task Processor durduruluyor.");
        }
    }
}
