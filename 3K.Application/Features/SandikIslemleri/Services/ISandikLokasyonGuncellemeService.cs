using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Commands;

namespace _3K.Application.Features.SandikIslemleri.Services
{
    public interface ISandikLokasyonGuncellemeService
    {
        Task<Result<SandikLokasyonOnayliUygulaCommand>> PlanlaAsync(
            IReadOnlyCollection<int>? sandikIds,
            int depoLokasyonId,
            CancellationToken cancellationToken);

        Task<Result<bool>> UygulaAsync(
            SandikLokasyonOnayliUygulaCommand plan,
            CancellationToken cancellationToken);
    }
}
