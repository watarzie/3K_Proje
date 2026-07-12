namespace _3K.Core.Enums
{
    public enum OnayDurumu
    {
        Bekliyor = 1,
        Onaylandi = 2,
        Reddedildi = 3
    }

    /// <summary>
    /// İnsan kararından bağımsız olarak, onaylanan komutun çalıştırılma yaşam döngüsünü belirtir.
    /// </summary>
    public enum OnayCalistirmaDurumu
    {
        /// <summary>Eski kayıtta çalıştırma sonucu güvenilir biçimde belirlenemiyor.</summary>
        Bilinmiyor = 0,
        Bekliyor = 1,
        Calisiyor = 2,
        Basarili = 3,
        Basarisiz = 4,
        Atlandi = 5
    }
}
