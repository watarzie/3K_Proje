using _3K.Core.Common;

namespace _3K.Core.Models;

/// <summary>
/// Müşteriye verilen gerçekleşen çeki listesi ("Gerçekleşen Çeki Listesi / Actual Packing List"): yalnız projenin
/// sevkiyat kayıtlarındaki sandıklar ve bu sandıklara fiilen konulan ürünler. İç rapordaki kalan, trafoda, Grid/3K
/// durumu ve sevk edilmemiş sandıklar yoktur. Veri sevkiyat raporuyla aynı kaynaktan kurulur (<see cref="Kur"/>); PDF ve
/// Excel aynı modeli yazar.
/// </summary>
public sealed record MusteriCekiListesi(
    string ProjeNo,
    string Musteri,
    string? Lokasyon,
    DateTime OlusturmaTarihi,
    IReadOnlyList<MusteriCekiSandigi> Sandiklar)
{
    /// <summary>Bilinen ağırlıkların toplamı; ağırlığı girilmemiş sandık toplama katılmaz.</summary>
    public decimal NetKg => Sandiklar.Sum(s => s.NetKg ?? 0);
    public decimal BrutKg => Sandiklar.Sum(s => s.BrutKg ?? 0);
    public int UrunSatiri => Sandiklar.Sum(s => s.Urunler.Count);

    /// <summary>
    /// Sevkiyat raporu seferlerinden (yalnız bu projenin sevkiyatları) müşteri listesini kurar. Sandıklar doğal sandık no
    /// sırasıyla; ürünlerden yalnız sandığa konulan miktarı olanlar, konulan miktarla yazılır (Grid'de kapanan ürün
    /// tamamı konulmuş sayılır — sevkiyat raporunun kuralı). Aynı sandık birden çok sevkiyatta görünürse en son sevk
    /// edildiği satır kalır.
    /// </summary>
    public static MusteriCekiListesi Kur(
        string projeNo,
        string musteri,
        string? lokasyon,
        DateTime olusturmaTarihi,
        IEnumerable<SevkiyatRaporSeferi> seferler)
    {
        var sandiklar = seferler
            .SelectMany(sefer => sefer.Projeler
                .Where(p => p.ProjeNo == projeNo)
                .SelectMany(p => p.Sandiklar.Select(sandik => (Sefer: sefer, Sandik: sandik))))
            .GroupBy(x => x.Sandik.SandikNo, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => x.Sefer.SevkTarihi).First())
            .OrderBy(x => x.Sandik.SandikNo, SandikNumarasiComparer.Instance)
            .Select(x => new MusteriCekiSandigi(
                x.Sandik.SandikNo,
                x.Sandik.Ad,
                x.Sandik.AdIngilizce,
                x.Sandik.Boy,
                x.Sandik.En,
                x.Sandik.Yukseklik,
                x.Sandik.NetKg,
                x.Sandik.BrutKg,
                x.Sefer.SevkTarihi,
                x.Sefer.AracPlaka,
                x.Sandik.Urunler
                    .Where(u => u.Konulan > 0)
                    .Select(u => new MusteriCekiUrunu(u.PozNo, u.MalzemeKodu, u.Aciklama, u.Konulan, u.Birim))
                    .ToList()))
            .ToList();
        return new MusteriCekiListesi(projeNo, musteri, lokasyon, olusturmaTarihi, sandiklar);
    }
}

/// <summary>
/// Sevk edilen sandık; ölçüler mm, ağırlıklar kg. Sevk tarihi ve araç müşteri belgesine yazılmaz; aynı sandığın son
/// sevkini seçmek için tutulur.
/// </summary>
public sealed record MusteriCekiSandigi(
    string SandikNo,
    string Ad,
    string? AdIngilizce,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    decimal? NetKg,
    decimal? BrutKg,
    DateTime SevkTarihi,
    string? AracPlaka,
    IReadOnlyList<MusteriCekiUrunu> Urunler);

/// <summary>Sevk edilen sandıktaki ürün; miktar sandığa fiilen konulan miktardır.</summary>
public sealed record MusteriCekiUrunu(
    string? PozNo,
    string MalzemeKodu,
    string Aciklama,
    decimal Miktar,
    string? Birim);
