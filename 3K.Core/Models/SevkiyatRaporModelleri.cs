namespace _3K.Core.Models;

/// <summary>
/// Sevkiyat raporu (Sevkiyatlar sayfası): süzgece uyan tır seferleri; her seferin projeleri, sandıkları ve sandık
/// içerikleri. Sandık ölçü, ağırlık, depo ve içerik değerleri raporun alındığı andaki kayıtlardır; sevk anının
/// görüntüsü ayrıca saklanmaz (sevkiyat düzeltmesi açık sandık işaretlenir).
/// </summary>
public sealed record SevkiyatRaporu(
    string Baslik,
    string SuzgecAciklamasi,
    DateTime OlusturmaTarihi,
    IReadOnlyList<SevkiyatRaporSeferi> Seferler)
{
    public int ProjeSayisi => Seferler.SelectMany(s => s.Projeler).Select(p => p.ProjeNo).Distinct(StringComparer.Ordinal).Count();
    public int SandikSayisi => Seferler.Sum(s => s.SandikSayisi);
    public decimal NetKg => Seferler.Sum(s => s.NetKg);
    public decimal BrutKg => Seferler.Sum(s => s.BrutKg);

    /// <summary>Çekici plakası bazında özet (en çok seferden aza); plakası olmayan seferler tek "Plakasız" satırında.</summary>
    public IReadOnlyList<SevkiyatRaporAraci> AracOzetleri() => Seferler
        .GroupBy(s => s.AracPlaka ?? string.Empty, StringComparer.Ordinal)
        .Select(g => new SevkiyatRaporAraci(
            g.Key.Length == 0 ? SevkiyatRaporKurali.PlakasizEtiketi : g.Key,
            g.Count(),
            g.SelectMany(s => s.Projeler).Select(p => p.ProjeNo).Distinct(StringComparer.Ordinal).Count(),
            g.Sum(s => s.SandikSayisi),
            g.Sum(s => s.BrutKg),
            g.Min(s => s.SevkTarihi),
            g.Max(s => s.SevkTarihi),
            g.SelectMany(s => s.Projeler).Select(p => p.ProjeNo).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()))
        .OrderByDescending(a => a.SeferSayisi)
        .ThenByDescending(a => a.SonSevk)
        .ThenBy(a => a.Plaka, StringComparer.Ordinal)
        .ToList();
}

/// <summary>Bir tır seferi; eski kayıtta (sefer öncesi sevkiyatlar) aynı gün ve aynı plakalı sevkiyatlar.</summary>
public sealed record SevkiyatRaporSeferi(
    string SeferNo,
    bool EskiKayit,
    string? AracPlaka,
    string? DorsePlaka,
    string? Sofor,
    DateTime SevkTarihi,
    string? Gonderen,
    string? Aciklama,
    IReadOnlyList<SevkiyatRaporProjesi> Projeler)
{
    /// <summary>Raporlarda seferin adı: sefer no; eski kayıtta "Eski kayıt".</summary>
    public string Etiket => EskiKayit || string.IsNullOrWhiteSpace(SeferNo) ? SevkiyatRaporKurali.EskiKayitEtiketi : SeferNo;
    public int SandikSayisi => Projeler.Sum(p => p.Sandiklar.Count);
    public decimal NetKg => Projeler.Sum(p => p.NetKg);
    public decimal BrutKg => Projeler.Sum(p => p.BrutKg);
    public bool DuzeltmeAcikSandikVar => Projeler.Any(p => p.Sandiklar.Any(s => s.DuzeltmeAcik));
}

/// <summary>Seferdeki bir projenin sevkiyatı (projenin kaçıncı sevkiyatı olduğu ve sandıkları).</summary>
public sealed record SevkiyatRaporProjesi(
    string ProjeNo,
    string Musteri,
    string? ProjeTipi,
    int SevkiyatNo,
    string? Lokasyon,
    IReadOnlyList<SevkiyatRaporSandigi> Sandiklar)
{
    /// <summary>Bilinen ağırlıkların toplamı; ağırlığı girilmemiş sandık toplama katılmaz.</summary>
    public decimal NetKg => Sandiklar.Sum(s => s.NetKg ?? 0);
    public decimal BrutKg => Sandiklar.Sum(s => s.BrutKg ?? 0);
}

/// <summary>Sevk edilen sandık; ölçüler mm, ağırlıklar kg.</summary>
public sealed record SevkiyatRaporSandigi(
    string SandikNo,
    string Ad,
    string? AdIngilizce,
    string? Tip,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    decimal? NetKg,
    decimal? BrutKg,
    string? DepoLokasyonu,
    bool DuzeltmeAcik,
    IReadOnlyList<SevkiyatRaporUrunu> Urunler);

/// <summary>Sandıktaki ürün (çeki satırı veya saha/yedek elle eklenen malzeme); miktar sandığa düşen tahsistir.</summary>
public sealed record SevkiyatRaporUrunu(
    string? PozNo,
    string MalzemeKodu,
    string Aciklama,
    decimal Miktar,
    decimal Konulan,
    decimal Eksik,
    string? Birim);

/// <summary>Araç (çekici plakası) özeti.</summary>
public sealed record SevkiyatRaporAraci(
    string Plaka,
    int SeferSayisi,
    int ProjeSayisi,
    int SandikSayisi,
    decimal BrutKg,
    DateTime IlkSevk,
    DateTime SonSevk,
    IReadOnlyList<string> Projeler);

/// <summary>Sevkiyat raporu sınırları ve ortak etiketler.</summary>
public static class SevkiyatRaporKurali
{
    /// <summary>Bir dönem raporunda en fazla sefer; aşılırsa tarih aralığı veya süzgeç daraltılır.</summary>
    public const int EnFazlaSefer = 1000;
    public const string EskiKayitEtiketi = "Eski kayıt";
    public const string PlakasizEtiketi = "Plakasız";
}
