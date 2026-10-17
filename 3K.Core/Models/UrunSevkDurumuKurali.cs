using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Satırın fiziksel sevk sonucu: durum, 3K ile fiilen sevk edilen adet ve 3K'dan sevki beklenen adet
/// (trafoyla ve iptal/Grid Kapandı ile kapanan kısım düşülmüş istenen).
/// </summary>
public readonly record struct UrunSevkSonucu(UrunSevkDurumu Durum, decimal SevkEdilen, decimal Beklenen);

/// <summary>
/// Satırın sahaya aktarılan bir kısmı (aktif aktarım kalemi): aktarılan miktar, saha satırının sevkli saha
/// sandıklarına fiilen konan adedi, saha satırının trafo adedi ve saha satırı Grid İptal/Grid Kapandı mı.
/// </summary>
public readonly record struct SahaAktarimParcasi(decimal Miktar, decimal SevkliSandigaKonulan, decimal Trafo, bool IptalVeyaKapandi);

/// <summary>
/// Ürün satırının gerçek (fiziksel) sevki (R-07). Bir adet yalnız fiziksel izle "gitti" sayılır: satırın kendi
/// projesinde sevk edilmiş sandığa fiilen konan adet ve sahaya aktarılan kısımda saha satırının sevk edilmiş saha
/// sandığına fiilen konan adedi (kalem miktarıyla sınırlı). Sandığın, aktarım kaleminin veya Grid'in durumu tek
/// başına sevk kanıtı değildir. Beklenen adet istenenden trafoyla kapanan ve iptal/Grid Kapandı ile kapanan kısım
/// düşülerek bulunur. İş tamamlanması (kalan miktar) bu hesaba girmez; hesap yalnız raporu okur.
/// </summary>
public static class UrunSevkDurumuKurali
{
    public static UrunSevkSonucu? Belirle(CekiSatiri satir, int projeId, IEnumerable<SahaAktarimParcasi>? sahaParcalari = null)
    {
        if (satir.IstenenAdet <= 0)
            return null;
        var parcalar = (sahaParcalari ?? []).ToList();

        // Yalnız raporlanan projenin sevkli sandıkları sayılır; başka projeye kalmış eski bağlantı bu projenin sevki değildir.
        var sevkEdilen = satir.SandikIcerikleri
            .Where(si => si.Sandik != null && si.Sandik.ProjeId == projeId && si.Sandik.DurumId == (int)SandikDurum.Sevkedildi)
            .Sum(si => Math.Max(si.KonulanAdet, 0))
            + parcalar.Sum(p => Math.Min(Math.Max(p.SevkliSandigaKonulan, 0), Math.Max(p.Miktar, 0)));

        var trafo = Math.Max(satir.TrafoSevkAdet, 0)
            + parcalar.Where(p => !p.IptalVeyaKapandi).Sum(p => Math.Min(Math.Max(p.Trafo, 0), Math.Max(p.Miktar, 0)));
        // Grid İptal/Grid Kapandı fiziksel teslim üretmez (O-05): satırın kendisi kapandıysa hiçbir adet beklenmez,
        // saha satırı kapandıysa o kalemin miktarı beklenmez. Yine de fiilen giden adet "gitti" görünür.
        var beklenen = SatirIptalVeyaKapandi(satir)
            ? 0
            : Math.Max(satir.IstenenAdet - trafo - parcalar.Where(p => p.IptalVeyaKapandi).Sum(p => Math.Max(p.Miktar, 0)), 0);

        if (beklenen > 0)
        {
            var gonderilen = Math.Min(sevkEdilen, beklenen);
            var durum = gonderilen >= beklenen
                ? UrunSevkDurumu.SevkEdildi
                : gonderilen > 0 ? UrunSevkDurumu.KismiSevk : UrunSevkDurumu.SevkEdilmedi;
            return new UrunSevkSonucu(durum, gonderilen, beklenen);
        }

        if (sevkEdilen > 0)
            return new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, sevkEdilen, sevkEdilen);
        if (trafo > 0 && !SatirIptalVeyaKapandi(satir))
            return new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0);
        return null;
    }

    /// <summary>Grid İptal, İptal Edildi veya Grid Kapandı satırı fiziksel teslim beklemez.</summary>
    public static bool SatirIptalVeyaKapandi(CekiSatiri satir) =>
        satir.GridDurumuId is (int)GridDurum.Iptal or (int)GridDurum.IptalEdildi or (int)GridDurum.GridKapandi;

    /// <summary>Sandık özetindeki sandığın sevk durumu.</summary>
    public static UrunSevkDurumu SandikDurumu(Sandik sandik) =>
        sandik.DurumId == (int)SandikDurum.Sevkedildi ? UrunSevkDurumu.SevkEdildi : UrunSevkDurumu.SevkEdilmedi;

    /// <summary>Durumun rapor metni; sevk durumu olmayan satırda "-". Kısmi sevkin adedi raporda eklenir.</summary>
    public static string Etiket(UrunSevkDurumu? durum) => durum switch
    {
        UrunSevkDurumu.SevkEdildi => "Sevk Edildi",
        UrunSevkDurumu.KismiSevk => "Kısmi Sevk",
        UrunSevkDurumu.SevkEdilmedi => "Sevk Edilmedi",
        UrunSevkDurumu.Trafoyla => "Trafoyla",
        _ => "-"
    };

    /// <summary>Rapor özetindeki satır sayıları: "x sevk edildi / y kısmi sevk / z sevk edilmedi / t trafoyla".</summary>
    public static string OzetMetni(IEnumerable<UrunSevkSonucu?> sonuclar)
    {
        var liste = sonuclar.Select(s => s?.Durum).ToList();
        return $"{liste.Count(d => d == UrunSevkDurumu.SevkEdildi)} sevk edildi / " +
               $"{liste.Count(d => d == UrunSevkDurumu.KismiSevk)} kısmi sevk / " +
               $"{liste.Count(d => d == UrunSevkDurumu.SevkEdilmedi)} sevk edilmedi / " +
               $"{liste.Count(d => d == UrunSevkDurumu.Trafoyla)} trafoyla";
    }
}
