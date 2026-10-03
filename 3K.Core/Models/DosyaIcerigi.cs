namespace _3K.Core.Models;

/// <summary>İndirilecek dosya: içerik, MIME türü ve kullanıcıya önerilen dosya adı.</summary>
public sealed record DosyaIcerigi(byte[] Icerik, string IcerikTuru, string DosyaAdi)
{
    public const string PdfTuru = "application/pdf";
    public const string ExcelTuru = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ZipTuru = "application/zip";

    public static DosyaIcerigi Pdf(byte[] icerik, string dosyaAdi) => new(icerik, PdfTuru, dosyaAdi);
    public static DosyaIcerigi Excel(byte[] icerik, string dosyaAdi) => new(icerik, ExcelTuru, dosyaAdi);
    public static DosyaIcerigi Zip(byte[] icerik, string dosyaAdi) => new(icerik, ZipTuru, dosyaAdi);
}
