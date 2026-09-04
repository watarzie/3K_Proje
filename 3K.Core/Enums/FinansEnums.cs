namespace _3K.Core.Enums
{
    /// <summary>
    /// Üretim ve manuel finans kayıtlarının ortak iş sınıflandırması.
    /// Sayısal değerler API/ön yüz sözleşmesinin geriye uyumluluğu için sabittir.
    /// </summary>
    public enum FinansIsTuru
    {
        AnaAmbalaj = 1,
        IlaveSandik = 2,
        IcSandik = 3,
        SahaSandigi = 4,
        YedekSandik = 5,
        Tadilat = 6,
        DigerAmbalaj = 7,
        OzelIs = 8,
        SarfKereste = 9
    }

    public enum FinansFiyatlandirmaBirimi
    {
        Adet = 1,
        Metrekup = 2,
        SabitTutar = 3
    }

    public enum FinansIsDurumu
    {
        SiparisBekliyor = 1,
        KismiSiparis = 2,
        SiparisAcildi = 3,
        KismiFaturalandi = 4,
        Faturalandi = 5,
        IptalEdildi = 6
    }

    public enum FinansSiparisDurumu
    {
        Acik = 1,
        KismiFaturalandi = 2,
        Faturalandi = 3,
        IptalEdildi = 4
    }

    public enum FinansFaturaDurumu
    {
        Aktif = 1,
        IptalEdildi = 2
    }

    public enum FinansTekrarSikligi
    {
        Aylik = 1
    }

    /// <summary>
    /// Referans finans ekranındaki aylık özel iş düzenleme davranışını belirler.
    /// Sayısal değerler ön yüz sözleşmesiyle sabittir.
    /// </summary>
    public enum FinansHesaplamaYontemi
    {
        SabitAylik = 1,
        DegiskenTutar = 2,
        DegiskenAdet = 3
    }
}
