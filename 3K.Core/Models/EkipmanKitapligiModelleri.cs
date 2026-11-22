using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Ekipman kitaplığı listesinin süzgeci.</summary>
public sealed record EkipmanListeKriteri(
    string? Arama,
    int? AileId,
    int? TipId,
    EkipmanDogrulamaDurumu? DogrulamaDurumu,
    bool PasifleriGoster);

/// <summary>Taşıma ailesi ve tipleri (küçük, sabit küme).</summary>
public sealed class EkipmanAilesiModeli
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public int Sira { get; init; }
    public IReadOnlyList<EkipmanTipiModeli> Tipler { get; init; } = [];
}

public sealed class EkipmanTipiModeli
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public EkipmanKategorisi Kategori { get; init; }
    public YuklemeYontemi YuklemeYontemleri { get; init; }
    public bool SandikTasimayaUygun { get; init; }
    public bool Aktif { get; init; }
}

/// <summary>Kitaplık listesinin satırı: son revizyonun özeti.</summary>
public sealed class EkipmanKonfigurasyonOzeti
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public string AileAdi { get; init; } = string.Empty;
    public string TipAdi { get; init; } = string.Empty;
    public EkipmanKategorisi Kategori { get; init; }
    public string Uretici { get; init; } = string.Empty;
    public string ModelAdi { get; init; } = string.Empty;
    public bool Aktif { get; init; }
    public int RevizyonNo { get; init; }
    public EkipmanDogrulamaDurumu DogrulamaDurumu { get; init; }
    public EkipmanKaynakTuru KaynakTuru { get; init; }
    public DateTime? KaynakTarihi { get; init; }
    public int? DisUzunlukMm { get; init; }
    public int? DisGenislikMm { get; init; }
    public int? DisYukseklikMm { get; init; }
    public int? TeknikKapasiteKg { get; init; }
}

/// <summary>Bir revizyonun kaynak ve doğrulama bilgisi.</summary>
public sealed class EkipmanRevizyonBilgisi
{
    public int RevizyonNo { get; init; }
    public EkipmanKaynakTuru KaynakTuru { get; init; }
    public string? KaynakAciklama { get; init; }
    public DateTime? KaynakTarihi { get; init; }
    public string? Aciklama { get; init; }
    public EkipmanDogrulamaDurumu DogrulamaDurumu { get; init; }
    public string? Dogrulayan { get; init; }
    public DateTime? DogrulamaTarihi { get; init; }
    public string? Olusturan { get; init; }
    public DateTime Tarih { get; init; }
}

/// <summary>
/// Konfigürasyon ayrıntısı: son revizyonun teknik verisi (motor şemasında JSON), eksik veri uyarıları ve revizyon
/// geçmişi (en yeni önce, en fazla <see cref="EkipmanKitapligiKurali.GosterilenRevizyonSayisi"/>).
/// </summary>
public sealed class EkipmanKonfigurasyonDetayi
{
    public int Id { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public bool Aktif { get; init; }
    public string AileAdi { get; init; } = string.Empty;

    /// <summary>Tipin kimliği (aileler yanıtındaki tip); ekrandaki düzenleme formu bu değerle dolar.</summary>
    public int TipId { get; init; }
    public string TipKodu { get; init; } = string.Empty;
    public string TipAdi { get; init; } = string.Empty;
    public EkipmanKategorisi Kategori { get; init; }
    public YuklemeYontemi YuklemeYontemleri { get; init; }
    public bool SandikTasimayaUygun { get; init; }
    public string Uretici { get; init; } = string.Empty;
    public string ModelAdi { get; init; } = string.Empty;
    public EkipmanRevizyonBilgisi Revizyon { get; init; } = new();
    public string TeknikVeriJson { get; init; } = "{}";
    public IReadOnlyList<string> VeriUyarilari { get; init; } = [];
    public IReadOnlyList<EkipmanRevizyonBilgisi> Revizyonlar { get; init; } = [];
}

/// <summary>
/// Ekrandan girilen konfigürasyon: Excel'deki bir konfigürasyon satırının ve alt sayfalarının karşılığı. Teknik veri
/// motor şemasında JSON'dur (ayrıntıdaki <see cref="EkipmanKonfigurasyonDetayi.TeknikVeriJson"/> ile aynı biçim).
/// </summary>
public sealed record EkipmanKonfigurasyonKaydi(
    string Kod,
    string Ad,
    int TipId,
    string Uretici,
    string ModelAdi,
    EkipmanKaynakTuru KaynakTuru,
    string? KaynakAciklama,
    DateTime? KaynakTarihi,
    string? Aciklama,
    string TeknikVeriJson);

/// <summary>
/// Ekrandan kaydın sonucu: kaydedildiyse (ya da değişiklik yoksa) güncel ayrıntı; kural sorunu varsa hiçbir şey
/// yazılmaz ve sorunlar satır satır döner.
/// </summary>
public sealed class EkipmanKayitSonucu
{
    public EkipmanKonfigurasyonDetayi? Detay { get; init; }
    public IReadOnlyList<string> Sorunlar { get; init; } = [];
}

/// <summary>
/// Excel içe aktarımının sonucu: önizlemede yalnız denetlenir; uygulamada sorun varsa hiçbir şey yazılmaz.
/// </summary>
public sealed class EkipmanIceAktarimSonucu
{
    public bool Uygulandi { get; init; }
    public int YeniSayisi { get; init; }
    public int YeniRevizyonSayisi { get; init; }
    public int DegismeyenSayisi { get; init; }
    public IReadOnlyList<EkipmanIceAktarimSatiri> Konfigurasyonlar { get; init; } = [];
    public IReadOnlyList<EkipmanIceAktarimSorunu> Sorunlar { get; init; } = [];
}

public sealed class EkipmanIceAktarimSatiri
{
    public string Kod { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;

    /// <summary>
    /// Ad değişiyorsa eski ad. Ad konfigürasyonun alanıdır, revizyon açmaz: teknik verisi aynı satır "Değişmedi" kalır
    /// ama adı güncellenir; önizleme bunu ayrıca gösterir.
    /// </summary>
    public string? OncekiAd { get; init; }
    public string TipKodu { get; init; } = string.Empty;
    public EkipmanIceAktarimIslemi Islem { get; init; }
    public int RevizyonNo { get; init; }
    public IReadOnlyList<string> VeriUyarilari { get; init; } = [];
}

/// <summary>Satır satır içe aktarım sorunu; satır Excel'deki satır numarasıdır.</summary>
public sealed record EkipmanIceAktarimSorunu(string Sayfa, int? Satir, string? KonfigurasyonKodu, string Mesaj);

/// <summary>Ekipman kitaplığının sınırları.</summary>
public static class EkipmanKitapligiKurali
{
    public const int KodEnFazla = 50;
    public const int AdEnFazla = 200;
    public const int UreticiEnFazla = 100;
    public const int ModelEnFazla = 100;
    public const int KaynakAciklamaEnFazla = 500;
    public const int AciklamaEnFazla = 1000;

