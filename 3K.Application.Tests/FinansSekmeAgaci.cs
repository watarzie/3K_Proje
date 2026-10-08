using System.Text.RegularExpressions;
using _3K.Core.Constants;

namespace _3K.Application.Tests;

/// <summary>
/// Testlerin kullandığı Finans izin hiyerarşisi: kök → sekme düğümü → sekmeye ait işlem kodu. Canlı ağaç
/// <c>20261008_03_Finans_Sekme_Yetkileri.sql</c> ile kurulur; <see cref="BetiktekiAltKodlar"/> bu fikstürün
/// betikle aynı kaldığını doğrulamak için betiği okur.
/// </summary>
internal static class FinansSekmeAgaci
{
    public const string BetikAdi = "20261008_03_Finans_Sekme_Yetkileri.sql";

    public static readonly IReadOnlyDictionary<string, string> SekmeAltlari = new Dictionary<string, string>
    {
        [YetkiKodlari.Finans.SiparisOperasyonGoruntule] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.PoGir] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.PoDegistir] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.PoIptal] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.FaturaGir] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.FaturaDegistir] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.FaturaIptal] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.OdemeKaydet] = YetkiKodlari.Finans.SekmePoFatura,
        [YetkiKodlari.Finans.GiderEkle] = YetkiKodlari.Finans.SekmeGiderler,
        [YetkiKodlari.Finans.GiderDuzenle] = YetkiKodlari.Finans.SekmeGiderler,
        [YetkiKodlari.Finans.GiderIptal] = YetkiKodlari.Finans.SekmeGiderler,
        [YetkiKodlari.Finans.GiderKutuphanesiYonet] = YetkiKodlari.Finans.SekmeGiderKategorileri,
        [YetkiKodlari.Finans.DuzenliIsYonet] = YetkiKodlari.Finans.SekmeTanimlar,
        [YetkiKodlari.Finans.IsKutuphanesiYonet] = YetkiKodlari.Finans.SekmeTanimlar,
        [YetkiKodlari.Finans.IsIptal] = YetkiKodlari.Finans.SekmeIsTakibi,
        [YetkiKodlari.Finans.TarihDegistir] = YetkiKodlari.Finans.SekmeIsTakibi,
        [YetkiKodlari.Finans.FiyatlandirmaDegistir] = YetkiKodlari.Finans.SekmeIsTakibi
    };

    public static readonly IReadOnlyList<string> Sekmeler =
    [
        YetkiKodlari.Finans.SekmePanel, YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.SekmePoFatura,
        YetkiKodlari.Finans.SekmeGiderler, YetkiKodlari.Finans.SekmeGiderKategorileri, YetkiKodlari.Finans.SekmeTanimlar,
        YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.SekmeMunferitIsler
    ];

    /// <summary>Kodun üst düğümü: sekmeye ait kodlar için sekme, diğer finans kodları için modül kökü.</summary>
    public static string? Ust(string kod) =>
        kod == YetkiKodlari.Finans.Modul ? null
        : SekmeAltlari.TryGetValue(kod, out var sekme) ? sekme
        : kod.StartsWith("finans-", StringComparison.Ordinal) ? YetkiKodlari.Finans.Modul
        : null;

    public static string BetikYolu()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !File.Exists(Path.Combine(dizin.FullName, "scripts", "database", BetikAdi)))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);
        return Path.Combine(dizin.FullName, "scripts", "database", BetikAdi);
    }

    /// <summary>Betikteki <c>finans_sekme_altlari</c> satırları (alt kod → sekme kodu).</summary>
    public static IReadOnlyDictionary<string, string> BetiktekiAltKodlar()
    {
        var betik = File.ReadAllText(BetikYolu());
        var blok = betik[betik.IndexOf("INSERT INTO finans_sekme_altlari", StringComparison.Ordinal)..];
        blok = blok[..blok.IndexOf(';')];
        return Regex.Matches(blok, @"\('(?<alt>finans-[a-z-]+)',\s*'(?<sekme>finans-sekme-[a-z-]+)'\)")
            .ToDictionary(x => x.Groups["alt"].Value, x => x.Groups["sekme"].Value);
    }
}
