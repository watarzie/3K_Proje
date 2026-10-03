using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Üretim modülünün doğrudan kullandığı daraltılmış finans aktarım kapısı.</summary>
public interface IFinansUretimAktarimService
{
    Task<FinansSenkronizasyonSonucModel> UretimKayitlariniAktarAsync(IReadOnlyList<FinansUretimAktarimModel> modeller, CancellationToken cancellationToken);
}
