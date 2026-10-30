using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Finans raporlarında iş satırının ölçü ve talep eden alanları için tek kaynak. İşler raporu (kayıt başına) ve aylık
/// rapor (proje satırı) aynı kuralla yazar: tek seferlik sandıklar (İlave, Saha, Yedek) Özel İş gibi ölçüsüyle görünür;
/// ana ambalaj ve iç sandık kalemleri proje/şablon bazında toplu olduğu için ölçüsüzdür.
/// </summary>
public static class FinansIsRaporAlanlari
{
    /// <summary>Ölçüsü rapora yazılan iş türleri; aylık raporda bu türlerin proje satırları ölçüye göre ayrılır.</summary>
    public static bool OlculuIsTuru(FinansIsTuru tur) => tur is
        FinansIsTuru.IlaveSandik or FinansIsTuru.SahaSandigi or FinansIsTuru.YedekSandik or FinansIsTuru.OzelIs;

    /// <summary>"Kişi / Bölüm"; ikisi de boşsa boş.</summary>
    public static string? TalepEden(string? kisi, string? bolum)
    {
        var parcalar = new[] { kisi, bolum }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        return parcalar.Length == 0 ? null : string.Join(" / ", parcalar);
    }

    /// <summary>Birden çok kaydı taşıyan satırın talep edenleri: farklı olanlar ilk görülme sırasıyla, virgülle.</summary>
    public static string? TalepEdenler(IEnumerable<(string? Kisi, string? Bolum)> kayitlar)
    {
        var metinler = kayitlar
            .Select(x => TalepEden(x.Kisi, x.Bolum))
            .OfType<string>()
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return metinler.Length == 0 ? null : string.Join(", ", metinler);
    }
}
