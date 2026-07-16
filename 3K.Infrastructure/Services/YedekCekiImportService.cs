using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services;

/// <summary>
/// Yedek proje çekisini, normal proje akışının kullandığı
/// CekiSatiri ve SandikIcerik modeline dönüştürür.
/// </summary>
public sealed class YedekCekiImportService : IYedekCekiImportService
{
    private const long MaksimumDosyaBoyutu = 20L * 1024 * 1024;
    private const long MaksimumAcikArsivBoyutu = 128L * 1024 * 1024;
    private const long MaksimumArsivGirdiBoyutu = 64L * 1024 * 1024;
    private const int MaksimumArsivGirdiSayisi = 2_048;
    private const int MaksimumSatirSayisi = 50_000;
    private const string BaslangicSandikNo = "1";
    private const decimal MaksimumMiktar = 99_999_999_999_999.9999m;

    private readonly AppDbContext _context;
    private readonly ILogger<YedekCekiImportService> _logger;

    public YedekCekiImportService(
        AppDbContext context,
        ILogger<YedekCekiImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<YedekCekiImportResult> ImportAsync(
        Stream excelDosya,
        string dosyaAdi,
        int kullaniciId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(excelDosya);

        if (kullaniciId <= 0)
            throw new CekiImportValidationException("Yedek çeki yüklemesi için geçerli kullanıcı bilgisi bulunamadı.");

        DosyaAdiniDogrula(dosyaAdi);

        // Kalıcı bir kayıt oluşturmadan önce dosyanın tamamı okunur ve doğrulanır.
        var dosyaBytes = await DosyayiLimitliOkuAsync(excelDosya, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        OpenXmlArsiviniDogrula(dosyaBytes, cancellationToken);
        var import = ExceliOkuVeDogrula(dosyaBytes, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        string? kaliciDosyaYolu = null;
        string? yuklemeKlasoru = null;
        var commitBaslatildi = false;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // ProjeNo için DB seviyesinde şema değişikliği gerektirmeyen, transaction-scope kilit.
            // Aynı proje için eş zamanlı iki isteğin iki çeki oluşturmasını engeller.
            var lockId = ProjeKilidiOlustur(import.ProjeNo);
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({lockId})",
                cancellationToken);

            var normalizedProjeNo = import.ProjeNo.ToUpperInvariant();
            var eslesenProjeler = await _context.Projeler
                .Where(p => p.ProjeNo.Trim().ToUpper() == normalizedProjeNo
                    || (p.FBNo != null && p.FBNo.Trim().ToUpper() == normalizedProjeNo))
                .Take(2)
                .ToListAsync(cancellationToken);

            if (eslesenProjeler.Count > 1)
            {
                throw new CekiImportConflictException(
                    $"{import.ProjeNo} numarası birden fazla projeyle eşleşiyor. Veri yöneticisi kontrolü gereklidir.");
            }

            var proje = eslesenProjeler.SingleOrDefault();
            var projeOlusturuldu = proje == null;
            if (proje == null)
            {
                proje = new Proje
                {
                    ProjeNo = import.ProjeNo,
                    FBNo = import.ProjeNo,
                    Musteri = "Yedek Malzeme",
                    DurumId = (int)ProjeDurum.Hazirlaniyor,
                    ProjeTipiId = (int)ProjeTipi.Yedek
                };
                _context.Projeler.Add(proje);
            }
            else
            {
                MevcutProjeyiDogrula(proje);

                var sevkiyatVarMi = await _context.Sevkiyatlar
                    .AnyAsync(s => s.ProjeId == proje.Id, cancellationToken);
                if (sevkiyatVarMi)
                {
                    throw new CekiImportConflictException(
                        $"{proje.ProjeNo} yedek projesinde sevkiyat kaydı bulunduğu için çeki yüklenemez.");
                }

                var cekiVarMi = await _context.Cekiler
                    .AnyAsync(c => c.ProjeId == proje.Id, cancellationToken);
                if (cekiVarMi)
                {
                    throw new CekiImportConflictException(
                        $"{proje.ProjeNo} yedek projesine daha önce çeki yüklenmiş. Aynı projeye ikinci çeki yüklenemez.");
                }
            }

            var sandik = proje.Id > 0
                ? await _context.Sandiklar.FirstOrDefaultAsync(
                    s => s.ProjeId == proje.Id && s.SandikNo == BaslangicSandikNo,
                    cancellationToken)
                : null;

            if (sandik is { DurumId: (int)SandikDurum.Kapandi or (int)SandikDurum.Sevkedildi })
            {
                throw new CekiImportConflictException(
                    $"{proje.ProjeNo} projesinin {BaslangicSandikNo} numaralı sandığı kapalı veya sevk edilmiş. Çeki yüklenemez.");
            }

            if (sandik == null)
            {
                sandik = new Sandik
                {
                    Proje = proje,
                    SandikNo = BaslangicSandikNo,
                    Ad = "Yedek Malzemeler",
                    DurumId = (int)SandikDurum.Hazirlaniyor
                };
                _context.Sandiklar.Add(sandik);
            }
            else if (sandik.DurumId == (int)SandikDurum.Bos)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
            }

            var ceki = new Ceki
            {
                Proje = proje,
                OrijinalDosyaYolu = string.Empty,
                YuklemeTarihi = TurkeyTime.Now,
                CekiTipiId = (int)CekiTipi.Normal,
                Aciklama = "Yedek proje çekisi"
            };
            _context.Cekiler.Add(ceki);

            foreach (var importSatiri in import.Satirlar)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var depoYeriAciklamasi = string.IsNullOrWhiteSpace(importSatiri.UretimDepoYeri)
                    ? null
                    : $"Üretim depo yeri: {importSatiri.UretimDepoYeri}";

                var cekiSatiri = new CekiSatiri
                {
                    Ceki = ceki,
                    SiraNo = importSatiri.SistemSiraNo,
                    OlcuResmiPozNo = importSatiri.SistemSiraNo.ToString(CultureInfo.InvariantCulture),
                    BarkodNo = importSatiri.BarkodNo,
                    Aciklama = importSatiri.Aciklama,
                    IstenenAdet = importSatiri.Miktar,
                    BirimId = (int)Birim.Adet,
                    CekideGecenSandikNo = BaslangicSandikNo,
                    FiiliSandikNo = BaslangicSandikNo,
                    Remarks = depoYeriAciklamasi,
                    DurumId = (int)UrunDurum.Bekliyor,
                    GridDurumuId = (int)GridDurum.Gelmedi,
                    UcKDurumuId = (int)UcKDurum.Bekliyor,
                    UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
                };
                _context.CekiSatirlari.Add(cekiSatiri);

                _context.SandikIcerikleri.Add(new SandikIcerik
                {
                    Sandik = sandik,
                    CekiSatiri = cekiSatiri,
                    TahsisMiktari = importSatiri.Miktar,
                    KonulanAdet = 0,
                    EksikAdet = 0,
                    Miktar = importSatiri.Miktar,
                    BarkodNo = importSatiri.BarkodNo,
                    Isim = importSatiri.Aciklama,
                    BirimId = (int)Birim.Adet,
                    Aciklama = depoYeriAciklamasi
                });
            }

            // Kimlikler dosya yolu ve hareket kaydı için gerekir; transaction henüz commit edilmez.
            await _context.SaveChangesAsync(cancellationToken);

            yuklemeKlasoru = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                proje.Id.ToString(CultureInfo.InvariantCulture),
                "Yedek");
            Directory.CreateDirectory(yuklemeKlasoru);

