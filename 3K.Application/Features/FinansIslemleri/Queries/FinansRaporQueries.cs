using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansIsRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansGiderRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansAylikRaporDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeIsTakibi),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.KarlilikGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Format { get; init; } = "pdf";
    public int Yil { get; init; }
    public int Ay { get; init; }
    public IReadOnlyCollection<string> Gruplar { get; init; } = Array.Empty<string>();
}

public sealed class FinansSiparisDurumRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeIsTakibi),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

public sealed class FinansOzetRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Tur { get; init; } = "genel";
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>
/// Alacak (vade/tahsilat) raporu (D3): faturalar sipariş operasyonu verisidir (RR Q6) ve tutarları gelirdir.
/// </summary>
public sealed class FinansAlacakRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.SiparisOperasyonGoruntule),
        MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
        MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))
    ];
    public string Format { get; init; } = "pdf";
    public FinansListeFiltre Filtre { get; init; } = new();
}

/// <summary>
/// Rapor önizlemesi: aynı rapor dosyasının okuma izinleri (dışa aktarma izni hariç); satırlar dosyayla aynıdır.
/// </summary>
public sealed class FinansRaporOnizlemeQuery : FinansQuery<FinansRaporOnizlemeModel>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions => Tur switch
    {
        "is" =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.BirimFiyatGoruntule)
        ],
        "gider" =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GiderGoruntule)
        ],
        "alacak" =>
        [
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.SiparisOperasyonGoruntule),
            MenuPermissionRequirement.Read(YetkiKodlari.Finans.GelirGoruntule)
        ],
        _ => [MenuPermissionRequirement.Read(YetkiKodlari.Finans.SekmeRaporlar), MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule)]
    };
    public string Tur { get; init; } = "is";
    public FinansListeFiltre Filtre { get; init; } = new();
}

internal static class FinansRaporPermissionRules
{
    public static string ExportPermission(string? format)
        => string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase)
            ? YetkiKodlari.Finans.ExcelAktar
            : YetkiKodlari.Finans.PdfAktar;
}
