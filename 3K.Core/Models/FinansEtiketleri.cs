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
}
