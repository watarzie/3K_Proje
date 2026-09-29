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

/// <summary>
/// Mevcut aktif PO'ya yeni iş kalemi bağlar. Bu, aynı müşteri PO'su için sipariş açma işlemidir;
/// sipariş açma yetkisi ister.
/// </summary>
public sealed class FinansSiparisKalemEkleCommand : FinansCommand<FinansSiparisModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.PoGir;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
        => Model?.Kalemler?.Any(x => x.FinansUrunId.HasValue || x.BirimFiyat.HasValue || !string.IsNullOrWhiteSpace(x.ParaBirimi) || x.KdvOrani.HasValue) == true
            ? [MenuPermissionRequirement.Write(RequiredMenuKod), MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)]
            : [MenuPermissionRequirement.Write(RequiredMenuKod)];
    public int Id { get; init; }
    public FinansSiparisKalemEkleModel Model { get; init; } = null!;
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

/// <summary>Faturayı ödendi/ödenmedi işaretler (D3); tahsilat bilgisi dışarıdan gelir.</summary>
public sealed class FinansFaturaOdemeCommand : FinansCommand<FinansFaturaModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.OdemeKaydet;
    public int Id { get; init; }
    public FinansOdemeKaydetModel Model { get; init; } = null!;
}
