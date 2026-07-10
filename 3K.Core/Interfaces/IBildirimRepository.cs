using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IBildirimRepository
    {
        Task<(IReadOnlyList<KullaniciBildirimi> Bildirimler, int Toplam)> GetOkunmamisAsync(
            int kullaniciId,
            int limit,
            CancellationToken cancellationToken = default);

        Task<KullaniciBildirimi?> GetKullaniciBildirimiAsync(
            int bildirimId,
            int kullaniciId,
            CancellationToken cancellationToken = default);

        Task<int> TumOkunmamisBildirimleriOkunduIsaretleAsync(
            int kullaniciId,
            DateTime okunmaTarihi,
            CancellationToken cancellationToken = default);

        Task<BildirimSayfaliSorguSonucu> GetSayfaliAsync(
            int kullaniciId,
            BildirimListeFiltresi filtre,
            CancellationToken cancellationToken = default);

        Task<BildirimSorguKaydi?> GetDetayAsync(
            int bildirimId,
            int kullaniciId,
            CancellationToken cancellationToken = default);
    }
}
