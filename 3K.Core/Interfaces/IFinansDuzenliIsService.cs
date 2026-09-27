using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Düzenli işler ve dönem kayıtlarının üretimi.</summary>
public interface IFinansDuzenliIsService
{
    Task<FinansSayfaliSonuc<FinansDuzenliIsModel>> DuzenliIslerSayfaliAsync(bool sadeceAktif, string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<FinansDuzenliIsModel> DuzenliIsOlusturAsync(FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken);
    Task<FinansDuzenliIsModel?> DuzenliIsGuncelleAsync(int id, FinansDuzenliIsKaydetModel model, CancellationToken cancellationToken);
    Task<FinansDonemOlusturSonucModel> DuzenliIsDonemiOlusturAsync(DateTime referansTarihi, CancellationToken cancellationToken);
}
