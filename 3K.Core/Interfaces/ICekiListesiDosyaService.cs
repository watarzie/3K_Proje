namespace _3K.Core.Interfaces;

/// <summary>Çeki listesi dosyaları: operasyon verileri işlenmiş orijinal Excel şablonu ve aynı verilerle PDF.</summary>
public interface ICekiListesiDosyaService
{
    /// <summary>
    /// Orijinal Excel şablonunu açar, paketleyen/kontrol/remarks verilerini yazar,
    /// değiştirilmiş Excel'i byte[] olarak döner. "Excel neyse PDF odur."
    /// </summary>
    Task<byte[]> ExcelOlusturAsync(int projeId);

    /// <summary>
    /// QuestPDF ile PDF raporu oluşturur.
    /// </summary>
    Task<byte[]> PdfOlusturAsync(int projeId);
}
