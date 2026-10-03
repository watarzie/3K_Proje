namespace _3K.Core.Interfaces;

/// <summary>Eksik ürünler raporu (PDF ve Excel).</summary>
public interface IEksikUrunlerRaporService
{
    /// <summary>
    /// Normal projelerdeki eksik/gelmedi durumlu ve kalan > 0 olan ürünlerin PDF raporunu oluşturur.
    /// </summary>
    Task<byte[]> EksikUrunlerRaporuPdfOlusturAsync(int projeId, CancellationToken cancellationToken = default);

    Task<byte[]> EksikUrunlerRaporuExcelOlusturAsync(int projeId, CancellationToken cancellationToken = default);
}
