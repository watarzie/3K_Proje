namespace _3K.Core.Enums
{
    /// <summary>
    /// Fiziksel yükün türü (<c>LookupYukTurleri</c>, <c>Sandiklar.YukTuruId</c>). Yükleme planlaması trafoyu açıklama
    /// metninden değil bu alandan tanır (Simülasyon isterleri 10.1). Varsayılan Sandık'tır; normal projede 1 numaralı
    /// sandık veride transformatör olsa da (S-19) alan kendiliğinden işaretlenmez, sandık ekranından elle değiştirilir.
    /// </summary>
    public enum YukTuru
    {
        Sandik = 1,
        Trafo = 2,
        DigerOzelYuk = 3
    }
}
