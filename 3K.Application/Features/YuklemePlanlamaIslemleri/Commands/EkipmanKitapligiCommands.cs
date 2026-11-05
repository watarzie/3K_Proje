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

public sealed class EkipmanKitapligiCommandHandlers(IEkipmanKitapligiService service, ICurrentUserService currentUser) :
    IRequestHandler<EkipmanIceAktarCommand, Result<EkipmanIceAktarimSonucu>>,
    IRequestHandler<EkipmanDogrulaCommand, Result<EkipmanKonfigurasyonDetayi>>
{
    public const string IceAktarimSorunluMesaji = "Dosyada sorun var; hiçbir kayıt yazılmadı. Sorunları düzeltip tekrar yükleyin.";

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
