using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// İş Takibi proje → sandık kırılımı isteği. Proje anahtarı <see cref="ProjeId"/> ya da sisteme bağlı olmayan proje
/// için <see cref="ProjeNo"/> (+ verilirse <see cref="Musteri"/>) olur. <see cref="Yil"/>/<see cref="Ay"/> verilirse
/// yalnız o finans dönemi alınır; <see cref="Filtre"/> liste filtresinin proje dışındaki alanlarıdır (sayfalama yok).
/// </summary>
public sealed record FinansProjeSandikKirilimiIstegi(
    int? ProjeId,
    string? ProjeNo,
    string? Musteri,
    int? Yil,
    int? Ay,
    FinansListeFiltre Filtre);

/// <summary>Kırılımın girdisi: bir iş kaydının veritabanında hesaplanmış tutarları ve kayıt kuralıyla bulunmuş hacmi.</summary>
public sealed record FinansSandikKirilimKaydi(
    int Id,
    FinansIsTuru IsTuru,
    string KaynakBileseni,
    string? KaynakKayitId,
    string? SandikNo,
    string? SandikAdi,
    string? SandikTipi,
    AmbalajSandikCinsi? SandikCinsi,
    string IsAdi,
    DateTime FinansTarihi,
    decimal Adet,
    decimal ToplamM3,
    decimal AhsapM3,
    decimal KontraM3,
    string ParaBirimi,
    decimal Net,
    decimal Siparis,
    decimal Fatura,
    bool Aktif,
    IReadOnlyList<string> PoNumaralari,
    IReadOnlyList<string> FaturaNumaralari);

/// <summary>Projenin sandık kırılımı: sandık başına bir satır; sandığın kayıtları (NET/SARF) satırın altındadır.</summary>
public sealed class FinansProjeSandikKirilimiModel
{
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public int KayitSayisi { get; init; }
    public IReadOnlyList<FinansSandikKirilimSatiriModel> Sandiklar { get; init; } = Array.Empty<FinansSandikKirilimSatiriModel>();
}

/// <summary>
/// Bir sandığın finans özeti. Tutarlar sandığın aktif kayıtlarının (NET ve SARF) toplamıdır; adet ve m³ yalnız
/// sandığın kendi (NET) kayıtlarından gelir, sarf kereste hacmi sandık hacmine eklenmez. Durum, aktif kayıtların
/// en düşük iş durumudur.
/// </summary>
public sealed class FinansSandikKirilimSatiriModel
{
    public string Anahtar { get; init; } = string.Empty;
    public string? SandikNo { get; init; }
    public string? SandikAdi { get; init; }
    public string? SandikTipi { get; init; }
    public AmbalajSandikCinsi? SandikCinsi { get; init; }
    public FinansSandikMalzemesi SandikMalzemesi { get; init; }
    public int KayitSayisi { get; init; }
    /// <summary>Bedeli henüz olmayan (fiyat bekleyen) aktif kayıt sayısı.</summary>
    public int FiyatBekleyenKayitSayisi { get; init; }
    public decimal Adet { get; init; }
    public decimal ToplamM3 { get; init; }
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal NetTutar { get; init; }
    public decimal SiparisNetTutar { get; init; }
    public decimal FaturalananNetTutar { get; init; }
    public FinansIsDurumu Durum { get; init; }
    public DateTime IlkTarih { get; init; }
    public DateTime SonTarih { get; init; }
    public IReadOnlyList<string> PoNumaralari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<FinansSandikKirilimKalemModel> Kayitlar { get; init; } = Array.Empty<FinansSandikKirilimKalemModel>();
}

