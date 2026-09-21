using _3K.Core.Models;

namespace _3K.Core.Interfaces;

public partial interface IFinansService
{
    Task<FinansIsKaydiModel?> FinansTarihiDegistirAsync(int id, FinansTarihiDegistirModel model, CancellationToken cancellationToken);
    Task<FinansIsKaydiModel?> FiyatlandirAsync(int id, FinansFiyatlandirmaModel model, CancellationToken cancellationToken);
    Task<FinansPanelModel> PanelAsync(DateTime baslangic, DateTime bitis, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansHareketModel>> HareketlerAsync(FinansListeFiltre filtre, string? tur, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansIsKaydiModel>> GenelAramaAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<FinansSayfaliSonuc<FinansBekleyenModel>> YaslandirmaAsync(DateTime referansTarihi, int minimumGun, FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinansSablonModel>> SablonlarAsync(CancellationToken cancellationToken);
    Task<FinansSablonModel> SablonKaydetAsync(int? id, FinansSablonKaydetModel model, CancellationToken cancellationToken);
    Task<FinansKaliciSilOnizlemeModel?> KaliciSilOnizlemeAsync(string varlikTuru, int id, CancellationToken cancellationToken);
    Task<bool> KaliciSilAsync(FinansKaliciSilModel model, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinansBelgeModel>> BelgelerAsync(string hedefTuru, int hedefId, CancellationToken cancellationToken);
    Task<FinansBelgeModel> BelgeYukleAsync(FinansBelgeYukleModel model, CancellationToken cancellationToken);
    Task<FinansBelgeIcerikModel?> BelgeIndirAsync(int id, CancellationToken cancellationToken);
}
