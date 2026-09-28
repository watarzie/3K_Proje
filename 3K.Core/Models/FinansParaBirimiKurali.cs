using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>Finans para birimi kuralı: ISO-4217 üç harfli kod, büyük harfe normalize edilir.</summary>
public static class FinansParaBirimiKurali
{
    /// <summary>Gider ve fiyatlandırmada kabul edilen, özetlerde her zaman gösterilen para birimleri.</summary>
    public static readonly IReadOnlyList<string> Desteklenenler = ["TRY", "EUR", "USD"];

    public static bool DesteklenenMi(string paraBirimi) => Desteklenenler.Contains(paraBirimi);

    public static string Normalize(string paraBirimi)
    {
        var normalized = paraBirimi.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
            throw IsKuraliException.Cakisma("Para birimi ISO-4217 biçiminde üç karakter olmalıdır.");
        return normalized;
    }
}
