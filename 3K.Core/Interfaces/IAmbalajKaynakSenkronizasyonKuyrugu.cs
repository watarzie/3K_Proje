using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Ambalaj kaynak senkronizasyonunun kalici kuyruk kontrati.
/// Implementasyonlar en az bir kez teslim semantigi saglar; tuketici islemleri
/// idempotent olmalidir.
/// </summary>
public interface IAmbalajKaynakSenkronizasyonKuyrugu
{
    Task KuyrugaEkleAsync(int projeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AmbalajKaynakSenkronizasyonKuyrukIsi>> IsleriSahiplenAsync(
        int azamiIsSayisi,
        TimeSpan kilitSuresi,
        CancellationToken cancellationToken = default);

    Task<AmbalajKaynakSenkronizasyonSonlandirmaSonucu> BasariliTamamlaAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        CancellationToken cancellationToken = default);

    Task<AmbalajKaynakSenkronizasyonSonlandirmaSonucu> BasarisizTamamlaAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        string hata,
        DateTime yenidenDenemeTarihiUtc,
        int azamiDenemeSayisi,
        CancellationToken cancellationToken = default);

    Task<AmbalajKaynakSenkronizasyonKuyrukIstatistigi> IstatistikleriGetirAsync(
        CancellationToken cancellationToken = default);
}