/// <summary>Sandığın altındaki iş kaydı; sarf satırı <see cref="KaynakBileseni"/> = "SARF" ile ayrılır.</summary>
public sealed class FinansSandikKirilimKalemModel
{
    public int Id { get; init; }
    public FinansIsTuru IsTuru { get; init; }
    public FinansIsKategorisi Kategori { get; init; }
    public string KaynakBileseni { get; init; } = "NET";
    public string IsAdi { get; init; } = string.Empty;
    public DateTime FinansTarihi { get; init; }
    public decimal Adet { get; init; }
    public decimal ToplamM3 { get; init; }
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal NetTutar { get; init; }
    public decimal SiparisNetTutar { get; init; }
    public decimal FaturalananNetTutar { get; init; }
    public FinansIsDurumu Durum { get; init; }
    /// <summary>Kayıt iptal edilmemiş ve kaynağı (üretim kaydı) hâlâ aktif.</summary>
    public bool Aktif { get; init; }
    public IReadOnlyList<string> PoNumaralari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Proje → sandık kırılımının gruplama kuralı. Sandık; sandık no ve kaynak üretim kaydı (aynı sandığın NET ve SARF
/// kayıtları aynı kaynaktan gelir) ile ayrılır; farklı para birimli kayıtlar toplanamayacağı için ayrı satırdır.
/// Sandık bilgisi olmayan kayıtlar (elle girilmiş proje işleri) sandık no'suz satırda toplanır.
/// </summary>
public static class FinansSandikKirilimiKurali
{
    public const string SarfBileseni = "SARF";

    /// <summary>Sarf kereste satırı: sandığın tüketim payıdır, hacmi ve adedi sandığa eklenmez.</summary>
    public static bool Sarf(FinansIsTuru isTuru, string? kaynakBileseni) =>
        isTuru == FinansIsTuru.SarfKereste ||
        string.Equals(kaynakBileseni, SarfBileseni, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<FinansSandikKirilimSatiriModel> Olustur(IEnumerable<FinansSandikKirilimKaydi> kayitlar)
    {
        ArgumentNullException.ThrowIfNull(kayitlar);
        return kayitlar
            .GroupBy(x => (SandikNo: Metin(x.SandikNo), KaynakKayitId: Metin(x.KaynakKayitId), x.ParaBirimi))
            .Select(grup => Satir(grup.Key.SandikNo, grup.Key.KaynakKayitId, grup.Key.ParaBirimi, grup.ToList()))
            // Sandık no'lu satırlar önce; aynı önekli numaralar uzunluk + sıra ile doğal sırada (9, 10, 11).
            .OrderBy(x => x.SandikNo is null)
            .ThenBy(x => x.SandikNo?.Length ?? 0)
            .ThenBy(x => x.SandikNo, StringComparer.Ordinal)
            .ThenBy(x => x.ParaBirimi, StringComparer.Ordinal)
            .ThenBy(x => x.Anahtar, StringComparer.Ordinal)
            .ToArray();
    }

    private static FinansSandikKirilimSatiriModel Satir(
        string? sandikNo,
        string? kaynakKayitId,
        string paraBirimi,
        IReadOnlyCollection<FinansSandikKirilimKaydi> grup)
    {
        var liste = grup
            .OrderBy(x => Sarf(x.IsTuru, x.KaynakBileseni))
            .ThenBy(x => (int)x.IsTuru)
            .ThenBy(x => x.Id)
            .ToList();
        var aktif = liste.Where(x => x.Aktif).ToList();
        var sandigin = aktif.Where(x => !Sarf(x.IsTuru, x.KaynakBileseni)).ToList();
        var sandikCinsi = liste.Select(x => x.SandikCinsi).FirstOrDefault(x => x.HasValue);
        var sandikTipi = liste.Select(x => Metin(x.SandikTipi)).FirstOrDefault(x => x is not null);
        return new FinansSandikKirilimSatiriModel
        {
            Anahtar = $"{sandikNo}\u001f{kaynakKayitId}\u001f{paraBirimi}",
            SandikNo = sandikNo,
            SandikAdi = liste.Select(x => Metin(x.SandikAdi)).FirstOrDefault(x => x is not null),
            SandikTipi = sandikTipi,
            SandikCinsi = sandikCinsi,
            SandikMalzemesi = FinansHacimKurallari.Malzeme(sandikCinsi, sandikTipi),
            KayitSayisi = liste.Count,
            FiyatBekleyenKayitSayisi = aktif.Count(x => x.Net <= 0),
            Adet = sandigin.Sum(x => x.Adet),
            ToplamM3 = sandigin.Sum(x => x.ToplamM3),
            AhsapM3 = sandigin.Sum(x => x.AhsapM3),
            KontraM3 = sandigin.Sum(x => x.KontraM3),
            ParaBirimi = paraBirimi,
            NetTutar = aktif.Sum(x => x.Net),
            SiparisNetTutar = aktif.Sum(x => x.Siparis),
            FaturalananNetTutar = aktif.Sum(x => x.Fatura),
            Durum = aktif.Count == 0
                ? FinansIsDurumu.IptalEdildi
                : aktif.Min(x => FinansTutarKurallari.Durum(true, x.Net, x.Siparis, x.Fatura)),
            IlkTarih = liste.Min(x => x.FinansTarihi),
            SonTarih = liste.Max(x => x.FinansTarihi),
            PoNumaralari = liste.SelectMany(x => x.PoNumaralari).Distinct().Order(StringComparer.Ordinal).ToArray(),
            FaturaNumaralari = liste.SelectMany(x => x.FaturaNumaralari).Distinct().Order(StringComparer.Ordinal).ToArray(),
            Kayitlar = liste.Select(Kalem).ToArray()
        };
    }

    private static FinansSandikKirilimKalemModel Kalem(FinansSandikKirilimKaydi x) => new()
    {
        Id = x.Id,
        IsTuru = x.IsTuru,
        Kategori = FinansKategoriKurali.Kategori(x.IsTuru, null),
        KaynakBileseni = Sarf(x.IsTuru, x.KaynakBileseni) ? SarfBileseni : x.KaynakBileseni,
        IsAdi = x.IsAdi,
        FinansTarihi = x.FinansTarihi,
        Adet = x.Adet,
        ToplamM3 = x.ToplamM3,
        AhsapM3 = x.AhsapM3,
        KontraM3 = x.KontraM3,
        ParaBirimi = x.ParaBirimi,
        NetTutar = x.Net,
        SiparisNetTutar = x.Siparis,
        FaturalananNetTutar = x.Fatura,
        Durum = FinansTutarKurallari.Durum(x.Aktif, x.Net, x.Siparis, x.Fatura),
        Aktif = x.Aktif,
        PoNumaralari = x.PoNumaralari,
        FaturaNumaralari = x.FaturaNumaralari
    };

    private static string? Metin(string? deger) => string.IsNullOrWhiteSpace(deger) ? null : deger.Trim();
}
