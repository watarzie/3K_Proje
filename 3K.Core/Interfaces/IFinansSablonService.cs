using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>İş şablonları.</summary>
public interface IFinansSablonService
{
    Task<IReadOnlyList<FinansSablonModel>> SablonlarAsync(CancellationToken cancellationToken);
    Task<FinansSablonModel> SablonKaydetAsync(int? id, FinansSablonKaydetModel model, CancellationToken cancellationToken);
}
