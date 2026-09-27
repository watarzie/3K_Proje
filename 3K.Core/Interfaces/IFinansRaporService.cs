using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans rapor dosyaları (Excel, PDF ve aylık ZIP).</summary>
public interface IFinansRaporService
{
    Task<byte[]> OzetRaporAsync(string tur, bool excel, FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> AylikExcelAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
    Task<byte[]> AylikPdfAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
    Task<byte[]> AylikZipAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
}
