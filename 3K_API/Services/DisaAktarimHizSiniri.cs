using System.Threading.RateLimiting;
using _3K_API.Extensions;
using _3K.Core.Interfaces;

namespace _3K_API.Services;

/// <summary>
/// Dışa aktarım sınırı: kullanıcı başına dakikada <see cref="HizSiniriBolumleri.DisaAktarimDakikaLimiti"/> dosya
/// isteği (sabit pencere, kuyruk yok). Uygulama boyunca tek örnektir; sayaçlar bellektedir.
/// </summary>
public sealed class DisaAktarimHizSiniri : IDisaAktarimHizSiniri, IDisposable
{
    private readonly PartitionedRateLimiter<int> _sinir = PartitionedRateLimiter.Create<int, int>(kullaniciId =>
        RateLimitPartition.GetFixedWindowLimiter(kullaniciId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = HizSiniriBolumleri.DisaAktarimDakikaLimiti,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));

    public bool IzinVer(int kullaniciId)
    {
        using var izin = _sinir.AttemptAcquire(kullaniciId);
        return izin.IsAcquired;
    }

    public void Dispose() => _sinir.Dispose();
}
