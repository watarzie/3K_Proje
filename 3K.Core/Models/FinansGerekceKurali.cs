using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>
/// İptal, düzeltme, fiyatlandırma ve kalıcı silme gibi izli finans işlemlerinin gerekçe kuralı.
/// Doğrulayıcılar (400) ve servis koruması aynı sınırı kullanır.
/// </summary>
public static class FinansGerekceKurali
{
    public const int EnKisa = 5;
    public const int EnUzun = 1000;
    public const string Mesaj = "İşlem gerekçesi 5–1000 karakter olmalıdır.";

    public static bool Gecerli(string? gerekce) =>
        !string.IsNullOrWhiteSpace(gerekce) && gerekce.Trim().Length >= EnKisa && gerekce.Length <= EnUzun;

    /// <summary>Servis koruması: gerekçe kurala uymuyorsa iş kuralı hatası fırlatır.</summary>
    public static void Dogrula(string? gerekce)
    {
        if (!Gecerli(gerekce))
            throw IsKuraliException.Cakisma(Mesaj);
    }
}
