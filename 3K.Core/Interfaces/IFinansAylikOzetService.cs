using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Aylık iş özeti: ekrandaki sayfalı liste ve raporlar için dönemin tam listesi.</summary>
public interface IFinansAylikOzetService
{
    Task<IReadOnlyList<FinansAylikIsModel>> AylikOzetAsync(int yil, int ay, CancellationToken cancellationToken);
    Task<FinansAylikSayfaliSonuc> AylikOzetSayfaliAsync(int yil, int ay, FinansListeFiltre filtre, CancellationToken cancellationToken);

    /// <summary>
    /// Projenin sandık kırılımı: filtreye uyan bütün üretim kayıtları (sayfalama yok) sandık başına toplanır.
    /// </summary>
    Task<FinansProjeSandikKirilimiModel> ProjeSandikKirilimiAsync(FinansProjeSandikKirilimiIstegi istek, CancellationToken cancellationToken);
}
