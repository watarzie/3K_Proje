using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.DTOs;
using _3K.Core.Enums;

namespace _3K.Application.Features.SevkiyatIslemleri.Commands;

/// <summary>
/// Çok projeli toplu sevk (tır seferi): seçilen projelerin seçilen sandıkları tek işlemde, hepsi ya da hiçbiri sevk
/// edilir. Yetki her sandığın projesinin gerçek tipine göre o tipin sevk etme (W) iznidir; bütün projeler için aranır.
/// Aynı <see cref="IslemAnahtari"/> ile aynı istek tekrarlanırsa sefer yeniden oluşmaz.
/// </summary>
public sealed class TopluSevkEtCommand : IRequest<Result<TopluSevkSonucDto>>, ISecuredRequest, IRequiresSandikMenuPermission,
    ISevkSeferiBilgisi
{
    public Guid IslemAnahtari { get; set; }
    public DateTime SevkTarihi { get; set; }
    public string AracPlaka { get; set; } = string.Empty;
    public string? Sofor { get; set; }
    public string? DorsePlaka { get; set; }
    public string? Aciklama { get; set; }
    public List<TopluSevkProjesi> Projeler { get; set; } = new();

    DateTime? ISevkSeferiBilgisi.SeferSevkTarihi => SevkTarihi;

    public IReadOnlyCollection<int> PermissionSandikIds => Projeler.SelectMany(p => p.SandikIds).ToArray();
    public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.Shipment;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

public sealed class TopluSevkProjesi
{
    public int ProjeId { get; set; }
    public List<int> SandikIds { get; set; } = new();
}
