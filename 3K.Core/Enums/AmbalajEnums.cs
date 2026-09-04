namespace _3K.Core.Enums
{
    /// <summary>
    /// Bir ambalaj üretim kaydının iş içindeki kullanım grubunu belirtir.
    /// Değerler veritabanında kalıcı olduğu için değiştirilmemelidir.
    /// </summary>
    public enum AmbalajSandikTuru
    {
        Normal = 1,
        Ilave = 2,
        Saha = 3,
        Yedek = 4,
        Ic = 5,
        Diger = 6
    }

    /// <summary>
    /// Kaydın sisteme hangi modülden geldiğini belirtir.
    /// KaynakKayitId ile birlikte tekrar eden otomatik aktarımları önler.
    /// </summary>
    public enum AmbalajKaynakModulu
    {
        Sandik = 1,
        Saha = 2,
        Yedek = 3,
        Manuel = 4,
        Diger = 5
    }

    /// <summary>
    /// Üretim kaydının yaşam döngüsü. İptal ayrı alanlarla tutulur ve geçmiş silinmez.
    /// </summary>
    public enum AmbalajUretimDurumu
    {
        Planlandi = 1,
        Uretimde = 2,
        Tamamlandi = 3
    }

    /// <summary>
    /// Fiziksel sandık yapısını belirtir. "Diğer" seçildiğinde açıklama zorunludur.
    /// </summary>
    public enum AmbalajSandikCinsi
    {
        AhsapKapali = 1,
        Kafes = 2,
        Kontrplak = 3,
        Katlanir = 4,
        Diger = 99
    }
}
