using System.Globalization;

namespace _3K.Core.Models;

/// <summary>
/// Çekideki koli (sandık) numarasının tek biçimi: çeki yüklemesi ve revizyonu sandığı bu biçimdeki numarayla açar,
/// Ambalaj da sandığın çekiden gelip gelmediğini aynı biçimle karşılaştırır. Boşluklar ve uzun tireler temizlenir,
/// tam sayı değer ("05", "1.0") sade sayıya ("5", "1") çevrilir.
/// </summary>
public static class KoliNoKurali
{
    /// <summary>Koli numarasını çeki yüklemesindeki biçime getirir; boş değer boş metindir.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim()
            .Replace('\u00A0', ' ')
            .Replace(" ", string.Empty)
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        if (!text.Contains(',') && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var numericValue)
            && numericValue == decimal.Truncate(numericValue))
        {
            // Elle girilen uzun sayısal numara (ör. 10+ hane) int'e sığmaz; taşma yerine aynı sade sayı yazılır.
            return numericValue is >= int.MinValue and <= int.MaxValue
                ? ((int)numericValue).ToString(CultureInfo.InvariantCulture)
                : numericValue.ToString("0", CultureInfo.InvariantCulture);
        }

        return text;
    }
}
