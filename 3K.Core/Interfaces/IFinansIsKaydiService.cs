using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>İş kayıtları: seçim ve detay, özel iş oluşturma, iptal ve geri alma, finans tarihi ve fiyatlandırma.</summary>
public interface IFinansIsKaydiService
{
    Task<FinansSayfaliSonuc<FinansProjeSecenekModel>> ProjeSecenekleriAsync(string? arama, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinansIsKaydiModel>> IsKayitlariSecimAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
    Task<FinansIsKaydiModel?> IsKaydiGetirAsync(int id, CancellationToken cancellationToken);
    Task<FinansIsKaydiModel> IsKaydiOlusturAsync(FinansIsKaydiKaydetModel model, CancellationToken cancellationToken);
    Task<bool> IsKaydiIptalAsync(int id, string aciklama, CancellationToken cancellationToken);
    Task<bool> IsKaydiGeriAlAsync(int id, CancellationToken cancellationToken);
    Task<bool> OzelIsAylikDegerGuncelleAsync(int id, FinansAylikDegerModel model, CancellationToken cancellationToken);
    Task<FinansIsKaydiModel?> FinansTarihiDegistirAsync(int id, FinansTarihiDegistirModel model, CancellationToken cancellationToken);
    Task<FinansIsKaydiModel?> FiyatlandirAsync(int id, FinansFiyatlandirmaModel model, CancellationToken cancellationToken);
}
