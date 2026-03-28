namespace _3K.Core.Enums
{
    /// <summary>
    /// State Diagram'daki ürün durumları:
    /// Bekliyor → KismiGeldi / Tamamlandi / Eksik
    /// Eksik → StoktanKarsilandi / FBdenKarsilandi / SonraGidecek
    /// KismiGeldi → Tamamlandi
    /// Tamamlandi → SandikDegisti → Tamamlandi
    /// </summary>
    public enum UrunDurum
    {
        Bekliyor = 0,
        KismiGeldi = 1,
        Tamamlandi = 2,
        Eksik = 3,
        StoktanKarsilandi = 4,
        FBdenKarsilandi = 5,
        SonraGidecek = 6,
        SandikDegisti = 7
    }
}
