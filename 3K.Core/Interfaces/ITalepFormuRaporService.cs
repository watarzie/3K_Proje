namespace _3K.Core.Interfaces;

/// <summary>Talep formu Excel'i (G4): seçili projelerin Grid eksiği olan ürünleri tek sayfada.</summary>
public interface ITalepFormuRaporService
{
    /// <summary>
    /// Projeler seçim sırasıyla, her projede sandık numarası ve sıra numarasıyla listelenir. Yalnız
    /// <c>GridEksikMiktar &gt; 0</c> olan çeki satırları alınır; İstenen Miktar bu değerdir. Rapor salt okunurdur,
    /// süreç durumunu değiştirmez.
    /// </summary>
    Task<byte[]> TopluTalepFormuExcelAsync(IReadOnlyList<int> projeIds, CancellationToken cancellationToken = default);
}
