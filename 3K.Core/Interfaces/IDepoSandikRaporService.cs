namespace _3K.Core.Interfaces;

/// <summary>Depodaki sandıkların raporları.</summary>
public interface IDepoSandikRaporService
{
    /// <summary>
    /// Depoda aktif görünen sandıkların detaylı PDF raporunu oluşturur.
    /// </summary>
    Task<byte[]> DepoSandikRaporuPdfOlusturAsync(int? projeTipiId = null);

    Task<byte[]> ProjeDepoSandikRaporuPdfOlusturAsync(int projeId);

}
