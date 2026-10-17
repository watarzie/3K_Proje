namespace _3K.Core.Enums
{
    /// <summary>
    /// Ürün satırının fiziksel sevk durumu (R-07). İş tamamlanmasından ayrı bir ölçüttür (S-03, S-08);
    /// sevkli sandığa (sahaya aktarılan kısımda saha sandığına) fiilen konan adetten türetilir, saklanmaz.
    /// </summary>
    public enum UrunSevkDurumu
    {
        SevkEdilmedi = 1,
        KismiSevk = 2,
        SevkEdildi = 3,

        /// <summary>Tamamı trafoyla kapatılmış; 3K sandığıyla gitmez, trafonun sevk zamanı 3K'da tutulmaz.</summary>
        Trafoyla = 4
    }
}
