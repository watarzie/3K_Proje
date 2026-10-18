namespace _3K.Core.Models;

/// <summary>
/// Sandıktaki ürün satırının kalıcı "Ürün bilgisi" alanı (seri no, son kullanma tarihi, lot). Alan işlem
/// açıklamasından (<c>CekiSatiri.UcKAciklama</c>) bağımsızdır: Grid/3K karşılama, geri gönderme, geri alma,
/// kapatma ve sevk akışları bu alana yazmaz; yalnız kullanıcı düzenler ve sandık taşıması korur.
/// Grid ve 3K ekranları aynı alanı düzenler; ekrandaki eski değer kayıtlı değerle eşleşmeyen kayıt reddedilir.
/// </summary>
public static class SandikIcerikUrunBilgisiKurali
{
    public const int EnFazlaUzunluk = 1000;
    public const string UzunlukMesaji = "Ürün bilgisi en fazla 1000 karakter olabilir.";
    public const string CakismaMesaji =
        "Ürün bilgisi siz düzenlerken başka bir kullanıcı tarafından değiştirildi. Listeyi yenileyip güncel bilgiyi görerek tekrar deneyin.";
    private const string Ayirac = " | ";

    /// <summary>Kenar boşlukları atılır; boş metin bilgi yok (null) sayılır.</summary>
    public static string? Normalize(string? deger) =>
        string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();

    public static bool Gecerli(string? deger) => (Normalize(deger)?.Length ?? 0) <= EnFazlaUzunluk;

    /// <summary>
    /// Kullanıcının düzenlemeye başladığı değer hâlâ kayıtlı değer mi? Grid ve 3K aynı alanı düzenler; başkasının
    /// girdiği bilgiyi ekranda görmeden üzerine yazmak, o bilgiyi sessizce kaybettirir.
    /// </summary>
    public static bool EkrandakiDegerGuncel(string? kayitli, string? ekrandaki) =>
        string.Equals(Normalize(kayitli), Normalize(ekrandaki), StringComparison.Ordinal);

    /// <summary>
    /// Taşımada kaynak satır, hedef sandıkta aynı ürünün mevcut satırıyla birleşirken iki bilgi de korunur:
    /// biri boşsa diğeri, aynıysa tek değer, farklıysa "hedef | kaynak". Kolon sınırı aşılırsa birleşik metin
    /// sınırda kesilir; taşımanın kendisi engellenmez.
    /// </summary>
    public static string? Birlestir(string? hedef, string? kaynak)
    {
        var h = Normalize(hedef);
        var k = Normalize(kaynak);
        if (k == null || string.Equals(h, k, StringComparison.Ordinal))
            return h;
        if (h == null)
            return k;
        var birlesik = h + Ayirac + k;
        return birlesik.Length <= EnFazlaUzunluk ? birlesik : birlesik[..EnFazlaUzunluk];
    }
}
