using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Queries;

public sealed class FinansIsRaporuDosyaQuery : FinansQuery<DosyaIcerigi>
{
    public override string RequiredMenuKod => YetkiKodlari.Finans.RaporGoruntule;
    public override IReadOnlyCollection<MenuPermissionRequirement> RequiredMenuPermissions =>
    [
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
        [MenuPermissionRequirement.Read(YetkiKodlari.Finans.RaporGoruntule), MenuPermissionRequirement.Read(FinansRaporPermissionRules.ExportPermission(Format))];
    public string Tur { get; init; } = "genel";
    public string Format { get; init; } = "pdf";
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
