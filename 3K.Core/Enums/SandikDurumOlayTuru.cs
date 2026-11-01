namespace _3K.Core.Enums
{
    /// <summary>
    /// Sandık açılış/kapanış olayının türü (<c>SandikDurumOlaylari.Tur</c>). Kapanma: sandık "Kapandı"ya girdi;
    /// açılma: kapalı sandık "Hazırlanıyor"a (veya içi boşalınca "Boş"a) döndü.
    /// </summary>
    public enum SandikDurumOlayTuru
    {
        Kapandi = 1,
        Acildi = 2
    }

    /// <summary>
    /// Olayın nasıl oluştuğu (<c>SandikDurumOlaylari.Kaynak</c>). Elle ve toplu işlemler kullanıcının kendi
    /// kararıdır; otomatik olay ürün güncelleme, taşıma, saha aktarımı gibi başka bir işlemin yan etkisidir ve
    /// sebebi yoktur. Geçmişten aktarılan olaylar 27 numaralı betikle eski hareket kayıtlarından kurulmuştur.
    /// </summary>
    public enum SandikDurumOlayKaynagi
    {
        Elle = 1,
        Toplu = 2,
        Otomatik = 3,
        GecmistenAktarildi = 4
    }
}
