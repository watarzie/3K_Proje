using System.Globalization;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SevkiyatIslemleri.Queries;

/// <summary>Sevkiyat raporları: dönem raporu (PDF özet / Excel döküm) ve tek sefer PDF'i.</summary>
public sealed class SevkiyatRaporQueryHandlers(
    IUnitOfWork unitOfWork,
    IReadQueryExecutor readQueries,
    ILookupCacheService lookup,
    ISevkiyatRaporDosyaService dosyalar) :
    IRequestHandler<SevkiyatDonemRaporuQuery, Result<DosyaIcerigi>>,
    IRequestHandler<SevkSeferiRaporuQuery, Result<DosyaIcerigi>>
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<Result<DosyaIcerigi>> Handle(SevkiyatDonemRaporuQuery request, CancellationToken cancellationToken)
    {
        var format = SevkiyatRaporBicimleri.Coz(request.Format);
        if (format is null)
            return Result<DosyaIcerigi>.Failure("Rapor biçimi pdf veya excel olmalıdır.");

        // Sayfadaki sefer listesiyle aynı seçim: tarih ve proje tipi sorguda, plaka ve proje no sefer üzerinde.
        var satirlar = await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, request.Baslangic, request.Bitis,
            request.ProjeTipiId, cancellationToken);
        var anahtarlar = SevkSeferiOkuyucu.Suz(SevkSeferiOkuyucu.Seferler(satirlar), request.Plaka, request.ProjeNo)
            .Select(s => s.Anahtar)
            .ToHashSet(StringComparer.Ordinal);
        if (anahtarlar.Count == 0)
            return Result<DosyaIcerigi>.Failure("Seçilen süzgeçlere uyan sevkiyat bulunamadı.", 404);
        if (anahtarlar.Count > SevkiyatRaporKurali.EnFazlaSefer)
            return Result<DosyaIcerigi>.Failure(
                $"Süzgece uyan {anahtarlar.Count} sefer var; bir raporda en fazla {SevkiyatRaporKurali.EnFazlaSefer} sefer olabilir. Tarih aralığını daraltın.");

        var seferler = await SevkiyatRaporOlusturucu.SeferlerAsync(unitOfWork, readQueries, lookup,
            satirlar.Where(s => anahtarlar.Contains(s.SeferAnahtari)).ToList(), cancellationToken);
        var rapor = new SevkiyatRaporu("Sevkiyat Raporu", SuzgecAciklamasi(request), TurkeyTime.Now, seferler);
        var dosyaAdi = $"Sevkiyat_Raporu_{RaporDosyaAdi.TarihSaat()}";
        return Result<DosyaIcerigi>.Success(format == SevkiyatRaporBicimleri.Excel
            ? DosyaIcerigi.Excel(dosyalar.DonemExcelOlustur(rapor), dosyaAdi + ".xlsx")
            : DosyaIcerigi.Pdf(dosyalar.DonemPdfOlustur(rapor), dosyaAdi + ".pdf"));
    }

    public async Task<Result<DosyaIcerigi>> Handle(SevkSeferiRaporuQuery request, CancellationToken cancellationToken)
    {
        List<SevkiyatSatiri> satirlar;
        if (request.SeferId is int seferId)
        {
            var sevkiyatIdleri = await readQueries.ToListAsync(unitOfWork.GetRepository<Sevkiyat>().Queryable()
                .Where(s => s.SevkSeferiId == seferId)
                .Select(s => s.Id), cancellationToken);
            satirlar = sevkiyatIdleri.Count == 0
                ? []
                : await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, null, null, null, cancellationToken, sevkiyatIdleri);
        }
        else if (request.EskiTarih is DateTime gun)
        {
            // Eski kayıt seferi: sefere bağlı olmayan, aynı gün ve aynı (normalize) plakalı sevkiyatlar.
            var plaka = AracPlakaKurali.Normalize(request.EskiPlaka);
            satirlar = (await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, gun, gun, null, cancellationToken))
                .Where(s => !s.SeferId.HasValue && s.Plaka == plaka)
                .ToList();
        }
        else
        {
            return Result<DosyaIcerigi>.Failure("Sefer belirtilmelidir.");
        }

        var sefer = (await SevkiyatRaporOlusturucu.SeferlerAsync(unitOfWork, readQueries, lookup, satirlar, cancellationToken))
            .FirstOrDefault();
        if (sefer is null)
            return Result<DosyaIcerigi>.Failure("Sefer bulunamadı.", 404);

        var dosyaAdi = sefer.EskiKayit
            ? $"Sevkiyat_{AracPlakaKurali.Normalize(sefer.AracPlaka) ?? "Plakasiz"}_{sefer.SevkTarihi:yyyyMMdd}.pdf"
            : $"Sefer_{sefer.SeferNo}.pdf";
        return Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf(dosyalar.SeferPdfOlustur(sefer, TurkeyTime.Now), dosyaAdi));
    }

    /// <summary>Rapor başlığındaki süzgeç özeti, ör. "01.10.2026 – 13.10.2026 · Plaka: 34 ABC · Proje no: PA727".</summary>
    private string SuzgecAciklamasi(SevkiyatDonemRaporuQuery request)
    {
        var parcalar = new List<string>
        {
            (request.Baslangic, request.Bitis) switch
            {
                (DateTime b, DateTime s) => $"{b.ToString("dd.MM.yyyy", Tr)} – {s.ToString("dd.MM.yyyy", Tr)}",
                (DateTime b, null) => $"{b.ToString("dd.MM.yyyy", Tr)} ve sonrası",
                (null, DateTime s) => $"{s.ToString("dd.MM.yyyy", Tr)} ve öncesi",
                _ => "Tüm tarihler"
            }
        };
        if (!string.IsNullOrWhiteSpace(request.Plaka))
            parcalar.Add($"Plaka: {request.Plaka.Trim().ToUpper(Tr)}");
        if (!string.IsNullOrWhiteSpace(request.ProjeNo))
            parcalar.Add($"Proje no: {request.ProjeNo.Trim()}");
        if (request.ProjeTipiId is int tip)
        {
            var ad = lookup.GetDeger<LookupProjeTipi>(tip);
            parcalar.Add($"Proje tipi: {(ad == "?" ? tip.ToString(Tr) : ad)}");
        }
        return string.Join(" · ", parcalar);
    }
}
