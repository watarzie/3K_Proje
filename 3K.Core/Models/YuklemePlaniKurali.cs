using System.Globalization;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Yükleme planı sınırları ve kuralları (Simülasyon isterleri 3, 4, 17, 27). Seçilen projelerin bütün sandıkları, sevk
/// edilmişler dahil, plana gelir (saha geri bildirimi 19.11.2026 madde 6); proje tipi sınırlanmaz.
/// </summary>
public static class YuklemePlaniKurali
{
    public const int AdEnFazla = 200;
    public const int AciklamaEnFazla = 1000;
    public const int EnFazlaProje = 50;

    /// <summary>Bir revizyondaki en fazla fiziksel yük; 3B sahne hepsini birlikte çizdiği için liste sayfasızdır.</summary>
    public const int EnFazlaYuk = 2000;

    public static readonly string YukSiniriMesaji = $"Bir planda en fazla {EnFazlaYuk} yük olabilir.";

    public const int ManuelKodEnFazla = 100;
    public const int ManuelAciklamaEnFazla = 500;
    public const int ManuelAdetEnFazla = 500;
    public const int OzelTasimaNotuEnFazla = 500;

    /// <summary>
    /// Yükler ve araç duvarları arası yerleşim boşluğu (isterler 17). Planda isteğe bağlıdır; girilmezse (boş) bu değer
    /// kullanılır.
    /// </summary>
    public const int VarsayilanYerlesimBosluguMm = 30;

    public const int YerlesimBosluguEnFazlaMm = 500;

    /// <summary>Hesapta kullanılan yükler arası boşluk: girilen değer, boşsa varsayılan.</summary>
    public static int UygulananYerlesimBoslugu(YuklemePlanAyarlari ayarlar) => ayarlar.YerlesimBosluguMm ?? VarsayilanYerlesimBosluguMm;

    /// <summary>Ağırlık merkezi sapma sınırının üst değeri (araç boyu/genişliğinin yüzdesi).</summary>
    public const int AgirlikMerkeziSapmaEnFazlaYuzde = 50;

    /// <summary>
    /// Tek adımlı hesapta (saha geri bildirimi 19.11.2026 madde 5) plana verilen ad: seçilen proje numaraları, sınırı
    /// aşarsa kısaltılır. Sevk edilmiş sandıklar da plana gelir (madde 6); plan kaynak sandığın durumuna bakmaz.
    /// </summary>
    public static string OtomatikPlanAdi(IEnumerable<string> projeNolari)
    {
        var ad = string.Join(", ", projeNolari);
        return ad.Length <= AdEnFazla ? ad : string.Concat(ad.AsSpan(0, AdEnFazla - 1), "…");
    }

    /// <summary>Sandığın plandaki kodu: proje numarası ve sandık numarası (ör. PA600-35-01).</summary>
    public static string SandikKodu(string projeNo, string sandikNo) => $"{projeNo.Trim()}-{sandikNo.Trim()}";

    /// <summary>
    /// Çizimde ve 3B görünümde yükün üstüne yazılan ad (saha geri bildirimi 19.11.2026 madde 8): tek projeli planda yalnız
    /// sandık (koli) numarası, çok projeli planda proje numarasıyla birlikte tam kod (ör. PA600-23-01). Kodu proje
    /// numarasıyla başlamayan yük (elle eklenen) tam koduyla yazılır. Ön yüzdeki <c>sandikEtiketi</c> aynı kuraldır.
    /// </summary>
    public static string SandikEtiketi(string kod, string? projeNo, bool tekProje)
    {
        if (!tekProje || string.IsNullOrWhiteSpace(projeNo))
            return kod;
        var onek = projeNo.Trim() + "-";
        return kod.Length > onek.Length && kod.StartsWith(onek, StringComparison.Ordinal) ? kod[onek.Length..] : kod;
    }

    /// <summary>Global benzersiz plan numarası: oluşturma yılı ve o yılın sırası (ör. YP-2026-000123).</summary>
    public static string PlanNo(int yil, int sira) => PlanNoOneki(yil) + sira.ToString("D6", CultureInfo.InvariantCulture);

    /// <summary>Yılın plan numaralarının ortak öneki (ör. YP-2026-); yılın son numarası bu önekle aranır.</summary>
    public static string PlanNoOneki(int yil) => string.Create(CultureInfo.InvariantCulture, $"YP-{yil}-");

