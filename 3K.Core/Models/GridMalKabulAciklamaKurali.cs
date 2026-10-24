using System.Globalization;
using System.Text.RegularExpressions;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Grid mal kabul hareketinin açıklamasındaki miktar ekleri: Tam/Eksik Geldi ve Grid manuel ürün hareketinin
/// açıklamasına Grid'e gelen adet <c> | Gelen: {miktar}</c> biçiminde eklenir (23.10.2026'dan sonraki kayıtlar); Trafo
/// Sevk'te önce trafoya giden adet gelir: <c> | Trafo: {trafo} | Gelen: {gelen}</c> (24.10.2026). Grid Kapandı miktar
/// değiştirmediği için ek yazmaz. Mal kabul kayıtları aynı ekleri ayrıştırır; ek yoksa (eski kayıt) miktar bilinmez
/// (null). Miktar noktalı yazılır; virgüllü yazım da okunur.
/// </summary>
public static partial class GridMalKabulAciklamaKurali
{
    private const string Etiket = "Gelen:";
    private const string TrafoEtiketi = "Trafo:";

    /// <summary>Hareket açıklamasının sonuna eklenen gelen adet eki.</summary>
    public static string GelenEki(decimal gridGelenAdet) =>
        $" | {Etiket} {Sayi(gridGelenAdet)}";

    /// <summary>
    /// Durum güncellemesinin mal kabul eki: Tam/Eksik Geldi'de gelen adet, Trafo Sevk'te trafo ve gelen adet; diğer
    /// durumlarda (Grid Kapandı dahil) boş.
    /// </summary>
    public static string MalKabulEki(int gridDurumuId, decimal gridGelenAdet, decimal trafoSevkAdet) => gridDurumuId switch
    {
        (int)GridDurum.TamGeldi or (int)GridDurum.EksikGeldi => GelenEki(gridGelenAdet),
        (int)GridDurum.TrafoSevk => $" | {TrafoEtiketi} {Sayi(trafoSevkAdet)}" + GelenEki(gridGelenAdet),
        _ => string.Empty
    };

    /// <summary>
    /// Açıklamanın sonundaki gelen adet ekini okur; ek yoksa veya sayı değilse null. Ek her zaman sona yazıldığı için
    /// yalnız sondaki eşleşme sayılır: kullanıcının notunda geçen "| Gelen: 999" gibi bir metin miktar sanılmaz.
    /// </summary>
    public static decimal? GelenMiktariniAyristir(string? aciklama) => Ayristir(aciklama, GelenEkiDeseni());

    /// <summary>Trafo Sevk ekindeki trafo adedini okur (yalnız sondaki "| Trafo: x | Gelen: y" eki); yoksa null.</summary>
    public static decimal? TrafoMiktariniAyristir(string? aciklama) => Ayristir(aciklama, TrafoEkiDeseni());

    private static decimal? Ayristir(string? aciklama, Regex desen)
    {
        if (string.IsNullOrWhiteSpace(aciklama)) return null;
        var eslesme = desen.Match(aciklama);
        if (!eslesme.Success) return null;
        var metin = eslesme.Groups["miktar"].Value.Replace(',', '.');
        return decimal.TryParse(metin, NumberStyles.Number, CultureInfo.InvariantCulture, out var miktar) ? miktar : null;
    }

    private static string Sayi(decimal miktar) => miktar.ToString("0.####", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\|\s*Gelen:\s*(?<miktar>\d+(?:[.,]\d+)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex GelenEkiDeseni();

    [GeneratedRegex(@"\|\s*Trafo:\s*(?<miktar>\d+(?:[.,]\d+)?)\s*\|\s*Gelen:\s*\d+(?:[.,]\d+)?\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex TrafoEkiDeseni();
}
