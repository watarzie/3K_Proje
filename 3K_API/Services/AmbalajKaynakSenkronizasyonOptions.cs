namespace _3K_API.Services;

public sealed class AmbalajKaynakSenkronizasyonOptions
{
    public const string SectionName = "Ambalaj:KaynakSenkronizasyon";

    public bool Enabled { get; set; } = true;
    public int? SystemUserId { get; set; }
    public int BatchSize { get; set; } = 10;
    public int FallbackPollSeconds { get; set; } = 30;
    public int LeaseSeconds { get; set; } = 300;
    public int MaxAttempts { get; set; } = 10;

    internal AmbalajKaynakSenkronizasyonCalismaAyarlari EtkinDegerleriAl()
        => new(
            Enabled,
            SystemUserId,
            Math.Clamp(BatchSize, 1, 100),
            Math.Clamp(FallbackPollSeconds, 5, 3600),
            Math.Clamp(LeaseSeconds, 30, 3600),
            Math.Clamp(MaxAttempts, 1, 100));
}

internal sealed record AmbalajKaynakSenkronizasyonCalismaAyarlari(
    bool Enabled,
    int? SystemUserId,
    int BatchSize,
    int FallbackPollSeconds,
    int LeaseSeconds,
    int MaxAttempts);
