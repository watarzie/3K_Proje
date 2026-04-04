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
    }
}
