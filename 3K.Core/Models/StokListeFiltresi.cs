using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Stok sekmelerinin sayaçları: aktif ve tükenmiş (miktarı sıfır) kayıt sayısı.</summary>
public sealed record StokDurumSayilari(int Aktif, int Tukendi);

/// <summary>
/// Stok listesinin sunucu filtresi. Liste sunucuda sayfalandığı için Aktif/Tükendiler sekmeleri istemcide
/// bölünmez; durum ve arama veritabanı sorgusuna uygulanır. Durum verilmezse bütün kayıtlar döner.
/// </summary>
public static class StokListeFiltresi
{
    public static IQueryable<StokKaydi> Uygula(IQueryable<StokKaydi> stoklar, string? aramaMetni, int? durumId)
    {
        if (!string.IsNullOrEmpty(aramaMetni))
        {
            var arama = aramaMetni.ToLower();
            stoklar = stoklar.Where(s => s.MalzemeKodu.ToLower().Contains(arama) ||
                                         s.MalzemeAdi.ToLower().Contains(arama) ||
                                         (s.KaynakProje != null && s.KaynakProje.ToLower().Contains(arama)));
        }

        return durumId.HasValue ? stoklar.Where(s => s.DurumId == durumId.Value) : stoklar;
    }

    public static bool GecerliDurum(int? durumId) =>
        !durumId.HasValue || Enum.IsDefined(typeof(StokDurum), durumId.Value);
}
