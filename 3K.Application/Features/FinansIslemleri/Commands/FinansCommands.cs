using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.FinansIslemleri.Commands
{
    public abstract class FinansCommand<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public abstract string RequiredMenuKod { get; }
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
    }

    public abstract class FinansCommand : IRequest<Result>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public abstract string RequiredMenuKod { get; }
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.W;
        public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
    }

    public sealed class FinansIsKaydiOlusturCommand : FinansCommand<FinansIsKaydiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.ManuelIsEkle;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
            => FinansCommandPermissionRules.RequiresPriceOverride(Model)
                ? [MenuPermissionRequirement.Write(RequiredMenuKod), MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)]
                : [MenuPermissionRequirement.Write(RequiredMenuKod)];
        public FinansIsKaydiKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansIsKaydiGuncelleCommand : FinansCommand<FinansIsKaydiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.ManuelIsDuzenle;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions
            => FinansCommandPermissionRules.RequiresPriceOverride(Model)
                ? [MenuPermissionRequirement.Write(RequiredMenuKod), MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)]
                : [MenuPermissionRequirement.Write(RequiredMenuKod)];
        public int Id { get; init; }
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

    /// <summary>
    /// Yalnız uygulama içi üretim handler'ları içindir; controller tarafından dışarı açılmaz.
    /// Üretim kullanıcısının ayrıca finans rolü taşımasını zorunlu kılmamak için secured request değildir.
    /// </summary>
    public sealed class FinansUretimAktarCommand : IRequest<Result<FinansSenkronizasyonSonucModel>>
    {
        public IReadOnlyList<FinansUretimAktarimModel> Kayitlar { get; init; } = Array.Empty<FinansUretimAktarimModel>();
    }

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

    public sealed class FinansSiparisGeriAlCommand : FinansCommand
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.PoDegistir;
        public int Id { get; init; }
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

    public sealed class FinansFaturaGeriAlCommand : FinansCommand
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaYonet;
        public int Id { get; init; }
    }

    public sealed class FinansDuzenliIsOlusturCommand : FinansCommand<FinansDuzenliIsModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
        public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansDuzenliIsGuncelleCommand : FinansCommand<FinansDuzenliIsModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
        public int Id { get; init; }
        public FinansDuzenliIsKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansDuzenliIsDonemOlusturCommand : FinansCommand<FinansDonemOlusturSonucModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
        public DateTime ReferansTarihi { get; init; }
    }

    public sealed class FinansGiderOlusturCommand : FinansCommand<FinansGiderModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderEkle;
        public FinansGiderKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGiderGuncelleCommand : FinansCommand<FinansGiderModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderDuzenle;
        public int Id { get; init; }
        public FinansGiderKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGiderIptalCommand : FinansCommand
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderIptal;
        public int Id { get; init; }
        public string Aciklama { get; init; } = string.Empty;
    }

    public sealed class FinansGiderGeriAlCommand : FinansCommand
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderDuzenle;
        public int Id { get; init; }
    }

    public sealed class FinansGiderKategoriOlusturCommand : FinansCommand<FinansGiderKategoriModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGiderKategoriGuncelleCommand : FinansCommand<FinansGiderKategoriModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public int Id { get; init; }
        public FinansGiderKategoriKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGiderKalemiOlusturCommand : FinansCommand<FinansGiderKalemiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGiderKalemiGuncelleCommand : FinansCommand<FinansGiderKalemiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public int Id { get; init; }
        public FinansGiderKalemiKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansGideriKutuphaneyeKaydetCommand : FinansCommand<FinansGiderKalemiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public int GiderId { get; init; }
        public FinansGideriKutuphaneyeKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansUrunOlusturCommand : FinansCommand<FinansUrunModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
        public FinansUrunKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansUrunGuncelleCommand : FinansCommand<FinansUrunModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
        public int Id { get; init; }
        public FinansUrunKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansUrunPasiflestirCommand : FinansCommand
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
        public int Id { get; init; }
    }

    public sealed class FinansFiyatTarifesiOlusturCommand : FinansCommand<FinansFiyatTarifesiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.TarifeYonet;
        public FinansFiyatTarifesiKaydetModel Model { get; init; } = null!;
    }

    public sealed class FinansFiyatTarifesiGuncelleCommand : FinansCommand<FinansFiyatTarifesiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.TarifeYonet;
        public int Id { get; init; }
        public FinansFiyatTarifesiKaydetModel Model { get; init; } = null!;
    }

    internal static class FinansCommandPermissionRules
    {
        public static bool RequiresPriceOverride(FinansIsKaydiKaydetModel? model)
            => model is not null &&
               (model.ManuelBirimFiyat.HasValue || !string.IsNullOrWhiteSpace(model.ParaBirimi) || model.KdvOrani.HasValue);
    }
}
