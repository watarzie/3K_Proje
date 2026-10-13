using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// Sevkiyat raporu dosyaları. Veri Application'da hazırlanır (<see cref="SevkiyatRaporu"/>); bu servis yalnız PDF ve
/// Excel üretir.
/// </summary>
public interface ISevkiyatRaporDosyaService
{
    /// <summary>Dönem raporu PDF: özet, araç özeti ve sefer × proje tablosu (sandık numaralarıyla).</summary>
    byte[] DonemPdfOlustur(SevkiyatRaporu rapor);

    /// <summary>Dönem dökümü Excel: Seferler, Araçlar, Projeler, Sandıklar ve Ürünler sayfaları.</summary>
    byte[] DonemExcelOlustur(SevkiyatRaporu rapor);

    /// <summary>Tek seferin yazdırılabilir raporu: araç/şoför bilgisi, proje proje sandık listesi ve imza alanı.</summary>
    byte[] SeferPdfOlustur(SevkiyatRaporSeferi sefer, DateTime olusturmaTarihi);
}
