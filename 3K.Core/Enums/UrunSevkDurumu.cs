namespace _3K.Core.Enums
{
    /// <summary>
    /// Ürün satırının sevk durumu (R-07). İş tamamlanmasından ayrı bir ölçüttür (S-03, S-08); projenin kendi sevkli
    /// sandığına fiilen konan, 3K'nın teslim aldığı adetten ve Grid İptal/Grid Kapandı kararından türetilir, saklanmaz.
    /// </summary>
    public enum UrunSevkDurumu
    {
        SevkEdilmedi = 1,

        /// <summary>Beklenen adedin bir kısmı gitti (ör. 40'ın 20'si): "Eksik Sevk (20/40)".</summary>
        EksikSevk = 2,

        SevkEdildi = 3,

        /// <summary>Tamamı trafoyla kapatılmış; 3K sandığıyla gitmez, trafonun sevk zamanı 3K'da tutulmaz.</summary>
        Trafoyla = 4,

        /// <summary>Grid tarafında kapatılıp orada sevk edilmiş: "Sevk Edildi (Grid Kapandı)".</summary>
        GridKapandi = 5
    }
}
