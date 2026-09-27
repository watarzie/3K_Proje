using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans kayıtlarına bağlı PDF belgeleri.</summary>
public interface IFinansBelgeService
{
    Task<IReadOnlyList<FinansBelgeModel>> BelgelerAsync(string hedefTuru, int hedefId, CancellationToken cancellationToken);
    Task<FinansBelgeModel> BelgeYukleAsync(FinansBelgeYukleModel model, CancellationToken cancellationToken);
    Task<DosyaIcerigi?> BelgeIndirAsync(int id, CancellationToken cancellationToken);
}
