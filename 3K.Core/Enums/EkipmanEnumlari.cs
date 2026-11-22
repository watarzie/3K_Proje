namespace _3K.Core.Enums
{
    /// <summary>
    /// Ekipman tipinin yapısal kategorisi (<c>EkipmanTipleri.Kategori</c>). Yükleme yöntemi, gereken açıklıklar ve
    /// sandık taşımaya uygunluk bu değerden türetilir (<see cref="Models.EkipmanKategoriKurali"/>); tip adından
    /// davranış seçilmez.
    /// </summary>
    public enum EkipmanKategorisi
    {
        KapaliKonteyner = 1,
        HardtopKonteyner = 2,
        OpenTopKonteyner = 3,
        FlatRack = 4,
        PlatformKonteyner = 5,
        AcikKasa = 6,
        Platform = 7,
        UzamaliPlatform = 8,
        Tenteli = 9,
        KapaliKasa = 10,
        Frigo = 11,
        Lowbed = 12,
        AgirYukDorsesi = 13,
        ModulerAgirYuk = 14,
        KonteynerSasisi = 15,
        Damper = 16,
        HareketliTaban = 17,
        Silobas = 18,
        Tanker = 19,
        Diger = 99
    }

    /// <summary>
    /// Ekipmanın yük alma yönü (isterler 13). Bayraklıdır: Open Top hem arkadan hem üstten yüklenir. Tanımlanmamış
    /// yöntem varsayılmaz (<see cref="Yok"/>).
    /// </summary>
    [Flags]
    public enum YuklemeYontemi
    {
        Yok = 0,
        Arkadan = 1,
        Yandan = 2,
        Ustten = 4
    }

    /// <summary>Teknik verinin nereden geldiği; uygunluk ancak doğrulanmış veriyle üretilir.</summary>
    public enum EkipmanKaynakTuru
    {
        IsoAsgari = 1,
        TasiyiciFoyu = 2,
        UreticiFoyu = 3,
        SahadaOlculdu = 4,
        Diger = 5
    }

    /// <summary>
    /// Konfigürasyon revizyonunun doğrulama durumu. Doğrulanmamış veriye dayanan kontrol "Teyit edilmedi" olur;
    /// teknik veri değişince yeni revizyon doğrulanmamış başlar.
    /// </summary>
    public enum EkipmanDogrulamaDurumu
    {
        Dogrulanmadi = 1,
        Dogrulandi = 2
    }

    /// <summary>İçe aktarımda konfigürasyona ne olacağı: yeni kayıt, yeni revizyon ya da değişiklik yok.</summary>
    public enum EkipmanIceAktarimIslemi
    {
        Yeni = 1,
        YeniRevizyon = 2,
        Degismedi = 3
    }
}
