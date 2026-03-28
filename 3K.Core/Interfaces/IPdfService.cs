namespace _3K.Core.Interfaces
{
    /// <summary>
    /// İş akışı 9: PDF oluşturma
    /// Orijinal çeki Excel şablonu açılır, operasyon verileri işlenir, PDF'e çevrilir
    /// </summary>
    public interface IPdfService
    {
        Task<byte[]> PdfOlusturAsync(int projeId);
    }
}
