using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri;

/// <summary>Rapor indirmelerinde kullanıcıya önerilen dosya adlarını ve rapor projesini tek yerde çözer.</summary>
internal static class RaporDosyaAdi
{
    public static readonly Result<DosyaIcerigi> ProjeBulunamadi =
        Result<DosyaIcerigi>.Failure("Proje bulunamadı.", 404);

    public static string Tarih() => TurkeyTime.Now.ToString("yyyyMMdd");

    public static string TarihSaat() => TurkeyTime.Now.ToString("yyyyMMdd_HHmmss");

    public static async Task<Proje?> ProjeBulAsync(IUnitOfWork unitOfWork, int projeId, CancellationToken cancellationToken)
    {
        var proje = await unitOfWork.GetRepository<Proje>().GetByIdAsync(projeId);
        cancellationToken.ThrowIfCancellationRequested();
        return proje;
    }

    public static bool YedekMi(Proje proje) => proje.ProjeTipiId == (int)ProjeTipi.Yedek;

    /// <summary>Saha/Yedek sandık raporu adı projenin gerçek tipine göre seçilir.</summary>
    public static string SahaRaporAdi(Proje proje) => YedekMi(proje) ? "YedekRaporu" : "SahaRaporu";

    /// <summary>Saha projesinde eksik raporu sevk sonrası eksikleri gösterir.</summary>
    public static string EksikRaporAdi(int projeTipiId) =>
        projeTipiId == (int)ProjeTipi.Saha ? "SevkSonrasiEksikRaporu" : "EksikRaporu";
}
