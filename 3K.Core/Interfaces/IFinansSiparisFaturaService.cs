using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Sipariş (PO) ve fatura kayıtları.</summary>
public interface IFinansSiparisFaturaService
{
    Task<FinansSayfaliSonuc<FinansSiparisModel>> SiparislerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansSiparisGrubuModel>> SiparisGruplariAsync(FinansSiparisGrupFiltre filtre, CancellationToken cancellationToken);
    Task<FinansSiparisModel?> SiparisGetirAsync(int id, CancellationToken cancellationToken);
    Task<FinansSiparisModel> SiparisOlusturAsync(FinansSiparisOlusturModel model, CancellationToken cancellationToken);
    Task<FinansSiparisModel?> SiparisKalemEkleAsync(int id, FinansSiparisKalemEkleModel model, CancellationToken cancellationToken);
    Task<FinansSiparisModel?> SiparisGuncelleAsync(int id, FinansSiparisGuncelleModel model, CancellationToken cancellationToken);
    Task<bool> SiparisIptalAsync(int id, string aciklama, CancellationToken cancellationToken);

    Task<FinansSayfaliSonuc<FinansFaturaModel>> FaturalarAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansFaturaModel?> FaturaGetirAsync(int id, CancellationToken cancellationToken);
    Task<FinansFaturaModel> FaturaOlusturAsync(FinansFaturaOlusturModel model, CancellationToken cancellationToken);
    Task<FinansFaturaModel?> FaturaGuncelleAsync(int id, FinansFaturaGuncelleModel model, CancellationToken cancellationToken);
    Task<bool> FaturaIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
    /// <summary>Faturayı ödendi/ödenmedi işaretler (D3); iptal edilmiş fatura işaretlenemez.</summary>
    Task<FinansFaturaModel?> OdemeKaydetAsync(int id, FinansOdemeKaydetModel model, CancellationToken cancellationToken);
    /// <summary>Vadesi geçen ve uyarı penceresinde vadesi gelen ödenmemiş faturalar.</summary>
    Task<FinansVadeUyarilariModel> VadeUyarilariAsync(int enFazlaKalem, CancellationToken cancellationToken);
}
