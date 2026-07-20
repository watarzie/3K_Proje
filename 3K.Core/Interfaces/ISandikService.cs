using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Core.Interfaces
{
    public interface ISandikService
    {
        Task<IEnumerable<Sandik>> GetProjeSandiklariAsync(int projeId);
        Task<Sandik?> GetSandikDetayAsync(int sandikId);
        Task<Sandik?> GetSandikByNoAsync(int projeId, string sandikNo);
        Task<Sandik> SandikOlusturAsync(int projeId, string sandikNo, string depoLokasyonu = "Belirsiz");
        Task<bool> SandikDegistirAsync(int cekiSatiriId, int yeniSandikId, int kullaniciId, int projeId);
        Task<IEnumerable<SandikIcerik>> GetSandikIcerikAsync(int sandikId);

        /// <summary>
        /// Fiziksel tahsisleri ve yalnızca sandık numarasıyla ilişkilendirilebilen eski çeki
        /// satırlarını tek bir salt-okunur sandık içeriği görünümünde birleştirir.
        /// </summary>
        Task<IReadOnlyDictionary<int, IReadOnlyCollection<SandikIcerik>>> GetEtkinSandikIcerikleriAsync(
            IEnumerable<int> sandikIds,
            CancellationToken cancellationToken = default);
    }
}