            var kaliciDosyaAdi = $"{TurkeyTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{GuvenliDosyaAdi(dosyaAdi)}";
            kaliciDosyaYolu = Path.Combine(yuklemeKlasoru, kaliciDosyaAdi);
            await File.WriteAllBytesAsync(kaliciDosyaYolu, dosyaBytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            ceki.OrijinalDosyaYolu = kaliciDosyaYolu;
            if (projeOlusturuldu)
            {
                _context.HareketGecmisleri.Add(new HareketGecmisi
                {
                    ProjeId = proje.Id,
                    ReferansTipi = "Proje",
                    ReferansId = proje.Id.ToString(CultureInfo.InvariantCulture),
                    ReferansMetni = proje.ProjeNo,
                    Islem = "Yedek Projesi Oluşturuldu",
                    IslemTipiId = (int)IslemTipi.ProjeOlusturuldu,
                    KullaniciId = kullaniciId,
                    Tarih = TurkeyTime.Now,
                    Aciklama = $"{proje.ProjeNo} yedek projesi çeki yüklemesiyle otomatik oluşturuldu."
                });
            }

            _context.HareketGecmisleri.Add(new HareketGecmisi
            {
                ProjeId = proje.Id,
                ReferansTipi = "Ceki",
                ReferansId = ceki.Id.ToString(CultureInfo.InvariantCulture),
                ReferansMetni = $"Yedek Çekisi - {dosyaAdi}",
                Islem = "Yedek Çekisi Yüklendi",
                IslemTipiId = (int)IslemTipi.CekiYuklendi,
                KullaniciId = kullaniciId,
                Tarih = TurkeyTime.Now,
                Aciklama = $"{import.Satirlar.Count} ürün, {BaslangicSandikNo} numaralı başlangıç sandığına aktarıldı."
            });

            await _context.SaveChangesAsync(cancellationToken);
            // Commit sırasında cancellation sonucu belirsiz bir DB durumuna yol açmamalı;
            // bu noktaya kadar iptal token'ı tüm okuma/yazmalarda uygulanmıştır.
            commitBaslatildi = true;
            await transaction.CommitAsync(CancellationToken.None);

            return new YedekCekiImportResult(
                ceki.Id,
                proje.Id,
                proje.ProjeNo,
                import.Satirlar.Count,
                1);
        }
        catch
        {
            if (!commitBaslatildi)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    // Rollback hatası asıl import hatasını gölgelememelidir.
                    _logger.LogWarning(rollbackException, "Yedek çeki transaction rollback işlemi tamamlanamadı.");
                }

