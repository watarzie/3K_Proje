using _3K.Core.Models;

namespace _3K.Core.Interfaces;

public interface IKullaniciYetkiService
{
    Task<IReadOnlyList<KullaniciYetkiModel>?> GetAsync(int kullaniciId, CancellationToken cancellationToken = default);
    Task<KullaniciYetkiSonucu> UpdateAsync(int kullaniciId, IReadOnlyCollection<KullaniciYetkiKarari> kararlar,
        CancellationToken cancellationToken = default);
    Task<KullaniciYetkiSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
        CancellationToken cancellationToken = default);
}
