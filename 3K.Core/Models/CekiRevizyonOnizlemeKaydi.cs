namespace _3K.Core.Models
{
    /// <summary>
    /// Onay ekranına büyük dosya içeriğini taşımadan yalnızca doğrulanabilir
    /// ön izleme snapshot'ını getirir.
    /// </summary>
    public sealed class CekiRevizyonOnizlemeKaydi
    {
        public string OnizlemeJson { get; init; } = string.Empty;
        public string OnizlemeHash { get; init; } = string.Empty;
        public int OnizlemeSurumu { get; init; }
    }
}
