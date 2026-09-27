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

    public sealed class FinansDashboardQuery : FinansQuery<FinansDashboardDto>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.Modul;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansGelirOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GelirGoruntule;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansDurumTutarOzetiQuery : FinansQuery<FinansDurumTutarOzetiDto>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule)
        ];
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansGiderOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.GiderGoruntule;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansNetOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KarlilikGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KarlilikGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule)
        ];
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansProjelerQuery : FinansQuery<FinansSayfaliSonuc<FinansProjeOzetModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
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

    public sealed class FinansIsKayitlariQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
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

    public sealed class FinansOzelIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansOzelIsModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
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

    public sealed class FinansSiparisOperasyonQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturalamaSiparisleriQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaYonet;
        public FinansListeFiltre Filtre { get; init; } = new();
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

    public sealed class FinansFaturaOperasyonQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaYonet;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturaOperasyonDetayQuery : FinansQuery<FinansFaturaModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.FaturaYonet;
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

    public sealed class FinansAylikOperasyonIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
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

    public sealed class FinansUrunSecenekleriQuery : FinansQuery<IReadOnlyList<FinansUrunSecenekModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.Modul;
    }

    public sealed class FinansUrunKutuphaneQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
        public bool SadeceAktif { get; init; }
        public DateTime? TarifeTarihi { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansFiyatTarifeleriQuery : FinansQuery<FinansSayfaliSonuc<FinansFiyatTarifesiModel>>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.TarifeYonet;
        public int? UrunId { get; init; }
        public int? Yil { get; init; }
        public bool SadeceAktif { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansRaporVerisiQuery : FinansQuery<FinansRaporModel>
    {
        public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.KarlilikGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
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
