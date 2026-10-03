using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>
/// Finans para birimi kuralı: ISO-4217 üç harfli kod, büyük harfe normalize edilir. Kabul edilen para birimleri
/// kullanıcının yönettiği FinansParaBirimleri tablosundadır (F1); kodda sabit liste yoktur.
/// </summary>
public static class FinansParaBirimiKurali
{
    public static string Normalize(string paraBirimi)
    {
        var normalized = paraBirimi.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
            throw IsKuraliException.Cakisma("Para birimi ISO-4217 biçiminde üç karakter olmalıdır.");
        return normalized;
    }
}
