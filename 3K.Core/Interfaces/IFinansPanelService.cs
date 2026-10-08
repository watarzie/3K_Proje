using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans paneli: dönem özeti, para hareketleri, genel arama, yaşlandırma ve denetim geçmişi.</summary>
public interface IFinansPanelService
{
    Task<FinansPanelModel> PanelAsync(DateTime baslangic, DateTime bitis, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansHareketModel>> HareketlerAsync(FinansListeFiltre filtre, string? tur, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansIsKaydiModel>> GenelAramaAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    /// <summary>Münferit İşler sekmesi: elle açılmış, düzenli işe bağlı olmayan özel işler (filtre tarih/arama/iptal).</summary>
    Task<FinansSayfaliSonuc<FinansIsKaydiModel>> MunferitIslerAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    /// <param name="asama">Yalnız bu aşamadaki kalemler (<see cref="FinansBekleyenAsamalari"/>); null ise ikisi de.</param>
    Task<FinansSayfaliSonuc<FinansBekleyenModel>> YaslandirmaAsync(DateTime referansTarihi, int minimumGun, string? asama, FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansDegisiklikModel>> DegisiklikGecmisiAsync(string? varlikTuru, int? varlikId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
