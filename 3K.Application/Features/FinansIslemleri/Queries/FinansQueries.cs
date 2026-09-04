using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.DTOs;
using _3K.Application.Features.FinansIslemleri;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries
{
    public abstract class FinansQuery<T> : IRequest<Result<T>>, ISecuredRequest, IRequiresMenuPermission, IRequiresMenuPermissions
    {
        public abstract string RequiredMenuKod { get; }
        public virtual IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Array.Empty<MenuPermissionRequirement>();
    }

    public sealed class FinansDashboardQuery : FinansQuery<FinansDashboardDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.Modul;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansGelirOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansDurumTutarOzetiQuery : FinansQuery<FinansDurumTutarOzetiDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansGiderOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderGoruntule;
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansNetOzetiQuery : FinansQuery<FinansHassasOzetDto>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.KarlilikGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.KarlilikGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GiderGoruntule)
        ];
        public DateTime? Baslangic { get; init; }
        public DateTime? Bitis { get; init; }
    }

    public sealed class FinansProjelerQuery : FinansQuery<FinansSayfaliSonuc<FinansProjeOzetModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansProjeSecenekleriQuery : FinansQuery<FinansSayfaliSonuc<FinansProjeSecenekModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.Modul;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
            [FinansYetkiKodlari.Read(FinansYetkiKodlari.Modul)];
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansIsKayitlariQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansIsKaydiGetirQuery : FinansQuery<FinansIsKaydiModel>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public int Id { get; init; }
    }

    public sealed class FinansIsKayitlariSecimQuery : FinansQuery<IReadOnlyList<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public IReadOnlyCollection<int> Ids { get; init; } = Array.Empty<int>();
    }

    public sealed class FinansOzelIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansOzelIsModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansSiparislerQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansSiparisGetirQuery : FinansQuery<FinansSiparisModel>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public int Id { get; init; }
    }

    public sealed class FinansSiparisOperasyonQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.SiparisOperasyonGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturalamaSiparisleriQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.FaturaYonet;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturalarQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturaGetirQuery : FinansQuery<FinansFaturaModel>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public int Id { get; init; }
    }

    public sealed class FinansFaturaOperasyonQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.FaturaYonet;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansFaturaOperasyonDetayQuery : FinansQuery<FinansFaturaModel>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.FaturaYonet;
        public int Id { get; init; }
    }

    public sealed class FinansDuzenliIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansDuzenliIsModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.DuzenliIsYonet;
        public bool SadeceAktif { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansAylikIslerQuery : FinansQuery<FinansAylikSayfaliSonuc>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GelirGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public int Yil { get; init; }
        public int Ay { get; init; }
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansAylikOperasyonIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.SiparisOperasyonGoruntule;
        public int Yil { get; init; }
        public int Ay { get; init; }
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderlerQuery : FinansQuery<FinansSayfaliSonuc<FinansGiderModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderGoruntule;
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansGiderKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderGoruntule;
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderGoruntule;
        public int? KategoriId { get; init; }
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKutuphaneKategorileriQuery : FinansQuery<IReadOnlyList<FinansGiderKategoriModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderKutuphanesiYonet;
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansGiderKutuphaneKalemleriQuery : FinansQuery<IReadOnlyList<FinansGiderKalemiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.GiderKutuphanesiYonet;
        public int? KategoriId { get; init; }
        public bool SadeceAktif { get; init; }
    }

    public sealed class FinansUrunlerQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.IsKutuphanesiYonet;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.IsKutuphanesiYonet),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public bool SadeceAktif { get; init; }
        public DateTime? TarifeTarihi { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansUrunSecenekleriQuery : FinansQuery<IReadOnlyList<FinansUrunSecenekModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.Modul;
    }

    public sealed class FinansUrunKutuphaneQuery : FinansQuery<FinansSayfaliSonuc<FinansUrunModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.IsKutuphanesiYonet;
        public bool SadeceAktif { get; init; }
        public DateTime? TarifeTarihi { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansFiyatTarifeleriQuery : FinansQuery<FinansSayfaliSonuc<FinansFiyatTarifesiModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.TarifeYonet;
        public int? UrunId { get; init; }
        public int? Yil { get; init; }
        public bool SadeceAktif { get; init; }
        public string? Arama { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }

    public sealed class FinansRaporVerisiQuery : FinansQuery<FinansRaporModel>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GiderGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.KarlilikGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public FinansListeFiltre Filtre { get; init; } = new();
    }

    public sealed class FinansDegisiklikGecmisiQuery : FinansQuery<FinansSayfaliSonuc<FinansDegisiklikModel>>
    {
        public override string RequiredMenuKod => FinansYetkiKodlari.RaporGoruntule;
        public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [
            FinansYetkiKodlari.Read(FinansYetkiKodlari.RaporGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GelirGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.GiderGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.KarlilikGoruntule),
            FinansYetkiKodlari.Read(FinansYetkiKodlari.BirimFiyatGoruntule)
        ];
        public string? VarlikTuru { get; init; }
        public int? VarlikId { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 25;
    }
}
