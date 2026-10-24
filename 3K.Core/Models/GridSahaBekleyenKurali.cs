using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// "Grid'de bekleyen" tanımının tek kaynağı: Grid'e fiziksel gelmiş (Tam Geldi / Eksik Geldi; Trafo Sevk'in Grid'e gelen
/// payı) ve henüz 3K'ya ulaşmamış miktar. Aktif sevk partisinin yoldaki (3K'nın henüz karşılamadığı) bölümü Grid'de bekleyen sayılmaz;
/// erken sonuçlandırılmış parti yolda değildir. Saf kuraldır, veritabanı okumaz; SQL ikizi Application katmanındaki
/// <c>GridSorgulari.GriddeBekleyenIfadesi</c>'dir ve eşdeğerliği <c>GridSahaTests.SqlIfadeleri_RastgeleSatirlardaSafKuralaEsittir</c> ile sabitlenir.
/// </summary>
public static class GridSahaBekleyenKurali
{
    /// <summary>
    /// Grid Sahası listesindeki ("Tümü" kapsamı) durumlar: Grid sahasına gelmiş ürünler ve orada sonuçlananlar — Tam
    /// Geldi, Eksik Geldi, Trafo Sevk (sahadan trafoya giden) ve Grid Kapandı (24.10.2026).
    /// </summary>
    public static readonly IReadOnlyList<int> SahaDurumlari =
        [(int)GridDurum.TamGeldi, (int)GridDurum.EksikGeldi, (int)GridDurum.TrafoSevk, (int)GridDurum.GridKapandi];

    /// <summary>
    /// Grid'de bekleyen miktarı olabilen durumlar: Tam Geldi, Eksik Geldi ve Trafo Sevk. Trafo Sevk'te trafoya giden pay
    /// <c>GridGelenAdet</c>'e girmez; Grid'e gelen pay 3K'ya ulaşana kadar sahada bekler. Grid Kapandı terminal iştir,
    /// bekleyeni yoktur.
    /// </summary>
    public static readonly IReadOnlyList<int> BekleyebilenDurumlar = [(int)GridDurum.TamGeldi, (int)GridDurum.EksikGeldi, (int)GridDurum.TrafoSevk];

    /// <summary>
    /// Durum Grid Sahası listesinde ("Tümü") görünür mü? Aynı dört duruma geçiş mal kabul kaydı sayılır (Grid personelinin
    /// sahada teslim alıp sonuçlandırdığı ürün; 24.10.2026).
    /// </summary>
    public static bool SahaDurumu(int gridDurumuId) => SahaDurumlari.Contains(gridDurumuId);

    /// <summary>
    /// Aktif partiden 3K'nın karşıladığı miktar. Sayaçsız (legacy) partide yalnız 3K'da Tam/Fazla Geldi ile parti
    /// kadar teslim alınmışsa parti karşılanmış sayılır, aksi halde 0 (belirsiz legacy kayıt teslim alınmış varsayılmaz).
    /// </summary>
    public static decimal AktifPartidenKarsilananMiktar(CekiSatiri satir)
    {
        ArgumentNullException.ThrowIfNull(satir);
        if (satir.AktifGridSevkKarsilananMiktari.HasValue)
            return satir.AktifGridSevkKarsilananMiktari.Value;
        var tamamlanmisLegacyDurum = satir.UcKDurumuId is (int)UcKDurum.TamGeldi or (int)UcKDurum.FazlaGeldi;
        return tamamlanmisLegacyDurum && satir.GelenMiktar == satir.GridSevkMiktari ? satir.GridSevkMiktari ?? 0m : 0m;
    }

    /// <summary>
    /// Aktif Grid sevk partisinin yoldaki bölümü: sevk durumu Sevk Edildi ve parti erken sonuçlandırılmamışsa
    /// parti miktarı − karşılanan (en az 0); diğer durumlarda 0.
    /// </summary>
    public static decimal YoldakiMiktar(CekiSatiri satir)
    {
        ArgumentNullException.ThrowIfNull(satir);
        if (satir.GridSevkDurumuId != (int)GridSevkDurum.SevkEdildi || satir.AktifGridSevkPartisiErkenSonuclandirildiMi == true)
            return 0m;
        return Math.Max((satir.GridSevkMiktari ?? 0m) - AktifPartidenKarsilananMiktar(satir), 0m);
    }

    /// <summary>
    /// Grid'de fiziksel bekleyen miktar: <see cref="BekleyebilenDurumlar"/> için Grid gelen − 3K'ya ulaşan − yolda, en az
    /// 0; diğer durumlarda (Grid Kapandı dahil) 0.
    /// </summary>
    public static decimal BekleyenMiktar(CekiSatiri satir)
    {
        ArgumentNullException.ThrowIfNull(satir);
        if (!BekleyebilenDurumlar.Contains(satir.GridDurumuId))
            return 0m;
        return Math.Max(satir.GridGelenAdet - satir.GelenMiktar - YoldakiMiktar(satir), 0m);
    }

    /// <summary>Satır "Grid'de bekleyen" kapsamına girer mi: Tam/Eksik Geldi ya da Trafo Sevk ve bekleyen miktar pozitif.</summary>
    public static bool GriddeBekliyorMu(CekiSatiri satir)
    {
        ArgumentNullException.ThrowIfNull(satir);
        return BekleyenMiktar(satir) > 0m;
    }
}