                KaliciDosyayiTemizle(kaliciDosyaYolu, yuklemeKlasoru);
            }
            else
            {
                // Commit çağrısı hata verdiğinde sunucu tarafındaki sonuç bilinmeyebilir. DB commit
                // olmuş olabileceği için dosyayı silmek kırık bir Ceki.OrijinalDosyaYolu üretir.
                // Olası orphan dosya, olası kırık DB referansından daha güvenli ve temizlenebilirdir.
                _logger.LogError(
                    "Yedek çeki transaction commit sonucu doğrulanamadı; dosya güvenlik amacıyla korundu. Dosya: {DosyaYolu}",
                    kaliciDosyaYolu);
            }

            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private static YedekCekiImportBilgisi ExceliOkuVeDogrula(
        byte[] dosyaBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(dosyaBytes, writable: false);
            using var workbook = new XLWorkbook(stream);
            cancellationToken.ThrowIfCancellationRequested();

            var worksheet = workbook.Worksheets.FirstOrDefault(BasliklarGecerliMi);
            if (worksheet == null)
            {
                throw new CekiImportValidationException(
                    "Yedek çeki başlıkları bulunamadı. Beklenen format: A Kalem no, B Bileşen numarası, C1 Proje No/C2+ Açıklama, D Bileşen miktarı (BÖB), E Üretim depo yeri.");
            }

            var projeNo = HucreMetni(worksheet.Cell(1, 3));
            if (string.IsNullOrWhiteSpace(projeNo))
                throw new CekiImportValidationException("C1 hücresinde yedek proje numarası bulunamadı.");
            if (projeNo.Length > 100)
                throw new CekiImportValidationException("C1 hücresindeki proje numarası 100 karakterden uzun olamaz.");

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaksimumSatirSayisi)
            {
                throw new CekiImportValidationException(
                    $"Yedek çeki en fazla {MaksimumSatirSayisi:N0} veri satırı içerebilir.");
            }

            var satirlar = new List<YedekCekiSatirBilgisi>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                if ((rowNumber & 0x7F) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                var barkodNo = HucreMetni(worksheet.Cell(rowNumber, 2));
                var aciklama = HucreMetni(worksheet.Cell(rowNumber, 3));
                var miktarMetni = HucreMetni(worksheet.Cell(rowNumber, 4));
                var uretimDepoYeri = HucreMetni(worksheet.Cell(rowNumber, 5));

                if (string.IsNullOrWhiteSpace(barkodNo)
                    && string.IsNullOrWhiteSpace(aciklama)
                    && string.IsNullOrWhiteSpace(miktarMetni)
                    && string.IsNullOrWhiteSpace(uretimDepoYeri))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(barkodNo))
                    throw SatirHatasi(rowNumber, "Bileşen numarası boş olamaz.");
                if (barkodNo.Length > 200)
                    throw SatirHatasi(rowNumber, "Bileşen numarası 200 karakterden uzun olamaz.");

                // Dış sistemdeki Kalem No tekrar edebildiği için iç kimlik olarak kullanılmaz.
                // Sistem sıra numarası yalnızca geçerli ürün satırlarının dosyadaki sırasına göre üretilir.

                if (string.IsNullOrWhiteSpace(aciklama))
                    throw SatirHatasi(rowNumber, "Bileşen açıklaması boş olamaz.");
                if (aciklama.Length > 2_000)
                    throw SatirHatasi(rowNumber, "Bileşen açıklaması 2000 karakterden uzun olamaz.");
                if (uretimDepoYeri.Length > 250)
                    throw SatirHatasi(rowNumber, "Üretim depo yeri 250 karakterden uzun olamaz.");

                var miktar = MiktariOku(worksheet.Cell(rowNumber, 4), rowNumber);
                var sistemSiraNo = satirlar.Count + 1;
                satirlar.Add(new YedekCekiSatirBilgisi(
                    sistemSiraNo,
                    barkodNo,
                    aciklama,
                    miktar,
                    uretimDepoYeri));
            }

            if (satirlar.Count == 0)
                throw new CekiImportValidationException("Yedek çeki dosyasında aktarılacak ürün satırı bulunamadı.");

            cancellationToken.ThrowIfCancellationRequested();
            return new YedekCekiImportBilgisi(projeNo, satirlar);
        }
        catch (CekiImportValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new CekiImportValidationException(
                "Yedek çeki dosyası okunamadı. Dosyanın bozuk olmadığını ve .xlsx formatında kaydedildiğini kontrol edin.",
                exception);
        }
    }

    private static decimal MiktariOku(IXLCell cell, int rowNumber)
    {
        decimal miktar;
        if (!cell.TryGetValue(out miktar))
        {
            var text = HucreMetni(cell)
                .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal);

            if (text.Contains(',') && text.Contains('.'))
                throw SatirHatasi(rowNumber, "Miktar aynı anda virgül ve nokta içeremez.");

            text = text.Replace(',', '.');
            if (!decimal.TryParse(
                    text,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out miktar))
            {
                throw SatirHatasi(rowNumber, "Bileşen miktarı sayısal olmalıdır.");
            }
        }

        if (miktar <= 0)
            throw SatirHatasi(rowNumber, "Bileşen miktarı sıfırdan büyük olmalıdır.");
        if (miktar > MaksimumMiktar)
            throw SatirHatasi(rowNumber, "Bileşen miktarı numeric(18,4) sınırını aşıyor.");

        var scale = (decimal.GetBits(miktar)[3] >> 16) & 0x7F;
        if (scale > 4)
            throw SatirHatasi(rowNumber, "Bileşen miktarı en fazla 4 ondalık basamak içerebilir.");

        return miktar;
    }

    private static bool BasliklarGecerliMi(IXLWorksheet worksheet)
    {
        var kalemNo = NormalizeExcelText(worksheet.Cell(1, 1).GetString());
        var bilesenNo = NormalizeExcelText(worksheet.Cell(1, 2).GetString());
        var miktar = NormalizeExcelText(worksheet.Cell(1, 4).GetString());
        var depoYeri = NormalizeExcelText(worksheet.Cell(1, 5).GetString());

        return kalemNo.Contains("KALEM", StringComparison.Ordinal)
            && kalemNo.Contains("NO", StringComparison.Ordinal)
            && bilesenNo.Contains("BILESEN", StringComparison.Ordinal)
            && bilesenNo.Contains("NUMARA", StringComparison.Ordinal)
            && miktar.Contains("BILESEN", StringComparison.Ordinal)
            && miktar.Contains("MIKTAR", StringComparison.Ordinal)
            && depoYeri.Contains("URETIM", StringComparison.Ordinal)
            && depoYeri.Contains("DEPO", StringComparison.Ordinal)
            && depoYeri.Contains("YER", StringComparison.Ordinal);
    }

    private static void MevcutProjeyiDogrula(Proje proje)
    {
        if (proje.ProjeTipiId != (int)ProjeTipi.Yedek)
        {
            throw new CekiImportConflictException(
                $"{proje.ProjeNo} numarası zaten normal veya saha projesinde kullanılıyor. Yedek çekisi bu projeye yüklenemez.");
        }

        if (proje.DurumId is (int)ProjeDurum.Tamamlandi
            or (int)ProjeDurum.SevkEdildi
            or (int)ProjeDurum.EksikSevkEdildi)
        {
            throw new CekiImportConflictException(
                $"{proje.ProjeNo} projesi tamamlanmış veya sevkiyat sürecine girmiş. Üzerinde çeki yükleme işlemi yapılamaz.");
        }

        if (proje.GerceklesenSevkTarihi.HasValue)
        {
            throw new CekiImportConflictException(
                $"{proje.ProjeNo} projesinin gerçekleşen sevk tarihi bulunduğu için çeki yüklenemez.");
        }
    }

    private static void OpenXmlArsiviniDogrula(
        byte[] dosyaBytes,
        CancellationToken cancellationToken)
    {
        if (!OpenXmlImzasiVarMi(dosyaBytes))
            throw new CekiImportValidationException("Dosya içeriği geçerli bir .xlsx (Open XML) dosyası değil.");

        try
        {
            using var stream = new MemoryStream(dosyaBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            if (archive.Entries.Count == 0)
                throw new CekiImportValidationException("Yüklenen .xlsx arşivi boş.");
            if (archive.Entries.Count > MaksimumArsivGirdiSayisi)
            {
                throw new CekiImportValidationException(
                    $"Yüklenen .xlsx en fazla {MaksimumArsivGirdiSayisi:N0} arşiv girdisi içerebilir.");
            }

            long toplamAcikBoyut = 0;
            var adlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var contentTypesVar = false;
            var workbookVar = false;
            var worksheetVar = false;

            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedName = entry.FullName.Replace('\\', '/').TrimStart('/');
                if (string.IsNullOrWhiteSpace(normalizedName))
                    continue;
                if (normalizedName.Split('/', StringSplitOptions.RemoveEmptyEntries).Contains(".."))
                    throw new CekiImportValidationException("Yüklenen .xlsx geçersiz bir arşiv yolu içeriyor.");
                if (!adlar.Add(normalizedName))
                    throw new CekiImportValidationException("Yüklenen .xlsx yinelenen arşiv girdileri içeriyor.");
                if (entry.Length > MaksimumArsivGirdiBoyutu)
                {
                    throw new CekiImportValidationException(
                        "Yüklenen .xlsx içindeki tek bir içerik güvenli boyut sınırını aşıyor.");
                }

                toplamAcikBoyut = checked(toplamAcikBoyut + entry.Length);
                if (toplamAcikBoyut > MaksimumAcikArsivBoyutu)
                {
                    throw new CekiImportValidationException(
                        "Yüklenen .xlsx açıldığında güvenli toplam boyut sınırını aşıyor.");
                }

                contentTypesVar |= string.Equals(
                    normalizedName,
                    "[Content_Types].xml",
                    StringComparison.OrdinalIgnoreCase);
                workbookVar |= string.Equals(
                    normalizedName,
                    "xl/workbook.xml",
                    StringComparison.OrdinalIgnoreCase);
                worksheetVar |= normalizedName.StartsWith(
                        "xl/worksheets/",
                        StringComparison.OrdinalIgnoreCase)
                    && normalizedName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
            }

            if (!contentTypesVar || !workbookVar || !worksheetVar)
            {
                throw new CekiImportValidationException(
                    "Yüklenen dosyada zorunlu Excel çalışma kitabı içerikleri bulunamadı.");
            }
        }
        catch (CekiImportValidationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or OverflowException)
        {
            throw new CekiImportValidationException(
                "Yüklenen .xlsx arşivi bozuk veya güvenli boyut sınırlarını aşıyor.",
                exception);
        }
    }

    private static async Task<byte[]> DosyayiLimitliOkuAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead)
            throw new CekiImportValidationException("Yüklenen dosya okunamıyor.");

        if (stream.CanSeek && stream.Length == 0)
            throw new CekiImportValidationException("Yüklenen dosya boş.");
        if (stream.CanSeek && stream.Length > MaksimumDosyaBoyutu)
            throw new CekiImportValidationException("Yüklenen dosya 20 MB boyut sınırını aşıyor.");

        using var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        long toplamOkunan = 0;

        while (true)
        {
            var okunan = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (okunan == 0)
                break;

            toplamOkunan += okunan;
            if (toplamOkunan > MaksimumDosyaBoyutu)
                throw new CekiImportValidationException("Yüklenen dosya 20 MB boyut sınırını aşıyor.");

            await memoryStream.WriteAsync(buffer.AsMemory(0, okunan), cancellationToken);
        }

        if (toplamOkunan == 0)
            throw new CekiImportValidationException("Yüklenen dosya boş.");

        return memoryStream.ToArray();
    }

    private static void DosyaAdiniDogrula(string dosyaAdi)
    {
        if (string.IsNullOrWhiteSpace(dosyaAdi))
            throw new CekiImportValidationException("Dosya adı boş olamaz.");
        if (!string.Equals(Path.GetExtension(dosyaAdi), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new CekiImportValidationException("Yedek çeki yalnızca .xlsx formatında yüklenebilir.");
    }

    private static long ProjeKilidiOlustur(string projeNo)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"yedek-ceki:{projeNo.ToUpperInvariant()}"));
        return BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(0, sizeof(long)));
    }

    private static bool OpenXmlImzasiVarMi(IReadOnlyList<byte> bytes)
    {
        return bytes.Count >= 4
            && bytes[0] == 0x50
            && bytes[1] == 0x4B
            && (bytes[2] == 0x03 || bytes[2] == 0x05 || bytes[2] == 0x07)
            && (bytes[3] == 0x04 || bytes[3] == 0x06 || bytes[3] == 0x08);
    }

    private static string HucreMetni(IXLCell cell)
    {
        return cell.GetFormattedString(CultureInfo.GetCultureInfo("tr-TR"))
            .Replace('\u00A0', ' ')
            .Trim();
    }

    private static string NormalizeExcelText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim()
            .Replace('\u00A0', ' ')
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"))
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string GuvenliDosyaAdi(string dosyaAdi)
    {
        var result = Path.GetFileName(dosyaAdi);
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            result = result.Replace(invalidCharacter, '_');

        if (string.IsNullOrWhiteSpace(result))
            return "yedek-ceki.xlsx";

        var extension = Path.GetExtension(result);
        var baseName = Path.GetFileNameWithoutExtension(result);
        if (baseName.Length > 120)
            baseName = baseName[..120];

        return $"{baseName}{extension}";
    }

    private static CekiImportValidationException SatirHatasi(int rowNumber, string message)
    {
        return new CekiImportValidationException($"Excel satırı {rowNumber}: {message}");
    }

    private void KaliciDosyayiTemizle(string? dosyaYolu, string? klasor)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(dosyaYolu) && File.Exists(dosyaYolu))
                File.Delete(dosyaYolu);

            if (!string.IsNullOrWhiteSpace(klasor)
                && Directory.Exists(klasor)
                && !Directory.EnumerateFileSystemEntries(klasor).Any())
            {
                Directory.Delete(klasor);
            }
        }
        catch (Exception exception)
        {
            // Temizlik hatası ana iş hatasını gölgelememelidir.
            _logger.LogWarning(
                exception,
                "Başarısız yedek çeki importuna ait dosya temizlenemedi. Dosya: {DosyaYolu}",
                dosyaYolu);
        }
    }

    private sealed record YedekCekiImportBilgisi(
        string ProjeNo,
        List<YedekCekiSatirBilgisi> Satirlar);

    private sealed record YedekCekiSatirBilgisi(
        int SistemSiraNo,
        string BarkodNo,
        string Aciklama,
        decimal Miktar,
        string UretimDepoYeri);
}
