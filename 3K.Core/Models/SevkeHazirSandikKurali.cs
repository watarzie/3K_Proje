using _3K.Core.Common;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Sandığın sevke hazır sandık raporundaki yeri; rapora alınmayanlarda neden.</summary>
public enum SevkeHazirSandikDurumu
{
    Hazir = 0,
    Kapanmadi = 1,
    SevkEdildi = 2,
    OlcuAgirlikEksik = 3
}

/// <summary>Rapora alınmayan sandık ve nedeni.</summary>
public sealed record SevkeHazirOlmayanSandik(Sandik Sandik, SevkeHazirSandikDurumu Neden, IReadOnlyList<string> EksikAlanlar);

/// <summary>Saha/Yedek projesinin sandıklarının sevke hazır olanlar ve rapora alınmayanlar olarak ayrımı.</summary>
public sealed class SevkeHazirSandikAyrimi
{
    public required IReadOnlyList<Sandik> Hazir { get; init; }
    public required IReadOnlyList<SevkeHazirOlmayanSandik> Disarida { get; init; }
    public int ToplamSandik => Hazir.Count + Disarida.Count;

    /// <summary>Raporun başındaki not: kaç sandığın neden listelenmediği (PDF ve Excel aynı metni yazar).</summary>
    public string Not => SevkeHazirSandikKurali.NotMetni(this);
}

/// <summary>
/// Sevke hazır sandık raporunun kapsamı (beyanname için): yalnız durumu Kapandı olan ve En, Boy, Yükseklik, Net kg ve
/// Brüt kg değerlerinin beşi de girilmiş (sıfırdan büyük) sandıklar. Mevcut sandık raporu bu ayrımı yapmaz; beyannamede
/// gerekli olan ölçü ve iki ağırlık birlikte girildiği için biri eksik olan sandık hazır sayılmaz. Sevk edilmiş sandık
/// zaten gönderildiğinden yeni beyannameye girmez.
/// </summary>
public static class SevkeHazirSandikKurali
{
    /// <summary>Rapor başlığının altındaki kapsam açıklaması.</summary>
    public const string KapsamMetni =
        "Yalnız durumu Kapandı olan ve ölçü (Boy/En/Yükseklik) ile ağırlığı (Net/Brüt kg) girilmiş sandıklar listelenir.";

    public static SevkeHazirSandikDurumu Durum(Sandik sandik) => sandik.DurumId switch
    {
        (int)SandikDurum.Sevkedildi => SevkeHazirSandikDurumu.SevkEdildi,
        (int)SandikDurum.Kapandi => EksikAlanlar(sandik).Count == 0
            ? SevkeHazirSandikDurumu.Hazir
            : SevkeHazirSandikDurumu.OlcuAgirlikEksik,
        _ => SevkeHazirSandikDurumu.Kapanmadi
    };

    /// <summary>Girilmemiş (boş ya da sıfır) ölçü/ağırlık alanlarının rapor adları.</summary>
    public static IReadOnlyList<string> EksikAlanlar(Sandik sandik)
    {
        var eksik = new List<string>(5);
        if (!Girilmis(sandik.Boy)) eksik.Add("Boy");
        if (!Girilmis(sandik.En)) eksik.Add("En");
        if (!Girilmis(sandik.Yukseklik)) eksik.Add("Yükseklik");
        if (!Girilmis(sandik.NetKg)) eksik.Add("Net kg");
        if (!Girilmis(sandik.GrossKg)) eksik.Add("Brüt kg");
        return eksik;
    }

    /// <summary>Sandıkları doğal numara sırasıyla hazır olanlar ve rapora alınmayanlar olarak ayırır.</summary>
    public static SevkeHazirSandikAyrimi Ayir(IEnumerable<Sandik> sandiklar)
    {
        var sirali = sandiklar.OrderBy(s => s.SandikNo, SandikNumarasiComparer.Instance).ThenBy(s => s.Id).ToList();
        var hazir = new List<Sandik>();
        var disarida = new List<SevkeHazirOlmayanSandik>();
        foreach (var sandik in sirali)
        {
            var durum = Durum(sandik);
            if (durum == SevkeHazirSandikDurumu.Hazir)
                hazir.Add(sandik);
            else
                disarida.Add(new SevkeHazirOlmayanSandik(sandik, durum, EksikAlanlar(sandik)));
        }

        return new SevkeHazirSandikAyrimi { Hazir = hazir, Disarida = disarida };
    }

    /// <summary>Rapora alınmama nedeninin rapor metni.</summary>
    public static string NedenMetni(SevkeHazirSandikDurumu durum) => durum switch
    {
        SevkeHazirSandikDurumu.Kapanmadi => "Kapanmadı",
        SevkeHazirSandikDurumu.SevkEdildi => "Sevk edildi",
        SevkeHazirSandikDurumu.OlcuAgirlikEksik => "Ölçü/ağırlık eksik",
        _ => "Sevke hazır"
    };

    /// <summary>
    /// Örnek: "3 sandıktan 2'si sevke hazır. 1 sandık kapanmadığı için listelenmedi (No: 3)."
    /// </summary>
    public static string NotMetni(SevkeHazirSandikAyrimi ayrim)
    {
        var parcalar = new List<string>(4)
        {
            ayrim.Disarida.Count == 0
                ? $"Projedeki {ayrim.ToplamSandik} sandığın tamamı sevke hazır."
                : $"{ayrim.ToplamSandik} sandıktan {ayrim.Hazir.Count} sandık sevke hazır."
        };

        foreach (var (neden, aciklama) in new[]
                 {
                     (SevkeHazirSandikDurumu.Kapanmadi, "kapanmadığı için"),
                     (SevkeHazirSandikDurumu.OlcuAgirlikEksik, "ölçü/ağırlık girilmediği için"),
                     (SevkeHazirSandikDurumu.SevkEdildi, "zaten sevk edildiği için")
                 })
        {
            var grup = ayrim.Disarida.Where(x => x.Neden == neden).Select(x => x.Sandik.SandikNo).ToList();
            if (grup.Count > 0)
                parcalar.Add($"{grup.Count} sandık {aciklama} listelenmedi (No: {string.Join(", ", grup)}).");
        }

        return string.Join(" ", parcalar);
    }

    private static bool Girilmis(decimal? deger) => deger is > 0;
}
