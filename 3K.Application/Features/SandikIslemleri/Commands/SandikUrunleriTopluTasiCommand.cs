using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.SandikIslemleri.Commands;

public sealed class SandikUrunleriTopluTasiCommand : IRequest<Result>, ISecuredRequest, IRequiresSandikMenuPermission
{
    public int ProjeId { get; set; }
    public int KaynakSandikId { get; set; }
    public int HedefSandikId { get; set; }
    public Guid IslemAnahtari { get; set; }
    public List<SandikTopluTasimaSatiri> Satirlar { get; set; } = new();

    public IReadOnlyCollection<int> PermissionSandikIds => new[] { KaynakSandikId, HedefSandikId };
    public ProjectMenuOperation PermissionOperation => ProjectMenuOperation.CrateWrite;
    public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
}

public sealed class SandikTopluTasimaSatiri
{
    public int KaynakSandikIcerikId { get; set; }
    public decimal TasinanAdet { get; set; }
}
