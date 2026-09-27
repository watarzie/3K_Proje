using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Core.Constants;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public abstract class FinansQuery<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public abstract string RequiredMenuKod { get; }
        public YetkiTipi RequiredYetkiTipi => YetkiTipi.R;
        public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
    }

    public sealed class FinansProjeSecenekleriQuery : FinansQuery<FinansSayfaliSonuc<FinansProjeSecenekModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.Modul;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [MenuPermissionRequirement.Read(YetkiKodlari.Finans.Modul)];
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansIsKaydiGetirQuery : FinansQuery<FinansIsKaydiModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public int Id { get; init; }
    }

    public sealed class FinansIsKayitlariSecimQuery : FinansQuery<IReadOnlyList<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public IReadOnlyCollection<int> Ids { get; init; } = Array.Empty<int>();
    }

    public sealed class FinansSiparislerQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansSiparisGetirQuery : FinansQuery<FinansSiparisModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public int Id { get; init; }
    }

    public sealed class FinansFaturalarQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturaGetirQuery : FinansQuery<FinansFaturaModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public int Id { get; init; }
    }

    public sealed class FinansDuzenliIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansDuzenliIsModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
        public bool SadeceAktif { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansAylikIslerQuery : FinansQuery<FinansAylikSayfaliSonuc>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public int Yil { get; init; }
        public int Ay { get; init; }
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderlerQuery : FinansQuery<FinansSayfaliSonuc<FinansGiderModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
        public int? KategoriId { get; init; }
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKutuphaneKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKutuphaneKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderKutuphanesiYonet;
        public int? KategoriId { get; init; }
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansUrunlerQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.IsKutuphanesiYonet),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule)
        ];
        public bool SadeceAktif { get; init; }
        public DateTime? TarifeTarihi { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansDegisiklikGecmisiQuery : FinansQuery<FinansSayfaliSonuc<FinansDegisiklikModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.DenetimGor;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.DenetimGor)
        ];
        public string? VarlikTuru { get; init; }
        public int? VarlikId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }
}
