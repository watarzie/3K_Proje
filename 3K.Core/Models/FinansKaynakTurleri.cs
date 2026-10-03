namespace _3K.Core.Models;

/// <summary>Finans iş kaydının kaynağı (FinansIsKaydi.KaynakTuru); kaynak anahtarıyla birlikte idempotent aktarımı belirler.</summary>
public static class FinansKaynakTurleri
{
    /// <summary>Finans ekranında elle girilen iş (özel/tek seferlik iş).</summary>
    public const string Manuel = "Manuel";

    /// <summary>Düzenli işten aylık üretilen dönem kaydı; kaynak kimliği "şablonId:yyyyMM".</summary>
    public const string DuzenliIs = "DuzenliIs";

    /// <summary>Ambalaj üretim kaydından aktarılan iş; kaynak kimliği üretim kaydının iş akışı kimliği.</summary>
    public const string AmbalajUretim = "AMBALAJURETIM";
}
