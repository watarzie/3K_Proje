using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>Yeni plan: başlık bilgileri ve projeler; projelerin sevk edilmemiş bütün sandıkları seçili gelir.</summary>
public sealed record YuklemePlaniOlusturModel(
    string Ad,
    YuklemeCalismaBicimi CalismaBicimi,
    int TasimaAilesiId,
    YuklemeProjeKarisimi ProjeKarisimi,
    DateTime? YuklemeTarihi,
    string? Aciklama,
    IReadOnlyList<int> ProjeIdleri);

/// <summary>
/// Sandık yükünün kullanıcı seçimleri. Ölçü, ağırlık ve lokasyon istemciden alınmaz: önceki revizyondaki kopya korunur,
/// plana yeni giren sandık o anki kaynaktan kopyalanır.
/// </summary>
public sealed record PlanSandikYukuKaydi(
    int SandikId,
    bool Dahil,
    bool DondurulebilirMi,
    bool UsteKonabilirMi,
    bool AltaKalabilirMi,
    decimal? UstYukSiniriKg,
    PlanYukIzni ElleDegisenIzinler);

/// <summary>Elle eklenen yükün bir biriminin seçimleri; birimler 1'den numaralanır.</summary>
public sealed record PlanManuelYukBirimi(
    int BirimNo,
    bool Dahil,
    bool DondurulebilirMi,
    bool UsteKonabilirMi,
    bool AltaKalabilirMi,
    decimal? UstYukSiniriKg,
    PlanYukIzni ElleDegisenIzinler);

/// <summary>
/// Elle eklenen yük (isterler 4): ortak bilgiler ve adet kadar birim. Gönderilmeyen birim varsayılan izinlerle
/// kurulur.
/// </summary>
public sealed record PlanManuelYukKaydi(
    string Kod,
    string? Aciklama,
    int YukTuruId,
    decimal? BoyMm,
    decimal? EnMm,
    decimal? YukseklikMm,
    decimal? BrutKg,
    int Adet,
    int? ProjeId,
    int? LokasyonId,
    string? OzelTasimaNotu,
    IReadOnlyList<PlanManuelYukBirimi> Birimler);

/// <summary>Kaydetme: son revizyon numarası beklenir (arada kaydedilmişse 409) ve yeni revizyon yazılır.</summary>
public sealed record YuklemePlaniKaydetModel(
    int PlanId,
    int BeklenenRevizyonNo,
    string Ad,
    YuklemeCalismaBicimi CalismaBicimi,
    int TasimaAilesiId,
    YuklemeProjeKarisimi ProjeKarisimi,
    DateTime? YuklemeTarihi,
    string? Aciklama,
    string? RevizyonAciklamasi,
    YuklemePlanAyarlari Ayarlar,
    IReadOnlyList<int> ProjeIdleri,
    IReadOnlyList<PlanSandikYukuKaydi> SandikYukleri,
    IReadOnlyList<PlanManuelYukKaydi> ManuelYukler);

/// <summary>Plan listesindeki satır.</summary>
public sealed class YuklemePlaniOzeti
{
    public int Id { get; init; }
    public string PlanNo { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public YuklemeCalismaBicimi CalismaBicimi { get; init; }
    public string TasimaAilesiAdi { get; init; } = string.Empty;
    public string Projeler { get; init; } = string.Empty;
    public int YukSayisi { get; init; }
    public int DahilYukSayisi { get; init; }
    public int RevizyonNo { get; init; }
    public DateTime SonKayitTarihi { get; init; }
    public string? SonKaydeden { get; init; }
}

/// <summary>Planın son revizyonu: başlık, ayarlar, projeler, yükler, sayaçlar ve kaynak farkları.</summary>
public sealed class YuklemePlaniDetayi
{
    public int Id { get; init; }
    public string PlanNo { get; init; } = string.Empty;
    public string Ad { get; init; } = string.Empty;
    public YuklemeCalismaBicimi CalismaBicimi { get; init; }
    public int TasimaAilesiId { get; init; }
    public string TasimaAilesiAdi { get; init; } = string.Empty;
    public YuklemeProjeKarisimi ProjeKarisimi { get; init; }
    public YuklemeLokasyonTercihi LokasyonTercihi { get; init; }
    public DateTime? YuklemeTarihi { get; init; }
    public string? Aciklama { get; init; }
    public int RevizyonNo { get; init; }
    public DateTime RevizyonTarihi { get; init; }
    public string? Kaydeden { get; init; }
    public string? RevizyonAciklamasi { get; init; }
    public YuklemePlanAyarlari Ayarlar { get; init; } = new();
    public IReadOnlyList<PlanProjeBilgisi> Projeler { get; init; } = [];
    public IReadOnlyList<PlanYukSatiri> Yukler { get; init; } = [];
    public PlanYukSayaclari Sayaclar { get; init; } = new();
    public IReadOnlyList<PlanKaynakFarki> KaynakFarklari { get; init; } = [];