    /// <summary>Tek dosyada en fazla konfigürasyon; dışa aktarım da bu sınırla yazar.</summary>
    public const int DosyaEnFazlaKonfigurasyon = 2000;

    /// <summary>Yüklenen Excel en fazla 10 MB olabilir.</summary>
    public const long DosyaEnFazlaBayt = 10L * 1024 * 1024;

    /// <summary>
    /// Bir sayfada taranacak en fazla satır (başlıklar hariç). Uzaktaki tek dolu hücre bütün sayfanın taranmasına yol
    /// açmasın diye sınır, satırlar okunmadan önce denetlenir.
    /// </summary>
    public const int SayfaEnFazlaSatir = 20_000;

    /// <summary>
    /// Konfigürasyon kodu büyük harfle saklanır ve eşlenir: dosyadaki "lb-3d" mevcut "LB-3D"nin revizyonu olur, ikinci
    /// kayıt açmaz (veritabanındaki benzersiz indeks harf duyarlıdır).
    /// </summary>
    public static string KodNormalize(string kod) => kod.Trim().ToUpperInvariant();

    /// <summary>
    /// Ekrandan gelen teknik veri JSON'unun en fazla uzunluğu (karakter). Excel'in dosya ve satır sınırlarının ekrandaki
    /// karşılığıdır: gerçek bir ekipmanın verisi bunun çok altındadır.
    /// </summary>
    public const int TeknikVeriJsonEnFazla = 1_000_000;

    /// <summary>
    /// Ekrandan girilen metin Excel hücresi gibi okunur: baştaki ve sondaki boşluk atılır, boş metin "yok" sayılır.
    /// </summary>
    public static string? MetinNormalize(string? deger)
    {
        var metin = deger?.Trim();
        return string.IsNullOrEmpty(metin) ? null : metin;
    }

    /// <summary>Zorunlu metin: kırpılınca boş değildir ve sınırı aşmaz.</summary>
    public static bool ZorunluMetinGecerli(string? deger, int enFazla) =>
        MetinNormalize(deger) is { } metin && metin.Length <= enFazla;

    /// <summary>İsteğe bağlı metin: boş olabilir, doluysa kırpılmış hali sınırı aşmaz.</summary>
    public static bool IstegeBagliMetinGecerli(string? deger, int enFazla) =>
        (MetinNormalize(deger)?.Length ?? 0) <= enFazla;

    /// <summary>
    /// Kaynak tarihi Excel'deki gibi saatsiz gündür. Saat bileşeni ya da saat dilimi (UTC/yerel) taşıyan değer çevrilirken
    /// günü kaydırabileceği için kabul edilmez; tahminle düzeltilmez.
    /// </summary>
    public static bool KaynakTarihiGecerli(DateTime? tarih) =>
        tarih is not { } gun || (gun.Kind == DateTimeKind.Unspecified && gun.TimeOfDay == TimeSpan.Zero);

    public const string KaynakTarihiMesaji = "Kaynak tarihi saatsiz gün olarak (yyyy-aa-gg) gönderilmelidir; saat ya da saat dilimi içeremez.";

    public const int GosterilenRevizyonSayisi = 50;

    public const string DogrulanmisMesaji = "Bu revizyon zaten doğrulanmış.";
    public const string GuncelRevizyonDegilMesaji = "Yalnız konfigürasyonun son revizyonu doğrulanabilir; sayfayı yenileyin.";

    public const string KodZatenVarMesaji = "Bu kodla bir ekipman zaten var; farklı bir kod girin ya da var olan ekipmanı düzenleyin.";
    public const string KodDegistirilemezMesaji = "Ekipman kodu değiştirilemez.";
    public const string KayitDegistiMesaji = "Ekipman siz açtıktan sonra değişti; sayfayı yenileyin.";
    public const string TipBulunamadiMesaji = "Seçilen ekipman tipi tanımlı değil.";
}
