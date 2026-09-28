using ClosedXML.Excel;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Models;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Packaging;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace _3K.Infrastructure.Services.Cekiler;

/// <summary>Revizyon dosyasının güvenli okunması: ad, boyut ve paket sınırları, SHA-256 bütünlüğü, ön izleme JSON sınırı ve kaydedilen dosyanın silinmesi.</summary>
internal static class CekiRevizyonDosyasi
{
    private const int MaxRevizyonDosyaBoyutuBytes = 20 * 1024 * 1024;

    private const long MaxRevizyonPaketAcikBoyutuBytes = 100L * 1024 * 1024;

    private const long MaxRevizyonPaketParcaBoyutuBytes = 75L * 1024 * 1024;

    private const int MaxRevizyonPaketParcaSayisi = 5_000;

    private const int MaxRevizyonOnizlemeJsonBoyutuBytes = 10 * 1024 * 1024;

    private static readonly JsonSerializerOptions RevizyonJsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<RevizyonDosyaBilgisi> RevizyonDosyasiniOkuAsync(
        Stream excelDosya,
        string dosyaAdi,
        CancellationToken cancellationToken = default)
    {
        if (excelDosya == null || !excelDosya.CanRead)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyası okunamıyor.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        _ = RevizyonDosyaAdiniDogrula(dosyaAdi);

        if (excelDosya.CanSeek && excelDosya.Length - excelDosya.Position > MaxRevizyonDosyaBoyutuBytes)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyası 20 MB boyut sınırını aşıyor.",
                CekiRevizyonSorunKodlari.DosyaBoyutuAsildi);
        }

        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        int okunan;
        while ((okunan = await excelDosya.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            if (memoryStream.Length + okunan > MaxRevizyonDosyaBoyutuBytes)
            {
                CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                    "Revizyon Excel dosyası 20 MB boyut sınırını aşıyor.",
                    CekiRevizyonSorunKodlari.DosyaBoyutuAsildi);
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, okunan), cancellationToken);
        }

        var dosyaBytes = memoryStream.ToArray();

        if (dosyaBytes.Length == 0)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon Excel dosyası boş.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        try
        {
            RevizyonPaketBoyutunuDogrula(dosyaBytes);
            var cleanedBytes = CekiExcelOkuyucu.SanitizePictureNames(dosyaBytes);
            using var excelStream = new MemoryStream(cleanedBytes);
            using var workbook = new XLWorkbook(excelStream);
            return new RevizyonDosyaBilgisi(dosyaBytes, CekiExcelOkuyucu.OkuCiktiImportBilgisi(workbook));
        }
        catch (CekiRevizyonValidationException)
        {
            throw;
        }
        catch (Exception exception) when (RevizyonDosyaFormatiHatasiMi(exception))
        {
            throw new CekiRevizyonValidationException(
                "Yüklenen dosya geçerli ve okunabilir bir Excel (.xlsx) dosyası değil.",
                new[]
                {
                    CekiRevizyonHatalari.GenelRevizyonSorunuOlustur(
                        CekiRevizyonSorunKodlari.GecersizDosya,
                        "Dosya içeriği okunamadı. Dosyayı Excel'de açıp .xlsx biçiminde yeniden kaydedin.")
                },
                exception);
        }
    }

    internal static string RevizyonDosyaAdiniDogrula(string? dosyaAdi)
    {
        var guvenliDosyaAdi = (dosyaAdi ?? string.Empty)
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault()?
            .Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(guvenliDosyaAdi) ||
            guvenliDosyaAdi.Length > 255 ||
            guvenliDosyaAdi.Any(char.IsControl))
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon dosya adı geçersiz veya 255 karakter sınırını aşıyor.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        if (!string.Equals(Path.GetExtension(guvenliDosyaAdi), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon dosyası .xlsx uzantılı olmalıdır.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        return guvenliDosyaAdi;
    }

    private static void RevizyonPaketBoyutunuDogrula(byte[] dosyaBytes)
    {
        using var paketStream = new MemoryStream(dosyaBytes, writable: false);
        using var paket = new ZipArchive(paketStream, ZipArchiveMode.Read, leaveOpen: false);

        if (paket.Entries.Count == 0 ||
            paket.Entries.Count > MaxRevizyonPaketParcaSayisi ||
            !paket.Entries.Any(entry =>
                string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase)))
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon dosyasının Excel paket yapısı geçersiz.",
                CekiRevizyonSorunKodlari.GecersizDosya);
        }

        long toplamAcikBoyut = 0;
        foreach (var entry in paket.Entries)
        {
            if (entry.Length < 0 || entry.Length > MaxRevizyonPaketParcaBoyutuBytes)
            {
                CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                    "Revizyon dosyasındaki bir Excel parçası güvenli boyut sınırını aşıyor.",
                    CekiRevizyonSorunKodlari.DosyaBoyutuAsildi);
            }

            if (toplamAcikBoyut > MaxRevizyonPaketAcikBoyutuBytes - entry.Length)
            {
                CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                    "Revizyon dosyasının açılmış içeriği güvenli boyut sınırını aşıyor.",
                    CekiRevizyonSorunKodlari.DosyaBoyutuAsildi);
            }

            toplamAcikBoyut += entry.Length;
        }
    }

    private static bool RevizyonDosyaFormatiHatasiMi(Exception exception)
    {
        return exception is InvalidDataException
            or IOException
            or OpenXmlPackageException
            or System.Xml.XmlException
            || string.Equals(
                exception.GetType().FullName,
                "ClosedXML.Excel.IO.PartStructureException",
                StringComparison.Ordinal);
    }

    internal static byte[] RevizyonTalepDosyasiniDogrula(CekiRevizyonTalebi talep)
    {
        var dosyaIcerigi = talep.DosyaIcerigi ?? Array.Empty<byte>();
        if (dosyaIcerigi.Length == 0)
        {
            CekiRevizyonHatalari.RevizyonOnayCakismasiFirlat(
                "Onaylanan revizyon dosyasının içeriği bulunamadı. İşlem uygulanmadı.",
                CekiRevizyonSorunKodlari.OnayDosyasiDegisti);
        }

        if (dosyaIcerigi.Length > MaxRevizyonDosyaBoyutuBytes)
        {
            CekiRevizyonHatalari.RevizyonOnayCakismasiFirlat(
                "Onaylanan revizyon dosyası 20 MB boyut sınırını aşıyor. İşlem uygulanmadı.",
                CekiRevizyonSorunKodlari.DosyaBoyutuAsildi);
        }

        var guncelDosyaHash = Sha256Olustur(dosyaIcerigi);
        if (!GuvenliHashEsitMi(talep.DosyaSha256, guncelDosyaHash))
        {
            CekiRevizyonHatalari.RevizyonOnayCakismasiFirlat(
                "Onaya sunulan revizyon dosyasının bütünlük doğrulaması başarısız oldu. İşlem uygulanmadı.",
                CekiRevizyonSorunKodlari.OnayDosyasiDegisti);
        }

        return dosyaIcerigi;
    }

    internal static string Sha256Olustur(ReadOnlySpan<byte> icerik)
    {
        return Convert.ToHexString(SHA256.HashData(icerik)).ToLowerInvariant();
    }

    private static bool GuvenliHashEsitMi(string? beklenen, string? guncel)
    {
        if (string.IsNullOrWhiteSpace(beklenen) || string.IsNullOrWhiteSpace(guncel))
            return false;

        var beklenenBytes = Encoding.ASCII.GetBytes(beklenen.Trim().ToLowerInvariant());
        var guncelBytes = Encoding.ASCII.GetBytes(guncel.Trim().ToLowerInvariant());

        return beklenenBytes.Length == guncelBytes.Length &&
               CryptographicOperations.FixedTimeEquals(beklenenBytes, guncelBytes);
    }

    internal static string RevizyonOnizlemeJsoniniOlustur(
        CekiRevizyonOnizlemeSonuc onizleme)
    {
        var onizlemeJson = JsonSerializer.Serialize(onizleme, RevizyonJsonOptions);
        if (Encoding.UTF8.GetByteCount(onizlemeJson) > MaxRevizyonOnizlemeJsonBoyutuBytes)
        {
            CekiRevizyonHatalari.RevizyonValidationHatasiFirlat(
                "Revizyon ön izlemesi 10 MB güvenli kayıt boyutu sınırını aşıyor. Revizyonu daha küçük dosyalara bölerek yeniden deneyin.",
                CekiRevizyonSorunKodlari.OnizlemeBoyutuAsildi);
        }

        return onizlemeJson;
    }

    internal static string CleanFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidChar, '_');
        return string.IsNullOrWhiteSpace(safeName) ? "revizyon.xlsx" : safeName;
    }

    internal static void RevizyonDosyasiniGuvenleSil(ILogger logger, string? dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu) || !File.Exists(dosyaYolu))
            return;

        try
        {
            File.Delete(dosyaYolu);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(
                exception,
                "Başarısız revizyon işleminden kalan dosya silinemedi. Dosya yolu: {DosyaYolu}",
                dosyaYolu);
        }
    }
}
