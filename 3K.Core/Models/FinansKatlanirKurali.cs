using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>
/// Katlanır sandık elle fiyatlanır (2026-10-26): sistem tarife, m³ veya ölçüden fiyat hesaplamaz. Ambalaj'dan gelen iş
/// fiyatsız (bedel 0) açılır ve fiyatı bekler; kullanıcı Fiyatlandırma'da adet × birim fiyat veya net toplam girer ve
/// girilen tutar olduğu gibi kullanılır. Tarifeyle fiyatlanmış eski kayıtların fiyatı korunur ama tarife yenileme,
/// düzeltme ve geriye dönük doldurma artık onları değiştirmez. Diğer sandık tipleri bu kuraldan etkilenmez.
/// </summary>
public static class FinansKatlanirKurali
{
    public const string SandikTipiAdi = "Katlanır Sandık";

    public const string TarifeMesaji = "Katlanır sandık tarifeyle fiyatlanmaz; fiyatı işin Fiyatlandırma bölümünden elle girilir.";
    public const string HacimMesaji = "Katlanır sandıkta m³ girilmez; fiyatı işin Fiyatlandırma bölümünden elle girilir.";
    public const string YontemMesaji = "Katlanır sandık adet × birim fiyat veya net toplamla fiyatlanır; m³ ve sabit tutar kullanılmaz.";

    /// <summary>İş katlanır sandık mı: sandık cinsi; eski kayıtta cins boşsa sandık tipi metni.</summary>
    public static bool Katlanir(AmbalajSandikCinsi? cinsi, string? sandikTipi) =>
        cinsi == AmbalajSandikCinsi.Katlanir || cinsi is null && SandikTipiMi(sandikTipi);

    public static bool Katlanir(FinansIsKaydi kayit) => Katlanir(kayit.SandikCinsi, kayit.SandikTipi);

    /// <summary>Tarife eşleşmesindeki sandık tipi metni katlanır sandığı mı gösteriyor.</summary>
    public static bool SandikTipiMi(string? sandikTipi) =>
        string.Equals(sandikTipi?.Trim(), SandikTipiAdi, StringComparison.OrdinalIgnoreCase);

    /// <summary>Elle fiyatın yöntemi: adet × birim fiyat veya net toplam.</summary>
    public static bool YontemGecerli(FinansFiyatlandirmaBirimi birim) =>
        birim is FinansFiyatlandirmaBirimi.Adet or FinansFiyatlandirmaBirimi.ManuelToplam;

    /// <summary>Elle fiyatın bileşenleri m³ satırı içermez (malzemeli satır da m³ satırıdır).</summary>
    public static bool BilesenlerGecerli(IReadOnlyList<FinansFiyatBileseniModel>? bilesenler) =>
        bilesenler is null || bilesenler.All(x => x is null || x.Yontem != FinansFiyatlandirmaBirimi.Metrekup && x.Malzeme is null);

    /// <summary>Servis koruması: katlanır işin elle fiyatı yöntem ve bileşen kuralına uymuyorsa 409.</summary>
    public static void FiyatDogrula(FinansFiyatlandirmaBirimi birim, IReadOnlyList<FinansFiyatBileseniModel>? bilesenler)
    {
        if (!YontemGecerli(birim) || !BilesenlerGecerli(bilesenler))
            throw IsKuraliException.Cakisma(YontemMesaji);
    }

    /// <summary>
    /// Elle fiyatlanan katlanır işte hacim tutulmaz: m³ ve elle girilmiş ahşap/kontra payı temizlenir.
    /// </summary>
    public static void HacmiTemizle(FinansIsKaydi kayit)
    {
        kayit.BirimM3 = 0;
        kayit.ToplamM3 = 0;
        kayit.AhsapM3 = null;
        kayit.KontraM3 = null;
        kayit.HacimManuel = false;
    }
}
