using System.Text.Json.Serialization;
using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;

/// <summary>
/// Planın son revizyonunun yerleşimiyle yükleme oturumu başlatır (arac-yukleme W). Plan arada revize edildiyse ya da
/// süren oturum varsa 409; son revizyonda yerleşim yoksa 400.
/// </summary>
public sealed class YuklemeOturumuBaslatCommand : IRequest<Result<AracYuklemeDurumu>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int PlanId { get; set; }

    public int BeklenenRevizyonNo { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>
/// Yükü araca yüklendi işaretler. Planlama kaydıdır: sandık durumunu ve sevki değiştirmez. Yük zaten yüklüyse yeni kayıt
/// yazılmaz; sıradaki olmayan yük <see cref="SiraDisiOnayi"/> olmadan 409 alır.
/// </summary>
public sealed class YukuYuklendiIsaretleCommand : IRequest<Result<YukIsaretSonucu>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int OturumId { get; set; }

    public int AracNo { get; set; }
    public string YukAnahtari { get; set; } = string.Empty;
    public YuklemeIsaretKaynagi Kaynak { get; set; }
    public bool SiraDisiOnayi { get; set; }

    /// <summary>Barkod kaynağında okutulan metin; sunucu çözer ve <see cref="YukAnahtari"/> ile eşleşmesini ister.</summary>
    public string? Barkod { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>Yükleme işaretini gerekçeyle geri alır (kayıt silinmez; yük yeniden işaretlenebilir).</summary>
public sealed class YukIsaretiGeriAlCommand : IRequest<Result<AracYuklemeAraci>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int OturumId { get; set; }

    [JsonIgnore]
    public int IsaretId { get; set; }

    public string Gerekce { get; set; } = string.Empty;

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

/// <summary>
/// Oturumu tamamlar ya da iptal eder; kapanan oturum yeniden açılmaz. İptalde ve yüklenmemiş yük varken tamamlamada
/// gerekçe zorunludur (eksik yük denetimi serviste, güncel sayıyla yapılır).
/// </summary>
public sealed class YuklemeOturumuKapatCommand : IRequest<Result<AracYuklemeDurumu>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int OturumId { get; set; }

    /// <summary>İptal mi (rota belirler); değilse tamamlama.</summary>
    [JsonIgnore]
    public bool Iptal { get; set; }

    public string? Gerekce { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.AracYukleme;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

public sealed class AracYuklemeCommandHandlers(IYuklemeOturumuService service, ICurrentUserService currentUser) :
    IRequestHandler<YuklemeOturumuBaslatCommand, Result<AracYuklemeDurumu>>,
    IRequestHandler<YukuYuklendiIsaretleCommand, Result<YukIsaretSonucu>>,
    IRequestHandler<YukIsaretiGeriAlCommand, Result<AracYuklemeAraci>>,
    IRequestHandler<YuklemeOturumuKapatCommand, Result<AracYuklemeDurumu>>
{
    public async Task<Result<AracYuklemeDurumu>> Handle(YuklemeOturumuBaslatCommand request, CancellationToken cancellationToken) =>
        Result<AracYuklemeDurumu>.Success(
            await service.BaslatAsync(request.PlanId, request.BeklenenRevizyonNo, currentUser.UserId, cancellationToken));

    public async Task<Result<YukIsaretSonucu>> Handle(YukuYuklendiIsaretleCommand request, CancellationToken cancellationToken) =>
        Result<YukIsaretSonucu>.Success(await service.IsaretleAsync(
            new YukIsaretleModeli(request.OturumId, request.AracNo, request.YukAnahtari, request.Kaynak, request.SiraDisiOnayi, request.Barkod),
            currentUser.UserId, cancellationToken));

    public async Task<Result<AracYuklemeAraci>> Handle(YukIsaretiGeriAlCommand request, CancellationToken cancellationToken) =>
        Result<AracYuklemeAraci>.Success(
            await service.GeriAlAsync(request.OturumId, request.IsaretId, request.Gerekce, currentUser.UserId, cancellationToken));

    public async Task<Result<AracYuklemeDurumu>> Handle(YuklemeOturumuKapatCommand request, CancellationToken cancellationToken) =>
        Result<AracYuklemeDurumu>.Success(
            await service.KapatAsync(request.OturumId, request.Iptal, request.Gerekce, currentUser.UserId, cancellationToken));
}
