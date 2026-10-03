using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

/// <summary>
/// PO ve fatura kayıtları sipariş operasyonu verisidir: kayıt görüntüleme iznine ek olarak
/// finans-siparis-goruntule okuma izni sunucuda istenir (RR Q6).
/// </summary>
internal static class FinansSiparisFaturaIzinleri
{
    public static readonly IReadOnlyCollection<MenuPermissionRequirement> Okuma =
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.KayitGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SiparisOperasyonGoruntule)
    ];
}

public sealed class FinansSiparislerQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>PO açma ekranındaki proje × kategori / düzenli iş seçicisi.</summary>
public sealed class FinansSiparisGruplariQuery : FinansQuery<FinansSayfaliSonuc<FinansSiparisGrubuModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public FinansSiparisGrupFiltre Filtre { get; init; } = new();
}

public sealed class FinansSiparisGetirQuery : FinansQuery<FinansSiparisModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public int Id { get; init; }
}

public sealed class FinansFaturalarQuery : FinansQuery<FinansSayfaliSonuc<FinansFaturaModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansFaturaGetirQuery : FinansQuery<FinansFaturaModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public int Id { get; init; }
}

/// <summary>Vadesi geçen ve uyarı penceresinde vadesi gelen ödenmemiş faturalar (D3 uyarısı).</summary>
public sealed class FinansVadeUyarilariQuery : FinansQuery<FinansVadeUyarilariModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SiparisOperasyonGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => FinansSiparisFaturaIzinleri.Okuma;
    public int EnFazlaKalem { get; init; } = 20;
}
