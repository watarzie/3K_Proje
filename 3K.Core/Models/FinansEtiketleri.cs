using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Finans enum değerlerinin kullanıcıya gösterilen Türkçe adları (rapor, PDF, Excel ve önizleme). API ve
/// sorgular enum adını/sayısını kullanmaya devam eder; bu adlar yalnız görüntüleme içindir.
/// </summary>
public static class FinansEtiketleri
{
    public static string IsTuru(FinansIsTuru tur) => tur switch
    {
        FinansIsTuru.AnaAmbalaj => "Ana Ambalaj",
        FinansIsTuru.IlaveSandik => "İlave Sandık",
        FinansIsTuru.IcSandik => "İç Sandık",
        FinansIsTuru.SahaSandigi => "Saha Sandığı",
        FinansIsTuru.YedekSandik => "Yedek Sandık",
        FinansIsTuru.Tadilat => "Tadilat",
        FinansIsTuru.DigerAmbalaj => "Diğer Ambalaj",
        FinansIsTuru.OzelIs => "Özel İş",
        FinansIsTuru.SarfKereste => "Sarf Kereste",
        _ => tur.ToString()
    };

    public static string IsDurumu(FinansIsDurumu durum) => durum switch
    {
        FinansIsDurumu.SiparisBekliyor => "Sipariş Bekliyor",
        FinansIsDurumu.KismiSiparis => "Kısmi Sipariş",
        FinansIsDurumu.SiparisAcildi => "Sipariş Açıldı",
        FinansIsDurumu.KismiFaturalandi => "Kısmi Faturalandı",
        FinansIsDurumu.Faturalandi => "Faturalandı",
        FinansIsDurumu.IptalEdildi => "İptal Edildi",
        _ => durum.ToString()
    };

    public static string FiyatlandirmaBirimi(FinansFiyatlandirmaBirimi birim) => birim switch
    {
        FinansFiyatlandirmaBirimi.Adet => "Adet",
        FinansFiyatlandirmaBirimi.Metrekup => "m³",
        FinansFiyatlandirmaBirimi.SabitTutar => "Sabit Tutar",
        FinansFiyatlandirmaBirimi.ManuelToplam => "Manuel Toplam",
        _ => birim.ToString()
    };

    /// <summary>
    /// Sandık cinsinin adı (Ambalaj ekranlarındaki adlarla aynı). Cins yoksa işin sandık tipi metni, o da yoksa
    /// boş döner.
    /// </summary>
    public static string SandikCinsi(AmbalajSandikCinsi? cins, string? sandikTipi = null) => cins switch
    {
        AmbalajSandikCinsi.AhsapKapali => "Ahşap Kapalı",
        AmbalajSandikCinsi.Kafes => "Kafes Sandık",
        AmbalajSandikCinsi.Kontrplak => "Kontrplak Sandık",
        AmbalajSandikCinsi.Katlanir => "Katlanır Sandık",
        AmbalajSandikCinsi.Diger => string.IsNullOrWhiteSpace(sandikTipi) ? "Diğer" : sandikTipi.Trim(),
        _ => sandikTipi?.Trim() ?? string.Empty
    };

    /// <summary>İşler raporu kapsamının ve bölümlerinin adı.</summary>
    public static string IsRaporuKapsami(FinansIsRaporuKapsami kapsam) => kapsam switch
    {
        FinansIsRaporuKapsami.SiparisiAcilan => "Siparişi açılan işler",
        FinansIsRaporuKapsami.SiparisiAcilmayan => "Siparişi açılmayan işler",
        _ => "Siparişi açılan ve açılmayan işler"
    };

    public static string GiderRaporuGruplamasi(FinansGiderRaporuGruplamasi gruplama) => gruplama switch
    {
        FinansGiderRaporuGruplamasi.Kategori => "Kategoriye göre",
        FinansGiderRaporuGruplamasi.Proje => "Projeye göre",
        _ => "Düz liste"
    };
}
