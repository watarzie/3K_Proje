using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Kaynak sandıktan ambalaj kaydına aktarılan ad, cins ve milimetre dış ölçü.</summary>
public readonly record struct AmbalajKaynakSandikDegerleri(
    string? Ad,
    AmbalajSandikCinsi Cins,
    decimal Boy,
    decimal En,
    decimal Yukseklik);

/// <summary>Kaydı henüz olmayan kaynak sandığın ilk ambalaj grubu ve ambalaj kararı.</summary>
public readonly record struct AmbalajYeniKaynakKarari(AmbalajSandikTuru Grup, bool AmbalajaDahil);

public static class AmbalajUretimPolitikasi
{
    public static bool M3HesaplanabilirMi(AmbalajSandikCinsi cins) =>
        cins is AmbalajSandikCinsi.AhsapKapali or AmbalajSandikCinsi.Kafes;

    /// <summary>
    /// Kayıttaki ölçü sandığın dış ölçüsü mü? Kaynak sandıktan gelen kayıt ve İlave/Saha/Yedek özel sandık, sandık
    /// modülündeki gibi dış ölçü taşır; aynı ölçülü sandık her iki yolda aynı m³'ü bulsun diye formül bunlarda iç
    /// ölçüye düşer ve ayak kuralını dış boydan uygular. İç sandık ile plandaki manuel kalem girilen ölçüyü iç ölçü
    /// olarak kullanır.
    /// </summary>
    public static bool DisOlcuKullanirMi(AmbalajUretimKaydi kayit) =>
        kayit.BagimsizKayitMi
            ? OzelSandikDisOlcuKullanirMi(kayit.Tur)
            : kayit.KaynakKayitId.HasValue &&
              kayit.KaynakModul is AmbalajKaynakModulu.Sandik or AmbalajKaynakModulu.Saha or AmbalajKaynakModulu.Yedek;

    /// <summary>Özel sandık türünün ölçüsü dış ölçü mü (İlave, Saha, Yedek); İç sandık iç ölçüyle girilir.</summary>
    public static bool OzelSandikDisOlcuKullanirMi(AmbalajSandikTuru tur) =>
        tur is AmbalajSandikTuru.Ilave or AmbalajSandikTuru.Saha or AmbalajSandikTuru.Yedek;

    public static AmbalajSandikCinsi KaynakCinsi(int tipId) => tipId switch
    {
        (int)SandikTipi.AhsapKapali => AmbalajSandikCinsi.AhsapKapali,
        (int)SandikTipi.KatlanirSandik => AmbalajSandikCinsi.Katlanir,
        (int)SandikTipi.Kontrplak => AmbalajSandikCinsi.Kontrplak,
        _ => AmbalajSandikCinsi.Diger
    };

    /// <summary>
    /// Kaynak sandığın ambalaj kaydına taşıdığı ad, cins ve dış ölçü (boş ölçü 0). Senkronizasyon ve üretim
    /// formundaki kaynak tazelemesi aynı eşlemeyi kullanır.
    /// </summary>
    public static AmbalajKaynakSandikDegerleri KaynakDegerleri(Sandik sandik) =>
        new(sandik.Ad, KaynakCinsi(sandik.TipId), sandik.Boy ?? 0, sandik.En ?? 0, sandik.Yukseklik ?? 0);

    /// <summary>
    /// Kaynak sandık, kaydın finans fiyat eşleşmesini belirleyen ad, cins veya ölçüsünü değiştiriyor mu?
    /// PO geçmişi olan finans kaleminde bu değişiklik fiyat snapshot'ını bozacağı için reddedilir.
    /// </summary>
    public static bool KaynakFiyatEslesmesiniDegistirirMi(AmbalajUretimKaydi kayit, Sandik sandik)
    {
        var kaynak = KaynakDegerleri(sandik);
        return !string.Equals(kayit.Ad?.Trim(), kaynak.Ad?.Trim(), StringComparison.OrdinalIgnoreCase) ||
               kayit.SandikCinsi != kaynak.Cins ||
               kayit.Boy != kaynak.Boy ||
               kayit.En != kaynak.En ||
               kayit.Yukseklik != kaynak.Yukseklik;
    }

