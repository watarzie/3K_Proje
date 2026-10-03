using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans tanım parametreleri: para birimi tablosu (F1) ve vade parametreleri (D3).</summary>
public interface IFinansParametreService
{
    Task<IReadOnlyList<FinansParaBirimiModel>> ParaBirimleriAsync(bool sadeceAktif, CancellationToken cancellationToken);
    Task<FinansParaBirimiModel> ParaBirimiOlusturAsync(FinansParaBirimiKaydetModel model, CancellationToken cancellationToken);
    Task<FinansParaBirimiModel?> ParaBirimiGuncelleAsync(int id, FinansParaBirimiKaydetModel model, CancellationToken cancellationToken);
    Task<FinansParametrelerModel> ParametrelerAsync(CancellationToken cancellationToken);
    Task<FinansParametrelerModel> ParametreleriKaydetAsync(FinansParametrelerModel model, CancellationToken cancellationToken);
}
