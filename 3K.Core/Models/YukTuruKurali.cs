using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Yük türü kuralı (Simülasyon isterleri 3, 6, 10.1): tür kayıtlı alandan okunur; özel yükte (trafo, diğer özel yük)
/// sandık istif varsayılanları uygulanmaz.
/// </summary>
public static class YukTuruKurali
{
    public const string Mesaj = "Yük türü geçersiz (Sandık, Trafo veya Diğer özel yük).";

    public static bool Gecerli(int? yukTuruId) => yukTuruId.HasValue && Enum.IsDefined(typeof(YukTuru), yukTuruId.Value);

    /// <summary>Trafo veya diğer özel yük mü.</summary>
    public static bool OzelYukMu(int yukTuruId) => yukTuruId != (int)YukTuru.Sandik;
}
