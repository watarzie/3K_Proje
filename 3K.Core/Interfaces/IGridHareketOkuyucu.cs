using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Grid Sahası görünümünün hareket okumaları (salt okunur). Canlı ve arşiv hareket tablolarını birlikte okur; Grid'e
/// geliş tarihi ayrıca saklanmadığı için mal kabul tarihi ve personeli buradan gelir.
/// </summary>
public interface IGridHareketOkuyucu
{
    /// <summary>Süzgece uyan hareketler (yeniden eskiye, sayfalı) ve süzgecin personel özeti.</summary>
    Task<GridHareketSayfasi> GridHareketleriAsync(GridHareketFiltresi filtre, CancellationToken cancellationToken = default);

    /// <summary>Verilen çeki satırlarının en son mal kabul kaydı; kaydı olmayan satır sözlükte yoktur.</summary>
    Task<IReadOnlyDictionary<int, GridSonMalKabul>> SonMalKabulleriAsync(
        IReadOnlyCollection<int> cekiSatiriIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Her çeki satırının en son mal kabul tarihi (canlı ∪ arşiv, referans kimliğine göre gruplu) olarak birleştirilebilir
    /// sorgu; çalıştırılmaz. Aynı bağlamdaki çeki satırı sorgusuna sol birleştirmeyle eklenir (sıralama ve tarih süzgeci
    /// SQL'de yapılır). Tanım <see cref="SonMalKabulleriAsync"/> ile aynıdır.
    /// </summary>
    IQueryable<GridSonMalKabulTarihi> SonMalKabulTarihleri();

    /// <summary>Aralıktaki mal kabul kaydı sayısı (bitiş hariç).</summary>
    Task<int> MalKabulSayisiAsync(DateTime baslangicDahil, DateTime bitisHaric, CancellationToken cancellationToken = default);
}
