using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansDuzenliIslerQuery : FinansQuery<FinansSayfaliSonuc<FinansDuzenliIsModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.DuzenliIsYonet;
    public bool SadeceAktif { get; init; }
    public string? Arama { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
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

/// <summary>
/// Hatalı tarife düzeltmenin önizlemesi; yalnız düzeltmeyi yapabilen (birim fiyat değiştirme yazma yetkisi)
/// kullanıcıya açıktır. Fiyat/tutar alanları ayrıca HTTP sınırında alan izinleriyle maskelenir.
/// </summary>
public sealed class FinansTarifeDuzeltmeOnizlemeQuery : FinansQuery<FinansTarifeDuzeltmeOnizlemeModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.BirimFiyatDegistir;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
        [MenuPermissionRequirement.Write(YetkiKodlari.Finans.BirimFiyatDegistir)];
    public int UrunId { get; init; }
    public int TarifeId { get; init; }
    public decimal BirimFiyat { get; init; }
    public decimal KdvOrani { get; init; }
}

/// <summary>
/// Münferit iş formunun ürün seçicisi: aktif ürünlerin kod/ad/fiyatlandırma birimi (fiyatsız, en fazla 50).
/// Münferit İşler sekmesini görebilen kullanıcıya açıktır; tarife listesi yetkisi istemez.
/// </summary>
public sealed class FinansUrunSecenekleriQuery : FinansQuery<IReadOnlyList<FinansUrunSecenekModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.SekmeMunferitIsler;
    public string? Arama { get; init; }
    public _3K.Core.Enums.FinansIsTuru? IsTuru { get; init; }
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

/// <summary>Para birimi tanımları (F1); seçim listeleri, kartlar ve filtreler için finans modülüne erişen herkes okur.</summary>
public sealed class FinansParaBirimleriQuery : FinansQuery<IReadOnlyList<FinansParaBirimiModel>>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.Modul;
    public bool SadeceAktif { get; init; }
}

/// <summary>Vade parametreleri yalnız "İş ve Tarife Tanımları" ekranında okunur; yönetimiyle aynı kod (R).</summary>
public sealed class FinansParametrelerQuery : FinansQuery<FinansParametrelerModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.IsKutuphanesiYonet;
}
