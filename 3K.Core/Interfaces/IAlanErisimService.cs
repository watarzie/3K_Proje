using _3K.Core.Models;

namespace _3K.Core.Interfaces;

public interface IAlanErisimService
{
    Task<AlanErisimYetkileri> GetAsync(CancellationToken cancellationToken = default);
}
