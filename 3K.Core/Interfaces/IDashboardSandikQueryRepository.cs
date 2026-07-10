using _3K.Core.Models;

namespace _3K.Core.Interfaces
{
    public interface IDashboardSandikQueryRepository
    {
        Task<DashboardSandikDrillDownSonucu> GetProjeSandiklariAsync(
            DashboardSandikDrillDownFiltresi filtre,
            CancellationToken cancellationToken = default);
    }
}
