using _3K.Core.Enums;

namespace _3K.Core.Models;

public enum FinansSandikMalzemesi
{
    Bilinmiyor = 0,
    Ahsap = 1,
    Kontra = 2
}

/// <summary>
/// Ahşap ve kontra/katlanır m³ ayrımı (F11) için tek kaynak. Ahşap kapalı ve kafes sandıkların hacmi
/// üretim m³'üdür; kontra/katlanır sandıklar Ambalaj'da m³ hesaplanmadığı için Finans'ta girilen m³
/// fiyat satırlarından (bileşenler) okunur. Eski kayıtlarda sandık cinsi boşsa sandık tipi metni kullanılır.
/// </summary>
public static class FinansHacimKurallari
{
    public static FinansSandikMalzemesi Malzeme(AmbalajSandikCinsi? cinsi, string? sandikTipi) => cinsi switch
    {
        AmbalajSandikCinsi.AhsapKapali or AmbalajSandikCinsi.Kafes => FinansSandikMalzemesi.Ahsap,
        AmbalajSandikCinsi.Kontrplak or AmbalajSandikCinsi.Katlanir => FinansSandikMalzemesi.Kontra,
        _ => sandikTipi?.Trim() switch
        {
            "Ahşap Kapalı" or "Kafes Sandık" => FinansSandikMalzemesi.Ahsap,
            "Kontrplak Sandık" or "Katlanır Sandık" => FinansSandikMalzemesi.Kontra,
            _ => FinansSandikMalzemesi.Bilinmiyor
        }
    };

    /// <summary>
    /// Kaydın ahşap ve kontra m³ payı. Ahşap sandığın ahşabı üretim m³'üdür; kontra/katlanır sandığın hacmi m³ fiyat
    /// satırlarından okunur (satır yoksa kayıt m³'ü). Malzemesi açıkça yazılmış m³ satırı kendi malzemesine eklenir;
    /// malzemesi boş satır sandığın malzemesini alır (ahşap sandıkta üretim m³'ü zaten sayıldığı için eklenmez).
    /// </summary>
    public static (decimal Ahsap, decimal Kontra) Hacim(
        AmbalajSandikCinsi? cinsi,
        string? sandikTipi,
        decimal toplamM3,
        IReadOnlyList<FinansFiyatBileseniModel>? bilesenler)
    {
        var m3Satirlari = bilesenler?.Where(x => x.Yontem == FinansFiyatlandirmaBirimi.Metrekup).ToList() ?? [];
        var acikAhsap = m3Satirlari.Where(x => x.Malzeme == FinansSandikMalzemesi.Ahsap).Sum(x => x.Miktar);
        var acikKontra = m3Satirlari.Where(x => x.Malzeme == FinansSandikMalzemesi.Kontra).Sum(x => x.Miktar);
        var malzemesiz = m3Satirlari.Where(x => x.Malzeme is null or FinansSandikMalzemesi.Bilinmiyor).Sum(x => x.Miktar);
        switch (Malzeme(cinsi, sandikTipi))
        {
            case FinansSandikMalzemesi.Ahsap:
                return (toplamM3, acikKontra);
            case FinansSandikMalzemesi.Kontra:
                var kontra = acikKontra + malzemesiz;
                return (acikAhsap, kontra > 0 || acikAhsap > 0 ? kontra : toplamM3);
            default:
                return (acikAhsap, acikKontra);
        }
    }
}
