using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans kayıtlarının etki önizlemesiyle kalıcı silinmesi.</summary>
public interface IFinansKaliciSilmeService
{
    Task<FinansKaliciSilOnizlemeModel?> KaliciSilOnizlemeAsync(string varlikTuru, int id, CancellationToken cancellationToken);
    Task<bool> KaliciSilAsync(FinansKaliciSilModel model, CancellationToken cancellationToken);
}
