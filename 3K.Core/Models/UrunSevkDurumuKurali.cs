using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Satırın sevk sonucu: durum, sevk edilmiş sayılan adet ve sevki beklenen adet (istenen).
/// </summary>
public readonly record struct UrunSevkSonucu(UrunSevkDurumu Durum, decimal SevkEdilen, decimal Beklenen);

/// <summary>
/// Ürün satırının sevk durumu (R-07); hesap yalnız raporu okur. Satır yalnız kendi projesinin sevkiyatıyla
/// değerlendirilir: normal projenin raporu o projenin kendi sevkiyatının anlık görüntüsüdür; sahaya aktarılan kısmın
/// saha sevki, saha Grid kararı ve saha trafosu buraya karışmaz (saha projesinin kendi raporunda okunur). Kurallar:
/// <list type="bullet">
/// <item>Grid İptal ürün sevk edilmez: <b>Sevk Edilmedi</b>.</item>
/// <item>Grid Kapandı ürün 3K'ya fiziksel gelmez ama satırın sandığıyla gider: sandığı sevk edildiyse
/// <b>Sevk Edildi</b>, sandığı sevk edilmediyse (sevkiyatı geri alındıysa) <b>Sevk Edilmedi</b>.</item>
/// <item>Trafodaki adet de satırın sandığıyla gider: sandığı sevk edildiyse gitmiş sayılır.</item>
/// <item>Diğer adet, raporlanan projenin sevk edilmiş sandıklarına fiilen konan adettir; 3K'nın teslim aldığı adetle
/// (Gelen + Stok + Proje + Tedarikçi − Gönderilen) sınırlıdır. Grid'in gelen/sevk beyanı, sandığa atanmış olmak ve iş
/// tamamlanması sevk değildir.</item>
/// <item>Giden = sandığa konan + trafoda giden. İstenenin tamamı gittiyse <b>Sevk Edildi</b> (yalnız trafoyla gittiyse
/// <b>Trafoyla</b>), bir kısmı gittiyse <b>Eksik Sevk (giden/istenen)</b>, hiç gitmediyse <b>Sevk Edilmedi</b>.</item>
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
        var istenen = Math.Max(satir.IstenenAdet, 0);
        // Grid İptal kararı sandık izinden önce gelir: iptal edilen ürün sevk edilmez.
        if (GridIptalMi(satir.GridDurumuId))
            return new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, istenen);

        var sandik = SandikSevki(satir, projeId, tahsissizSandik);

        // Grid'in kapattığı ürün 3K'ya gelmez (kalan kendiliğinden 0 olur) ama satırın sandığıyla gider; sevki o
        // sandığın sevkinden okunur (sistemin sevkiyat raporu da bu ürünü sandık payıyla konmuş sayar).
        if (satir.GridDurumuId == (int)GridDurum.GridKapandi)
        {
            if (sandik.TumuSevkli)
                return new UrunSevkSonucu(UrunSevkDurumu.SevkEdildi, istenen, istenen);
            var gridGiden = Math.Min(sandik.SevkliPay, istenen);
            return gridGiden > 0
                ? new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, gridGiden, istenen)
                : new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, istenen);
        }

        if (istenen == 0)
            return null;

        // Sandıkta görünen adet 3K'nın teslim aldığı adetle sınırlıdır: Grid manuel eklemesinin kendiliğinden yazdığı
        // konan adet, 3K ürünü teslim almadıkça sevk edilmiş sayılmaz.
        var teslimAlinan = Math.Max(satir.KumulatifToplam, 0);
        var konanGiden = Math.Min(sandik.SevkliKonan(teslimAlinan), teslimAlinan);
        // Trafodaki adet satırın sandığıyla gider; satır birden çok sandığa bölünmüşse sevkli sandıkların payı kadar.
        var trafo = Math.Min(Math.Max(satir.TrafoSevkAdet, 0), istenen);
        var trafoGiden = sandik.TumuSevkli ? trafo : Math.Min(trafo, sandik.SevkliPay);
        var giden = Math.Min(konanGiden + trafoGiden, istenen);

        if (giden >= istenen)
            return new UrunSevkSonucu(
                konanGiden == 0 && trafoGiden > 0 ? UrunSevkDurumu.Trafoyla : UrunSevkDurumu.SevkEdildi, istenen, istenen);
        return giden > 0
            ? new UrunSevkSonucu(UrunSevkDurumu.EksikSevk, giden, istenen)
            : new UrunSevkSonucu(UrunSevkDurumu.SevkEdilmedi, 0, istenen);
    }

    /// <summary>
    /// Satırın bu projedeki sandıklarının sevki. Pay, sandığın satırdan taşıdığı miktardır (SandikTahsisHelper ile
    /// aynı: tahsis, yoksa konan); yalnız sevkli ve sevksiz sandığa bölünmüş satırda kullanılır.
    /// </summary>
    private readonly record struct SatirSandikSevki(bool TumuSevkli, decimal SevkliPay, decimal SevkliKonanToplam, bool Tahsissiz)
    {
        public decimal SevkliKonan(decimal teslimAlinan) => Tahsissiz ? (TumuSevkli ? teslimAlinan : 0) : SevkliKonanToplam;
    }

    private static SatirSandikSevki SandikSevki(CekiSatiri satir, int projeId, Sandik? tahsissizSandik)
    {
        if (satir.SandikIcerikleri.Count == 0)
        {
            var sevkli = tahsissizSandik != null && tahsissizSandik.ProjeId == projeId && SevkEdildiMi(tahsissizSandik);
            return new SatirSandikSevki(sevkli, 0, 0, Tahsissiz: true);
        }

        var icerikler = satir.SandikIcerikleri.Where(si => si.Sandik != null && si.Sandik.ProjeId == projeId).ToList();
        var sevkliIcerikler = icerikler.Where(si => SevkEdildiMi(si.Sandik)).ToList();
        return new SatirSandikSevki(
            TumuSevkli: icerikler.Count > 0 && sevkliIcerikler.Count == icerikler.Count,
            SevkliPay: sevkliIcerikler.Sum(si => si.TahsisMiktari > 0 ? si.TahsisMiktari : Math.Max(si.KonulanAdet, 0)),
            SevkliKonanToplam: sevkliIcerikler.Sum(si => Math.Max(si.KonulanAdet, 0)),
            Tahsissiz: false);
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
        UrunSevkDurumu.EksikSevk => "Eksik Sevk",
        UrunSevkDurumu.SevkEdilmedi => "Sevk Edilmedi",
        UrunSevkDurumu.Trafoyla => "Trafoyla",
        _ => "-"
    };

    /// <summary>
    /// Rapor özetindeki satır sayıları: "x sevk edildi / y eksik sevk / z sevk edilmedi / t trafoyla".
    /// </summary>
    public static string OzetMetni(IEnumerable<UrunSevkSonucu?> sonuclar)
    {
        var liste = sonuclar.Select(s => s?.Durum).ToList();
        return $"{liste.Count(d => d == UrunSevkDurumu.SevkEdildi)} sevk edildi / " +
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
