using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record class FinansIsKaydiModel
{
    public int Id { get; init; }
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public bool ManuelProjeMi { get; init; }
    public string IsAdi { get; init; } = string.Empty;
    public string? OzelIsTuru { get; init; }
    public FinansHesaplamaYontemi? HesaplamaYontemi { get; init; }
    public string? RaporGrubu { get; init; }
    public string? Aciklama { get; init; }
    public string? TalepEdenKisi { get; init; }
    public string? TalepEdenBolum { get; init; }
    public FinansIsTuru IsTuru { get; init; }
    public FinansIsKategorisi Kategori { get; init; }
    public int? DuzenliIsId { get; init; }
    public string? SandikNo { get; init; }
    public string? SandikAdi { get; init; }
    public string? SandikTipi { get; init; }
    /// <summary>Ambalaj kaynaklı işin sandık cinsi; raporda <see cref="FinansEtiketleri.SandikCinsi"/> ile yazılır.</summary>
    public AmbalajSandikCinsi? SandikCinsi { get; init; }
    public decimal? Boy { get; init; }
    public decimal? En { get; init; }
    public decimal? Yukseklik { get; init; }
    public int? IcSandikSablonId { get; init; }
    public decimal Adet { get; init; }
    public string Birim { get; init; } = string.Empty;
    public decimal BirimM3 { get; init; }
    public decimal ToplamM3 { get; init; }
    /// <summary>
    /// Kaydın ahşap ve kontra m³ payı: elle girildiyse saklanan değer, değilse türetilen pay (F11;
    /// <see cref="FinansHacimKurallari"/>). Bileşik toplam (ahşap + kontra) <see cref="ToplamM3"/>'tür.
    /// </summary>
    public decimal AhsapM3 { get; init; }
    public decimal KontraM3 { get; init; }
    /// <summary>Hacim "Hacim" bölümünden elle girildi; üretim aktarımı m³ değerlerini ezmez.</summary>
    public bool HacimManuel { get; init; }
    /// <summary>Sandığın malzemesi (ahşap / kontra; katlanırda bilinmiyor); ekran hacim girişini buna göre açar.</summary>
    public FinansSandikMalzemesi SandikMalzemesi { get; init; }
    /// <summary>
    /// Katlanır sandık: fiyat tarifeden ve m³'ten hesaplanmaz, elle girilir (adet × birim fiyat veya net toplam;
    /// <see cref="FinansKatlanirKurali"/>). Ekran m³ yöntemini ve hacim girişini kapatır.
    /// </summary>
    public bool ElleFiyatlanir { get; init; }
    public int? FinansUrunId { get; init; }
    public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
    public decimal BirimFiyat { get; init; }
    /// <summary>
    /// Malzemeli fiyat (kontrplak; eski katlanır kayıtları): doluysa <see cref="BirimFiyat"/> kontra m³ fiyatı, bu ahşap m³ fiyatıdır;
    /// net = kontra m³ × BirimFiyat + ahşap m³ × AhsapBirimFiyat. Boşsa iş tek tarifeyle fiyatlanır.
    /// </summary>
    public decimal? AhsapBirimFiyat { get; init; }
    public int? AhsapFinansUrunId { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal KdvOrani { get; init; }
    public int? TarifeYili { get; init; }
    public decimal NetTutar { get; init; }
    public decimal KdvTutari { get; init; }
    public decimal ToplamTutar { get; init; }
    public DateTime UretimTarihi { get; init; }
    public DateTime FinansDonemi { get; init; }
    public DateTime FinansTarihi { get; init; }
    public bool FinansTarihiManuel { get; init; }
    public bool FinansMiktariManuel { get; init; }
    public string KaynakBileseni { get; init; } = "NET";
    public bool FiyatlandirmaHazir { get; init; }
    public decimal SiparisNetTutar { get; init; }
    public decimal FaturalananNetTutar { get; init; }
    public decimal KalanSiparisNetTutar { get; init; }
    public decimal KalanFaturaNetTutar { get; init; }
    public decimal? ManuelNetTutar { get; init; }
    public IReadOnlyList<FinansFiyatBileseniModel>? Bilesenler { get; init; }
    public DateTime KayitTarihi { get; init; }
    public FinansIsDurumu Durum { get; init; }
    public decimal SiparisAdedi { get; init; }
    public decimal SiparisM3 { get; init; }
    public decimal SiparisBekleyenAdet { get; init; }
    public decimal SiparisBekleyenM3 { get; init; }
    public decimal FaturalananAdet { get; init; }
    public decimal FaturalananM3 { get; init; }
    public IReadOnlyList<string> PoNumaralari { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FaturaNumaralari { get; init; } = Array.Empty<string>();
    public string KaynakTuru { get; init; } = string.Empty;
    public string? KaynakKayitId { get; init; }
    public bool KaynakAktif { get; init; }
    public bool IptalEdildi { get; init; }
    public string? IptalAciklamasi { get; init; }
    public DateTime CreatedDate { get; init; }
    public string? CreatedBy { get; init; }
    /// <summary>
    /// PO öncesi onay durumu (<see cref="FinansPoOnayKurali"/>). Liste, seçim ve detay uçlarında doldurulur; panel ve
    /// rapor hesaplarında kullanılmaz.
    /// </summary>
    public FinansPoOnayDurumu PoOnayDurumu { get; init; }
    /// <summary>Ret nedeni veya onayın düşme/uygulanamama nedeni.</summary>
    public string? PoOnayAciklamasi { get; init; }
    public DateTime? PoOnayTarihi { get; init; }
}

/// <summary>
/// Finans formlarında proje seçimi için kullanılan, parasal veya operasyonel
/// özet taşımayan güvenli seçenek modelidir.
/// </summary>
public sealed record FinansProjeSecenekModel(
    int ProjeId,
    string ProjeNo,
    string Musteri);

public sealed record FinansIsKaydiKaydetModel(
    int? ProjeId,
    string? ManuelProjeNo,
    string? ManuelProjeAdi,
    string? Musteri,
    FinansIsTuru IsTuru,
    string IsAdi,
    string? Aciklama,
    string? TalepEdenKisi,
    string? TalepEdenBolum,
    decimal Adet,
    string Birim,
    decimal BirimM3,
    int? FinansUrunId,
    decimal? ManuelBirimFiyat,
    string? ParaBirimi,
    decimal? KdvOrani,
    DateTime UretimTarihi,
    DateTime FinansDonemi,
    string? SandikNo = null,
    string? SandikAdi = null,
    string? SandikTipi = null,
    decimal? Boy = null,
    decimal? En = null,
    decimal? Yukseklik = null,
    int? IcSandikSablonId = null,
    string? OzelIsTuru = null,
    FinansHesaplamaYontemi? HesaplamaYontemi = null,
    string? RaporGrubu = null,
    decimal? AhsapM3 = null,
    decimal? KontraM3 = null);

/// <summary>
/// Münferit (tek seferlik) iş: fiyat elle yazılmaz, seçilen finans ürününün iş tarihindeki tarifesinden gelir.
/// Ürün m³ ile fiyatlanıyorsa ahşap/kontra m³ zorunludur; bileşik toplam fiyatlandırma miktarıdır.
/// </summary>
/// <summary>
/// Münferit iş: İş ve Tarife Tanımları'ndan seçilen tarife, proje (sistem projesi ya da serbest proje adı) ve tarih.
/// İş adı tarifenin adıdır; fiyat, para birimi ve KDV tarifenin o tarihteki sürümünden gelir. Miktar tarifenin
/// birimine göredir: adet tarifesinde <see cref="Adet"/>, m³ tarifesinde ahşap/kontra m³, sabit tutarda 1.
/// </summary>
public sealed record FinansMunferitIsKaydetModel(
    int FinansUrunId,
    int? ProjeId,
    string? ProjeAdi,
    DateTime IsTarihi,
    decimal Adet = 1,
    decimal? AhsapM3 = null,
    decimal? KontraM3 = null);

/// <summary>Elle ahşap/kontra m³ girişi; gerekçe isteğe bağlıdır.</summary>
public sealed record FinansHacimModel(decimal AhsapM3, decimal KontraM3, string? Aciklama = null);

public sealed record FinansAylikDegerModel(decimal? Miktar = null, decimal? NetBirimFiyat = null);

public sealed record FinansTarihiDegistirModel(DateTime FinansTarihi, string Aciklama);

public sealed record FinansFiyatlandirmaModel(
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi, decimal Adet, decimal BirimM3,
    decimal BirimFiyat, string ParaBirimi, decimal KdvOrani, string Aciklama,
    decimal? ManuelNetTutar = null,
    IReadOnlyList<FinansFiyatBileseniModel>? Bilesenler = null);

/// <summary>
/// Fiyat satırı (F11): kontrplak sandıkta birden çok m³ satırı olabilir; her satır m³, birim fiyat ve
/// malzemesini taşır. Malzeme boşsa sandığın kendi malzemesi sayılır.
/// </summary>
public sealed record FinansFiyatBileseniModel(
    string Ad,
    FinansFiyatlandirmaBirimi Yontem,
    decimal Miktar,
    decimal BirimFiyat,
    FinansSandikMalzemesi? Malzeme = null);

/// <summary>
/// Üretim modülünün finans bağımlılığı olmadan yayınlayacağı idempotent aktarım sözleşmesi.
/// Aynı KaynakTuru + KaynakKayitId tekrar gönderildiğinde yeni kayıt açılmaz.
/// </summary>
public sealed record FinansUretimAktarimModel(
    string KaynakTuru,
    string KaynakKayitId,
    bool KaynakAktif,
    int? ProjeId,
    string ProjeNo,
    string Musteri,
    FinansIsTuru IsTuru,
    string IsAdi,
    decimal Adet,
    decimal BirimM3,
    DateTime UretimTarihi,
    DateTime FinansDonemi,
    string? SandikNo = null,
    string? SandikAdi = null,
    string? SandikTipi = null,
    decimal? Boy = null,
    decimal? En = null,
    decimal? Yukseklik = null,
    int? IcSandikSablonId = null,
    string? Aciklama = null,
    string? TalepEdenKisi = null,
    string? TalepEdenBolum = null,
    string KaynakBileseni = "NET",
    AmbalajSandikCinsi? SandikCinsi = null,
    decimal? SarfM3 = null,
    bool UretimM3Hesaplanabilir = true,
    DateTime? FinansTarihi = null);

public sealed record FinansSenkronizasyonSonucModel(int Olusturulan, int Guncellenen, int Pasiflestirilen);

/// <summary>
/// Referans Finans ekranının aylık proje/özel iş kırılımı. Miktar ve para
/// hesapları backendde decimal olarak gruplanır; önyüz yalnız gösterir.
/// </summary>
public sealed record FinansAylikIsModel(
    string KaynakTuru,
    int? OzelIsId,
    int? ProjeId,
    string ProjeBirimAnahtari,
    string Musteri,
    FinansIsTuru IsTuru,
    string IsGrubu,
    string ProjeNo,
    string IsAdi,
    string? SandikTipi,
    decimal? Boy,
    decimal? En,
    decimal? Yukseklik,
    DateTime UretimBaslangic,
    DateTime UretimBitis,
    decimal SandikAdedi,
    decimal Miktar,
    string Birim,
    decimal BirimFiyat,
    decimal KdvOrani,
    decimal NetTutar,
    decimal KdvTutari,
    decimal ToplamTutar,
    string ParaBirimi,
    decimal SiparisMiktari,
    decimal FaturalananMiktar,
    decimal SiparisToplamTutar,
    decimal FaturalananToplamTutar,
    IReadOnlyList<int> IsKaydiIds,
    IReadOnlyList<string> PoNumaralari,
    IReadOnlyList<string> FaturaNumaralari,
    string Durum,
    bool MiktarDuzenlenebilir,
    bool TutarDuzenlenebilir,
    bool IptalEdildi,
    string? IptalAciklamasi,
    FinansIsKategorisi Kategori = FinansIsKategorisi.AnaAmbalaj,
    FinansKarsilanma SiparisKarsilanma = FinansKarsilanma.Yok,
    FinansKarsilanma FaturaKarsilanma = FinansKarsilanma.Yok,
    decimal AhsapM3 = 0,
    decimal KontraM3 = 0,
    decimal KalanSiparisNetTutar = 0,
    decimal KalanFaturaNetTutar = 0,
    decimal? AhsapBirimFiyat = null)
{
    /// <summary>Satırın kalemlerinden birleşik PO öncesi onay durumu (<see cref="FinansPoOnayKurali.Birlesik"/>).</summary>
    public FinansPoOnayDurumu PoOnayDurumu { get; init; }
    /// <summary>Onay kapsamındaki aktif kalem sayısı ve bunlardan onayı geçerli olanlar.</summary>
    public int PoOnayGerekliKalemSayisi { get; init; }
    public int PoOnayliKalemSayisi { get; init; }
    public string? PoOnayAciklamasi { get; init; }
}

/// <summary>
/// Aylık PO/fatura alanında proje satırının özeti: projenin ayın bütün aktif kalemlerinden türetilen
/// sipariş ve fatura karşılanması, akış durumu, ahşap/kontra m³ ve para birimi bazında tutarlar.
/// </summary>
public sealed record FinansAylikProjeOzetiModel(
    string ProjeBirimAnahtari,
    int? ProjeId,
    string ProjeNo,
    string Musteri,
    FinansKarsilanma SiparisKarsilanma,
    FinansKarsilanma FaturaKarsilanma,
    FinansIsDurumu Durum,
    int GerekliKalem,
    int FiyatBekleyenKalem,
    decimal AhsapM3,
    decimal KontraM3,
    IReadOnlyList<FinansProjeTutarModel> Tutarlar);

/// <summary>Projenin tek para birimindeki net bedel, siparişe bağlanan ve faturalanan net tutarı.</summary>
public sealed record FinansProjeTutarModel(
    string ParaBirimi,
    decimal NetTutar,
    decimal SiparisNetTutar,
    decimal FaturalananNetTutar);

/// <summary>
/// Aylık iş takibinde sayfalama birimi proje veya tek bir özel iştir. Böylece
/// aynı projenin ana/alt iş satırları farklı sayfalara bölünmez.
/// </summary>
public sealed class FinansAylikSayfaliSonuc
{
    public IReadOnlyList<FinansAylikIsModel> Items { get; init; } = Array.Empty<FinansAylikIsModel>();
    /// <summary>Sayfadaki proje birimlerinin (özel işler hariç) birleşik sipariş/fatura durumu.</summary>
    public IReadOnlyList<FinansAylikProjeOzetiModel> Projeler { get; init; } = Array.Empty<FinansAylikProjeOzetiModel>();
    /// <summary>
    /// Sayfadaki projelere bağlı, aynı filtrelere uyan özel/düzenli işler (proje bloğunun "Özel/Düzenli" alt grubu).
    /// Sayfalama birimi değişmez: bu işler kendi birimleriyle <see cref="Items"/> içinde de yer alabilir.
    /// </summary>
    public IReadOnlyList<FinansAylikIsModel> ProjeOzelIsleri { get; init; } = Array.Empty<FinansAylikIsModel>();
    public IReadOnlyList<FinansAylikFinansOzetiModel> FinansOzeti { get; init; } = Array.Empty<FinansAylikFinansOzetiModel>();
    public IReadOnlyList<FinansAylikGrupToplamiModel> GrupToplamlari { get; init; } = Array.Empty<FinansAylikGrupToplamiModel>();
    /// <summary>PO öncesi iş onayı kuralı açık mı; kapalıyken PO onay aramaz ve "Onaya gönder" gösterilmez.</summary>
    public bool PoOnayKuraliAcik { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public sealed record FinansAylikFinansOzetiModel(
    string ParaBirimi,
    decimal Toplam,
    decimal SiparisAcik,
    decimal SiparisBekleyen,
    decimal Faturalanan,
    decimal FaturaBekleyen,
    decimal Gider,
    decimal Net);

public sealed record FinansAylikGrupToplamiModel(
    string Grup,
    string ParaBirimi,
    decimal NetTutar,
    decimal KdvTutari,
    decimal ToplamTutar);
