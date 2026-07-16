namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Aktif saha aktarım defterine bağlı kayıtların genel silme akışlarından
    /// kaldırılmasını engellemek için toplu ilişki sorguları sağlar.
    /// </summary>
    public interface ISahaAktarimSilmeKorumaService
    {
        Task<HashSet<int>> GetAktifAktarimBagliSandikIdsAsync(
            IEnumerable<int> sandikIds,
            CancellationToken cancellationToken = default);

        Task<HashSet<int>> GetAktifAktarimBagliCekiSatiriIdsAsync(
            IEnumerable<int> cekiSatiriIds,
            CancellationToken cancellationToken = default);
    }
}
