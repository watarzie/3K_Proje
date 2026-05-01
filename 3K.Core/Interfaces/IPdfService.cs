namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 9: PDF / Excel oluşturma
    /// Orijinal çeki Excel şablonu açılır, operasyon verileri işlenir.
    /// ExcelOlusturAsync → Şablon üzerinde verileri doldurur, Excel olarak döner.
    /// PdfOlusturAsync → Aynı verilerle PDF oluşturur.
    /// </summary>
    public interface IPdfService
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

        /// <summary>
        /// QuestPDF ile belirli bir saha sandığına ait PDF raporu oluşturur.
        /// </summary>
        Task<byte[]> SahaSandikPdfOlusturAsync(int sandikId);

        /// <summary>
        /// QuestPDF ile bir saha projesine ait tüm sandıkların PDF raporunu (toplu) oluşturur.
        /// </summary>
        Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId);

        /// <summary>
        /// Normal projelerdeki eksik/gelmedi durumlu ve kalan > 0 olan ürünlerin PDF raporunu oluşturur.
        /// </summary>
        Task<byte[]> EksikUrunlerRaporuPdfOlusturAsync(int projeId);

        /// <summary>
        /// Stok modülündeki tüm ürünlerin PDF raporunu oluşturur.
        /// </summary>
        Task<byte[]> StokRaporuPdfOlusturAsync();
    }
}
