using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Yükleme planı raporlarında görünen metinler (raporda enum adı yazılmaz). Ön yüzdeki aynası:
/// features/yukleme-planlama/ortak/yukleme-etiketleri.ts.
/// </summary>
public static class YuklemeEtiketleri
{
    public static string CalismaBicimi(YuklemeCalismaBicimi bicim) => bicim switch
    {
        YuklemeCalismaBicimi.IhtiyacaGoreArac => "İhtiyaca göre araç",
        YuklemeCalismaBicimi.MevcutAraclaraYerlestirme => "Mevcut araçlara yerleştirme",
        _ => "-"
    };

    public static string ProjeKarisimi(YuklemeProjeKarisimi karisim) => karisim switch
    {
        YuklemeProjeKarisimi.Karisik => "Projeler karışık yüklenebilir",
        YuklemeProjeKarisimi.ProjeBazindaAyri => "Her proje ayrı araçta",
        _ => "-"
    };

    public static string LokasyonTercihi(YuklemeLokasyonTercihi tercih) => tercih switch
    {
        YuklemeLokasyonTercihi.HenuzSecilmedi => "Henüz seçilmedi (ayrım yok)",
        YuklemeLokasyonTercihi.LokasyonaGoreAyri => "Lokasyonlara göre ayrı",
        YuklemeLokasyonTercihi.Karisik => "Lokasyonlar karışık",
        _ => "-"
    };

    public static string KontrolSonucu(YerlesimKontrolSonucu sonuc) => sonuc switch
    {
        YerlesimKontrolSonucu.Uygun => "Uygun",
        YerlesimKontrolSonucu.Uygunsuz => "Uygunsuz",
        YerlesimKontrolSonucu.TeyitEdilmedi => "Teyit edilmedi",
        _ => "-"
    };

    public static string KontrolTuru(YerlesimKontrolTuru tur) => tur switch
    {
        YerlesimKontrolTuru.Agirlik => "Ağırlık / kapasite",
        YerlesimKontrolTuru.KapidanGecis => "Kapıdan geçiş",
        YerlesimKontrolTuru.IcYukseklik => "İç yükseklik",
        YerlesimKontrolTuru.YasalYukseklik => "Yasal yükseklik",
        YerlesimKontrolTuru.YasalGenislik => "Yasal genişlik",
        YerlesimKontrolTuru.Tasma => "Kasa dışına taşma",
        YerlesimKontrolTuru.Dingil => "Dingil yükü",
        YerlesimKontrolTuru.BaglantiYuku => "Bağlantı (king-pin) yükü",
        YerlesimKontrolTuru.Destek => "Destek",
        YerlesimKontrolTuru.EkipmanVerisi => "Ekipman verisi",
        YerlesimKontrolTuru.GuvenlikPayi => "Güvenlik payı",
        YerlesimKontrolTuru.Mevzuat => "Mevzuat",
        _ => "-"
    };

    /// <summary>Yükleme yöntemi: üst açıklık, arka kapı ya da yandan.</summary>
    public static string GirisYonu(YerlesimGirisYonu yon) =>
        yon.HasFlag(YerlesimGirisYonu.Ustten) ? "Üstten"
        : yon.HasFlag(YerlesimGirisYonu.Arkadan) ? "Arkadan"
        : yon.HasFlag(YerlesimGirisYonu.Yandan) ? "Yandan"
        : "-";

    public static string KaynakFarki(PlanKaynakFarkTuru tur) => tur switch
    {
        PlanKaynakFarkTuru.YeniSandik => "Yeni sandık",
        PlanKaynakFarkTuru.SandikSilindi => "Sandık silindi",
        PlanKaynakFarkTuru.SandikSevkEdildi => "Sandık sevk edildi",
        PlanKaynakFarkTuru.OlcuDegisti => "Ölçü değişti",
        PlanKaynakFarkTuru.AgirlikDegisti => "Ağırlık değişti",
        PlanKaynakFarkTuru.LokasyonDegisti => "Lokasyon değişti",
        PlanKaynakFarkTuru.YukTuruDegisti => "Yük türü değişti",
        PlanKaynakFarkTuru.KodDegisti => "Sandık kodu değişti",
        PlanKaynakFarkTuru.ProjedenCikti => "Projeden çıktı",
        _ => "-"
    };

    /// <summary>Lokasyonu belli olmayan yükün metni (isterler 26).</summary>
    public const string BelirsizLokasyon = "Lokasyon henüz belirlenmedi";

    /// <summary>Lokasyonu belirsiz yük varsa raporlardaki açıklama (isterler 26).</summary>
    public const string OnPlanlamaNotu = "Ön planlama — yükleme lokasyonları kesinleşmemiştir.";

    /// <summary>Çözülmemiş uygunsuzluk varsa raporların ibaresi (isterler 29).</summary>
    public const string TaslakIbaresi = "TASLAK — YÜKLEMEYE UYGUN DEĞİL";

    /// <summary>Teknik uygunluk izin değildir (isterler 23).</summary>
    public const string IzinNotu = "Teknik olarak taşıyabilmek resmî izin veya güzergâh onayı anlamına gelmez.";
}
