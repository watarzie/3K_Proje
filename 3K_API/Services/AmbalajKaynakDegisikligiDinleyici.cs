using Microsoft.Extensions.Options;
using Npgsql;

namespace _3K_API.Services;

/// <summary>
/// PostgreSQL bildirimini düşük gecikmeli bir uyandırma sinyaline dönüştürür.
/// Bağlantı veya bildirim kaybı teslimat kaybına yol açmaz; kalıcı kuyruk esas kaynaktır.
/// </summary>
public sealed class AmbalajKaynakDegisikligiDinleyici
{
    internal const string BildirimKanali = "ambalaj_kaynak_degisti";

    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<AmbalajKaynakSenkronizasyonOptions> _options;
    private readonly ILogger<AmbalajKaynakDegisikligiDinleyici> _logger;
    private int _hataSayisi;

    public AmbalajKaynakDegisikligiDinleyici(
        IConfiguration configuration,
        IOptionsMonitor<AmbalajKaynakSenkronizasyonOptions> options,
        ILogger<AmbalajKaynakDegisikligiDinleyici> logger)
    {
        _configuration = configuration;
        _options = options;
        _logger = logger;
    }

    internal async Task DinleAsync(Action kuyruguUyandir, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(kuyruguUyandir);

        while (!cancellationToken.IsCancellationRequested)
        {
            var ayarlar = _options.CurrentValue.EtkinDegerleriAl();
            if (!ayarlar.Enabled || !ayarlar.SystemUserId.HasValue || ayarlar.SystemUserId <= 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(ayarlar.FallbackPollSeconds), cancellationToken);
                continue;
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (_hataSayisi++ == 0)
                {
                    _logger.LogError(
                        "PostgreSQL bildirim dinleyicisi başlatılamadı: DefaultConnection tanımlı değil. Fallback kuyruk kontrolü çalışmaya devam edecek.");
                }

                await Task.Delay(TimeSpan.FromSeconds(ayarlar.FallbackPollSeconds), cancellationToken);
                continue;
            }

            try
            {
                // Bildirim bağlantısı uzun süre boşta kalabilir. Sessiz ağ kopmalarını
                // tespit etmek için yalnız bu kalıcı bağlantıda keepalive kullanılır.
                var dinleyiciBaglantisi = new NpgsqlConnectionStringBuilder(connectionString);
                if (dinleyiciBaglantisi.KeepAlive == 0)
                    dinleyiciBaglantisi.KeepAlive = 30;
                await using var connection = new NpgsqlConnection(dinleyiciBaglantisi.ConnectionString);
                connection.Notification += (_, args) =>
                {
                    if (string.Equals(args.Channel, BildirimKanali, StringComparison.Ordinal))
                        kuyruguUyandir();
                };

                await connection.OpenAsync(cancellationToken);
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"LISTEN {BildirimKanali};";
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                _hataSayisi = 0;
                _logger.LogInformation(
                    "PostgreSQL ambalaj kaynak bildirim dinleyicisi bağlandı. Kanal: {Channel}",
                    BildirimKanali);
                kuyruguUyandir();

                while (!cancellationToken.IsCancellationRequested)
                    await connection.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _hataSayisi++;
                var gecikme = YenidenBaglanmaGecikmesiniHesapla(_hataSayisi);
                _logger.LogWarning(
                    exception,
                    "PostgreSQL ambalaj bildirim bağlantısı kesildi. {DelaySeconds} saniye sonra yeniden bağlanılacak; fallback kuyruk kontrolü çalışmaya devam ediyor.",
                    gecikme.TotalSeconds);
                await Task.Delay(gecikme, cancellationToken);
            }
        }
    }

    private static TimeSpan YenidenBaglanmaGecikmesiniHesapla(int hataSayisi)
        => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Clamp(hataSayisi - 1, 0, 5))));
}