    // Kaynakta amaç kodu bulunmadığından iki sandık adı ayrı ayrı değerlendirilir.
    // Sandık numarası ekipman türünün kanıtı değildir.
    public static bool VarsayilanYapilmazMi(string? ad, string? ingilizceAd = null)
    {
        return HedefSandikMi(NormalAd(ad)) || HedefSandikMi(NormalAd(ingilizceAd));
    }

    private static string NormalAd(string? kaynakAd)
    {
        if (string.IsNullOrWhiteSpace(kaynakAd)) return string.Empty;
        var metin = string.Concat(kaynakAd.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .ToUpperInvariant().Replace('İ', 'I').Replace('ı', 'I');
        return string.Join(' ', Regex.Matches(metin, "[A-Z0-9]+").Select(m => m.Value));
    }

    private static bool HedefSandikMi(string normalAd) =>
        Regex.IsMatch(normalAd, @"(?:^| )(?:BUSHING|BUSING)") ||
        Regex.IsMatch(normalAd,
            @"^(?:(?:TRANSFORMATOR(?: YAGSIZ)?|TRANSFORMER)(?: (?:SANDIGI|SANDIK|CASE|CRATE))?|TRANSFORMER MAIN BODY)$");

    /// <summary>
    /// Projenin çeki satırlarında (ilk yükleme ve revizyon) geçen sandık numaraları, <see cref="KoliNoKurali"/>
    /// biçiminde. Çağıran elle eklenen ürün satırlarını (Grid/sandık elle ürün, eksik tamamlama) vermez; satırın
    /// sonradan taşındığı fiili sandık değil, çekide geçen sandık sayılır.
    /// </summary>
    public static IReadOnlySet<string> CekiSandikNolari(IEnumerable<string?> cekideGecenSandikNolari) =>
        cekideGecenSandikNolari
            .Select(KoliNoKurali.Normalize)
            .Where(no => no.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sandık çeki dışından mı eklendi (ilave)? Çeki yüklemesi ve revizyonu sandığı çeki satırındaki numarayla
    /// açar; elle eklenen sandığın (Sandık ekle, Grid elle ürün ekle) numarası hiçbir çeki satırında geçmez.
    /// Projenin hiç çeki satırı yoksa plan çekiyle başlamamıştır ve sandıklar ilave sayılmaz.
    /// </summary>
    public static bool CekiDisiEklenenSandikMi(string? sandikNo, IReadOnlySet<string> cekiSandikNolari) =>
        cekiSandikNolari.Count > 0 &&
        !cekiSandikNolari.Contains(KoliNoKurali.Normalize(sandikNo));

    /// <summary>Projenin kaynak sandıklarının ambalaj grubu: saha ve yedek projede proje türü, diğerlerinde Normal.</summary>
    public static AmbalajSandikTuru ProjeGrubu(int projeTipiId) => projeTipiId switch
    {
        (int)ProjeTipi.Saha => AmbalajSandikTuru.Saha,
        (int)ProjeTipi.Yedek => AmbalajSandikTuru.Yedek,
        _ => AmbalajSandikTuru.Normal
    };

    /// <summary>
    /// Kaydı olan kaynak sandığın üretim grubu (RR Q2). Grup tek doğruluk kaynağı olan <c>Tur</c> alanındadır;
    /// kayıtlı Normal/İlave sınıflandırması sonraki senkronlarda ve form tazelemesinde korunur, saha/yedek
    /// projelerde grup proje türüdür.
    /// </summary>
    public static AmbalajSandikTuru KaynakGrubu(AmbalajSandikTuru projeGrubu, AmbalajSandikTuru kayitliGrup)
    {
        if (projeGrubu != AmbalajSandikTuru.Normal) return projeGrubu;
        return kayitliGrup == AmbalajSandikTuru.Ilave ? AmbalajSandikTuru.Ilave : AmbalajSandikTuru.Normal;
    }

    /// <summary>
    /// Kaynak sandığın üretim grubu. Kayıt ilk senkronda grubunu bir kez alır: normal projede çeki dışından
    /// eklenen sandık (<see cref="CekiDisiEklenenSandikMi"/>) İlave'dir. Grup, projenin ambalaj kaydının ne zaman
    /// oluştuğuna bağlı değildir; henüz kaydı olmayan sandık okuma tarafında aynı kuralla okunur.
    /// </summary>
    public static AmbalajSandikTuru KaynakGrubu(
        AmbalajSandikTuru projeGrubu,
        AmbalajSandikTuru? kayitliGrup,
        bool cekiDisiEklenen)
    {
        if (kayitliGrup is { } grup) return KaynakGrubu(projeGrubu, grup);
        if (projeGrubu != AmbalajSandikTuru.Normal) return projeGrubu;
        return cekiDisiEklenen ? AmbalajSandikTuru.Ilave : AmbalajSandikTuru.Normal;
    }

    /// <summary>
    /// Kaynak sandığın ilk ambalaj kararı. İlave sandık ana plana girmez, ambalaj kapsamı dışında başlar ve Özel
    /// Sandıklar'daki İlave kaydına kaynak olarak seçilir; transformatör/bushing sandığı da varsayılan yapılmaz.
    /// </summary>
    public static bool VarsayilanAmbalajaDahilMi(AmbalajSandikTuru grup, string? ad, string? ingilizceAd) =>
        grup != AmbalajSandikTuru.Ilave && !VarsayilanYapilmazMi(ad, ingilizceAd);

    /// <summary>
    /// Kaydı henüz olmayan kaynak sandığın ilk grubu ve ambalaj kararı. Senkron yeni kaydı bununla oluşturur;
    /// plan, proje listesi öngörüsü ve İlave aday listesi kaydı oluşmamış sandığı aynı kuralla okur.
    /// </summary>
    public static AmbalajYeniKaynakKarari YeniKaynakKarari(
        AmbalajSandikTuru projeGrubu,
        string? sandikNo,
        string? ad,
        string? ingilizceAd,
        IReadOnlySet<string> cekiSandikNolari)
    {
        var grup = KaynakGrubu(projeGrubu, null, CekiDisiEklenenSandikMi(sandikNo, cekiSandikNolari));
        return new AmbalajYeniKaynakKarari(grup, VarsayilanAmbalajaDahilMi(grup, ad, ingilizceAd));
    }

    /// <summary>
    /// Kaynak sandık plan çekmecesinde gösterilir mi? Ambalaj dışındaki İlave sandık ana planın parçası değildir;
    /// yalnız Özel Sandıklar'dan İlave sandık olarak yönetilir. Kullanıcı ambalaja dahil ettiyse gösterilir.
    /// </summary>
    public static bool PlandaGosterilirMi(AmbalajSandikTuru grup, bool ambalajaDahil) =>
        grup != AmbalajSandikTuru.Ilave || ambalajaDahil;

    public static AmbalajUretimDurumu ProjeDurumu(IEnumerable<AmbalajUretimKaydi> kayitlar)
    {
        var gerekli = kayitlar.Where(k => !k.IptalMi && k.AmbalajaDahil).ToList();
        if (gerekli.Count == 0) return AmbalajUretimDurumu.Planlandi;
        if (gerekli.All(k => k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi))
            return AmbalajUretimDurumu.Tamamlandi;
        return gerekli.Any(k => k.UretimDurumu != AmbalajUretimDurumu.Planlandi)
            ? AmbalajUretimDurumu.Uretimde : AmbalajUretimDurumu.Planlandi;
    }
}
