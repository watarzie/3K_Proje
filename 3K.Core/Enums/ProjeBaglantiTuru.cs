namespace _3K.Core.Enums
{
    /// <summary>
    /// Proje bağlantısının türü (<c>ProjeFotografBaglantilari.Tur</c>). Ekranda ayrı sekmelerde listelenir; sınır ve
    /// izin aynıdır. Değerler veritabanında kalıcı olduğu için değiştirilmemelidir.
    /// </summary>
    public enum ProjeBaglantiTuru
    {
        /// <summary>Projenin fotoğraflarının bulunduğu sayfa (varsayılan; tür eklenmeden önceki bütün kayıtlar).</summary>
        Fotograf = 1,

        /// <summary>Gerçekleşen çeki listesinin bulunduğu sayfa veya dosya.</summary>
        GerceklesenCeki = 2
    }
}
