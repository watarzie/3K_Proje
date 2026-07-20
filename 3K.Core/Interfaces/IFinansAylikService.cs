using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IFinansAylikService
    {
        Task<IReadOnlyList<FinansAylikIsDto>> ListeleAsync(int yil, int ay, CancellationToken cancellationToken = default);
    }
}