using System.Text.RegularExpressions;

namespace _3K.Core.Models;

/// <summary>
/// Araç plakası kuralı: "34 ABC 123", "34-abc-123" ve "34ABC123" aynı tırdır. Saklanan ve gruplanan değer boşluk ve
/// tire içermeyen büyük harfli biçimdir; ekranda Türk plakası biçiminde gösterilir.
/// </summary>
public static partial class AracPlakaKurali
{
    public const int EnFazlaUzunluk = 30;

    public static string? Normalize(string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka)) return null;
        var temiz = new string(plaka.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
        if (temiz.Length == 0) return null;
        return temiz.Length > EnFazlaUzunluk ? temiz[..EnFazlaUzunluk] : temiz;
    }

    /// <summary>Gösterim: "34ABC123" → "34 ABC 123"; Türk plakası biçiminde değilse normalize hâli.</summary>
    public static string? Bicimle(string? plaka)
    {
        var normal = Normalize(plaka);
        if (normal is null) return null;
        var eslesme = TurkPlakasi().Match(normal);
        return eslesme.Success
            ? $"{eslesme.Groups[1].Value} {eslesme.Groups[2].Value} {eslesme.Groups[3].Value}"
            : normal;
    }

    [GeneratedRegex("^([0-9]{2})([A-Z]{1,3})([0-9]{2,5})$")]
    private static partial Regex TurkPlakasi();
}

/// <summary>Tır seferi ve toplu sevk sınırları.</summary>
public static class SevkSeferiKurali
{
    public const int EnFazlaProje = 25;
    public const int EnFazlaSandik = 250;
    public const int SoforEnFazla = 100;
    public const int AciklamaEnFazla = 500;

    /// <summary>Global benzersiz sefer numarası: oluşturma yılı ve o yılın sırası (ör. 2026-000123).</summary>
    public static string SeferNo(int yil, int sira) => $"{yil}-{sira:D6}";

    /// <summary>Yılın son sefer numarasından sonraki numara; o yıl için ilk seferse 1.</summary>
    public static string SonrakiSeferNo(int yil, string? yilinSonNumarasi)
    {
        var onek = $"{yil}-";
        var sira = yilinSonNumarasi != null && yilinSonNumarasi.StartsWith(onek, StringComparison.Ordinal) &&
                   int.TryParse(yilinSonNumarasi.AsSpan(onek.Length), out var son)
            ? son + 1
            : 1;
        return SeferNo(yil, sira);
    }
}
