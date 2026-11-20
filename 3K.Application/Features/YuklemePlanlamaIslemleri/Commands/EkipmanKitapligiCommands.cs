using System.Text.Json.Serialization;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;

/// <summary>
/// Excel'den içe aktarım (ekipman-kitapligi W): sorun yoksa tek transaction'da yazılır, teknik verisi değişen
/// konfigürasyon yeni (doğrulanmamış) revizyon alır. Sorun varsa hiçbir şey yazılmaz, sorunlar döner.
/// </summary>
public sealed class EkipmanIceAktarCommand : IRequest<Result<EkipmanIceAktarimSonucu>>, ISecuredRequest, IRequiresMenuPermission
{
    public Stream Dosya { get; set; } = Stream.Null;
    public string DosyaAdi { get; set; } = string.Empty;
    public long DosyaBoyutu { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>
/// Konfigürasyonun son revizyonunu doğrulanmış işaretleme (ekipman-dogrulama W): veriyi giren ile doğrulayan ayrı
/// rol olabilir. Revizyon son değilse veya zaten doğrulanmışsa 409.
/// </summary>
public sealed class EkipmanDogrulaCommand : IRequest<Result<EkipmanKonfigurasyonDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int KonfigurasyonId { get; set; }

    public int RevizyonNo { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanDogrulama;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>
/// Ekrandan girilen konfigürasyonun gövdesi (ekleme ve güncellemede aynı; doğrulama kuralları ortak). Alanlar Excel'deki
/// konfigürasyon satırının karşılığıdır; teknik veri ayrıntıdaki <c>teknikVeriJson</c> ile aynı biçimde JSON metnidir.
/// İzin içe aktarımla aynıdır (ekipman-kitapligi W); kayıt revizyonu doğrulanmış yapmaz.
/// </summary>
public abstract class EkipmanKonfigurasyonKaydiIstegi : IRequest<Result<EkipmanKonfigurasyonDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int TipId { get; set; }
    public string Uretici { get; set; } = string.Empty;
    public string ModelAdi { get; set; } = string.Empty;
    public EkipmanKaynakTuru KaynakTuru { get; set; }
    public string? KaynakAciklama { get; set; }
    public DateTime? KaynakTarihi { get; set; }
    public string? Aciklama { get; set; }
    public string TeknikVeriJson { get; set; } = string.Empty;

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.EkipmanKitapligi;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;

    public EkipmanKonfigurasyonKaydi Kayit() =>
        new(Kod, Ad, TipId, Uretici, ModelAdi, KaynakTuru, KaynakAciklama, KaynakTarihi, Aciklama, TeknikVeriJson);
}

/// <summary>
/// Ekrandan yeni konfigürasyon (ekipman-kitapligi W): tek konfigürasyonluk içe aktarımla aynı kurallar; ilk revizyon
/// doğrulanmamış yazılır. Aynı kod varsa 409; kural sorunu varsa 400 ve sorun listesi, hiçbir şey yazılmaz.
/// </summary>
public sealed class EkipmanKonfigurasyonEkleCommand : EkipmanKonfigurasyonKaydiIstegi;

/// <summary>
/// Ekrandan güncelleme (ekipman-kitapligi W): teknik verisi ya da kaynağı değişen konfigürasyon yeni (doğrulanmamış)
/// revizyon alır; değişiklik yoksa revizyon yazılmaz. Beklenen revizyon son değilse 409; kod değiştirilemez (400).
/// </summary>
public sealed class EkipmanKonfigurasyonGuncelleCommand : EkipmanKonfigurasyonKaydiIstegi
{
    /// <summary>Rotadan gelir.</summary>
    [JsonIgnore]
    public int KonfigurasyonId { get; set; }

    public int BeklenenRevizyonNo { get; set; }
}

public sealed class EkipmanKitapligiCommandHandlers(IEkipmanKitapligiService service, ICurrentUserService currentUser) :
    IRequestHandler<EkipmanIceAktarCommand, Result<EkipmanIceAktarimSonucu>>,
    IRequestHandler<EkipmanKonfigurasyonEkleCommand, Result<EkipmanKonfigurasyonDetayi>>,
    IRequestHandler<EkipmanKonfigurasyonGuncelleCommand, Result<EkipmanKonfigurasyonDetayi>>,
    IRequestHandler<EkipmanDogrulaCommand, Result<EkipmanKonfigurasyonDetayi>>
{
    public const string IceAktarimSorunluMesaji = "Dosyada sorun var; hiçbir kayıt yazılmadı. Sorunları düzeltip tekrar yükleyin.";
    public const string KayitSorunluMesaji = "Ekipman kaydedilmedi; sorunları düzeltip tekrar deneyin.";

    public async Task<Result<EkipmanKonfigurasyonDetayi>> Handle(EkipmanKonfigurasyonEkleCommand request, CancellationToken cancellationToken) =>
        KayitSonucu(await service.EkleAsync(request.Kayit(), currentUser.UserId, cancellationToken));

    public async Task<Result<EkipmanKonfigurasyonDetayi>> Handle(EkipmanKonfigurasyonGuncelleCommand request, CancellationToken cancellationToken) =>
        KayitSonucu(await service.GuncelleAsync(
            request.KonfigurasyonId, request.BeklenenRevizyonNo, request.Kayit(), currentUser.UserId, cancellationToken));

    private static Result<EkipmanKonfigurasyonDetayi> KayitSonucu(EkipmanKayitSonucu sonuc) =>
        sonuc.Detay is { } detay
            ? Result<EkipmanKonfigurasyonDetayi>.Success(detay)
            : Result<EkipmanKonfigurasyonDetayi>.Failure(KayitSorunluMesaji, 400, sonuc.Sorunlar);

    public async Task<Result<EkipmanIceAktarimSonucu>> Handle(EkipmanIceAktarCommand request, CancellationToken cancellationToken)
    {
        var sonuc = await service.IceAktarAsync(request.Dosya, uygula: true, currentUser.UserId, cancellationToken);
        return sonuc.Uygulandi
            ? Result<EkipmanIceAktarimSonucu>.Success(sonuc)
            : Result<EkipmanIceAktarimSonucu>.Failure(IceAktarimSorunluMesaji, 400, sonuc.Sorunlar);
    }

    public async Task<Result<EkipmanKonfigurasyonDetayi>> Handle(EkipmanDogrulaCommand request, CancellationToken cancellationToken) =>
        currentUser.UserId is { } kullaniciId
            ? Result<EkipmanKonfigurasyonDetayi>.Success(
                await service.DogrulaAsync(request.KonfigurasyonId, request.RevizyonNo, kullaniciId, cancellationToken))
            : Result<EkipmanKonfigurasyonDetayi>.Failure("Oturum bulunamadı.", 401);
}
