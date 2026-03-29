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
    }
}
