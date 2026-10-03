namespace _3K.Core.Interfaces;

/// <summary>Gerçekleşen çeki listesi raporu (PDF ve Excel).</summary>
public interface IGerceklesenCekiListesiRaporService
{
    /// <summary>
    /// Projenin gerçek tipini doğrulayarak gerçekleşen çeki listesi PDF raporunu oluşturur.
    /// </summary>
    Task<byte[]> GerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId);

    /// <summary>
    /// Yalnız saha projeleri için gerçekleşen çeki listesi PDF raporunu oluşturur.
    /// </summary>
    Task<byte[]> SahaGerceklesenCekiListesiRaporuPdfOlusturAsync(int projeId);

    Task<byte[]> GerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId);

    Task<byte[]> SahaGerceklesenCekiListesiRaporuExcelOlusturAsync(int projeId);
}
