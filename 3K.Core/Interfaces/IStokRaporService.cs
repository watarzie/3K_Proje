namespace _3K.Core.Interfaces;

/// <summary>Stok raporu.</summary>
public interface IStokRaporService
{
    /// <summary>
    /// Stok modülündeki ürünlerin PDF raporunu oluşturur; durum verilirse yalnız o durumdaki kayıtlar basılır.
    /// </summary>
    Task<byte[]> StokRaporuPdfOlusturAsync(int? durumId = null);
}
