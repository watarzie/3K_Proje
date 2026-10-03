namespace _3K.Core.Interfaces;

/// <summary>Stok raporu.</summary>
public interface IStokRaporService
{
    /// <summary>
    /// Stok modülündeki tüm ürünlerin PDF raporunu oluşturur.
    /// </summary>
    Task<byte[]> StokRaporuPdfOlusturAsync();
}
