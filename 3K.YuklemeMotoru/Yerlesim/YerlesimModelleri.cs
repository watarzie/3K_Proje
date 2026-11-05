using _3K.YuklemeMotoru.Ekipman;

namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>Yükün özel ekipman zorunluluğu: yok, ya da her zaman özel yük ekipmanı (trafonun kendisi, isterler 10.1).</summary>
public enum OzelEkipmanZorunlulugu
{
    Yok = 0,

    /// <summary>Yük her zaman özel yük ekipmanıyla (lowbed) planlanır; genel aile tercihi bunu değiştirmez.</summary>
    Her = 1
}

/// <summary>Ekipmanın yükü alma yönü (bayrak); motor kategori bilmez, sunucu kategoriden çevirir.</summary>
[Flags]
public enum GirisYonu
{
    Yok = 0,
    Arkadan = 1,
    Yandan = 2,
    Ustten = 4
}

/// <summary>Kontrol sonucu. Veri yoksa kontrol yapılmış gibi uygunluk üretilmez: "Teyit edilmedi".</summary>
public enum KontrolSonucu
{
    Uygun = 1,
    Uygunsuz = 2,
    TeyitEdilmedi = 3
}

/// <summary>Araç kontrolünün türü (isterler 22–23).</summary>
public enum KontrolTuru
{
    Agirlik = 1,
    KapidanGecis = 2,
    IcYukseklik = 3,
    YasalYukseklik = 4,
    YasalGenislik = 5,
    Tasma = 6,
    Dingil = 7,
    BaglantiYuku = 8,
    Destek = 9,
    EkipmanVerisi = 10,
    GuvenlikPayi = 11,
    Mevzuat = 12
}

/// <summary>
/// Yerleşime giren bir fiziksel yük (adetli kaydın her birimi ayrı). Ölçüler tamsayı mm (boy X'e, en Y'ye), ağırlık kg.
/// Eksik veya geçersiz veriyle yük motora gelmez; sunucu "Veri eksik" olarak ayırır.
/// </summary>
public sealed record YerlesimYuku(
    string Anahtar,
    string Kod,
    int? ProjeId,
    string? ProjeNo,
    int? LokasyonId,
    int BoyMm,
    int EnMm,
    int YukseklikMm,
    decimal BrutKg,
    bool Dondurulebilir,
    bool UsteKonabilir,
    bool AltaKalabilir,
    decimal? UstYukSiniriKg,
    bool OzelYuk,
    OzelEkipmanZorunlulugu Zorunluluk,
    string? YukTuruAdi);

/// <summary>
/// Aday ekipman konfigürasyonu (son revizyonu). <see cref="AileIci"/> plandaki genel taşıma ailesinin adayıdır;
/// <see cref="OzelYukEkipmani"/> zorunlu özel yük için aile dışında değerlendirilebilen ekipmandır (lowbed).
/// </summary>
public sealed record EkipmanAdayi(
    int KonfigurasyonId,
    int RevizyonNo,
    string Kod,
    string Ad,
    string AileAdi,
    string TipAdi,
    bool AileIci,
    bool OzelYukEkipmani,
    GirisYonu Giris,
    bool Duvarli,
    bool Dingilli,
    bool Dogrulandi,
    EkipmanTeknikVerisi TeknikVeri);

/// <summary>Yerleşim boşluğu ve güvenlik payları (isterler 17). Boş pay "tanımlanmadı" demektir; sıfır sayılmaz.</summary>
public sealed record YerlesimAyarlari(
    int YerlesimBosluguMm,
    int? YukseklikPayiMm,
    int? GenislikPayiMm,
    int? UzunlukPayiMm,
    int? AgirlikPayiKg,
    int? YuklemeHareketiPayiMm);

/// <summary>
/// Onaylı mevzuat kural setinden gelen yasal sınırlar. Onaylı set yoksa null verilir ve yasal kontroller "Teyit
/// edilmedi" olur; taslak set kontrolde kullanılmaz. Kuralda bulunmayan değer null'dır (sıfır sayılmaz).
/// </summary>
public sealed record YasalSinirlar(int? GenislikMm, int? YukseklikMm, int? YandanTasmaMm, string KuralSetiAdi);

