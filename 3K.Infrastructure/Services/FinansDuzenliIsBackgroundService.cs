using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// Aylık düzenli finans işlerini günlük ve idempotent olarak üretir.
    /// Bir şablon/dönem çifti veritabanındaki kaynak unique anahtarı sayesinde
    /// birden fazla API instance'ı çalışsa dahi tek finans kaydına dönüşür.
    /// </summary>
    public sealed class FinansDuzenliIsBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FinansDuzenliIsBackgroundService> _logger;

        public FinansDuzenliIsBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<FinansDuzenliIsBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunSafelyAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = TurkeyTime.Now;
                var next = now.Date.AddDays(1).AddHours(3).AddMinutes(15);
                var delay = next - now;
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                await RunSafelyAsync(stoppingToken);
            }
        }

        private async Task RunSafelyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IFinansService>();
                var result = await service.DuzenliIsDonemiOlusturAsync(TurkeyTime.Now, cancellationToken);
                if (result.Olusturulan > 0)
                {
                    _logger.LogInformation(
                        "Düzenli finans işleri oluşturuldu. Taranan: {Taranan}, oluşturulan: {Olusturulan}, dönem: {Donem}",
                        result.Taranan,
                        result.Olusturulan,
                        result.ReferansTarihi.ToString("yyyy-MM"));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal uygulama kapanışı.
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Düzenli finans işleri oluşturulurken hata oluştu; API çalışmaya devam edecek.");
            }
        }
    }
}
