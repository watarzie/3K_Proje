using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Barkodla ürün arama (barkod-urun-arama R): barkodun Normal/Saha/Yedek bütün projelerdeki satırları
    /// (<see cref="BarkodUrunAramaKurali"/>). <see cref="YalnizEksikler"/> varsayılan açıktır; eksikler önce gelir.
    /// Proje tipinden bağımsız tek izin; satırdan proje ekranına geçiş ön yüzde tipin kendi iznine bağlıdır.
    /// </summary>
    public sealed class BarkodUrunAraQuery : IRequest<Result<BarkodUrunAramaSonucu>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string? Barkod { get; set; }
        public bool YalnizEksikler { get; set; } = true;
        public int? ProjeTipiId { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Sandik.BarkodUrunArama;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    }

    /// <summary>Aynı aramanın PDF veya Excel raporu (<see cref="Format"/> "pdf" / "excel"); her satırda proje no.</summary>
    public sealed class BarkodUrunAramaRaporuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string? Barkod { get; set; }
        public bool YalnizEksikler { get; set; } = true;
        public int? ProjeTipiId { get; set; }
        public string Format { get; set; } = SevkiyatRaporBicimleri.Pdf;

        public string RequiredMenuKod => YetkiKodlari.Sandik.BarkodUrunArama;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    }

    public sealed class BarkodUrunAramaQueryHandlers(IBarkodUrunAramaService service) :
        IRequestHandler<BarkodUrunAraQuery, Result<BarkodUrunAramaSonucu>>,
        IRequestHandler<BarkodUrunAramaRaporuQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<BarkodUrunAramaSonucu>> Handle(BarkodUrunAraQuery request, CancellationToken cancellationToken) =>
            Result<BarkodUrunAramaSonucu>.Success(
                await service.AraAsync(request.Barkod!, request.YalnizEksikler, request.ProjeTipiId, cancellationToken));

        public async Task<Result<DosyaIcerigi>> Handle(BarkodUrunAramaRaporuQuery request, CancellationToken cancellationToken)
        {
            var format = SevkiyatRaporBicimleri.Coz(request.Format);
            if (format is null)
                return Result<DosyaIcerigi>.Failure("Rapor biçimi pdf veya excel olmalıdır.");
            var sonuc = await service.AraAsync(request.Barkod!, request.YalnizEksikler, request.ProjeTipiId, cancellationToken);
            var dosyaAdi = $"BarkodArama_{DosyaAdiParcasi(sonuc.Barkod)}_{RaporDosyaAdi.TarihSaat()}";
            return Result<DosyaIcerigi>.Success(format == SevkiyatRaporBicimleri.Excel
                ? DosyaIcerigi.Excel(service.ExcelOlustur(sonuc), dosyaAdi + ".xlsx")
                : DosyaIcerigi.Pdf(service.PdfOlustur(sonuc), dosyaAdi + ".pdf"));
        }

        /// <summary>Kullanıcının yazdığı barkod dosya adına yalnız harf, rakam ve tire olarak girer.</summary>
        private static string DosyaAdiParcasi(string barkod)
        {
            var temiz = new string(barkod.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
            return temiz.Length > 40 ? temiz[..40] : temiz;
        }
    }
}
