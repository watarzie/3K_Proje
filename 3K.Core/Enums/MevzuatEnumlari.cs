namespace _3K.Core.Enums
{
    /// <summary>Mevzuat kaynağının türü; taslaktan gelen kural yürürlükteymiş gibi gösterilmez.</summary>
    public enum MevzuatKaynakTuru
    {
        Kanun = 1,
        Yonetmelik = 2,
        YonetmelikTaslagi = 3,
        Genelge = 4,
        Standart = 5,
        IcKural = 6
    }

    /// <summary>
    /// Kural seti sürümünün durumu. Hesap yalnız onaylı sürümle yapılır; taslak sürüm görüntülenir ama kontrolde
    /// kullanılmaz.
    /// </summary>
    public enum MevzuatKuralSetiDurumu
    {
        Taslak = 1,
        Onayli = 2,
        Kaldirildi = 3
    }

    /// <summary>
    /// Kapalı kural tipi kümesi: her tipin parametre biçimi sabittir ve değerlendiricisi kodda tek yerde yazılır.
    /// Yeni ülke yeni kural seti demektir; yeni bir tip ancak kod ve testle eklenir.
    /// </summary>
    public enum MevzuatKuralTipi
    {
        AzamiGenislik = 1,
        AzamiYukseklik = 2,
        AzamiUzunluk = 3,
        TasmaSiniri = 4,
        TekDingilSiniri = 5,
        DingilGrubuSiniri = 6,
        KatarToplamAgirligi = 7,
        OlcumToleransi = 8,
        OzelYukIzinKategorisi = 9,
        DestekAraci = 10,
        ZabitaDestegi = 11,
        OzelYukYeterlilikBelgesi = 12,
        GeceYasagi = 13,
        OtoyolKisiti = 14,
        Isaretleme = 15
    }
}
