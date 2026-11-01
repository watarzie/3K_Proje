namespace _3K.Core.Interfaces;

/// <summary>Saha/Yedek sandık raporları.</summary>
public interface ISahaSandikRaporService
{
    /// <summary>
    /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
    /// </summary>
    Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId);

    /// <summary>
    /// Yalnız sevke hazır (kapalı, ölçü ve ağırlığı girilmiş) sandıkların PDF raporu; başta listelenmeyen sandık notu.
    /// </summary>
    Task<byte[]> SevkeHazirSandiklarPdfOlusturAsync(int projeId);

    /// <summary>Sevke hazır sandık raporunun PDF ile aynı içerikli Excel'i.</summary>
    Task<byte[]> SevkeHazirSandiklarExcelOlusturAsync(int projeId);
}
