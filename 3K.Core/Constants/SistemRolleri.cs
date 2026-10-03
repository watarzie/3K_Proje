namespace _3K.Core.Constants;

/// <summary>
/// Kurulumla gelen sistem rolleri. Rol kimliğe göre tanınır; ad değişse de kural değişmez.
/// Admin menü/işlem kontrollerini atlamaz; yalnız onay yönetiminde ve rol yönetiminde korunur.
/// </summary>
public static class SistemRolleri
{
    public const int AdminId = 1;
    public const string AdminAdi = "Admin";

    public static bool AdminMi(int rolId) => rolId == AdminId;

    /// <summary>"Admin" adı yalnız sistem rolüne aittir; başka role verilemez.</summary>
    public static bool AyrilmisAdMi(string? ad) =>
        string.Equals(ad?.Trim(), AdminAdi, StringComparison.OrdinalIgnoreCase);
}
