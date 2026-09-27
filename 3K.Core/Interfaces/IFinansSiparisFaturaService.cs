using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Sipariş (PO) ve fatura kayıtları.</summary>
public interface IFinansSiparisFaturaService
{
    Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken);
    Task<FinansSiparisModel> SiparisOlusturAsync(FinansSiparisOlusturModel model, CancellationToken cancellationToken);
    Task<FinansSiparisModel?> SiparisGuncelleAsync(int id, FinansSiparisGuncelleModel model, CancellationToken cancellationToken);
    Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken);

    Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken);
    Task<FinansFaturaModel> FaturaOlusturAsync(FinansFaturaOlusturModel model, CancellationToken cancellationToken);
    Task<FinansFaturaModel?> FaturaGuncelleAsync(int id, FinansFaturaGuncelleModel model, CancellationToken cancellationToken);
    Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
}
