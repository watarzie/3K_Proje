using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

public sealed class FinansIsKaydiOlusturCommand : FinansCommand<FinansIsKaydiModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.ManuelIsEkle;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
        => FinansCommandPermissionRules.RequiresPriceOverride(Model)
            ? [MenuPermissionRequirement.Write(RequiredMenuKod), MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)]
            : [MenuPermissionRequirement.Write(RequiredMenuKod)];
    public FinansIsKaydiKaydetModel Model { get; init; } = null!;
}

public sealed class FinansIsKaydiIptalCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsIptal;
    public int Id { get; init; }
    public string Aciklama { get; init; } = string.Empty;
}

public sealed class FinansIsKaydiGeriAlCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsIptal;
    public int Id { get; init; }
}

public sealed class FinansOzelIsAylikDegerGuncelleCommand : FinansCommand
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.FiyatlandirmaDegistir;
    public int Id { get; init; }
    public FinansAylikDegerModel Model { get; init; } = null!;
}

public sealed class FinansTarihiDegistirCommand : FinansCommand<FinansIsKaydiModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.TarihDegistir;
    public int Id { get; init; }
    public FinansTarihiDegistirModel Model { get; init; } = null!;
}

public sealed class FinansFiyatlandirCommand : FinansCommand<FinansIsKaydiModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.FiyatlandirmaDegistir;
    public int Id { get; init; }
    public FinansFiyatlandirmaModel Model { get; init; } = null!;
}
