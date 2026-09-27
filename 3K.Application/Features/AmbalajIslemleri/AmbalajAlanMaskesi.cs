using _3K.Application.Features.AmbalajIslemleri.DTOs;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Planlama yanıtlarındaki ölçü (boy/en/yükseklik) ve m³ alanlarını kullanıcının görünüm
/// izinlerine göre boşaltır. Sayaçlar, kimlikler ve durumlar her zaman korunur.
/// </summary>
internal static class AmbalajAlanMaskesi
{
    public static AmbalajPlanlamaProjeleriSayfasiDto Maskele(AmbalajPlanlamaProjeleriSayfasiDto sayfa, AmbalajGorunumYetkileri izin) =>
        izin.M3Gorunur ? sayfa : sayfa with
        {
            Items = sayfa.Items.Select(ozet => ozet with
            {
                ToplamHacimM3 = null,
                UretimHacimM3 = null,
                ProjeSandiklariHacimM3 = null,
                IlaveSandiklarHacimM3 = null,
                IcSandiklarHacimM3 = null
            }).ToList(),
            FilteredSummary = sayfa.FilteredSummary is null ? null : sayfa.FilteredSummary with { ToplamHacimM3 = null }
        };

    public static AmbalajPlanlamaPlanDto Maskele(AmbalajPlanlamaPlanDto plan, AmbalajGorunumYetkileri izin) => plan with
    {
        SeciliHacimM3 = izin.M3Gorunur ? plan.SeciliHacimM3 : null,
        Kalemler = plan.Kalemler.Select(kalem => Maskele(kalem, izin)).ToList()
    };

    public static AmbalajPlanlamaKalemDto Maskele(AmbalajPlanlamaKalemDto kalem, AmbalajGorunumYetkileri izin) => kalem with
    {
        Boy = izin.OlcuGorunur ? kalem.Boy : null,
        En = izin.OlcuGorunur ? kalem.En : null,
        Yukseklik = izin.OlcuGorunur ? kalem.Yukseklik : null,
        HacimM3 = izin.M3Gorunur ? kalem.HacimM3 : null
    };

    public static AmbalajBagimsizSandiklarSayfasiDto Maskele(AmbalajBagimsizSandiklarSayfasiDto sayfa, AmbalajGorunumYetkileri izin) => sayfa with
    {
        Items = sayfa.Items.Select(sandik => Maskele(sandik, izin)).ToList(),
        FilteredSummary = sayfa.FilteredSummary is null || izin.M3Gorunur ? sayfa.FilteredSummary : sayfa.FilteredSummary with
        {
            ToplamHacimM3 = null,
            TurOzetleri = sayfa.FilteredSummary.TurOzetleri.Select(tur => tur with { ToplamHacimM3 = null }).ToList()
        }
    };

    public static AmbalajBagimsizSandikDto Maskele(AmbalajBagimsizSandikDto sandik, AmbalajGorunumYetkileri izin) => sandik with
    {
        Boy = izin.OlcuGorunur ? sandik.Boy : null,
        En = izin.OlcuGorunur ? sandik.En : null,
        Yukseklik = izin.OlcuGorunur ? sandik.Yukseklik : null,
        HacimM3 = izin.M3Gorunur ? sandik.HacimM3 : null
    };

    public static AmbalajIcSandikSablonDto Maskele(AmbalajIcSandikSablonDto sablon, AmbalajGorunumYetkileri izin) =>
        izin.OlcuGorunur ? sablon : sablon with { Boy = null, En = null, Yukseklik = null };

    public static AmbalajIlaveSandikAdayDto Maskele(AmbalajIlaveSandikAdayDto aday, AmbalajGorunumYetkileri izin) =>
        izin.OlcuGorunur ? aday : aday with { Boy = null, En = null, Yukseklik = null };

    public static AmbalajSandikSecenegiDto Maskele(AmbalajSandikSecenegiDto secenek, AmbalajGorunumYetkileri izin) =>
        izin.OlcuGorunur ? secenek : secenek with { Boy = null, En = null, Yukseklik = null };
}