    /// <summary>Yılın son plan numarasından sonraki numara; o yıl için ilk plansa 1.</summary>
    public static string SonrakiPlanNo(int yil, string? yilinSonNumarasi)
    {
        var onek = PlanNoOneki(yil);
        var sira = yilinSonNumarasi != null && yilinSonNumarasi.StartsWith(onek, StringComparison.Ordinal) &&
                   int.TryParse(yilinSonNumarasi.AsSpan(onek.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var son)
            ? son + 1
            : 1;
        return PlanNo(yil, sira);
    }

    /// <summary>Faz 1 durumu: seçilmeyen yük hariç, verisi eksik yük hesaplanmaz, öbürü yerleşim bekler.</summary>
    public static PlanYukDurumu Durum(bool dahil, IReadOnlyCollection<string> veriSorunlari) =>
        !dahil ? PlanYukDurumu.HaricTutuldu
        : veriSorunlari.Count > 0 ? PlanYukDurumu.EksikVeri
        : PlanYukDurumu.Bekliyor;

    public static bool AyarlarGecerli(YuklemePlanAyarlari ayarlar) =>
        ayarlar.YerlesimBosluguMm is null or (>= 0 and <= YerlesimBosluguEnFazlaMm) &&
        ayarlar.AgirlikMerkeziSapmaYuzde is null or (>= 1 and <= AgirlikMerkeziSapmaEnFazlaYuzde);

    /// <summary>Elle yerleşimde en fazla araç ve koordinat sınırı (mm, ekipman ölçüsünün çok dışı reddedilir).</summary>
    public const int ElleAracEnFazla = 200;
    public const int ElleKoordinatSiniriMm = 100_000;
    public const string ElleYukGecersizMesaji = "Yerleşimde planda olmayan ya da birden fazla kez geçen yük var; sayfayı yenileyip tekrar deneyin.";
    public const string ElleEkipmanGecersizMesaji = "Elle yerleşimdeki ekipmanlardan biri bulunamadı, pasif, sandık taşımaya uygun değil ya da teknik verisi okunamıyor.";

    /// <summary>Mevcut araçlara yerleştirmede en fazla araç satırı ve satır başına adet.</summary>
    public const int MevcutAracSatiriEnFazla = 20;
    public const int MevcutAracAdetEnFazla = 50;
    public const string MevcutAraclarMesaji = "Mevcut araçlar listesi geçersiz: her konfigürasyon bir kez, adet 1–50, en fazla 20 satır.";
    public const string MevcutAracGerekliMesaji = "Mevcut araçlara yerleştirmede en az bir araç seçilmelidir.";

    /// <summary>Taşıma ailesinde seçilebilecek en fazla tip (ör. Konteyner → 40' High Cube).</summary>
    public const int TipSecimiEnFazla = 20;
    public const string TipSecimiMesaji = "Seçilen araç tipleri geçersiz: her tip bir kez, en fazla 20 tip.";
    public const string TipAileDisiMesaji = "Seçilen araç tiplerinden biri bu taşıma ailesine ait değil, pasif ya da sandık taşımaya uygun değil.";

    public static bool TipIdleriGecerli(IReadOnlyList<int>? tipIdleri) =>
        tipIdleri is null ||
        (tipIdleri.Count <= TipSecimiEnFazla && tipIdleri.All(id => id > 0) && tipIdleri.Distinct().Count() == tipIdleri.Count);

    /// <summary>
    /// Aile tipinin otomatik yerleşimdeki kademesi (saha geri bildirimi 19.11.2026 madde 1, 3). Tip seçilmemişse ailenin
    /// sadelik sırası olan tipleri kullanılır (sırası boş tip, ör. Reefer, gelmez). Tip seçilmişse normal yükler yalnız
    /// seçilen tiplere gider; seçilmeyen tipler yalnız seçilenlere sığmayan yük için yedektir (gerekçesiyle).
    /// </summary>
    public static (int? Kademe, bool Yedek) AracTipiKademesi(int tipId, int? otomatikSecimSirasi, IReadOnlyCollection<int> seciliTipler) =>
        seciliTipler.Count == 0 ? (otomatikSecimSirasi, false)
        : seciliTipler.Contains(tipId) ? (otomatikSecimSirasi ?? 1, false)
        : (otomatikSecimSirasi, true);

    /// <summary>
    /// Yük listesindeki satırın anahtarı (yerleşim sonucu ile satırı eşler; ön yüz aynı biçimi kullanır): sandıkta
    /// "s:{sandıkId}", elle eklenen yükte "m:{grupNo}:{birimNo}".
    /// </summary>
    public static string YukAnahtari(PlanYukKaynagi kaynak, int? sandikId, int? manuelGrupNo, int birimNo) =>
        kaynak == PlanYukKaynagi.Sandik
            ? string.Create(CultureInfo.InvariantCulture, $"s:{sandikId}")
            : string.Create(CultureInfo.InvariantCulture, $"m:{manuelGrupNo}:{birimNo}");

    /// <summary>
    /// Lokasyonu belirsiz yük (isterler 26): lokasyonu yok ya da depo lokasyonu "Belirsiz". Raporlar, lokasyon tercihi ve
    /// lokasyona göre ayrı planlama bu tek kuralı kullanır.
    /// </summary>
    public static bool LokasyonBelirsizMi(int? lokasyonId) => lokasyonId is null || lokasyonId == (int)DepoLokasyon.Belirsiz;

    /// <summary>
    /// Planlamaya dahil ve verisi tam yüklerin farklı lokasyon sayısı (belirsiz lokasyonlar tek ayrı lokasyon sayılır). Plan
    /// detayı ve yük önizlemesi aynı hesabı kullanır; 1'den fazlaysa ekran yükleme yeri tercihini sorar.
    /// </summary>
    public static int FarkliLokasyonSayisi(IEnumerable<PlanYukSatiri> satirlar) =>
        satirlar.Where(s => s.Dahil && s.VeriSorunlari.Count == 0)
            .Select(s => LokasyonBelirsizMi(s.LokasyonId) ? null : s.LokasyonId).Distinct().Count();

    public const string LokasyonTercihiMesaji = "Yükleme yeri tercihi geçersiz.";

    /// <summary>Tek adımlı hesapta yükleme yeri tercihi isteğe bağlıdır; gönderilirse tanımlı değerlerden biri olmalıdır.</summary>
    public static bool HesapLokasyonTercihiGecerli(YuklemeLokasyonTercihi? tercih) => tercih is null || Enum.IsDefined(tercih.Value);

    /// <summary>
    /// Tek adımlı hesapta uygulanan yükleme yeri tercihi (saha isteği, 20 Kasım 2026): gönderilmeyen ya da "henüz
    /// seçilmedi" tek yerden (karışık) sayılır; yalnız açıkça seçilen "lokasyona göre ayrı" araçları lokasyona böler.
    /// </summary>
    public static YuklemeLokasyonTercihi HesapLokasyonTercihi(YuklemeLokasyonTercihi? tercih) =>
        tercih == YuklemeLokasyonTercihi.LokasyonaGoreAyri ? YuklemeLokasyonTercihi.LokasyonaGoreAyri : YuklemeLokasyonTercihi.Karisik;

    public const string SandikTekrariMesaji = "Bir sandık plana birden fazla kez eklenemez.";

    /// <summary>Sandık seçimleri (dahil ve izinler) sandık başına bir kez gönderilir.</summary>
    public static bool SandikSecimleriTekil(IReadOnlyCollection<int> sandikIdleri) => sandikIdleri.Distinct().Count() == sandikIdleri.Count;

    public static bool MevcutAraclarGecerli(IReadOnlyList<PlanMevcutArac>? araclar) =>
        araclar is null ||
        (araclar.Count <= MevcutAracSatiriEnFazla &&
         araclar.All(a => a.KonfigurasyonId > 0 && a.Adet is >= 1 and <= MevcutAracAdetEnFazla) &&
         araclar.Select(a => a.KonfigurasyonId).Distinct().Count() == araclar.Count);

}

/// <summary>
/// Planın ayarları: yükler arası yerleşim boşluğu ve araç seçimi. Yükseklik, genişlik, uzunluk, ağırlık ve yükleme
/// hareketi güvenlik payları kaldırıldı (saha isteği, 20 Kasım 2026); kontroller paysız yapılır. Eski revizyonlarda
/// saklı pay değerleri okunmaz.
/// </summary>
public sealed record YuklemePlanAyarlari
{
    /// <summary>Yükler arası boşluk (mm, isteğe bağlı); boşsa <see cref="YuklemePlaniKurali.VarsayilanYerlesimBosluguMm"/>.</summary>
    public int? YerlesimBosluguMm { get; init; }

