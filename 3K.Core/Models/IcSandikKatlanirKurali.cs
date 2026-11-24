using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// "İç Sandık Katlanır" sandık tipi (2026-11-23). Yalnız iç sandıkta seçilir ve m³ hesaplanmaz. "Katlanır Sandık"ın
/// elle fiyat kuralı (<see cref="FinansKatlanirKurali"/>) bu tipe uygulanmaz: Finans'ta Fiyat ve Tarifeler'den
/// hesaplanır. Tutar = adet × işin kayıtlı iç sandık tipine eşlenmiş, adet birimli ürünün finans tarihindeki tarife
/// fiyatı. Tipe bağlı değilse veya eşleşme/tarife yoksa iş fiyatsız kalır.
/// </summary>
public static class IcSandikKatlanirKurali
{
    public const string SandikTipiAdi = "İç Sandık Katlanır";

    public const string YalnizIcSandikMesaji = "İç Sandık Katlanır yalnız iç sandıkta seçilebilir.";

    public const string EslesmeMesaji =
        "İç Sandık Katlanır tarifesi iç sandık iş türünde, kayıtlı bir iç sandık tipine ve adet birimli ürüne bağlanır.";

    /// <summary>İş iç sandık katlanır mı: sandık cinsi; eski/elle kayıtta cins boşsa sandık tipi metni.</summary>
    public static bool IcKatlanir(AmbalajSandikCinsi? cinsi, string? sandikTipi) =>
        cinsi == AmbalajSandikCinsi.IcKatlanir || cinsi is null && SandikTipiMi(sandikTipi);

    public static bool IcKatlanir(FinansIsKaydi kayit) => IcKatlanir(kayit.SandikCinsi, kayit.SandikTipi);

    public static bool SandikTipiMi(string? sandikTipi) =>
        string.Equals(sandikTipi?.Trim(), SandikTipiAdi, StringComparison.OrdinalIgnoreCase);

    /// <summary>Ambalaj kaydında bu cins yalnız iç sandık türünde geçerlidir.</summary>
    public static bool TurUygun(AmbalajSandikCinsi cinsi, AmbalajSandikTuru tur) =>
        cinsi != AmbalajSandikCinsi.IcKatlanir || tur == AmbalajSandikTuru.Ic;

    /// <summary>
    /// Ürün eşleşmesi iç sandık katlanır işi fiyatlayabilir mi: eşleşme işin iç sandık tipine bağlı, ürün adet
    /// birimli ve malzemesiz olmalı. Tipe bağlı olmayan iş hiçbir eşleşmeyle fiyatlanmaz.
    /// </summary>
    public static bool EslesmeUygun(FinansUrunEslesmesi eslesme, FinansIsKaydi kayit) =>
        kayit.IcSandikSablonId.HasValue &&
        eslesme.IcSandikSablonId == kayit.IcSandikSablonId &&
        !eslesme.Malzeme.HasValue &&
        eslesme.FinansUrun.FiyatlandirmaBirimi == FinansFiyatlandirmaBirimi.Adet;
}
