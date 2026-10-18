using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Satırın sevk sonucu: durum, sevk edilmiş sayılan adet ve sevki beklenen adet (trafoyla kapanan düşülmüş istenen).
/// </summary>
public readonly record struct UrunSevkSonucu(UrunSevkDurumu Durum, decimal SevkEdilen, decimal Beklenen);

/// <summary>
/// Ürün satırının sevk durumu (R-07); hesap yalnız raporu okur. Satır yalnız kendi projesinin sevkiyatıyla
/// değerlendirilir: normal projenin raporu o projenin kendi sevkiyatının anlık görüntüsüdür; sahaya aktarılan kısmın
/// saha sevki, saha Grid kararı ve saha trafosu buraya karışmaz (saha projesinin kendi raporunda okunur). Kurallar:
/// <list type="bullet">
/// <item>Grid İptal ürün sevk edilmez: <b>Sevk Edilmedi</b>.</item>
/// <item>Grid Kapandı ürün Grid tarafında kapatılıp orada sevk edilmiştir: <b>Sevk Edildi (Grid Kapandı)</b>.</item>
/// <item>Diğerlerinde giden adet, raporlanan projenin sevk edilmiş sandıklarına fiilen konan adettir; 3K'nın teslim
/// aldığı adetle (Gelen + Stok + Proje + Tedarikçi − Gönderilen) sınırlıdır. Grid'in gelen/sevk beyanı, sandığa
/// atanmış olmak ve iş tamamlanması sevk değildir.</item>
/// <item>Beklenen = istenen − satırın trafosu. Tamamı gittiyse <b>Sevk Edildi</b>, bir kısmı gittiyse
/// <b>Eksik Sevk (giden/beklenen)</b>, hiç gitmediyse <b>Sevk Edilmedi</b>; tamamı trafoyla kapandıysa
/// <b>Trafoyla</b>.</item>
/// </list>
/// </summary>
public static class UrunSevkDurumuKurali
{
    /// <param name="satir">Sandık içerikleri ve sandıkları yüklenmiş satır.</param>
    /// <param name="projeId">Raporlanan proje; başka projeye kalmış eski sandık bağlantısı bu projenin sevki değildir.</param>
    /// <param name="tahsissizSandik">
    /// Hiç sandık içeriği olmayan eski satırın sandık numarasıyla eşleşen proje sandığı (sistemin etkin içerik kuralı);
    /// ekranlar bu satırı o sandıkta 3K'nın teslim aldığı adetle gösterdiği için sevk de o sandıktan okunur.
    /// </param>
    public static UrunSevkSonucu? Belirle(CekiSatiri satir, int projeId, Sandik? tahsissizSandik = null)
    {
        var istenen = satir.IstenenAdet;
        // Grid kararı istenen adetten ve sandık izinden önce gelir: iptal edilen ürün sevk edilmez, Grid'in kapattığı
        // ürün Grid'de gitmiştir (istenen adedi 0'a çekilmiş satırda da).
        var gridBeklenen = Math.Max(istenen, 0);
        if (GridIptalMi(satir.GridDurumuId))
            return new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, gridBeklenen);
        if (satir.GridDurumuId == (int)GridDurum.GridKapandi)
            return new UrunSevkSonucu(UrunSevkDurumu.GridKapandi, gridBeklenen, gridBeklenen);
        if (istenen <= 0)
            return null;

        var beklenen = Math.Max(istenen - Math.Max(satir.TrafoSevkAdet, 0), 0);
        if (beklenen == 0)
            return new UrunSevkSonucu(UrunSevkDurumu.Trafoyla, 0, 0);

        // Sandıkta görünen adet 3K'nın teslim aldığı adetle sınırlıdır: Grid manuel eklemesinin kendiliğinden yazdığı
        // konan adet, 3K ürünü teslim almadıkça sevk edilmiş sayılmaz.
        var teslimAlinan = Math.Max(satir.KumulatifToplam, 0);
        var giden = Math.Min(SevkliSandigaKonan(satir, projeId, tahsissizSandik, teslimAlinan), teslimAlinan);

