using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansSiparisOlusturCommand : FinansCommand<FinansSiparisModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.PoGir;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
        => Model?.Kalemler.Any(x => x.FinansUrunId.HasValue || x.BirimFiyat.HasValue || !string.IsNullOrWhiteSpace(x.ParaBirimi) || x.KdvOrani.HasValue) == true
            ? [MenuPermissionRequirement.Write(RequiredMenuKod), MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)]
            : [MenuPermissionRequirement.Write(RequiredMenuKod)];
    public FinansSiparisOlusturModel Model { get; init; } = null!;
}

public sealed class FinansSiparisGuncelleCommand : FinansCommand<FinansSiparisModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.PoDegistir;
    public int Id { get; init; }
    public FinansSiparisGuncelleModel Model { get; init; } = null!;
}

public sealed class FinansSiparisIptalCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.PoIptal;
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

public sealed class FinansFaturaOlusturCommand : FinansCommand<FinansFaturaModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaGir;
    public FinansFaturaOlusturModel Model { get; init; } = null!;
}

public sealed class FinansFaturaGuncelleCommand : FinansCommand<FinansFaturaModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaDegistir;
    public int Id { get; init; }
    public FinansFaturaGuncelleModel Model { get; init; } = null!;
}

public sealed class FinansFaturaIptalCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaIptal;
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}
