using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    /// <summary>
    /// Müşteriye verilen gerçekleşen çeki listesi (PDF veya Excel; <see cref="Format"/> "pdf" / "excel"): projenin
    /// sevkiyat kayıtlarındaki sandıklar ve bu sandıklara konulan ürünler, TR/EN başlıklı. İzin projenin gerçek tipinin
    /// gerçekleşen çeki raporu okumasıdır (iç raporla aynı). Durum değiştirmez.
    /// </summary>
    public class GetMusteriCekiListesiQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public string Format { get; set; } = SevkiyatRaporBicimleri.Pdf;
    }

    public sealed class GetMusteriCekiListesiQueryHandler(
        IUnitOfWork unitOfWork,
        IReadQueryExecutor readQueries,
        ILookupCacheService lookup,
        ISevkiyatRaporDosyaService dosyalar)
        : IRequestHandler<GetMusteriCekiListesiQuery, Result<DosyaIcerigi>>
    {
        private const string SevkYok = "Projenin sevk edilmiş sandığı yok; müşteri çeki listesi yalnız sevk edilen sandıklardan oluşur.";

        public async Task<Result<DosyaIcerigi>> Handle(GetMusteriCekiListesiQuery request, CancellationToken cancellationToken)
        {
            var format = SevkiyatRaporBicimleri.Coz(request.Format);
            if (format is null)
                return Result<DosyaIcerigi>.Failure("Rapor biçimi pdf veya excel olmalıdır.");
            var proje = await RaporDosyaAdi.ProjeBulAsync(unitOfWork, request.ProjeId, cancellationToken);
            if (proje == null) return RaporDosyaAdi.ProjeBulunamadi;

            // Sevkiyat raporuyla aynı kaynak: geri alınan sevkiyatın sandığı kayıtta olmadığı için listeye girmez.
            var sevkiyatIdleri = await readQueries.ToListAsync(unitOfWork.GetRepository<Sevkiyat>().Queryable()
                .Where(s => s.ProjeId == request.ProjeId)
                .Select(s => s.Id), cancellationToken);
            if (sevkiyatIdleri.Count == 0)
                return Result<DosyaIcerigi>.Failure(SevkYok);
            var satirlar = await SevkSeferiOkuyucu.SatirlarAsync(unitOfWork, readQueries, null, null, null, cancellationToken, sevkiyatIdleri);
            var seferler = await SevkiyatRaporOlusturucu.SeferlerAsync(unitOfWork, readQueries, lookup, satirlar, cancellationToken);
            var liste = MusteriCekiListesi.Kur(proje.ProjeNo, proje.Musteri, proje.Lokasyon, TurkeyTime.Now, seferler);
            if (liste.Sandiklar.Count == 0)
                return Result<DosyaIcerigi>.Failure(SevkYok);

            var tipEki = RaporDosyaAdi.YedekMi(proje) ? "_Yedek" : string.Empty;
            var dosyaAdi = $"{proje.ProjeNo}{tipEki}_GerceklesenCekiListesi_Musteri";
            return Result<DosyaIcerigi>.Success(format == SevkiyatRaporBicimleri.Excel
                ? DosyaIcerigi.Excel(dosyalar.MusteriCekiExcelOlustur(liste), dosyaAdi + ".xlsx")
                : DosyaIcerigi.Pdf(dosyalar.MusteriCekiPdfOlustur(liste), dosyaAdi + ".pdf"));
        }
    }
}
