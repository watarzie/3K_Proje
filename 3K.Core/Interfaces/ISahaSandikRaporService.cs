namespace _3K.Core.Interfaces;

/// <summary>Saha sandık PDF raporları.</summary>
public interface ISahaSandikRaporService
{
    /// <summary>
    /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
    /// </summary>
    Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId);
}
