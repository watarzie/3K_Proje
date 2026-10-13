using System.Text.RegularExpressions;
using _3K.Core.Constants;

namespace _3K.Application.Tests;

/// <summary>
/// Testlerin kullandığı Finans izin hiyerarşisi: kök → sekme düğümü (ve vade uyarısı). Finans yetkisi yalnız sekme
/// düzeyindedir; sekmelerin altında işlem/alan kodu yoktur. <see cref="SekmeAltlari"/> yalnız sekme düğümlerini ekleyen
/// eski <c>20261008_03_Finans_Sekme_Yetkileri.sql</c> betiğinin taşıdığı alt kodları (<see cref="EskiKodlar"/>) tutar;
/// <see cref="BetiktekiAltKodlar"/> bu fikstürün betikle aynı kaldığını doğrulamak için betiği okur.
/// </summary>
internal static class FinansSekmeAgaci
{
    public const string BetikAdi = "20261008_03_Finans_Sekme_Yetkileri.sql";

    /// <summary>
    /// 08.10.2026 canlı ağacındaki eski Finans alt işlem/alan kodları. Uygulamadan kaldırıldılar (yetki artık yalnız
    /// sekme düzeyinde); yalnız eski göç betiğinin ve canlı ağaç dökümünün testlerde aynen kurulabilmesi için tutulur.
    /// </summary>
    public static class EskiKodlar
    {
        public const string GelirGoruntule = "finans-gelir-goruntule";
        public const string GiderGoruntule = "finans-gider-goruntule";
        public const string SiparisOperasyonGoruntule = "finans-siparis-goruntule";
        public const string RaporGoruntule = "finans-rapor-goruntule";
        public const string ManuelIsEkle = "finans-is-ekle";
        public const string IsIptal = "finans-is-iptal";
        public const string TarihDegistir = "finans-tarih-degistir";
        public const string PoGir = "finans-po-gir";
        public const string PoDegistir = "finans-po-degistir";
        public const string BirimFiyatGoruntule = "finans-birim-fiyat-goruntule";
        public const string BirimFiyatDegistir = "finans-birim-fiyat-degistir";
        public const string KarlilikGoruntule = "finans-karlilik-goruntule";
        public const string ExcelAktar = "finans-excel-aktar";
        public const string PdfAktar = "finans-pdf-aktar";
        public const string GiderEkle = "finans-gider-ekle";
        public const string GiderDuzenle = "finans-gider-duzenle";
        public const string GiderKutuphanesiYonet = "finans-gider-kutuphanesi-yonet";
        public const string IsKutuphanesiYonet = "finans-is-kutuphanesi-yonet";
        public const string DuzenliIsYonet = "finans-duzenli-is-yonet";
        public const string ParasalVeriGoruntule = "finans-parasal-veri-goruntule";
        public const string TutarGoruntule = "finans-tutar-goruntule";
        public const string KayitGoruntule = "finans-kayit-goruntule";
        public const string FaturaGir = "finans-fatura-gir";
        public const string FaturaDegistir = "finans-fatura-degistir";
        public const string FaturaIptal = "finans-fatura-iptal";
        public const string PoIptal = "finans-po-iptal";
        public const string GiderIptal = "finans-gider-iptal";
        public const string KaliciSil = "finans-kalici-sil";
        public const string BelgeYukle = "finans-belge-yukle";
        public const string BelgeIndir = "finans-belge-indir";
        public const string DenetimGor = "finans-denetim-gor";
        public const string FiyatlandirmaDegistir = "finans-fiyatlandirma-degistir";
        public const string OdemeKaydet = "finans-odeme-kaydet";
    }

    public static readonly IReadOnlyDictionary<string, string> SekmeAltlari = new Dictionary<string, string>
    {
        [EskiKodlar.SiparisOperasyonGoruntule] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.PoGir] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.PoDegistir] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.PoIptal] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.FaturaGir] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.FaturaDegistir] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.FaturaIptal] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.OdemeKaydet] = YetkiKodlari.Finans.SekmePoFatura,
        [EskiKodlar.GiderEkle] = YetkiKodlari.Finans.SekmeGiderler,
        [EskiKodlar.GiderDuzenle] = YetkiKodlari.Finans.SekmeGiderler,
        [EskiKodlar.GiderIptal] = YetkiKodlari.Finans.SekmeGiderler,
        [EskiKodlar.GiderKutuphanesiYonet] = YetkiKodlari.Finans.SekmeGiderKategorileri,
        [EskiKodlar.DuzenliIsYonet] = YetkiKodlari.Finans.SekmeTanimlar,
        [EskiKodlar.IsKutuphanesiYonet] = YetkiKodlari.Finans.SekmeTanimlar,
        [EskiKodlar.IsIptal] = YetkiKodlari.Finans.SekmeIsTakibi,
        [EskiKodlar.TarihDegistir] = YetkiKodlari.Finans.SekmeIsTakibi,
        [EskiKodlar.FiyatlandirmaDegistir] = YetkiKodlari.Finans.SekmeIsTakibi
    };

    public static readonly IReadOnlyList<string> Sekmeler =
    [
        YetkiKodlari.Finans.SekmePanel, YetkiKodlari.Finans.SekmeIsTakibi, YetkiKodlari.Finans.SekmePoFatura,
        YetkiKodlari.Finans.SekmeGiderler, YetkiKodlari.Finans.SekmeGiderKategorileri, YetkiKodlari.Finans.SekmeTanimlar,
        YetkiKodlari.Finans.SekmeRaporlar, YetkiKodlari.Finans.SekmeMunferitIsler
    ];

    /// <summary>Kodun üst düğümü: sekme düğümleri ve vade uyarısı dahil bütün finans kodları için modül kökü.</summary>
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
