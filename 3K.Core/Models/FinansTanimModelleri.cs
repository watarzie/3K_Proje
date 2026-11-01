using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record FinansDuzenliIsKaydetModel(
    int? ProjeId,
    string? ManuelProjeNo,
    string? ManuelProjeAdi,
    string IsAdi,
    FinansIsTuru IsTuru,
    string Musteri,
    string? Aciklama,
    DateTime BaslangicTarihi,
    DateTime? BitisTarihi,
    int OlusturmaGunu,
    decimal Miktar,
    string Birim,
    int? FinansUrunId,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani,
    bool Aktif,
    string? OzelIsTuru = null,
    FinansHesaplamaYontemi HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet,
    string RaporGrubu = "Özel İş");

public sealed class FinansDuzenliIsModel
{
    public int Id { get; init; }
    public int? ProjeId { get; init; }
    public string? ManuelProjeNo { get; init; }
    public string IsAdi { get; init; } = string.Empty;
    public string IsTuru { get; init; } = string.Empty;
    public FinansHesaplamaYontemi HesaplamaYontemi { get; init; }
    public string RaporGrubu { get; init; } = "Özel İş";
    public string Musteri { get; init; } = string.Empty;
    public string? Aciklama { get; init; }
    public string TekrarSikligi { get; init; } = "Aylık";
    public DateTime BaslangicTarihi { get; init; }
    public DateTime? BitisTarihi { get; init; }
    public int OlusturmaGunu { get; init; }
    public decimal Miktar { get; init; }
    public string Birim { get; init; } = string.Empty;
    public int? FinansUrunId { get; init; }
    public decimal BirimFiyat { get; init; }
    public string ParaBirimi { get; init; } = string.Empty;
    public decimal KdvOrani { get; init; }
    public bool Aktif { get; init; }
}

public sealed record FinansDonemOlusturSonucModel(int Taranan, int Olusturulan, DateTime ReferansTarihi)
{
    /// <summary>İş kuralına takıldığı için bu turda üretilemeyen şablonlar ("#id ad: neden").</summary>
    public IReadOnlyList<string> Hatalar { get; init; } = [];
}

/// <summary>
/// Ürünün iş türü eşleşmesi. <see cref="Malzeme"/> doluysa malzemeli tarifedir: kontrplak sandıkta ahşap m³
/// ahşap, kontra m³ kontra eşleşmesinin tarifesiyle fiyatlanır. Katlanır sandık tarifeyle eşleşmez, elle fiyatlanır
/// (<see cref="FinansKatlanirKurali"/>).
/// </summary>
public sealed record FinansUrunEslesmesiModel(
    int? Id,
    FinansIsTuru IsTuru,
    string? SandikAdi,
    string? SandikTipi,
    int? IcSandikSablonId,
    FinansSandikMalzemesi? Malzeme = null,
    bool Aktif = true);

/// <summary>Münferit iş formunun ürün seçeneği: fiyat taşımaz (fiyat kayıtta tarifeden gelir).</summary>
/// <summary>
/// Münferit iş formunun tarife seçeneği: istenen tarihte geçerli tarife sürümünün birim fiyatı, para birimi ve KDV'si
/// (o tarihte tarife yoksa boş). Kullanıcı tarifeyi seçtiği anda fiyatın eşleşip eşleşmediğini görür.
/// </summary>
public sealed record FinansUrunSecenekModel(
    int Id,
    string Kod,
    string Ad,
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
    decimal? BirimFiyat = null,
    string? ParaBirimi = null,
    decimal? KdvOrani = null);

public sealed class FinansUrunModel
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public FinansFiyatlandirmaBirimi FiyatlandirmaBirimi { get; init; }
    public bool Aktif { get; init; }
    public int Sira { get; init; }
    public decimal? GuncelBirimFiyat { get; init; }
    public string? GuncelParaBirimi { get; init; }
    public decimal? GuncelKdvOrani { get; init; }
    /// <summary>Bugün geçerli tarife sürümünün başlangıç ve bitiş tarihi.</summary>
    public DateTime? GuncelGecerlilikBaslangici { get; init; }
    public DateTime? GuncelGecerlilikBitisi { get; init; }
    /// <summary>Ürünün aktif tarife sürümleri (eski → yeni); fiyat geçmişi bu sürümlerde korunur.</summary>
    public IReadOnlyList<FinansTarifeSurumuModel> Tarifeler { get; init; } = Array.Empty<FinansTarifeSurumuModel>();
    public decimal BirimFiyat => GuncelBirimFiyat ?? 0m;
    public string ParaBirimi => GuncelParaBirimi ?? "EUR";
    public decimal KdvOrani => GuncelKdvOrani ?? 0m;
    public IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler { get; init; } = Array.Empty<FinansUrunEslesmesiModel>();
}

