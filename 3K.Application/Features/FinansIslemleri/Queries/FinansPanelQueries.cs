using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansPanelQuery : FinansQuery<FinansPanelModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.Modul;
    public DateTime Baslangic { get; init; }
    public DateTime Bitis { get; init; }
}

public sealed class FinansHareketlerQuery : FinansQuery<FinansSayfaliSonuc<FinansHareketModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public FinansListeFiltre Filtre { get; init; } = new();
    public string? Tur { get; init; }
}

public sealed class FinansGenelAramaQuery : FinansQuery<FinansSayfaliSonuc<FinansIsKaydiModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.KayitGoruntule;
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansYaslandirmaQuery : FinansQuery<FinansSayfaliSonuc<FinansBekleyenModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public FinansListeFiltre Filtre { get; init; } = new();
    public DateTime ReferansTarihi { get; init; }
    public int MinimumGun { get; init; }
    public string? Asama { get; init; }
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
