using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Gider kütüphanesi: gider kategorileri ve kalemleri.</summary>
public interface IFinansGiderTanimService
{
    Task<IReadOnlyList<FinansGiderKategoriModel>> GiderKategorileriAsync(bool sadeceAktif, CancellationToken cancellationToken);
    Task<FinansGiderKategoriModel> GiderKategoriOlusturAsync(FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken);
    Task<FinansGiderKategoriModel?> GiderKategoriGuncelleAsync(int id, FinansGiderKategoriKaydetModel model, CancellationToken cancellationToken);
    Task<IReadOnlyList<FinansGiderKalemiModel>> GiderKalemleriAsync(int? kategoriId, bool sadeceAktif, CancellationToken cancellationToken);
    Task<FinansGiderKalemiModel> GiderKalemiOlusturAsync(FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken);
    Task<FinansGiderKalemiModel?> GiderKalemiGuncelleAsync(int id, FinansGiderKalemiKaydetModel model, CancellationToken cancellationToken);
}
