using System.Globalization;
using System.Text;
using _3K.Core.Helpers;

namespace _3K.Core.Models;

/// <summary>
/// Grid etiketlerinin alan kuralları (Godex şablonları: ürün 60×40 mm, sandık 100×75 mm). Yazıcının iç fontları Türkçe
/// harf basamadığı için etiket metni ASCII'ye çevrilir ("ÇİFT" → "CIFT"); barkodlar sıfırla doldurulur (sandık 12, ürün
/// 16 hane). PDF ve EZPL çıktısı aynı değerleri basar.
/// </summary>
public static class BarkodEtiketKurali
{
    public const int SandikBarkodHanesi = 12;
    public const int UrunBarkodHanesi = 16;
    /// <summary>Tek istekte seçilebilecek en fazla sandık/ürün.</summary>
    public const int EnFazlaSecim = 200;
    /// <summary>Bir sandık/ürün için en fazla etiket adedi.</summary>
    public const int EnFazlaAdet = 100;
    /// <summary>Tek dosyadaki en fazla etiket (adetler toplamı).</summary>
    public const int EnFazlaToplamEtiket = 1000;
    public const int RemarksEnFazlaUzunluk = 60;
    public const int AramaEnFazlaUzunluk = 100;
    /// <summary>REMARKS boşsa basılan değer (Grid etiketlerindeki gibi).</summary>
    public const string BosRemarks = "-";

    /// <summary>Sandık barkodu: sandık kimliği 12 haneye sıfırla doldurulur (kalıcı ve benzersiz; okununca sandık bulunur).</summary>
    public static string SandikBarkodu(int sandikId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sandikId);
        return sandikId.ToString(new string('0', SandikBarkodHanesi), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Ürün barkodu: kırpılıp büyük harfe çevrilir ve 16 haneye soldan sıfırla doldurulur (FCT00967433 →
    /// 00000FCT00967433). Boş, 16 karakterden uzun ya da Code 128 / EZPL ile basılamayan (Türkçe harf, ^, ~) barkod için
    /// <c>null</c>.
    /// </summary>
    public static string? UrunBarkodu(string? barkodNo)
    {
        // Denetim büyük harfe çevirmeden önce: sabit kültürde 'ı' → 'I' olur ve veritabanındakinden başka barkod basılırdı.
        var barkod = barkodNo?.Trim();
        if (string.IsNullOrEmpty(barkod) || barkod.Length > UrunBarkodHanesi || !Code128.Kodlanabilir(barkod)
            || barkod.Contains('^') || barkod.Contains('~'))
            return null;
        return barkod.ToUpperInvariant().PadLeft(UrunBarkodHanesi, '0');
    }

    /// <summary>
    /// Çeki açıklaması "Türkçe\n\İngilizce" biçimindedir: ilk satır TANIM, sonraki satırlar (baştaki ters bölü atılarak)
    /// DESCRIPTION olur. Tek satırlı açıklamada DESCRIPTION boştur.
    /// </summary>
    public static (string Tanim, string Aciklama) AciklamaAyir(string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(aciklama))
            return ("", "");
        var satirSonu = aciklama.IndexOf('\n');
        if (satirSonu < 0)
            return (aciklama.Trim(), "");
        var ingilizce = aciklama[(satirSonu + 1)..].Trim().TrimStart('\\').Trim();
        return (aciklama[..satirSonu].Trim(), ingilizce);
    }

    /// <summary>
    /// Etikete basılacak metin: Türkçe harfler ve aksanlar ASCII karşılığına çevrilir, satır sonları boşluk olur, EZPL'de
    /// değişken başlatan ^ ve ~ atılır, art arda boşluklar teke iner. Büyük/küçük harf korunur.
    /// </summary>
    public static string Metin(string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger))
            return "";
        var sb = new StringBuilder(deger.Length);
        foreach (var c in deger.Normalize(NormalizationForm.FormC))
        {
            var karsilik = c switch
            {
                'ı' => "i", 'İ' => "I", 'ğ' => "g", 'Ğ' => "G", 'ş' => "s", 'Ş' => "S",
                'ç' => "c", 'Ç' => "C", 'ö' => "o", 'Ö' => "O", 'ü' => "u", 'Ü' => "U",
                '^' or '~' => " ",
                _ when char.IsControl(c) || char.IsWhiteSpace(c) => " ",
                _ when c <= '~' => c.ToString(),
                // BMP dışı karakter (emoji) iki vekil yarımdır; tek "?" basılır, yarımlar ayrıca normalleştirilmez.
                _ when char.IsHighSurrogate(c) => "?",
                _ when char.IsLowSurrogate(c) => "",
                _ => AksansizKarsilik(c)
            };
            sb.Append(karsilik);
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>REMARKS: <see cref="Metin"/> ile çevrilir, boşsa "-".</summary>
    public static string Remarks(string? remarks)
    {
        var metin = Metin(remarks);
        return metin.Length == 0 ? BosRemarks : metin;
    }

    /// <summary>FB NO: projenin FB numarası; boşsa proje no (saha projesinde FB no kaynak projeninkidir).</summary>
    public static string SeriNo(string? fbNo, string projeNo) =>
        string.IsNullOrWhiteSpace(fbNo) ? projeNo.Trim() : fbNo.Trim();

    /// <summary>Sandık etiketindeki tarih AA/YY biçimindedir (Eylül 2026 → "09/26").</summary>
    public static string Tarih(int yil, int ay)
    {
        if (!TarihGecerli(yil, ay))
            throw new ArgumentOutOfRangeException(nameof(ay), "Etiket tarihi geçersiz.");
        return $"{ay:00}/{yil % 100:00}";
    }

    public static bool TarihGecerli(int yil, int ay) => yil is >= 2000 and <= 2099 && ay is >= 1 and <= 12;

    /// <summary>En az bir, en fazla <see cref="EnFazlaSecim"/> seçim; her adet 1–<see cref="EnFazlaAdet"/>; toplam en fazla <see cref="EnFazlaToplamEtiket"/>.</summary>
    public static bool AdetlerGecerli(IReadOnlyCollection<int>? adetler) =>
        adetler is { Count: > 0 and <= EnFazlaSecim }
        && adetler.All(a => a is >= 1 and <= EnFazlaAdet)
        && adetler.Sum() <= EnFazlaToplamEtiket;

    public static bool RemarksGecerli(string? remarks) => (remarks?.Trim().Length ?? 0) <= RemarksEnFazlaUzunluk;

    public static bool AramaGecerli(string? arama) => (arama?.Trim().Length ?? 0) <= AramaEnFazlaUzunluk;

    /// <summary>Aksanlı harfin temel harfi (é → e); karşılığı olmayan karakter "?" basılır.</summary>
    private static string AksansizKarsilik(char c)
    {
        var temel = c.ToString().Normalize(NormalizationForm.FormD)
            .Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark)
            .Where(x => x is >= ' ' and <= '~' and not '^')
            .ToArray();
        return temel.Length > 0 ? new string(temel) : "?";
    }
}
