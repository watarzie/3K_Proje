namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Dashboard özet istatistiklerini SQL aggregate sorgularıyla hesaplar.
    /// Implementasyon Infrastructure katmanında, AppDbContext kullanır.
    /// </summary>
    public interface IDashboardStatsProvider
    {
        Task<Entities.DashboardOzetRawStats> GetOzetStatsAsync(CancellationToken ct = default);
    }
}
