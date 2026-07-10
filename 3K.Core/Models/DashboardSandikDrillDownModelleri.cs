namespace _3K.Core.Models
{
    public sealed class DashboardSandikDrillDownFiltresi
    {
        public int ProjeId { get; init; }
        public int DurumId { get; init; }
        public string? SearchTerm { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public sealed class DashboardSandikDrillDownSatiri
    {
        public int SandikId { get; init; }
        public string SandikNo { get; init; } = string.Empty;
        public string? SandikAdi { get; init; }
        public int DurumId { get; init; }
        public string DurumMetni { get; init; } = string.Empty;
        public int DepoLokasyonId { get; init; }
        public string DepoLokasyonMetni { get; init; } = string.Empty;
    }

    public sealed class DashboardSandikDrillDownSonucu
    {
        public bool ProjeBulundu { get; init; }
        public IReadOnlyList<DashboardSandikDrillDownSatiri> Items { get; init; } = [];
        public int TotalCount { get; init; }
    }
}
