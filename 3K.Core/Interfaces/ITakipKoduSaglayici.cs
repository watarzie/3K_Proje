namespace _3K.Core.Interfaces
{
    /// <summary>
    /// Mevcut isteğin takip kodu: yanıtın X-Correlation-ID başlığı ve aynı isteğin bütün log satırlarındaki
    /// CorrelationId. Kalıcı kayda yazılan hata metni (ör. onay yürütme hatası) logla bu kodla eşleşir; ayrı kod
    /// üretilmez. HTTP isteği dışında (arka plan işi) null döner.
    /// </summary>
    public interface ITakipKoduSaglayici
    {
        string? TakipKodu { get; }
    }
}
