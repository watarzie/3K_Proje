using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// PO/fatura alanındaki proje satırlarının kategori kırılımı için tek kaynak. Üretim kategorileri iş
/// türünden gelir; özel iş türündeki kayıt düzenli işten üretildiyse "Düzenli İş", aksi hâlde "Özel İş"
/// kategorisindedir. Üretim kategorileri hiçbir zaman özel iş kategorisine düşmez.
/// </summary>
public static class FinansKategoriKurali
{
    public static FinansIsKategorisi Kategori(FinansIsTuru isTuru, int? duzenliIsId) => isTuru switch
    {
        FinansIsTuru.AnaAmbalaj => FinansIsKategorisi.AnaAmbalaj,
        FinansIsTuru.SarfKereste => FinansIsKategorisi.SarfKereste,
        FinansIsTuru.IlaveSandik => FinansIsKategorisi.Ilave,
        FinansIsTuru.IcSandik => FinansIsKategorisi.Ic,
        FinansIsTuru.SahaSandigi => FinansIsKategorisi.Saha,
        FinansIsTuru.YedekSandik => FinansIsKategorisi.Yedek,
        FinansIsTuru.OzelIs => duzenliIsId.HasValue ? FinansIsKategorisi.DuzenliIs : FinansIsKategorisi.OzelIs,
        _ => FinansIsKategorisi.DigerUretim
    };

    /// <summary>Kategorinin kapsadığı iş türleri; sorgu filtresi bu kümeyle kurulur.</summary>
    public static IReadOnlyList<FinansIsTuru> IsTurleri(FinansIsKategorisi kategori) => kategori switch
    {
        FinansIsKategorisi.AnaAmbalaj => [FinansIsTuru.AnaAmbalaj],
        FinansIsKategorisi.SarfKereste => [FinansIsTuru.SarfKereste],
        FinansIsKategorisi.Ilave => [FinansIsTuru.IlaveSandik],
        FinansIsKategorisi.Ic => [FinansIsTuru.IcSandik],
        FinansIsKategorisi.Saha => [FinansIsTuru.SahaSandigi],
        FinansIsKategorisi.Yedek => [FinansIsTuru.YedekSandik],
        FinansIsKategorisi.DigerUretim => [FinansIsTuru.Tadilat, FinansIsTuru.DigerAmbalaj],
        FinansIsKategorisi.OzelIs or FinansIsKategorisi.DuzenliIs => [FinansIsTuru.OzelIs],
        _ => []
    };

    public static string Ad(FinansIsKategorisi kategori) => kategori switch
    {
        FinansIsKategorisi.AnaAmbalaj => "Ana Ambalaj",
        FinansIsKategorisi.SarfKereste => "Sarf Kereste",
        FinansIsKategorisi.Ilave => "İlave",
        FinansIsKategorisi.Ic => "İç",
        FinansIsKategorisi.Saha => "Saha",
        FinansIsKategorisi.Yedek => "Yedek",
        FinansIsKategorisi.DigerUretim => "Diğer Üretim",
        FinansIsKategorisi.DuzenliIs => "Düzenli İş",
        _ => "Özel İş"
    };

    /// <summary>Üretim kategorisi mi; özel ve düzenli işler üretim kategorisi değildir.</summary>
    public static bool UretimKategorisi(FinansIsKategorisi kategori)
        => kategori is not (FinansIsKategorisi.OzelIs or FinansIsKategorisi.DuzenliIs);
}
