using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Gider kayıtları.</summary>
public interface IFinansGiderService
{
    Task<FinansSayfaliSonuc<FinansGiderModel>> GiderlerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansGiderModel> GiderOlusturAsync(FinansGiderKaydetModel model, CancellationToken cancellationToken);
    Task<FinansGiderModel?> GiderGuncelleAsync(int id, FinansGiderKaydetModel model, CancellationToken cancellationToken);
    Task<bool> GiderIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
}
