using System.Globalization;
using System.Text.RegularExpressions;

namespace _3K.Core.Models;

/// <summary>
/// Grid mal kabul hareketinin açıklamasındaki "Gelen" eki: Tam/Eksik Geldi ve Grid manuel ürün hareketinin
/// açıklamasına Grid'e gelen adet <c> | Gelen: {miktar}</c> biçiminde eklenir (23.10.2026'dan sonraki kayıtlar), mal
/// kabul kayıtları aynı eki ayrıştırır. Ek yoksa (eski kayıt) miktar bilinmez (null). Miktar noktalı yazılır; virgüllü
/// yazım da okunur.
/// </summary>
public static partial class GridMalKabulAciklamaKurali
{
    private const string Etiket = "Gelen:";

    /// <summary>Hareket açıklamasının sonuna eklenen gelen adet eki.</summary>
    public static string GelenEki(decimal gridGelenAdet) =>
        $" | {Etiket} {gridGelenAdet.ToString("0.####", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Açıklamanın sonundaki gelen adet ekini okur; ek yoksa veya sayı değilse null. Ek her zaman sona yazıldığı için
    /// yalnız sondaki eşleşme sayılır: kullanıcının notunda geçen "| Gelen: 999" gibi bir metin miktar sanılmaz.
    /// </summary>
    public static decimal? GelenMiktariniAyristir(string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(aciklama)) return null;
        var eslesme = GelenEkiDeseni().Match(aciklama);
        if (!eslesme.Success) return null;
        var metin = eslesme.Groups["miktar"].Value.Replace(',', '.');
        return decimal.TryParse(metin, NumberStyles.Number, CultureInfo.InvariantCulture, out var miktar) ? miktar : null;
    }

    [GeneratedRegex(@"\|\s*Gelen:\s*(?<miktar>\d+(?:[.,]\d+)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex GelenEkiDeseni();
}