/// <summary>Mevcut araçlara yerleştirmede kullanıcının verdiği araç: konfigürasyon ve adet.</summary>
public sealed record MevcutArac(int KonfigurasyonId, int Adet);

/// <summary>Motorun bütün girdisi.</summary>
public sealed record YerlesimGirdisi(
    IReadOnlyList<YerlesimYuku> Yukler,
    IReadOnlyList<EkipmanAdayi> Adaylar,
    YerlesimAyarlari Ayarlar,
    bool ProjeBazindaAyri,
    bool LokasyonaGoreAyri,
    IReadOnlyList<MevcutArac>? MevcutAraclar,
    YasalSinirlar? Yasal,
    string AileAdi);

/// <summary>Araçtaki bir yükün konumu (ekipman koordinatları: X önden arkaya, Y soldan sağa, Z yerden yukarı; mm).</summary>
public sealed record YukKonumu(
    string YukAnahtari,
    int XMm,
    int YMm,
    int ZMm,
    int BoyMm,
    int EnMm,
    int YukseklikMm,
    bool Donduruldu,
    string YuzeyKodu,
    int IstifNo,
    int Kat,
    int YuklemeSirasi,
    GirisYonu YuklemeYontemi);

/// <summary>Bir kontrolün sonucu ve okunur açıklaması (ölçülen, sınır, aşım).</summary>
public sealed record AracKontrolu(KontrolTuru Tur, KontrolSonucu Sonuc, string Mesaj);

/// <summary>Plandaki bir araç: ekipman, yükler, toplamlar, taşmalar ve kontroller.</summary>
public sealed record YerlesimAraci(
    int AracNo,
    int KonfigurasyonId,
    int RevizyonNo,
    string EkipmanKodu,
    string EkipmanAdi,
    string AileAdi,
    string TipAdi,
    bool AileDisi,
    string? AileDisiGerekce,
    IReadOnlyList<int?> ProjeIdleri,
    IReadOnlyList<int?> LokasyonIdleri,
    IReadOnlyList<YukKonumu> Konumlar,
    decimal ToplamKg,
    decimal? KapasiteKg,
    int TabanDolulukYuzde,
    int? AgirlikMerkeziXMm,
    int YukluYukseklikMm,
    int YukluGenislikMm,
    int YukluUzunlukMm,
    int SolTasmaMm,
    int SagTasmaMm,
    int OnTasmaMm,
    int ArkaTasmaMm,
    IReadOnlyList<AracKontrolu> Kontroller,
    KontrolSonucu Sonuc);

/// <summary>Yerleştirilemeyen yük ve nedeni.</summary>
public sealed record YerlesemeyenYuk(string YukAnahtari, string Kod, string Neden);

/// <summary>Mevcut araçlara yerleştirmede dışarıda kalan yükler için önerilen ek ekipman (plana eklenmez).</summary>
public sealed record EkEkipmanIhtiyaci(int KonfigurasyonId, string EkipmanKodu, string EkipmanAdi, int Adet);

/// <summary>Motorun çıktısı. Şema sürümü değişirse eski sonuçlar okunurken ayırt edilir.</summary>
public sealed record YerlesimSonucu
{
    public const int GuncelSemaSurumu = 1;

    public int SemaSurumu { get; init; } = GuncelSemaSurumu;
    public IReadOnlyList<YerlesimAraci> Araclar { get; init; } = [];
    public IReadOnlyList<YerlesemeyenYuk> Yerlesemeyenler { get; init; } = [];
    public IReadOnlyList<EkEkipmanIhtiyaci> EkIhtiyaclar { get; init; } = [];
    public IReadOnlyList<string> Uyarilar { get; init; } = [];
    public KontrolSonucu Sonuc { get; init; } = KontrolSonucu.Uygun;
}
