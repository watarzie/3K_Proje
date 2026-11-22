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

/// <summary>
/// Kontrol sonucu. Veri yoksa kontrol yapılmış gibi uygunluk üretilmez: "Teyit edilmedi". Teyit edilmeyen kontrol
/// araçta gösterilmez ve araç sonucuna katılmaz (AracKontrolcu.Gosterilecekler).
/// </summary>
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

    /// <summary>Artık üretilmez (mevzuat kuralları kaldırıldı, 20 Kasım 2026); değer eski kayıtlar için ayrılmıştır.</summary>
    YasalYukseklik = 4,

    /// <summary>Artık üretilmez; bkz. <see cref="YasalYukseklik"/>.</summary>
    YasalGenislik = 5,
    Tasma = 6,
    Dingil = 7,
    BaglantiYuku = 8,
    Destek = 9,
    EkipmanVerisi = 10,
    GuvenlikPayi = 11,

    /// <summary>Artık üretilmez; bkz. <see cref="YasalYukseklik"/>.</summary>
    Mevzuat = 12,

    /// <summary>Proje / lokasyon ayrımı ve zorunlu ekipman gibi plan tercihlerine uyum (elle düzenlemede).</summary>
    Organizasyon = 13,

    /// <summary>Elle yerleşimde yük düzeyindeki sorunların özeti (çakışma, destek, istif izni, giriş…).</summary>
    ElleYerlesim = 14,

    /// <summary>Yükün ağırlık merkezinin araç ortasından boyuna ve enine sapması.</summary>
    AgirlikDagilimi = 15
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
/// <see cref="OzelYukEkipmani"/> aileye sığmayan ya da özel ekipman zorunlu yük için aile dışında değerlendirilebilen
/// ekipmandır (lowbed, ağır yük dorsesi).
/// <para>
/// <see cref="Kademe"/>: aile adayının sadelik sırası (küçük = sade, ör. standart konteyner, sonra High Cube, Hardtop,
/// Open Top…). Her yük kendisini alan en sade kademeye gider; özel kademe aracına sığan daha sade yükler de konur, kalan
/// yükler kendi kademelerinin araçlarına gider. Boş kademe aday otomatik yerleşimde kullanılmaz (ör. Reefer, açıkça
/// seçilmedikçe). <see cref="Yedek"/>: kullanıcı tip seçtiğinde seçilmeyen aile tipi; yalnız seçilen tiplere sığmayan
/// yük için ve gerekçesiyle kullanılır.
/// </para>
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
    EkipmanTeknikVerisi TeknikVeri,
    int? Kademe = 1,
    bool Yedek = false);

/// <summary>
/// Yerleşim boşluğu ve güvenlik payları (isterler 17). Boş pay "tanımlanmadı" demektir; sıfır sayılmaz.
/// <see cref="AgirlikMerkeziSapmaYuzde"/>: yük ağırlık merkezinin araç ortasından izin verilen en fazla sapması (araç
/// boyunun ve genişliğinin yüzdesi); boşsa ağırlık dağılımı ölçülür ama "Teyit edilmedi" kalır.
/// </summary>
public sealed record YerlesimAyarlari(
    int YerlesimBosluguMm,
    int? YukseklikPayiMm,
    int? GenislikPayiMm,
    int? UzunlukPayiMm,
    int? AgirlikPayiKg,
    int? YuklemeHareketiPayiMm,
    int? AgirlikMerkeziSapmaYuzde = null);

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
    bool ZorunluOzelEkipman,
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
    KontrolSonucu Sonuc)
{
    /// <summary>Elle yerleşimde yük düzeyindeki sorunlar (yük anahtarı, sonuç, ölçülü açıklama); otomatik yerleşimde boştur.</summary>
    public IReadOnlyList<YukSorunu> YukSorunlari { get; init; } = [];
}

/// <summary>Bir yükün elle yerleşimdeki sorunu (isterler 22: yön ve miktar, ilgili yük kodları, sınır ve aşım).</summary>
public sealed record YukSorunu(string YukAnahtari, KontrolSonucu Sonuc, string Mesaj, KontrolTuru Tur = KontrolTuru.ElleYerlesim);

/// <summary>Elle yerleşimde bir yükün konumu (ekipman koordinatları, mm); ölçüler yükün kendisinden, döndürmeyle.</summary>
public sealed record ElleKonum(string YukAnahtari, int XMm, int YMm, int ZMm, bool Donduruldu);

/// <summary>Elle yerleşimde bir araç: ekipman konfigürasyonu ve yükleri (boş araç olabilir).</summary>
public sealed record ElleArac(int KonfigurasyonId, IReadOnlyList<ElleKonum> Konumlar);

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

    /// <summary>Yerleşim kullanıcı tarafından elle düzenlendi (otomatik hesap sonucu değil).</summary>
    public bool ElleDuzenlendi { get; init; }
}