    /// <summary>
    /// Bu revizyonun otomatik yerleşimi. Seçimler, ayarlar ya da kaynak değişip yeni revizyon kaydedilince yerleşim
    /// taşınmaz ve yeniden hesap gerekir (null).
    /// </summary>
    public PlanYerlesimi? Yerlesim { get; init; }

    /// <summary>Planlamaya dahil yüklerin farklı lokasyon sayısı (belirsiz lokasyon ayrı sayılır); 1'den fazlaysa lokasyon tercihi sorulur.</summary>
    public int FarkliLokasyonSayisi { get; init; }
}

/// <summary>Otomatik yerleşim sonucu (isterler 16–19, 22–23, 27): araçlar, yerleşemeyenler, ek ihtiyaç ve uyarılar.</summary>
public sealed class PlanYerlesimi
{
    public DateTime HesapTarihi { get; init; }
    public YerlesimKontrolSonucu Sonuc { get; init; }
    public bool LokasyonaGoreAyri { get; init; }
    public bool ProjeBazindaAyri { get; init; }
    public IReadOnlyList<PlanAraci> Araclar { get; init; } = [];
    public IReadOnlyList<PlanYerlesemeyenYuk> Yerlesemeyenler { get; init; } = [];
    public IReadOnlyList<PlanEkEkipmanIhtiyaci> EkIhtiyaclar { get; init; } = [];
    public IReadOnlyList<string> Uyarilar { get; init; } = [];
}

/// <summary>Plandaki araç: kullanılan ekipman revizyonu ve geometrisi (3B için), yükler, toplamlar, taşmalar, kontroller.</summary>
public sealed class PlanAraci
{
    public int AracNo { get; init; }
    public int KonfigurasyonId { get; init; }
    public int RevizyonNo { get; init; }
    public string EkipmanKodu { get; init; } = string.Empty;
    public string EkipmanAdi { get; init; } = string.Empty;
    public string AileAdi { get; init; } = string.Empty;
    public string TipAdi { get; init; } = string.Empty;
    public bool AileDisi { get; init; }
    public string? AileDisiGerekce { get; init; }
    public IReadOnlyList<string> ProjeNolari { get; init; } = [];
    public IReadOnlyList<string> Lokasyonlar { get; init; } = [];
    public IReadOnlyList<PlanYukKonumu> Konumlar { get; init; } = [];
    public decimal ToplamKg { get; init; }
    public decimal? KapasiteKg { get; init; }
    public int TabanDolulukYuzde { get; init; }
    public int? AgirlikMerkeziXMm { get; init; }
    public int YukluYukseklikMm { get; init; }
    public int YukluGenislikMm { get; init; }
    public int YukluUzunlukMm { get; init; }
    public int SolTasmaMm { get; init; }
    public int SagTasmaMm { get; init; }
    public int OnTasmaMm { get; init; }
    public int ArkaTasmaMm { get; init; }
    public IReadOnlyList<PlanAracKontrolu> Kontroller { get; init; } = [];
    public YerlesimKontrolSonucu Sonuc { get; init; }

