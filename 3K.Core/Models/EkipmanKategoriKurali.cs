using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Ekipman kategorisinin davranışı tek yerde: yükleme yöntemi (Simülasyon isterleri 13), teknik veride tanımlanması
/// gereken açıklıklar (isterler 14–15) ve sandık taşımaya uygunluk (isterler 11). Tanımlanmamış yöntem varsayılmaz.
/// </summary>
public static class EkipmanKategoriKurali
{
    /// <summary>
    /// Kapalı konteyner, kapalı kasa ve frigo yalnız arkadan; Open Top arkadan veya üstten; tenteli, açık kasa, platform,
    /// lowbed ve Flat Rack yandan yüklenir. Hardtop'ta çatı kaldırarak yükleme isterlerde tanımlı olmadığı için yalnız
    /// arkadan kabul edilir. Sandık taşımaya uygun olmayan ve "Diğer" kategoride yöntem yoktur.
    /// </summary>
    public static YuklemeYontemi YuklemeYontemleri(EkipmanKategorisi kategori) => kategori switch
    {
        EkipmanKategorisi.KapaliKonteyner or EkipmanKategorisi.HardtopKonteyner or
            EkipmanKategorisi.KapaliKasa or EkipmanKategorisi.Frigo => YuklemeYontemi.Arkadan,
        EkipmanKategorisi.OpenTopKonteyner => YuklemeYontemi.Arkadan | YuklemeYontemi.Ustten,
        EkipmanKategorisi.FlatRack or EkipmanKategorisi.PlatformKonteyner or EkipmanKategorisi.AcikKasa or
            EkipmanKategorisi.Platform or EkipmanKategorisi.UzamaliPlatform or EkipmanKategorisi.Tenteli or
            EkipmanKategorisi.Lowbed or EkipmanKategorisi.AgirYukDorsesi or EkipmanKategorisi.ModulerAgirYuk => YuklemeYontemi.Yandan,
        _ => YuklemeYontemi.Yok
    };

    /// <summary>Sandık taşımaya uygun mu: konteyner şasisi, damper, hareketli taban, silobas ve tanker kapasitesi yetse de aday olmaz.</summary>
    public static bool SandikTasimayaUygunMu(EkipmanKategorisi kategori) => kategori is not (
        EkipmanKategorisi.KonteynerSasisi or EkipmanKategorisi.Damper or EkipmanKategorisi.HareketliTaban or
        EkipmanKategorisi.Silobas or EkipmanKategorisi.Tanker);

    /// <summary>
    /// Teknik veride bulunması gereken açıklıklar: kapalı gövdede arka kapı (kapıdan geçiş ayrı kontrol edilir), Open
    /// Top'ta arka kapı ve üst açıklık, tentelide iki yan açıklık.
    /// </summary>
    public static IReadOnlyList<string> GerekenAcikliklar(EkipmanKategorisi kategori) => kategori switch
    {
        EkipmanKategorisi.KapaliKonteyner or EkipmanKategorisi.HardtopKonteyner or
            EkipmanKategorisi.KapaliKasa or EkipmanKategorisi.Frigo => [Arka],
        EkipmanKategorisi.OpenTopKonteyner => [Arka, Ust],
        EkipmanKategorisi.Tenteli => [Sol, Sag],
        _ => []
    };

    /// <summary>Kapalı ve tenteli gövdede yüzeyin iç (kullanılabilir) yüksekliği gerekir; açık platformda sınır yasal yüksekliktir.</summary>
    public static bool IcYukseklikGerekliMi(EkipmanKategorisi kategori) => kategori is
        EkipmanKategorisi.KapaliKonteyner or EkipmanKategorisi.HardtopKonteyner or EkipmanKategorisi.KapaliKasa or
        EkipmanKategorisi.Frigo or EkipmanKategorisi.Tenteli;

    /// <summary>Kendi dingili olan karayolu ekipmanı mı; konteynerin dingili yoktur, yükü taşıyan şasidir.</summary>
    public static bool DingilliMi(EkipmanKategorisi kategori) => kategori is not (
        EkipmanKategorisi.KapaliKonteyner or EkipmanKategorisi.HardtopKonteyner or EkipmanKategorisi.OpenTopKonteyner or
        EkipmanKategorisi.FlatRack or EkipmanKategorisi.PlatformKonteyner);

    /// <summary>Kategorinin rapor ve Excel'deki adı (enum adı yazılmaz).</summary>
    public static string Ad(EkipmanKategorisi kategori) => kategori switch
    {
        EkipmanKategorisi.KapaliKonteyner => "Kapalı konteyner",
        EkipmanKategorisi.HardtopKonteyner => "Hardtop konteyner",
        EkipmanKategorisi.OpenTopKonteyner => "Open Top konteyner",
        EkipmanKategorisi.FlatRack => "Flat Rack",
        EkipmanKategorisi.PlatformKonteyner => "Platform konteyner",
        EkipmanKategorisi.AcikKasa => "Açık kasa",
        EkipmanKategorisi.Platform => "Platform",
        EkipmanKategorisi.UzamaliPlatform => "Uzamalı platform",
        EkipmanKategorisi.Tenteli => "Tenteli",
        EkipmanKategorisi.KapaliKasa => "Kapalı kasa",
        EkipmanKategorisi.Frigo => "Frigo",
        EkipmanKategorisi.Lowbed => "Lowbed / low-loader",
        EkipmanKategorisi.AgirYukDorsesi => "Ağır yük dorsesi",
        EkipmanKategorisi.ModulerAgirYuk => "Modüler ağır yük",
        EkipmanKategorisi.KonteynerSasisi => "Konteyner şasisi",
        EkipmanKategorisi.Damper => "Damper",
        EkipmanKategorisi.HareketliTaban => "Hareketli taban",
        EkipmanKategorisi.Silobas => "Silobas",
        EkipmanKategorisi.Tanker => "Tanker",
        _ => "Diğer"
    };

    /// <summary>Yükleme yönteminin metni (ör. "Arkadan, üstten"); yöntem yoksa "Tanımsız".</summary>
    public static string YontemMetni(YuklemeYontemi yontem)
    {
        var parcalar = new List<string>(3);
        if (yontem.HasFlag(YuklemeYontemi.Arkadan)) parcalar.Add("arkadan");
        if (yontem.HasFlag(YuklemeYontemi.Yandan)) parcalar.Add("yandan");
        if (yontem.HasFlag(YuklemeYontemi.Ustten)) parcalar.Add("üstten");
        if (parcalar.Count == 0)
            return "Tanımsız";
        var metin = string.Join(", ", parcalar);
        return char.ToUpper(metin[0], System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + metin[1..];
    }

    // Teknik veri şemasındaki açıklık taraflarının adları (motor şemasıyla aynı).
    public const string Arka = "arka";
    public const string Sol = "sol";
    public const string Sag = "sag";
    public const string Ust = "ust";
}
