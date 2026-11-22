using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Araç yükleme oturumunun saf kuralları: sıradaki adım, sıra dışı yükleme, barkoddan yük anahtarı ve gerekçe.
/// Yükleme işareti planlama kaydıdır; sandık durumu ve sevk bu kurallarda yer almaz.
/// </summary>
public static class YuklemeOturumuKurali
{
    public const int GerekceEnAz = 5;
    public const int GerekceEnFazla = 500;

    /// <summary>Okutulan metnin kabul edilen en uzun hâli (okuyucu çöpünü reddetmek için; sandık barkodu 12 hanedir).</summary>
    public const int BarkodEnFazla = 64;

    /// <summary>Yük anahtarının ("s:{sandıkId}" / "m:{grup}:{birim}") saklanan en uzun hâli; doğrulama ve kolon aynı sınırı kullanır.</summary>
    public const int YukAnahtariEnFazla = 64;

    public const string OturumKapaliMesaji = "Yükleme oturumu kapalı; işaretleme ve geri alma yapılamaz.";
    public const string AcikOturumVarMesaji = "Bu planda süren bir yükleme oturumu var.";
    public const string YerlesimYokMesaji = "Planın son revizyonunda yerleşim yok; önce hesaplayın.";
    public const string GecersizBarkodMesaji = "Okutulan barkod sandık barkodu değil (12 haneli olmalı).";
    public const string PlandaYokMesaji = "Bu yük bu yükleme planının yerleşiminde yok.";
    public const string BarkodUyusmuyorMesaji = "Okutulan barkod işaretlenen yükün barkodu değil; barkodu yeniden okutun.";
    public const string GerekceMesaji = "Gerekçe 5–500 karakter olmalıdır.";
    public const string EksikYukGerekceMesaji = "Yüklenmemiş yük varken oturumu tamamlamak için gerekçe yazılmalıdır (5–500 karakter).";

    public static bool GerekceGecerli(string? gerekce) =>
        gerekce?.Trim().Length is >= GerekceEnAz and <= GerekceEnFazla;

    /// <summary>Sıradaki adım: yüklenmemiş yükü olan ilk adım; bütün yükler yüklendiyse null.</summary>
    public static int? SiradakiAdimNo(IReadOnlyList<YuklemeAdimi> adimlar, IReadOnlySet<string> yuklenenler) =>
        adimlar.FirstOrDefault(a => a.Yukler.Any(y => !yuklenenler.Contains(y.Konum.YukAnahtari)))?.AdimNo;

    /// <summary>
    /// Yük sıra dışı mı: adımı sıradaki adımdan sonra. Aynı adımdaki (istif grubu) yükler herhangi bir sırayla
    /// işaretlenebilir; grup dışarıda hazırlanır ve birlikte yüklenir.
    /// </summary>
    public static bool SiraDisiMi(IReadOnlyList<YuklemeAdimi> adimlar, IReadOnlySet<string> yuklenenler, string yukAnahtari)
    {
        var siradaki = SiradakiAdimNo(adimlar, yuklenenler);
        var adim = adimlar.FirstOrDefault(a => a.Yukler.Any(y => y.Konum.YukAnahtari == yukAnahtari));
        return siradaki != null && adim != null && adim.AdimNo > siradaki;
    }

    /// <summary>Okutulan sandık barkodunun plan yük anahtarı; sandık barkodu değilse null.</summary>
    public static string? BarkoddanYukAnahtari(string? barkod) =>
        BarkodEtiketKurali.SandikBarkoduCoz(barkod) is { } sandikId
            ? YuklemePlaniKurali.YukAnahtari(PlanYukKaynagi.Sandik, sandikId, null, 1)
            : null;

    /// <summary>
    /// "Barkodla okutuldu" kaydı yalnız okutulan barkod o yükü gösteriyorsa yazılır: kaynak istemcinin beyanına
    /// bırakılmaz. Elle eklenen yükün barkodu yoktur; barkod kaynağıyla işaretlenemez.
    /// </summary>
    public static bool BarkodYukleEslesir(string? barkod, string yukAnahtari) =>
        BarkoddanYukAnahtari(barkod) is { } anahtar && string.Equals(anahtar, yukAnahtari, StringComparison.Ordinal);
}
