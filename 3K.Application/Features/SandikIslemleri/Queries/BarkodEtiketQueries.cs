using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.SandikIslemleri.Queries
{
    /// <summary>
    /// Barkod etiketi isteklerinin ortak yetkisi (barkod-etiket R): proje tipinden bağımsız tek izin. Etiket üretmek veri
    /// değiştirmediği için dosya istekleri de okuma iznidir.
    /// </summary>
    public abstract class BarkodEtiketIstegi : ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => YetkiKodlari.Sandik.BarkodEtiket;
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
    }

    /// <summary>Etiket basılacak projeyi seçme listesi (sayfalı); arama proje no, FB no veya müşteride.</summary>
    public sealed class BarkodEtiketProjeleriQuery : BarkodEtiketIstegi, IRequest<Result<SayfaliSonuc<BarkodEtiketProjesi>>>
    {
        public string? Arama { get; set; }
        public int? Sayfa { get; set; }
        public int? Boyut { get; set; }
    }

    /// <summary>Seçilen projenin etiket alanları (FB NO, müşteri, varış yeri).</summary>
    public sealed class BarkodEtiketProjeQuery : BarkodEtiketIstegi, IRequest<Result<BarkodEtiketProjesi>>
    {
        public int ProjeId { get; set; }
    }

    /// <summary>Projenin sandıkları ve 12 haneli sandık barkodları (sayfalı).</summary>
    public sealed class BarkodEtiketSandiklariQuery : BarkodEtiketIstegi, IRequest<Result<SayfaliSonuc<BarkodEtiketSandikSatiri>>>
    {
        public int ProjeId { get; set; }
        public string? Arama { get; set; }
        public int? Sayfa { get; set; }
        public int? Boyut { get; set; }
    }

    /// <summary>Projenin ürünleri (çeki satırları ve elle eklenen malzemeler), isteğe bağlı tek sandıkla süzülür (sayfalı).</summary>
    public sealed class BarkodEtiketUrunleriQuery : BarkodEtiketIstegi, IRequest<Result<SayfaliSonuc<BarkodEtiketUrunSatiri>>>
    {
        public int ProjeId { get; set; }
        public int? SandikId { get; set; }
        public string? Arama { get; set; }
        public int? Sayfa { get; set; }
        public int? Boyut { get; set; }
    }

    /// <summary>Seçilen sandıkların etiket dosyası (PDF veya EZPL); tarih AA/YY olarak basılır.</summary>
    public sealed class SandikEtiketiOlusturQuery : BarkodEtiketIstegi, IRequest<Result<DosyaIcerigi>>
    {
        public int ProjeId { get; set; }
        public BarkodEtiketBicimi Bicim { get; set; } = BarkodEtiketBicimi.Pdf;
        public int Yil { get; set; }
        public int Ay { get; set; }
        public List<BarkodEtiketSandikSecimi> Secimler { get; set; } = [];
    }

    /// <summary>Seçilen ürünlerin etiket dosyası (PDF veya EZPL).</summary>
    public sealed class UrunEtiketiOlusturQuery : BarkodEtiketIstegi, IRequest<Result<DosyaIcerigi>>
    {
        public int ProjeId { get; set; }
        public BarkodEtiketBicimi Bicim { get; set; } = BarkodEtiketBicimi.Pdf;
        public List<BarkodEtiketUrunSecimi> Secimler { get; set; } = [];
    }

    public sealed class BarkodEtiketQueryHandlers(IBarkodEtiketService service) :
        IRequestHandler<BarkodEtiketProjeleriQuery, Result<SayfaliSonuc<BarkodEtiketProjesi>>>,
        IRequestHandler<BarkodEtiketProjeQuery, Result<BarkodEtiketProjesi>>,
        IRequestHandler<BarkodEtiketSandiklariQuery, Result<SayfaliSonuc<BarkodEtiketSandikSatiri>>>,
        IRequestHandler<BarkodEtiketUrunleriQuery, Result<SayfaliSonuc<BarkodEtiketUrunSatiri>>>,
        IRequestHandler<SandikEtiketiOlusturQuery, Result<DosyaIcerigi>>,
        IRequestHandler<UrunEtiketiOlusturQuery, Result<DosyaIcerigi>>
    {
        public async Task<Result<SayfaliSonuc<BarkodEtiketProjesi>>> Handle(BarkodEtiketProjeleriQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<BarkodEtiketProjesi>>.Success(
                await service.ProjeleriListeleAsync(request.Arama, request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<BarkodEtiketProjesi>> Handle(BarkodEtiketProjeQuery request, CancellationToken cancellationToken) =>
            Result<BarkodEtiketProjesi>.Success(await service.ProjeAsync(request.ProjeId, cancellationToken));

        public async Task<Result<SayfaliSonuc<BarkodEtiketSandikSatiri>>> Handle(BarkodEtiketSandiklariQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<BarkodEtiketSandikSatiri>>.Success(
                await service.SandiklariListeleAsync(request.ProjeId, request.Arama, request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<SayfaliSonuc<BarkodEtiketUrunSatiri>>> Handle(BarkodEtiketUrunleriQuery request, CancellationToken cancellationToken) =>
            Result<SayfaliSonuc<BarkodEtiketUrunSatiri>>.Success(await service.UrunleriListeleAsync(
                request.ProjeId, request.SandikId, request.Arama, request.Sayfa, request.Boyut, cancellationToken));

        public async Task<Result<DosyaIcerigi>> Handle(SandikEtiketiOlusturQuery request, CancellationToken cancellationToken) =>
            Result<DosyaIcerigi>.Success(await service.SandikEtiketleriAsync(
                request.ProjeId, request.Secimler, request.Yil, request.Ay, request.Bicim, cancellationToken));

        public async Task<Result<DosyaIcerigi>> Handle(UrunEtiketiOlusturQuery request, CancellationToken cancellationToken) =>
            Result<DosyaIcerigi>.Success(await service.UrunEtiketleriAsync(request.ProjeId, request.Secimler, request.Bicim, cancellationToken));
    }
}