    /// <summary>Hesapta kullanılan ekipman revizyonunun teknik verisi (3B görünüm; revizyonlar değişmez).</summary>
    public string TeknikVeriJson { get; init; } = "{}";
}

/// <summary>Araçtaki yükün konumu (ekipman koordinatları: X önden arkaya, Y soldan sağa, Z yerden yukarı; mm).</summary>
public sealed class PlanYukKonumu
{
    /// <summary>Yük listesindeki satırın anahtarı: "s:{sandıkId}" ya da "m:{grupNo}:{birimNo}".</summary>
    public string YukAnahtari { get; init; } = string.Empty;
    public string Kod { get; init; } = string.Empty;
    public string? ProjeNo { get; init; }
    public decimal BrutKg { get; init; }
    public int XMm { get; init; }
    public int YMm { get; init; }
    public int ZMm { get; init; }
    public int BoyMm { get; init; }
    public int EnMm { get; init; }
    public int YukseklikMm { get; init; }
    public bool Donduruldu { get; init; }
    public string YuzeyKodu { get; init; } = string.Empty;
    public int IstifNo { get; init; }
    public int Kat { get; init; }
    public int YuklemeSirasi { get; init; }
    public YerlesimGirisYonu YuklemeYontemi { get; init; }
}

public sealed class PlanAracKontrolu
{
    public YerlesimKontrolTuru Tur { get; init; }
    public YerlesimKontrolSonucu Sonuc { get; init; }
    public string Mesaj { get; init; } = string.Empty;
}

public sealed class PlanYerlesemeyenYuk
{
    public string YukAnahtari { get; init; } = string.Empty;
    public string Kod { get; init; } = string.Empty;
    public string Neden { get; init; } = string.Empty;
}

/// <summary>Mevcut araçlara sığmayan yükler için önerilen ek ekipman (plana eklenmez).</summary>
public sealed class PlanEkEkipmanIhtiyaci
{
    public int KonfigurasyonId { get; init; }
    public string EkipmanKodu { get; init; } = string.Empty;
    public string EkipmanAdi { get; init; } = string.Empty;
    public int Adet { get; init; }
}

/// <summary>Plandaki proje ve plana gelmeyen (sevk edilmiş) sandık sayısı.</summary>
public sealed class PlanProjeBilgisi
{
    public int? ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string? Musteri { get; init; }
    public int? ProjeTipiId { get; init; }
    public int SevkEdilmisSandikSayisi { get; init; }
}

/// <summary>Yük listesinin satırı (isterler 3); durum ve veri sorunları kopyadan hesaplanır.</summary>
public sealed class PlanYukSatiri
{
    public int SiraNo { get; init; }
    public PlanYukKaynagi Kaynak { get; init; }
    public int? SandikId { get; init; }
    public int? ManuelGrupNo { get; init; }
    public int BirimNo { get; init; }
    public int BirimSayisi { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string? Aciklama { get; init; }
    public int? ProjeId { get; init; }
    public string? ProjeNo { get; init; }
    public int YukTuruId { get; init; }
    public string YukTuruAdi { get; init; } = string.Empty;
    public decimal? BoyMm { get; init; }
    public decimal? EnMm { get; init; }
    public decimal? YukseklikMm { get; init; }
    public decimal? BrutKg { get; init; }
    public int? LokasyonId { get; init; }
    public string? LokasyonAdi { get; init; }
    public bool Dahil { get; init; }
    public bool DondurulebilirMi { get; init; }
    public bool UsteKonabilirMi { get; init; }
    public bool AltaKalabilirMi { get; init; }
    public decimal? UstYukSiniriKg { get; init; }
    public PlanYukIzni ElleDegisenIzinler { get; init; }
    public string? OzelTasimaNotu { get; init; }
    public PlanYukDurumu Durum { get; init; }
    public IReadOnlyList<string> VeriSorunlari { get; init; } = [];

    /// <summary>Son hesapta yerleştiği araç (yoksa null).</summary>
    public int? AracNo { get; init; }

    /// <summary>Son hesapta yerleşemediyse nedeni.</summary>
    public string? YerlesimNotu { get; init; }
}

/// <summary>
/// Sayaçlar (isterler 5): seçilen, hesaplanabilir, veri eksikliği nedeniyle hesaplanmayan ve hariç tutulan yükler
/// ayrı; plana gelmeyen sevk edilmiş sandıklar ayrıca. Yerleşen/yerleşemeyen sayıları motorla gelir.
/// </summary>
public sealed class PlanYukSayaclari
{
    public int Toplam { get; init; }
    public int Secilen { get; init; }
    public int Hesaplanabilir { get; init; }
    public int VeriEksik { get; init; }
    public int HaricTutulan { get; init; }
    public int SevkEdilmisSandik { get; init; }

    /// <summary>Son hesapta yerleşen ve yerleşemeyen yükler (hesap yoksa 0).</summary>
    public int Yerlesen { get; init; }
    public int Yerlesemeyen { get; init; }
}

/// <summary>Plandaki kopya ile güncel sandık arasındaki fark (gösterim için önceki/güncel değer metni).</summary>
public sealed class PlanKaynakFarki
{
    public PlanKaynakFarkTuru Tur { get; init; }
    public int? SandikId { get; init; }
    public string Kod { get; init; } = string.Empty;
    public string? Onceki { get; init; }
    public string? Guncel { get; init; }
}

/// <summary>Plana eklenebilecek proje (proje seçici, sayfalı).</summary>
public sealed class PlanProjeAdayi
{
    public int ProjeId { get; init; }
    public string ProjeNo { get; init; } = string.Empty;
    public string Musteri { get; init; } = string.Empty;
    public int ProjeTipiId { get; init; }
    public string ProjeTipiAdi { get; init; } = string.Empty;
    public int PlanaGelecekSandikSayisi { get; init; }
    public int SevkEdilmisSandikSayisi { get; init; }
}

/// <summary>Plan ekranının seçim listeleri: taşıma aileleri (sandık taşımaya uygun tipi olanlar), yük türleri, lokasyonlar.</summary>
public sealed class YuklemePlaniTanimlari
{
    public IReadOnlyList<SecimOgesi> TasimaAileleri { get; init; } = [];
    public IReadOnlyList<SecimOgesi> YukTurleri { get; init; } = [];
    public IReadOnlyList<SecimOgesi> Lokasyonlar { get; init; } = [];
}

/// <summary>Kimlik ve görünen ad.</summary>
public sealed record SecimOgesi(int Id, string Ad);
