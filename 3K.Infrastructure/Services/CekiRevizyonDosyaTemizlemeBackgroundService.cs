using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;

namespace _3K.Infrastructure.Services
{
    public sealed class CekiRevizyonDosyaTemizlemeBackgroundService : BackgroundService
    {
        private static readonly TimeSpan[] CalismaSaatleri =
        {
            new(22, 45, 0),
            new(22, 55, 0)
        };

        private const int MaksimumDenemeSayisi = 3;
        private static readonly TimeSpan TekrarDenemeAraligi = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CekiRevizyonDosyaTemizlemeBackgroundService> _logger;

        public CekiRevizyonDosyaTemizlemeBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CekiRevizyonDosyaTemizlemeBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Çeki revizyon dosya temizleme servisi başlatıldı. Günlük çalışma saatleri: 22:45 ve 22:55 (Türkiye).");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var simdi = TurkeyTime.Now;
                    var sonrakiCalisma = SonrakiCalismaZamaniniBul(simdi);

                    _logger.LogInformation(
                        "Sonraki çeki revizyon dosya temizliği: {SonrakiCalisma:dd.MM.yyyy HH:mm}",
                        sonrakiCalisma);

                    await Task.Delay(sonrakiCalisma - simdi, stoppingToken);
                    await TemizligiCalistirAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Çeki revizyon dosya temizleme zamanlayıcısında beklenmeyen hata oluştu.");

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task TemizligiCalistirAsync(CancellationToken stoppingToken)
        {
            for (var deneme = 1; deneme <= MaksimumDenemeSayisi; deneme++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var temizlemeService = scope.ServiceProvider
                        .GetRequiredService<ICekiRevizyonDosyaTemizlemeService>();

                    await temizlemeService
                        .BugunYuklenenUygulanmisDosyaIcerikleriniTemizleAsync(stoppingToken);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception) when (deneme < MaksimumDenemeSayisi)
                {
                    _logger.LogWarning(
                        exception,
                        "Çeki revizyon dosya temizliği başarısız oldu. Deneme: {Deneme}/{MaksimumDeneme}. {BeklemeDakika} dakika sonra tekrar denenecek.",
                        deneme,
                        MaksimumDenemeSayisi,
                        TekrarDenemeAraligi.TotalMinutes);

                    await Task.Delay(TekrarDenemeAraligi, stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Çeki revizyon dosya temizliği {MaksimumDeneme} denemede tamamlanamadı. Bir sonraki zamanlı taramada yeniden denenecek.",
                        MaksimumDenemeSayisi);
                }
            }
        }

        private static DateTime SonrakiCalismaZamaniniBul(DateTime simdi)
        {
            foreach (var calismaSaati in CalismaSaatleri)
            {
                var aday = simdi.Date.Add(calismaSaati);
                if (aday > simdi)
                    return aday;
            }

            return simdi.Date.AddDays(1).Add(CalismaSaatleri[0]);
        }
    }
}
