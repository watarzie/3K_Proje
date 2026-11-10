namespace _3K.Core.Enums
{
    /// <summary>Yükleme planının çalışma biçimi (isterler 2).</summary>
    public enum YuklemeCalismaBicimi
    {
        /// <summary>Kullanıcı taşıma ailesini seçer; sistem uygun alt tipleri, adetleri ve yerleşimi hesaplar.</summary>
        IhtiyacaGoreArac = 1,

        /// <summary>Kullanıcı araç tiplerini ve adetlerini belirler; sistem bu araçlara yerleştirir.</summary>
        MevcutAraclaraYerlestirme = 2
    }

    /// <summary>Farklı projelerin aynı araçta bulunup bulunamayacağı (isterler 24).</summary>
    public enum YuklemeProjeKarisimi
    {
        Karisik = 1,
        ProjeBazindaAyri = 2
    }

    /// <summary>
    /// Lokasyon tercihi (isterler 25): ilk hesap lokasyon ayrımı yapılmadan yapılır, tercih sonra sorulur.
    /// </summary>
    public enum YuklemeLokasyonTercihi
    {
        HenuzSecilmedi = 1,
        LokasyonaGoreAyri = 2,
        Karisik = 3
    }

    /// <summary>Plan yükünün kaynağı: projedeki sandık ya da plana elle eklenen yük (isterler 4).</summary>
    public enum PlanYukKaynagi
    {
        Sandik = 1,
        Manuel = 2
    }

    /// <summary>
    /// Kullanıcının elle değiştirdiği izinler. Kaynaktan yenilemede bu izinler korunur, öbürleri varsayılandan yeniden
    /// kurulur (isterler 6).
    /// </summary>
    [Flags]
    public enum PlanYukIzni
    {
        Yok = 0,
        Dondurme = 1,
        UsteKonma = 2,
        AltaKalma = 4,
        UstYukSiniri = 8
    }

    /// <summary>Plan yükünün Faz 1'deki durumu; yerleşim durumları motorla gelir.</summary>
    public enum PlanYukDurumu
    {
        HaricTutuldu = 1,
        EksikVeri = 2,
        Bekliyor = 3,

        /// <summary>Son hesapta bir araca yerleşti.</summary>
        Yerlesti = 4,

        /// <summary>Son hesapta yerleşemedi; neden satırda yazılır.</summary>
        Yerlesemedi = 5
    }

    /// <summary>Yerleşim kontrolünün sonucu; veri yoksa uygunluk üretilmez ("Teyit edilmedi").</summary>
    public enum YerlesimKontrolSonucu
    {
        Uygun = 1,
        Uygunsuz = 2,
        TeyitEdilmedi = 3
    }

    /// <summary>Araç kontrolünün türü (Simülasyon isterleri 22–23); değerler motorla aynıdır.</summary>
    public enum YerlesimKontrolTuru
    {
        Agirlik = 1,
        KapidanGecis = 2,
        IcYukseklik = 3,
        YasalYukseklik = 4,
        YasalGenislik = 5,
        Tasma = 6,
        Dingil = 7,
        BaglantiYuku = 8,
        Destek = 9,
        EkipmanVerisi = 10,
        GuvenlikPayi = 11,
        Mevzuat = 12,
        Organizasyon = 13,
        ElleYerlesim = 14,
        AgirlikDagilimi = 15
    }

    /// <summary>Yükün (ya da istif grubunun) araca giriş yönü.</summary>
    [Flags]
    public enum YerlesimGirisYonu
    {
        Yok = 0,
        Arkadan = 1,
        Yandan = 2,
        Ustten = 4
    }

    /// <summary>
    /// Plandaki kopya ile güncel kaynak arasındaki fark (isterler 27): plan kendiliğinden değişmez, kullanıcı kaynaktan
    /// güncellemeyi seçerse yeni revizyon yazılır.
    /// </summary>
    public enum PlanKaynakFarkTuru
    {
        YeniSandik = 1,
        SandikSilindi = 2,
        SandikSevkEdildi = 3,
        OlcuDegisti = 4,
        AgirlikDegisti = 5,
        LokasyonDegisti = 6,
        YukTuruDegisti = 7,
        KodDegisti = 8,

        /// <summary>Sandık planın projelerinden başka bir projeye geçmiş.</summary>
        ProjedenCikti = 9
    }

    /// <summary>Araç yükleme oturumunun durumu; kapanan oturum yeniden açılmaz.</summary>
    public enum YuklemeOturumDurumu
    {
        Acik = 1,
        Tamamlandi = 2,
        IptalEdildi = 3
    }

    /// <summary>Yükleme işaretinin nasıl verildiği: sandık barkodu okutularak ya da listeden elle.</summary>
    public enum YuklemeIsaretKaynagi
    {
        Barkod = 1,
        Elle = 2
    }

    /// <summary>Okutulan barkodun çözüm sonucu (araç yükleme ekranı operatörü buna göre yönlendirir).</summary>
    public enum YukCozumSonucu
    {
        /// <summary>Yük ekrandaki araçta ve sırada: işaretlenebilir.</summary>
        Yuklenebilir = 1,

        /// <summary>Yük ekrandaki araçta ama sıradaki adım tamamlanmadı: onayla işaretlenebilir.</summary>
        SiraDisi = 2,

        /// <summary>Yük oturumun başka bir aracına ait.</summary>
        BaskaAracta = 3,
        ZatenYuklendi = 4,

        /// <summary>Sandık oturumun yerleşiminde yok (plan dışı, hariç tutulmuş ya da yerleşememiş).</summary>
        PlandaYok = 5,
        GecersizBarkod = 6
    }
}
