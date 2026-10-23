using _3K.Core.Constants;

namespace _3K.Core.Models;

/// <summary>
/// Saha aktarımında sandık numarasına bağlı tek kural: ana projenin "1" numaralı sandığı (transformatör) hiçbir yolla
/// sandık olarak sahaya aktarılmaz; saha projesinde aynı numara sevk sonrası eksikler sandığıdır. Numara çeki
/// yüklemesindeki biçimle (<see cref="KoliNoKurali.Normalize"/>) karşılaştırılır; "01" ile "1" aynı sandıktır.
/// </summary>
public static class SahaAktarimSandikKurali
{
    /// <summary>Sandık numarası "1" numaralı sandığı mı gösteriyor.</summary>
    public static bool BirNumaraliSandikMi(string? sandikNo) =>
        KoliNoKurali.Normalize(sandikNo) == SahaAktarimConstants.EksiklerSandikNo;
}
