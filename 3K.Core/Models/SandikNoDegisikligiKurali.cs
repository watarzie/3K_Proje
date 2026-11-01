using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Sandık numarası değişikliğinin reddi: kullanıcıya gösterilecek mesaj ve HTTP durum kodu.</summary>
public sealed record SandikNoDegisikligiReddi(string Mesaj, int Kod);

/// <summary>
/// Elle eklenen sandığın (Sandık ekle, Grid elle ürün ekle, saha aktarımında açılan sandık) numarası sonradan
/// değiştirilebilir. Çekiden gelen sandığın numarası çeki satırındaki koli numarasıdır; çeki revizyonu sandığı bu
/// numarayla bulur, değiştirilemez. Çekiden gelme, numaranın elle eklenmemiş bir çeki satırının
/// <c>CekideGecenSandikNo</c>'sunda geçmesidir (<see cref="AmbalajUretimPolitikasi.CekiDisiEklenenSandikMi"/> ile
/// aynı ayrım). Numaralar çeki yüklemesindeki biçimle (<see cref="KoliNoKurali.Normalize"/>) karşılaştırılır.
/// </summary>
public static class SandikNoDegisikligiKurali
{
    /// <summary>Ambalaj kaydındaki sandık no sütunu sınırı; daha uzun numara aynalanamaz.</summary>
    public const int MaksimumUzunluk = 100;

    public const string BosMesaji = "Sandık numarası boş olamaz.";
    public static readonly string UzunlukMesaji = $"Sandık numarası en fazla {MaksimumUzunluk} karakter olabilir.";
    public const string CekiSandigiMesaji =
        "Çekiden gelen sandığın numarası çeki satırlarına bağlıdır; yalnız elle eklenen sandığın numarası değiştirilebilir.";
    public const string SevkliMesaji = "Sevk edilmiş sandığın numarası değiştirilemez.";
    public const string BirNumaraMesaji =
        "1 numaralı sandık sistemde özel anlam taşır (trafo / sevk sonrası eksikler / yedek malzemeler); bu numara değiştirilemez ve başka sandığa verilemez.";
    public const string CekiNumarasiMesaji =
        "Bu numara projenin çekisinde geçen bir sandık numarası; elle eklenen sandığa verilemez.";
    public const string AmbalajMesaji =
        "Sandığın ambalaj kaydı üretime alınmış, kilitlenmiş ya da Özel Sandıklar'da yönetiliyor; numarası değiştirilemez.";

    public static string CakismaMesaji(string yeniNo) => $"'{yeniNo}' numaralı sandık bu projede zaten mevcut.";

    /// <summary>Sandık numarası çekideki bir koli numarası mı (çekiden gelen sandık).</summary>
    public static bool CekidenGelenSandikMi(string? sandikNo, IReadOnlySet<string> cekiSandikNolari) =>
        cekiSandikNolari.Contains(KoliNoKurali.Normalize(sandikNo));

    /// <summary>
    /// Sandığın numarası neden değiştirilemez; değiştirilebiliyorsa <c>null</c>. Ön yüzdeki numara alanı ve sunucu
    /// denetimi aynı nedeni kullanır. Ambalaj kaydı yalnız planlı ve kilitsizse otomatik senkronla yeni numaraya
    /// geçer; üretime alınmış, kilitli ya da bağımsız (Özel Sandıklar) kaydın numarası ezilmez.
    /// </summary>
    public static string? KilitNedeni(
        Sandik sandik,
        IReadOnlySet<string> cekiSandikNolari,
        IEnumerable<AmbalajUretimKaydi> bagliAmbalajKayitlari)
    {
        if (sandik.DurumId == (int)SandikDurum.Sevkedildi)
            return SevkliMesaji;
        if (CekidenGelenSandikMi(sandik.SandikNo, cekiSandikNolari))
            return CekiSandigiMesaji;
        if (SahaAktarimSandikKurali.BirNumaraliSandikMi(sandik.SandikNo))
            return BirNumaraMesaji;
        if (bagliAmbalajKayitlari.Any(k => !k.IptalMi &&
                (k.BagimsizKayitMi || k.KaynakSenkronizasyonuKilitliMi || k.UretimDurumu != AmbalajUretimDurumu.Planlandi)))
            return AmbalajMesaji;
        return null;
    }

    /// <summary>Numara gerçekten değişiyor mu: kırpılmış metin aynıysa değişiklik yoktur.</summary>
    public static bool DegisiyorMu(string mevcutNo, string? istenenNo) =>
        istenenNo != null && !string.Equals(mevcutNo.Trim(), istenenNo.Trim(), StringComparison.Ordinal);

    /// <summary>Değişikliği denetler; reddedilmesi gerekiyorsa nedeni, yoksa <c>null</c> döner.</summary>
    public static SandikNoDegisikligiReddi? Denetle(
        Sandik sandik,
        string yeniNo,
        IReadOnlySet<string> cekiSandikNolari,
        IEnumerable<string> projedekiDigerSandikNolari,
        IEnumerable<AmbalajUretimKaydi> bagliAmbalajKayitlari)
    {
        var no = yeniNo.Trim();
        if (no.Length == 0)
            return new(BosMesaji, 400);
        if (no.Length > MaksimumUzunluk)
            return new(UzunlukMesaji, 400);
        if (KilitNedeni(sandik, cekiSandikNolari, bagliAmbalajKayitlari) is { } kilit)
            return new(kilit, 409);
        if (SahaAktarimSandikKurali.BirNumaraliSandikMi(no))
            return new(BirNumaraMesaji, 409);
        if (CekidenGelenSandikMi(no, cekiSandikNolari))
            return new(CekiNumarasiMesaji, 409);

        var normalize = KoliNoKurali.Normalize(no);
        if (projedekiDigerSandikNolari.Any(diger =>
                string.Equals(KoliNoKurali.Normalize(diger), normalize, StringComparison.OrdinalIgnoreCase)))
            return new(CakismaMesaji(no), 409);

        return null;
    }
}