    /// <summary>
    /// Yük ağırlık merkezinin araç ortasından izin verilen en fazla sapması (araç boyunun ve genişliğinin yüzdesi, 1–50).
    /// Boşsa ağırlık dağılımı ölçülür ama "Teyit edilmedi" kalır; sayı işletmeyle kesinleşir.
    /// </summary>
    public int? AgirlikMerkeziSapmaYuzde { get; init; }

    /// <summary>Mevcut araçlara yerleştirmede kullanıcının verdiği araçlar (konfigürasyon ve adet); diğer biçimde boştur.</summary>
    public IReadOnlyList<PlanMevcutArac> MevcutAraclar { get; init; } = [];

    /// <summary>
    /// Taşıma ailesinde seçilen tipler (<c>EkipmanTipleri.Id</c>; ör. Konteyner → 40' High Cube). Boşsa ailenin otomatik
    /// tipleri sadelik sırasıyla kullanılır. Aile değişince eski seçim geçersizdir.
    /// </summary>
    public IReadOnlyList<int> TipIdleri { get; init; } = [];
}

/// <summary>Mevcut araçlara yerleştirmede bir araç tipi: ekipman konfigürasyonu ve adet.</summary>
public sealed record PlanMevcutArac(int KonfigurasyonId, int Adet);