        if (giden >= beklenen)
            return new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, beklenen, beklenen);
        return giden > 0
            ? new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, giden, beklenen)
            : new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, beklenen);
    }

    private static decimal SevkliSandigaKonan(CekiSatiri satir, int projeId, Sandik? tahsissizSandik, decimal teslimAlinan)
    {
        if (satir.SandikIcerikleri.Count > 0)
            return satir.SandikIcerikleri
                .Where(si => si.Sandik != null && si.Sandik.ProjeId == projeId && SevkEdildiMi(si.Sandik))
                .Sum(si => Math.Max(si.KonulanAdet, 0));

        return tahsissizSandik != null && tahsissizSandik.ProjeId == projeId && SevkEdildiMi(tahsissizSandik)
            ? teslimAlinan
            : 0;
    }

    private static bool SevkEdildiMi(Sandik sandik) => sandik.DurumId == (int)SandikDurum.Sevkedildi;

    /// <summary>Grid İptal veya İptal Edildi: ürün sevk edilmez.</summary>
    public static bool GridIptalMi(int? gridDurumuId) =>
        gridDurumuId is (int)GridDurum.Iptal or (int)GridDurum.IptalEdildi;

    /// <summary>
    /// Sandık özetindeki sandığın sevk durumu: yalnız sandığın kendi sevkiyatı. Sahaya aktarılıp saha sandığıyla
    /// giden sandık bu projenin sevkiyatıyla gitmemiştir; sahaya aktarıldığı ayrıca gösterilir.
    /// </summary>
    public static UrunSevkDurumu SandikDurumu(Sandik sandik) =>
        SevkEdildiMi(sandik) ? UrunSevkDurumu.SevkEdildi : UrunSevkDurumu.SevkEdilmedi;

    /// <summary>Durumun rapor metni; sevk durumu olmayan satırda "-". Eksik sevkin adedi raporda eklenir.</summary>
    public static string Etiket(UrunSevkDurumu? durum) => durum switch
    {
        UrunSevkDurumu.SevkEdildi => "Sevk Edildi",
        UrunSevkDurumu.GridKapandi => "Sevk Edildi (Grid Kapandı)",
        UrunSevkDurumu.EksikSevk => "Eksik Sevk",
        UrunSevkDurumu.SevkEdilmedi => "Sevk Edilmedi",
        UrunSevkDurumu.Trafoyla => "Trafoyla",
        _ => "-"
    };

    /// <summary>
    /// Rapor özetindeki satır sayıları: "x sevk edildi / g sevk edildi (Grid Kapandı) / y eksik sevk /
    /// z sevk edilmedi / t trafoyla".
    /// </summary>
    public static string OzetMetni(IEnumerable<UrunSevkSonucu?> sonuclar)
    {
        var liste = sonuclar.Select(s => s?.Durum).ToList();
        return $"{liste.Count(d => d == UrunSevkDurumu.SevkEdildi)} sevk edildi / " +
               $"{liste.Count(d => d == UrunSevkDurumu.GridKapandi)} sevk edildi (Grid Kapandı) / " +
               $"{liste.Count(d => d == UrunSevkDurumu.EksikSevk)} eksik sevk / " +
               $"{liste.Count(d => d == UrunSevkDurumu.SevkEdilmedi)} sevk edilmedi / " +
               $"{liste.Count(d => d == UrunSevkDurumu.Trafoyla)} trafoyla";
    }

    /// <summary>
    /// Sandık bazlı sahaya aktarılan sandığın rapor metni: sevki beklenen ürünlerinin hepsi sahaya aktarıldıysa
    /// "Aktarıldı", bir kısmı aktarıldıysa "Kısmen (a/b ürün)".
    /// </summary>
    public static string SandikSahaAktarimMetni(int aktarilanUrun, int sevkBeklenenUrun) =>
        sevkBeklenenUrun == 0 || aktarilanUrun >= sevkBeklenenUrun
            ? "Aktarıldı"
            : $"Kısmen ({aktarilanUrun}/{sevkBeklenenUrun} ürün)";
}
