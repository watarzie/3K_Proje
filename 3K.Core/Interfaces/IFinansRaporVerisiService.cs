using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Rapor dosyalarının ortak veri kümesi: filtreye uyan işler, giderler ve para birimi toplamları.</summary>
public interface IFinansRaporVerisiService
{
    Task<FinansRaporModel> RaporVerisiAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
}
