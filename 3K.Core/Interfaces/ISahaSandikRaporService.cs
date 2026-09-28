namespace _3K.Core.Interfaces;

/// <summary>Saha sandık PDF raporları.</summary>
public interface ISahaSandikRaporService
{
    /// <summary>
    /// QuestPDF ile belirli bir saha sandığına ait PDF raporu oluşturur.
    /// </summary>
    Task<byte[]> SahaSandikPdfOlusturAsync(int sandikId);

    /// <summary>
    /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
    /// </summary>
    Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId);
}
