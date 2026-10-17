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
        SabitTutar = 3,
        ManuelToplam = 4
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

    /// <summary>Faturanın tahsilat durumu (D3); vade ve ödeme işaretinden türetilir, elle atanmaz.</summary>
    public enum FinansOdemeDurumu
    {
        Bekliyor = 1,
        VadesiYaklasiyor = 2,
        VadesiGecti = 3,
        ZamanindaOdendi = 4,
        GecOdendi = 5
    }

    /// <summary>Fatura listesi ödeme filtresi.</summary>
    public enum FinansOdemeFiltresi
    {
        Odenmedi = 1,
        VadesiYaklasan = 2,
        VadesiGecen = 3,
        Odendi = 4
    }

    /// <summary>
    /// Proje veya grup düzeyinde siparişin/faturanın gerekli kalemleri ne kadar karşıladığı.
    /// Sayısal değerler ön yüz sözleşmesiyle sabittir.
    /// </summary>
    public enum FinansKarsilanma
    {
        Yok = 0,
        Kismi = 1,
        Tam = 2
    }

    /// <summary>
    /// PO/fatura alanında proje satırının açıldığı kategori. Üretim kategorileri iş türünden, düzenli
    /// işler kaynak düzenli işinden türetilir. Sayısal değerler ön yüz sözleşmesiyle sabittir.
    /// </summary>
    public enum FinansIsKategorisi
    {
        AnaAmbalaj = 1,
        SarfKereste = 2,
        Ilave = 3,
        Ic = 4,
        Saha = 5,
        Yedek = 6,
        DigerUretim = 7,
        OzelIs = 8,
        DuzenliIs = 9
    }

    /// <summary>
    /// İşin PO öncesi onay durumu. Saklanmaz; işin onay alanlarından ve son onay talebinin Onay Merkezi
    /// kararından türetilir (<see cref="Models.FinansPoOnayKurali"/>). Sayısal değerler ön yüz sözleşmesiyle sabittir.
    /// </summary>
    public enum FinansPoOnayDurumu
    {
        /// <summary>Onay kapsamı dışında (özel/düzenli iş) veya iptal/pasif kalem.</summary>
        Gerekmiyor = 0,
        OnayaSunulmadi = 1,
        OnayBekliyor = 2,
        Onaylandi = 3,
        Reddedildi = 4,
        /// <summary>Onaydan sonra net tutar veya para birimi değişti ya da onay uygulanamadı; yeniden onay ister.</summary>
        OnayDustu = 5
    }

    /// <summary>
    /// Referans finans ekranındaki aylık özel iş düzenleme davranışını belirler.
    /// Sayısal değerler ön yüz sözleşmesiyle sabittir.
    /// </summary>
    public enum FinansHesaplamaYontemi
    {
        SabitAylik = 1,
        DegiskenTutar = 2,
        DegiskenAdet = 3,
        Metrekup = 4,
        Bilesenli = 5
    }
}
