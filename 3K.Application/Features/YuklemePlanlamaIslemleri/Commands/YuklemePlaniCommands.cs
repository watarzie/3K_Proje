using System.Text.Json.Serialization;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.YuklemePlanlamaIslemleri.DTOs;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;

/// <summary>Oluşturma ve kaydetme komutlarının ortak plan başlığı (doğrulama kuralları ortak).</summary>
public interface IYuklemePlaniBasligi
{
    string Ad { get; }
    YuklemeCalismaBicimi CalismaBicimi { get; }
    int TasimaAilesiId { get; }
    YuklemeProjeKarisimi ProjeKarisimi { get; }
    string? Aciklama { get; }
    List<int> ProjeIdleri { get; }
}

/// <summary>
/// Yeni yükleme planı (yukleme-planlari W): seçilen projelerin sevk edilmemiş bütün sandıkları seçili ve varsayılan
/// izinlerle gelir (isterler 3, 6); ilk revizyon yazılır.
/// </summary>
public sealed class YuklemePlaniOlusturCommand : IRequest<Result<YuklemePlaniDetayi>>, ISecuredRequest, IRequiresMenuPermission, IYuklemePlaniBasligi
{
    public string Ad { get; set; } = string.Empty;
    public YuklemeCalismaBicimi CalismaBicimi { get; set; }
    public int TasimaAilesiId { get; set; }
    public YuklemeProjeKarisimi ProjeKarisimi { get; set; }
    public DateTime? YuklemeTarihi { get; set; }
    public string? Aciklama { get; set; }
    public List<int> ProjeIdleri { get; set; } = [];

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;

    public YuklemePlaniOlusturModel Model() =>
        new(Ad.Trim(), CalismaBicimi, TasimaAilesiId, ProjeKarisimi, YuklemeTarihi,
            string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(), ProjeIdleri.Distinct().ToList());
}

/// <summary>
/// Planı kaydetme (yukleme-planlari W): yeni değişmez revizyon. Arada başka biri kaydettiyse (beklenen revizyon son
/// değilse) 409 ve hiçbir şey yazılmaz.
/// </summary>
public sealed class YuklemePlaniKaydetCommand : IRequest<Result<YuklemePlaniDetayi>>, ISecuredRequest, IRequiresMenuPermission, IYuklemePlaniBasligi
{
    /// <summary>Rotadan gelir.</summary>
    [JsonIgnore]
    public int PlanId { get; set; }

    public int BeklenenRevizyonNo { get; set; }
    public string Ad { get; set; } = string.Empty;
    public YuklemeCalismaBicimi CalismaBicimi { get; set; }
    public int TasimaAilesiId { get; set; }
    public YuklemeProjeKarisimi ProjeKarisimi { get; set; }
    public DateTime? YuklemeTarihi { get; set; }
    public string? Aciklama { get; set; }
    public string? RevizyonAciklamasi { get; set; }
    public YuklemePlanAyarlari Ayarlar { get; set; } = new();
    public List<int> ProjeIdleri { get; set; } = [];
    public List<PlanSandikYukuDto> SandikYukleri { get; set; } = [];
    public List<PlanManuelYukDto> ManuelYukler { get; set; } = [];

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;

    public YuklemePlaniKaydetModel Model() =>
        new(PlanId, BeklenenRevizyonNo, Ad.Trim(), CalismaBicimi, TasimaAilesiId, ProjeKarisimi, YuklemeTarihi,
            string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(),
            string.IsNullOrWhiteSpace(RevizyonAciklamasi) ? null : RevizyonAciklamasi.Trim(),
            Ayarlar, ProjeIdleri.Distinct().ToList(),
            SandikYukleri.Select(y => y.Model()).ToList(),
            ManuelYukler.Select(y => y.Model()).ToList());
}

/// <summary>
/// Kaynak farklarını uygulayan yeni revizyon (yukleme-planlari W): değişen ölçü/ağırlık/lokasyon/yük türü kopyalanır,
/// elle değiştirilmemiş izinler yeni varsayılandan kurulur, yeni sandık eklenir, silinen veya sevk edilen çıkar.
/// </summary>
public sealed class YuklemePlaniKaynaktanGuncelleCommand : IRequest<Result<YuklemePlaniDetayi>>, ISecuredRequest, IRequiresMenuPermission
{
    [JsonIgnore]
    public int PlanId { get; set; }

    public int BeklenenRevizyonNo { get; set; }

    public string RequiredMenuKod => YetkiKodlari.YuklemePlanlama.Planlar;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

public sealed class YuklemePlaniCommandHandlers(IYuklemePlaniService service, ICurrentUserService currentUser) :
    IRequestHandler<YuklemePlaniOlusturCommand, Result<YuklemePlaniDetayi>>,
    IRequestHandler<YuklemePlaniKaydetCommand, Result<YuklemePlaniDetayi>>,
    IRequestHandler<YuklemePlaniKaynaktanGuncelleCommand, Result<YuklemePlaniDetayi>>
{
    public async Task<Result<YuklemePlaniDetayi>> Handle(YuklemePlaniOlusturCommand request, CancellationToken cancellationToken) =>
        Result<YuklemePlaniDetayi>.Success(await service.OlusturAsync(request.Model(), currentUser.UserId, cancellationToken));

    public async Task<Result<YuklemePlaniDetayi>> Handle(YuklemePlaniKaydetCommand request, CancellationToken cancellationToken) =>
        Result<YuklemePlaniDetayi>.Success(await service.KaydetAsync(request.Model(), currentUser.UserId, cancellationToken));

    public async Task<Result<YuklemePlaniDetayi>> Handle(YuklemePlaniKaynaktanGuncelleCommand request, CancellationToken cancellationToken) =>
        Result<YuklemePlaniDetayi>.Success(
            await service.KaynaktanGuncelleAsync(request.PlanId, request.BeklenenRevizyonNo, currentUser.UserId, cancellationToken));
}