/// <summary>
/// Ürün/tarife kaydı. Fiyat verilirse <see cref="GecerlilikBaslangici"/> tarihinden (boşsa bugünden)
/// itibaren geçerli yeni tarife sürümü açılır; önceki sürüm kapatılır, geçmiş sürümler değişmez.
/// </summary>
public sealed record FinansUrunKaydetModel(
    string Kod,
    string Ad,
    FinansFiyatlandirmaBirimi FiyatlandirmaBirimi,
    bool Aktif,
    int Sira,
    IReadOnlyList<FinansUrunEslesmesiModel> Eslesmeler,
    decimal? BirimFiyat = null,
    string? ParaBirimi = null,
    decimal? KdvOrani = null,
    DateTime? GecerlilikBaslangici = null);

public sealed record FinansTarifeSurumuModel(
    int Id,
    DateTime GecerlilikBaslangici,
    DateTime GecerlilikBitisi,
    decimal BirimFiyat,
    string ParaBirimi,
    decimal KdvOrani);

/// <summary>Para birimi tanımı (F1); yeni para birimi eklendiğinde kartlar, grafikler, filtreler ve raporlar kendiliğinden gösterir.</summary>
public sealed record FinansParaBirimiModel(int Id, string Kod, string Ad, string? Sembol, bool Aktif, int Sira);

public sealed record FinansParaBirimiKaydetModel(string Kod, string Ad, string? Sembol, bool Aktif, int Sira);

/// <summary>Finans parametreleri (D3): faturanın varsayılan vade günü ve vade uyarısının kaç gün önceden verileceği.</summary>
public sealed record FinansParametrelerModel(int VarsayilanVadeGunu, int VadeUyariGunu);

/// <summary>D4 yeniden fiyatlama sonucu: kapsamdaki açık kalem sayısı ve fiyatı değişen kalem sayısı.</summary>
public sealed record FinansYenidenFiyatlamaSonucModel(int UrunId, int IncelenenKalem, int FiyatlananKalem);

/// <summary>Hatalı tarife düzeltme girdisi: sürümün doğru birim fiyatı, KDV oranı ve zorunlu gerekçe.</summary>
public sealed record FinansTarifeDuzeltModel(decimal BirimFiyat, decimal KdvOrani, string Aciklama);

/// <summary>
/// Tarife düzeltme önizlemesi. Fiyat ve tutarlar maskeli alan adlarıyla (birimFiyat, kdvOrani, netTutar) iç içe
/// nesnelerde taşınır; böylece fiyat/tutar görme yetkisi olmayan kullanıcıya HTTP sınırında null döner.
/// </summary>
public sealed class FinansTarifeDuzeltmeOnizlemeModel
{
    public int TarifeId { get; init; }
    public int UrunId { get; init; }
    public string UrunAdi { get; init; } = string.Empty;
    public string ParaBirimi { get; init; } = string.Empty;
    public DateTime GecerlilikBaslangici { get; init; }
    public DateTime GecerlilikBitisi { get; init; }
    public FinansTarifeDuzeltmeDegerModel Mevcut { get; init; } = null!;
    public FinansTarifeDuzeltmeDegerModel Yeni { get; init; } = null!;
    public int DuzeltilecekKalemSayisi { get; init; }
    public int KorunanKalemSayisi { get; init; }
    /// <summary>Önce korunan, sonra düzeltilecek kalemler; en fazla <see cref="EnFazlaKalem"/> satır.</summary>
    public IReadOnlyList<FinansTarifeDuzeltmeKalemModel> Kalemler { get; init; } = Array.Empty<FinansTarifeDuzeltmeKalemModel>();
    public bool KalemListesiKisaltildi { get; init; }

    public const int EnFazlaKalem = 200;
}

/// <summary>Sürüm değeri ve düzeltilecek kalemlerin toplam net bedeli (mevcut ya da yeni fiyatla).</summary>
public sealed record FinansTarifeDuzeltmeDegerModel(decimal BirimFiyat, decimal KdvOrani, decimal NetTutar);

/// <summary>Sürümle fiyatlanmış kalem; Duzeltilir false ise KorunmaNedeni doludur.</summary>
public sealed record FinansTarifeDuzeltmeKalemModel(
    int IsKaydiId,
    string ProjeNo,
    string IsAdi,
    DateTime FinansTarihi,
    string KaynakTuru,
    bool Duzeltilir,
    string? KorunmaNedeni,
    decimal NetTutar);

/// <summary>Tarife düzeltme sonucu: düzeltilen ve korunan kalem sayıları.</summary>
public sealed record FinansTarifeDuzeltmeSonucModel(int TarifeId, int DuzeltilenKalemSayisi, int KorunanKalemSayisi);
