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

    /// <summary>Tek seferin yazdırılabilir raporu: araç/şoför bilgisi, proje proje sandık listesi (imza alanı yok).</summary>
    byte[] SeferPdfOlustur(SevkiyatRaporSeferi sefer, DateTime olusturmaTarihi);

    /// <summary>
    /// Müşteri gerçekleşen çeki listesi PDF ("Gerçekleşen Çeki Listesi / Actual Packing List"): sevk edilen sandıklar ve
    /// sandık sandık ürünleri, TR/EN başlıklarla; sevk tarihi yazılmaz.
    /// </summary>
    byte[] MusteriCekiPdfOlustur(MusteriCekiListesi liste);

    /// <summary>Müşteri gerçekleşen çeki listesi Excel: PDF ile aynı içerik ("Sandıklar - Cases", "Ürünler - Items").</summary>
    byte[] MusteriCekiExcelOlustur(MusteriCekiListesi liste);
}
