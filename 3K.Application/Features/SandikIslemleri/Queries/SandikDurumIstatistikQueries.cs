using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Sandığın açılış/kapanış zaman çizelgesi (en yeniden eskiye): kim, ne zaman, nasıl ve elle geri açmada hangi
    /// sebeple. Sandık okuma izniyle açılır (sandık detayıyla aynı).
    /// </summary>
    public sealed class GetSandikDurumGecmisiQuery : IRequest<Result<IReadOnlyList<SandikDurumOlayModeli>>>, ISecuredRequest
    {
        public int SandikId { get; set; }
    }

    /// <summary>
    /// Sandık açılış/kapanış istatistiği (sandik-acilis-istatistigi R): süzgeçteki olayların özeti, sebep dağılımı ve
    /// en çok geri açılan sandıklar. Proje, müşteri, kişi ve olay tabloları sayfalı ayrı sorgulardır
    /// (<see cref="SandikDurumIstatistikSayfaQuery{T}"/>). Proje tipinden bağımsız tek izin.
    /// </summary>
    public sealed class SandikDurumIstatistigiQuery : IRequest<Result<SandikDurumIstatistigi>>, ISecuredRequest, IRequiresMenuPermission
    {
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public string? ProjeArama { get; set; }
        public string? Musteri { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Sandik.SandikAcilisIstatistigi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        public SandikDurumIstatistikKriteri Kriter() => new(Baslangic, Bitis, ProjeArama, Musteri);
    }

    /// <summary>Aynı istatistiğin Excel çıktısı; süzgeçteki bütün olaylar ayrı sayfadadır.</summary>
    public sealed class SandikDurumIstatistigiExcelQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
    {
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public string? ProjeArama { get; set; }
        public string? Musteri { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Sandik.SandikAcilisIstatistigi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        public SandikDurumIstatistikKriteri Kriter() => new(Baslangic, Bitis, ProjeArama, Musteri);
    }

    /// <summary>
    /// İstatistiğin sayfalı tablosu: süzgeç <see cref="SandikDurumIstatistigiQuery"/> ile aynı; <see cref="Sayfa"/> 1'den,
    /// <see cref="Boyut"/> en fazla <see cref="SayfaKurali.EnFazlaBoyut"/>.
    /// </summary>
    public abstract class SandikDurumIstatistikSayfaQuery<T> : IRequest<Result<SayfaliSonuc<T>>>, ISecuredRequest, IRequiresMenuPermission
    {
        public DateTime? Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public string? ProjeArama { get; set; }
        public string? Musteri { get; set; }
        public int? Sayfa { get; set; }
        public int? Boyut { get; set; }

        public string RequiredMenuKod => YetkiKodlari.Sandik.SandikAcilisIstatistigi;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

        public SandikDurumIstatistikKriteri Kriter() => new(Baslangic, Bitis, ProjeArama, Musteri);
    }

    public sealed class SandikDurumProjeleriQuery : SandikDurumIstatistikSayfaQuery<SandikDurumProjeSatiri>;
    public sealed class SandikDurumMusterileriQuery : SandikDurumIstatistikSayfaQuery<SandikDurumMusteriSatiri>;
    public sealed class SandikDurumKisileriQuery : SandikDurumIstatistikSayfaQuery<SandikDurumKisiSatiri>;
    public sealed class SandikDurumOlaylariQuery : SandikDurumIstatistikSayfaQuery<SandikDurumOlayModeli>;

    public sealed class SandikDurumIstatistikQueryHandlers(ISandikDurumIstatistikService service) :
        IRequestHandler<GetSandikDurumGecmisiQuery, Result<IReadOnlyList<SandikDurumOlayModeli>>>,
        IRequestHandler<SandikDurumIstatistigiQuery, Result<SandikDurumIstatistigi>>,
        IRequestHandler<SandikDurumIstatistigiExcelQuery, Result<DosyaIcerigi>>,
        IRequestHandler<SandikDurumProjeleriQuery, Result<SayfaliSonuc<SandikDurumProjeSatiri>>>,
        IRequestHandler<SandikDurumMusterileriQuery, Result<SayfaliSonuc<SandikDurumMusteriSatiri>>>,
        IRequestHandler<SandikDurumKisileriQuery, Result<SayfaliSonuc<SandikDurumKisiSatiri>>>,
        IRequestHandler<SandikDurumOlaylariQuery, Result<SayfaliSonuc<SandikDurumOlayModeli>>>
    {
        public async Task<Result<SayfaliSonuc<SandikDurumProjeSatiri>>> Handle(
            SandikDurumProjeleriQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<SandikDurumProjeSatiri>>.Success(
                await service.ProjelerAsync(request.Kriter(), request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<SayfaliSonuc<SandikDurumMusteriSatiri>>> Handle(
            SandikDurumMusterileriQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<SandikDurumMusteriSatiri>>.Success(
                await service.MusterilerAsync(request.Kriter(), request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<SayfaliSonuc<SandikDurumKisiSatiri>>> Handle(
            SandikDurumKisileriQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<SandikDurumKisiSatiri>>.Success(
                await service.KisilerAsync(request.Kriter(), request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<SayfaliSonuc<SandikDurumOlayModeli>>> Handle(
            SandikDurumOlaylariQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<SandikDurumOlayModeli>>.Success(
                await service.OlaylarAsync(request.Kriter(), request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<IReadOnlyList<SandikDurumOlayModeli>>> Handle(
            GetSandikDurumGecmisiQuery request, CancellationToken cancellationToken) =>
            Result<IReadOnlyList<SandikDurumOlayModeli>>.Success(
                await service.SandikGecmisiAsync(request.SandikId, cancellationToken));

        public async Task<Result<SandikDurumIstatistigi>> Handle(
            SandikDurumIstatistigiQuery request, CancellationToken cancellationToken) =>
            Result<SandikDurumIstatistigi>.Success(await service.IstatistikAsync(request.Kriter(), cancellationToken));

        public async Task<Result<DosyaIcerigi>> Handle(
            SandikDurumIstatistigiExcelQuery request, CancellationToken cancellationToken) =>
            Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(
                await service.ExcelOlusturAsync(request.Kriter(), cancellationToken),
                $"SandikAcilisKapanisIstatistigi_{RaporDosyaAdi.TarihSaat()}.xlsx"));
    }
}
