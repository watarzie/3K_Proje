using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;

/// <summary>Taşıma aileleri ve tipleri (ekipman-kitapligi R).</summary>
public sealed class EkipmanAileleriQuery : IRequest<Result<IReadOnlyList<EkipmanAilesiModeli>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Kitaplık listesi: kod/ad/üretici/model araması, aile, tip ve doğrulama süzgeci; sayfalı.</summary>
public sealed class EkipmanKonfigurasyonlariQuery : IRequest<Result<SayfaliSonuc<EkipmanKonfigurasyonOzeti>>>, ISecuredRequest, IRequiresMenuPermission
{
    public string? Arama { get; set; }
    public int? AileId { get; set; }
    public int? TipId { get; set; }
    public EkipmanDogrulamaDurumu? DogrulamaDurumu { get; set; }
    public bool PasifleriGoster { get; set; }
    public int? Sayfa { get; set; }
    public int? Boyut { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;

    public EkipmanListeKriteri Kriter() => new(Arama, AileId, TipId, DogrulamaDurumu, PasifleriGoster);
}

/// <summary>Konfigürasyonun son revizyonu, teknik verisi, eksik veri uyarıları ve revizyon geçmişi.</summary>
public sealed class EkipmanKonfigurasyonGetirQuery : IRequest<Result<EkipmanKonfigurasyonDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    public int Id { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Boş içe aktarım şablonu (Excel).</summary>
public sealed class EkipmanSablonuQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>Kitaplığın etkin konfigürasyonları şablon biçiminde (düzenleyip yeniden içe aktarmak için).</summary>
public sealed class EkipmanDisaAktarQuery : IRequest<Result<DosyaIcerigi>>, ISecuredRequest, IRequiresMenuPermission
{
    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
}

/// <summary>
/// İçe aktarım önizlemesi (ekipman-kitapligi W): dosya okunur ve denetlenir, hiçbir şey yazılmaz. İşlem izni ister,
/// çünkü önizleme içe aktarımın ilk adımıdır.
/// </summary>
public sealed class EkipmanIceAktarOnizleQuery : IRequest<Result<EkipmanIceAktarimSonucu>>, ISecuredRequest, IRequiresMenuPermission
{
    public Stream Dosya { get; set; } = Stream.Null;
    public string DosyaAdi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

public sealed class EkipmanKitapligiQueryHandlers(IEkipmanKitapligiService service) :
    IRequestHandler<EkipmanAileleriQuery, Result<IReadOnlyList<EkipmanAilesiModeli>>>,
    IRequestHandler<EkipmanKonfigurasyonlariQuery, Result<SayfaliSonuc<EkipmanKonfigurasyonOzeti>>>,
    IRequestHandler<EkipmanKonfigurasyonGetirQuery, Result<EkipmanKonfigurasyonDetayi>>,
    IRequestHandler<EkipmanSablonuQuery, Result<DosyaIcerigi>>,
    IRequestHandler<EkipmanDisaAktarQuery, Result<DosyaIcerigi>>,
    IRequestHandler<EkipmanIceAktarOnizleQuery, Result<EkipmanIceAktarimSonucu>>
{
    public const string KonfigurasyonBulunamadi = "Ekipman konfigürasyonu bulunamadı.";

    public async Task<Result<IReadOnlyList<EkipmanAilesiModeli>>> Handle(EkipmanAileleriQuery request, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<EkipmanAilesiModeli>>.Success(await service.AileleriListeleAsync(cancellationToken));

    public async Task<Result<SayfaliSonuc<EkipmanKonfigurasyonOzeti>>> Handle(EkipmanKonfigurasyonlariQuery request, CancellationToken cancellationToken) =>
        Result<SayfaliSonuc<EkipmanKonfigurasyonOzeti>>.Success(
            await service.KonfigurasyonlariListeleAsync(request.Kriter(), request.Sayfa, request.Boyut, cancellationToken));

    public async Task<Result<EkipmanKonfigurasyonDetayi>> Handle(EkipmanKonfigurasyonGetirQuery request, CancellationToken cancellationToken) =>
        await service.KonfigurasyonGetirAsync(request.Id, cancellationToken) is { } detay
            ? Result<EkipmanKonfigurasyonDetayi>.Success(detay)
            : Result<EkipmanKonfigurasyonDetayi>.Failure(KonfigurasyonBulunamadi, 404);

    public async Task<Result<DosyaIcerigi>> Handle(EkipmanSablonuQuery request, CancellationToken cancellationToken) =>
        Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(
            await service.SablonOlusturAsync(cancellationToken), $"EkipmanKitapligi_Sablon_{RaporDosyaAdi.TarihSaat()}.xlsx"));

    public async Task<Result<DosyaIcerigi>> Handle(EkipmanDisaAktarQuery request, CancellationToken cancellationToken) =>
        Result<DosyaIcerigi>.Success(DosyaIcerigi.Excel(
            await service.DisaAktarAsync(cancellationToken), $"EkipmanKitapligi_{RaporDosyaAdi.TarihSaat()}.xlsx"));

    public async Task<Result<EkipmanIceAktarimSonucu>> Handle(EkipmanIceAktarOnizleQuery request, CancellationToken cancellationToken) =>
        Result<EkipmanIceAktarimSonucu>.Success(await service.IceAktarAsync(request.Dosya, uygula: false, kullaniciId: null, cancellationToken));
}
