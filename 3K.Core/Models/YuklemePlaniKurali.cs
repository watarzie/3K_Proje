using System.Globalization;
using System.Linq.Expressions;
using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Yükleme planı sınırları ve kuralları (Simülasyon isterleri 3, 4, 17, 27). Plana yalnız sevk edilmemiş sandıklar gelir
/// (kullanıcı kararı, 5 Kasım 2026); proje tipi sınırlanmaz.
/// </summary>
public static class YuklemePlaniKurali
{
    public const int AdEnFazla = 200;
    public const int AciklamaEnFazla = 1000;
    public const int EnFazlaProje = 50;

    /// <summary>Bir revizyondaki en fazla fiziksel yük; 3B sahne hepsini birlikte çizdiği için liste sayfasızdır.</summary>
    public const int EnFazlaYuk = 2000;

    public const int ManuelKodEnFazla = 100;
    public const int ManuelAciklamaEnFazla = 500;
    public const int ManuelAdetEnFazla = 500;
    public const int OzelTasimaNotuEnFazla = 500;

    /// <summary>Sandıklar ve araç engelleri arası başlangıç yerleşim boşluğu (isterler 17); yasal güvenlik payından ayrıdır.</summary>
    public const int VarsayilanYerlesimBosluguMm = 30;

    public const int YerlesimBosluguEnFazlaMm = 500;
    public const int GuvenlikPayiEnFazlaMm = 1000;
    public const int AgirlikPayiEnFazlaKg = 10_000;

    /// <summary>Plana gelen sandık: sevk edilmemiş olan (sevk sonrası düzeltmedeki sandık da sevk edilmiş sayılır).</summary>
    public static bool PlanaGelirMi(int sandikDurumId) => sandikDurumId != (int)SandikDurum.Sevkedildi;

    /// <summary>
    /// <see cref="PlanaGelirMi"/> kuralının veritabanı sorgusundaki karşılığı (aynı koşul; eşdeğerliği testle sabittir).
    /// </summary>
    public static readonly Expression<Func<Sandik, bool>> PlanaGelenSandik = s => s.DurumId != (int)SandikDurum.Sevkedildi;

    /// <summary>Sandığın plandaki kodu: proje numarası ve sandık numarası (ör. PA600-35-01).</summary>
    public static string SandikKodu(string projeNo, string sandikNo) => $"{projeNo.Trim()}-{sandikNo.Trim()}";

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
        ayarlar.YerlesimBosluguMm is >= 0 and <= YerlesimBosluguEnFazlaMm &&
        PayGecerli(ayarlar.YukseklikPayiMm, GuvenlikPayiEnFazlaMm) &&
        PayGecerli(ayarlar.GenislikPayiMm, GuvenlikPayiEnFazlaMm) &&
        PayGecerli(ayarlar.UzunlukPayiMm, GuvenlikPayiEnFazlaMm) &&
        PayGecerli(ayarlar.YuklemeHareketiPayiMm, GuvenlikPayiEnFazlaMm) &&
        PayGecerli(ayarlar.AgirlikPayiKg, AgirlikPayiEnFazlaKg);

    /// <summary>Mevcut araçlara yerleştirmede en fazla araç satırı ve satır başına adet.</summary>
    public const int MevcutAracSatiriEnFazla = 20;
    public const int MevcutAracAdetEnFazla = 50;
    public const string MevcutAraclarMesaji = "Mevcut araçlar listesi geçersiz: her konfigürasyon bir kez, adet 1–50, en fazla 20 satır.";
    public const string MevcutAracGerekliMesaji = "Mevcut araçlara yerleştirmede en az bir araç seçilmelidir.";

    /// <summary>
    /// Yük listesindeki satırın anahtarı (yerleşim sonucu ile satırı eşler; ön yüz aynı biçimi kullanır): sandıkta
    /// "s:{sandıkId}", elle eklenen yükte "m:{grupNo}:{birimNo}".
    /// </summary>
    public static string YukAnahtari(PlanYukKaynagi kaynak, int? sandikId, int? manuelGrupNo, int birimNo) =>
        kaynak == PlanYukKaynagi.Sandik
            ? string.Create(CultureInfo.InvariantCulture, $"s:{sandikId}")
            : string.Create(CultureInfo.InvariantCulture, $"m:{manuelGrupNo}:{birimNo}");

    public static bool MevcutAraclarGecerli(IReadOnlyList<PlanMevcutArac>? araclar) =>
        araclar is null ||
        (araclar.Count <= MevcutAracSatiriEnFazla &&
         araclar.All(a => a.KonfigurasyonId > 0 && a.Adet is >= 1 and <= MevcutAracAdetEnFazla) &&
         araclar.Select(a => a.KonfigurasyonId).Distinct().Count() == araclar.Count);

    private static bool PayGecerli(int? pay, int enFazla) => pay is null || (pay >= 0 && pay <= enFazla);
}

/// <summary>
/// Planın yerleşim boşluğu ve güvenlik payları (isterler 17). Sayısal güvenlik payları işletmeyle kesinleşene kadar
/// boştur; boş pay "tanımlı değil" demektir ve sıfır sayılmaz. Paylar yasal aşım toleransı olarak kullanılmaz.
/// </summary>
public sealed record YuklemePlanAyarlari
{
    public int YerlesimBosluguMm { get; init; } = YuklemePlaniKurali.VarsayilanYerlesimBosluguMm;
    public int? YukseklikPayiMm { get; init; }
    public int? GenislikPayiMm { get; init; }
    public int? UzunlukPayiMm { get; init; }
    public int? AgirlikPayiKg { get; init; }
    public int? YuklemeHareketiPayiMm { get; init; }

    /// <summary>Mevcut araçlara yerleştirmede kullanıcının verdiği araçlar (konfigürasyon ve adet); diğer biçimde boştur.</summary>
    public IReadOnlyList<PlanMevcutArac> MevcutAraclar { get; init; } = [];
}

/// <summary>Mevcut araçlara yerleştirmede bir araç tipi: ekipman konfigürasyonu ve adet.</summary>
public sealed record PlanMevcutArac(int KonfigurasyonId, int Adet);
