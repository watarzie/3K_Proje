using _3K.Core.Entities;

namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 3-4: Sandık operasyonları
    /// UML Sequence Diagram: SandikService
    /// </summary>
    public interface ISandikService
    {
        Task<IEnumerable<Sandik>> GetProjeSandiklariAsync(int projeId);
        Task<Sandik?> GetSandikDetayAsync(int sandikId);
        Task<Sandik?> GetSandikByNoAsync(int projeId, string sandikNo);
        Task<Sandik> SandikOlusturAsync(int projeId, string sandikNo, _3K.Core.Enums.DepoLokasyon depoLokasyonu = _3K.Core.Enums.DepoLokasyon.Belirsiz);
        Task<bool> SandikDegistirAsync(int cekiSatiriId, int yeniSandikId, int kullaniciId, int projeId);
        Task<IEnumerable<SandikIcerik>> GetSandikIcerikAsync(int sandikId);
    }
}
