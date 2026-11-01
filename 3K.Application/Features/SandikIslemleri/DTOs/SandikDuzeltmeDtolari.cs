namespace _3K.Application.Features.SandikIslemleri.DTOs
{
    /// <summary>Sandığın sevk sonrası düzeltme durumu: açık düzeltme, şimdiye kadarki değişiklikler ve geçmiş.</summary>
    public sealed class SandikDuzeltmeDurumuDto
    {
        public int SandikId { get; init; }
        public bool AcikMi { get; init; }
        public string? Gerekce { get; init; }
        public string? AcanKullanici { get; init; }
        public DateTime? AcilisTarihi { get; init; }
        /// <summary>Açılıştan bu yana yapılan değişiklikler (tamamlamadan önceki önizleme).</summary>
        public IReadOnlyList<string> Degisiklikler { get; init; } = [];
        public IReadOnlyList<SandikDuzeltmeGecmisDto> Gecmis { get; init; } = [];
    }

    /// <summary>Tamamlanmış bir sevk sonrası düzeltme.</summary>
    public sealed class SandikDuzeltmeGecmisDto
    {
        public string Gerekce { get; init; } = string.Empty;
        public string? AcanKullanici { get; init; }
        public DateTime AcilisTarihi { get; init; }
        public string? TamamlayanKullanici { get; init; }
        public DateTime? TamamlanmaTarihi { get; init; }
        public IReadOnlyList<string> Degisiklikler { get; init; } = [];
    }
}
