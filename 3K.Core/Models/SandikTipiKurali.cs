using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Sandık tipi zorunluluğu (G2): sandık eklenirken ve güncellenirken tip seçilmelidir; tip Ambalaj
/// senkronunda sandık cinsine <see cref="AmbalajUretimPolitikasi.KaynakCinsi"/> ile eşlenir.
/// </summary>
public static class SandikTipiKurali
{
    public const string Mesaj = "Sandık tipi zorunludur (Ahşap Kapalı, Katlanır Sandık veya Kontrplak Sandık).";

    public static bool Gecerli(int? tipId) => tipId.HasValue && Enum.IsDefined(typeof(SandikTipi), tipId.Value);
}
