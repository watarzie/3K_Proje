using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>Finans rapor dosyaları (Excel, PDF ve aylık ZIP).</summary>
public interface IFinansRaporService
{
    Task<byte[]> OzetRaporAsync(string tur, bool excel, FinansListeFiltre filtre, CancellationToken cancellationToken);
    /// <summary>
    /// Rapor türünün (is, gider, alacak, finansal-durum veya özet türleri) başlık, sütun ve ilk satırları; iş ve gider
    /// raporunda <paramref name="secenekler"/> kapsamı/gruplamayı dosyayla aynı uygular.
    /// </summary>
    Task<FinansRaporOnizlemeModel> OnizlemeAsync(string tur, FinansListeFiltre filtre, FinansRaporSecenekleri secenekler, CancellationToken cancellationToken);
    /// <summary>İşler raporu: siparişi açılan/açılmayan bölümleri, bölüm ve genel toplamlarıyla (para birimi bazında).</summary>
    Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken cancellationToken);
    Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken cancellationToken);
    /// <summary>Gider raporu: gruplamaya göre ara toplamlar, genel toplam; avans maliyete girmez, ayrı toplanır.</summary>
    Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken cancellationToken);
    Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken cancellationToken);
    /// <summary>Finansal durum (gelir–gider) raporu: Kontrol Paneliyle aynı tutarlar, kırılımlar ve aylık trend.</summary>
    Task<byte[]> FinansalDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> FinansalDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    /// <summary>Alacak (vade/tahsilat) raporu (D3): faturalar, vade durumu ve gecikme kovaları.</summary>
    Task<byte[]> AlacakExcelAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> AlacakPdfAsync(FinansListeFiltre filtre, CancellationToken cancellationToken);
    Task<byte[]> AylikExcelAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
    Task<byte[]> AylikPdfAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
    Task<byte[]> AylikZipAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken cancellationToken);
}
