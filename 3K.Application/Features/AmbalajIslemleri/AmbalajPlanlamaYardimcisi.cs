using System.Text.RegularExpressions;
using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

internal static partial class AmbalajPlanlamaYardimcisi
{
    /// <param name="cekiSandikNolari">
    /// Projenin çeki satırlarındaki sandık numaraları (<see cref="AmbalajCekiSandiklari"/>); kaydı henüz oluşmamış
    /// sandığın İlave grubu ve varsayılan ambalaj kararı senkronla aynı kuralla buradan okunur.
    /// </param>
    public static AmbalajPlanlamaPlanDto PlanDtoOlustur(
        Proje proje,
        string projeTipiMetni,
        IReadOnlyList<Sandik> sandiklar,
        IReadOnlyList<AmbalajUretimKaydi> kayitlar,
        IReadOnlySet<string> cekiSandikNolari,
        int? grup = null,
        int[]? izinliDurumlar = null)
    {
        var genelDurum = AmbalajUretimPolitikasi.ProjeDurumu(kayitlar);
        var gerekenler = kayitlar.Where(k => !k.IptalMi && k.AmbalajaDahil).ToList();
        var aktifKayitlar = kayitlar.Where(k => !k.IptalMi && !k.BagimsizKayitMi).ToList();
        // İptal edilmiş kaynağı "henüz senkronize edilmemiş" sayıp yeniden
        // öngörüye katma. Aynı kaynak için aktif bir halef varsa onun kararı geçerlidir.
        var kaynakMap = kayitlar
            .Where(k => !k.BagimsizKayitMi && k.KaynakKayitId.HasValue)
            .GroupBy(k => k.KaynakKayitId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(k => k.IptalMi).ThenByDescending(k => k.Id).First());
        var projeGrubu = AmbalajUretimPolitikasi.ProjeGrubu(proje.ProjeTipiId);
        // Özel Sandıklar'da İlave'ye dönüştürülen kaynak artık o özel kayıtla yönetilir; plan satırı olarak
        // (kaydı yokmuş gibi) yeniden görünmez.
        var ozelSandigaBaglananlar = kayitlar
            .Where(k => k.BagimsizKayitMi && !k.IptalMi && k.KaynakKayitId.HasValue)
            .Select(k => k.KaynakKayitId!.Value)
            .ToHashSet();

        var kaynaklar = grup == 3
            ? []
            : sandiklar
                .Where(s => (!kaynakMap.TryGetValue(s.Id, out var kayit) || !kayit.IptalMi) &&
                            !ozelSandigaBaglananlar.Contains(s.Id))
                .OrderBy(s => SandikSiraAnahtari(s.SandikNo))
                .ThenBy(s => s.SandikNo, StringComparer.OrdinalIgnoreCase)
                .Select(s =>
                {
                    var dto = KaynakKalemDtoOlustur(s, kaynakMap.GetValueOrDefault(s.Id), projeGrubu, cekiSandikNolari);
                    return genelDurum == AmbalajUretimDurumu.Tamamlandi && !kaynakMap.ContainsKey(s.Id)
                        ? dto with { AmbalajaDahilMi = false, AmbalajKarariOneriliyor = true } : dto;
                })
                // Ambalaj dışındaki İlave sandık yalnız Özel Sandıklar'dan yönetilir, plan çekmecesinde listelenmez.
                .Where(dto => AmbalajUretimPolitikasi.PlandaGosterilirMi(
                    dto.Tur == 2 ? AmbalajSandikTuru.Ilave : AmbalajSandikTuru.Normal, dto.AmbalajaDahilMi != false))
                .ToList();
        var manueller = aktifKayitlar
            .Where(k => !k.KaynakKayitId.HasValue)
            .OrderBy(k => (int)k.Tur)
            .ThenBy(k => SandikSiraAnahtari(k.SandikNo))
            .ThenBy(k => k.SandikNo, StringComparer.OrdinalIgnoreCase)
            .Select(KalemDtoOlustur)
            .ToList();
        // Tamamlama sayacı: kaydı henüz oluşmamış (senkronize edilmemiş) kaynak sandıklar da gereklidir; "Yapılmaz"
        // varsayılanı olan (ilave dahil) ve tamamlanmış projeye sonradan eklenen sandık sayılmaz (plan satırındaki
        // kararla aynı). Herhangi bir kayda (özel sandık dahil) bağlanmış kaynak zaten kaydıyla sayılır. Grup
        // filtresinden bağımsızdır.
        var kayitliKaynaklar = kayitlar.Where(k => k.KaynakKayitId.HasValue).Select(k => k.KaynakKayitId!.Value).ToHashSet();
        var gerekliSandiklar = gerekenler
            .Select(k => new GerekliSandik(k.BagimsizKayitMi, k.Tur, k.Adet, k.UretimDurumu == AmbalajUretimDurumu.Tamamlandi))
            .Concat(genelDurum == AmbalajUretimDurumu.Tamamlandi ? [] : sandiklar
                .Where(s => !kayitliKaynaklar.Contains(s.Id))
                .Select(s => (Sandik: s, Karar: AmbalajUretimPolitikasi.YeniKaynakKarari(
                    projeGrubu, s.SandikNo, s.Ad, s.AdIngilizce, cekiSandikNolari)))
                .Where(x => x.Karar.AmbalajaDahil)
                .Select(x => new GerekliSandik(false, x.Karar.Grup, SandikAdediHesapla(x.Sandik.SandikNo), false)))
            .ToList();
        var tumKalemler = kaynaklar.Concat(manueller)
            .Where(k => izinliDurumlar == null || izinliDurumlar.Contains(k.UretimDurumu)).ToList();
        aktifKayitlar = aktifKayitlar.Where(k => izinliDurumlar == null || izinliDurumlar.Contains((int)k.UretimDurumu)).ToList();

        var normal = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Normal);
        var ilave = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Ilave);
        var ic = GrupKayitlari(aktifKayitlar, AmbalajSandikTuru.Ic);
        return new AmbalajPlanlamaPlanDto(
            proje.Id,
            proje.ProjeNo,
            proje.FBNo,
            proje.Musteri,
            proje.ProjeTipiId,
            projeTipiMetni,
            normal.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            ilave.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            ic.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.FirinPartiNo))?.FirinPartiNo,
            GrupDurumu(normal),
            GrupDurumu(ilave),
            GrupDurumu(ic),
            tumKalemler,
            tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.Adet),
            tumKalemler.Where(k => k.UretimeAlindi).Sum(k => k.HacimM3),
            (int)genelDurum, gerekliSandiklar.Sum(k => k.Adet), gerekliSandiklar.Where(k => k.Tamamlandi).Sum(k => k.Adet),
            GerekliSandikKirilimi(gerekliSandiklar));
    }

    /// <summary>Tamamlama sayacının bir sandığı: kayıt ya da kaydı henüz oluşmamış kaynak sandık.</summary>
    private sealed record GerekliSandik(bool Ozel, AmbalajSandikTuru Tur, int Adet, bool Tamamlandi);

    /// <summary>
    /// Tamamlama için gerekli kayıtların grup kırılımı. Özel Sandıklar listesindeki bağımsız kayıtlar plan
    /// çekmecesinde görünmediği için ayrı grupta sayılır.
    /// </summary>
    private static IReadOnlyList<AmbalajGrupSandikAdediDto> GerekliSandikKirilimi(IEnumerable<GerekliSandik> gerekenler) =>
        gerekenler
            .GroupBy(k => (k.Ozel, k.Tur))
            .OrderBy(g => g.Key.Ozel)
            .ThenBy(g => GrupSirasi(g.Key.Tur))
            .GroupBy(g => g.Key.Ozel ? "Özel sandık" : AmbalajUretimYardimcilari.TurMetni(g.Key.Tur))
            .Select(g => new AmbalajGrupSandikAdediDto(
                g.Key,
                g.SelectMany(x => x).Sum(k => k.Adet),
                g.SelectMany(x => x).Where(k => k.Tamamlandi).Sum(k => k.Adet)))
            .ToList();

    private static int GrupSirasi(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Normal => 0,
        AmbalajSandikTuru.Ilave => 1,
        AmbalajSandikTuru.Ic => 2,
        AmbalajSandikTuru.Saha => 3,
        AmbalajSandikTuru.Yedek => 4,
        _ => 5
    };

    public static AmbalajPlanlamaProjeOzetDto ProjeOzetDtoOlustur(
        Proje proje,
        string projeTipiMetni,
        IReadOnlyList<Sandik> sandiklar,
        IReadOnlyList<AmbalajUretimKaydi> kayitlar,
        IReadOnlySet<string> cekiSandikNolari,
        int[]? izinliDurumlar = null)
    {
        var plan = PlanDtoOlustur(proje, projeTipiMetni, sandiklar, kayitlar, cekiSandikNolari, izinliDurumlar: izinliDurumlar);
        var ambalajKaynaklari = plan.Kalemler.Where(k => k.KaynakSandikId.HasValue && k.AmbalajaDahilMi != false).ToList();
        var seciliKaynaklar = ambalajKaynaklari.Where(k => k.UretimeAlindi).ToList();
        var olculu = ambalajKaynaklari.Where(k => k.OlcuHazir).ToList();
        var manueller = plan.Kalemler.Where(k => !k.KaynakSandikId.HasValue && k.AmbalajaDahilMi != false).ToList();
        var seciliManueller = manueller.Where(k => k.UretimeAlindi).ToList();
        var eksikler = ambalajKaynaklari.Concat(manueller).Where(k => !k.OlcuHazir).Select(k => k.SandikNo).ToList();
        var projeKaynaklari = ambalajKaynaklari.Where(k => k.Tur == 1).ToList();
        var ilaveKaynaklari = ambalajKaynaklari.Where(k => k.Tur == 2).ToList();

        return new AmbalajPlanlamaProjeOzetDto(
            proje.Id,
            proje.ProjeNo,
            proje.FBNo,
            proje.Musteri,
            proje.ProjeTipiId,
            projeTipiMetni,
            ambalajKaynaklari.Sum(k => k.Adet),
            olculu.Count,
            eksikler.Count,
            eksikler,
            ambalajKaynaklari.Sum(k => k.HacimM3) + manueller.Sum(k => k.HacimM3),
            plan.FirinPartiNo,
            seciliKaynaklar.Sum(k => k.Adet) + seciliManueller.Sum(k => k.Adet),
            ilaveKaynaklari.Count + manueller.Count(k => k.Tur == 2),
            manueller.Count(k => k.Tur == 3),
            seciliKaynaklar.Sum(k => k.HacimM3) + seciliManueller.Sum(k => k.HacimM3),
            plan.ProjeSandiklariDurumId,
            plan.IlaveSandiklarDurumId,
            plan.IcSandiklarDurumId,
            plan.IlaveFirinPartiNo,
            plan.IcSandikFirinPartiNo,
            projeKaynaklari.Sum(k => k.Adet) + manueller.Where(k => k.Tur == 1).Sum(k => k.Adet),
            projeKaynaklari.Sum(k => k.HacimM3) + manueller.Where(k => k.Tur == 1).Sum(k => k.HacimM3),
            ilaveKaynaklari.Sum(k => k.HacimM3) + manueller.Where(k => k.Tur == 2).Sum(k => k.HacimM3),
            manueller.Where(k => k.Tur == 3).Sum(k => k.HacimM3),
            plan.GenelUretimDurumu, plan.GerekliSandikAdedi, plan.TamamlananSandikAdedi, plan.GerekliSandikKirilimi);
    }

    public static AmbalajPlanlamaKalemDto KalemDtoOlustur(AmbalajUretimKaydi kayit) =>
        new(
            kayit.Id,
            kayit.KaynakKayitId,
            kayit.UstKayitId,
            kayit.IcSandikSablonId,
            GrupTuruneDonustur(kayit.Tur),
            TurMetni(kayit.Tur, kayit.KaynakKayitId.HasValue),
            kayit.UretimeAlindi,
            kayit.SandikNo,
            kayit.Ad,
            SandikTipiMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            kayit.Adet,
            kayit.Boy,
            kayit.En,
            kayit.Yukseklik,
            kayit.KullanimAmaci,
            kayit.TalimatVeren,
            kayit.Aciklama,
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(kayit.SandikCinsi) ? kayit.M3Override ?? kayit.HesaplananToplamM3 : null,
            kayit.AmbalajaDahil,
            false,
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(kayit.SandikCinsi), (int)kayit.UretimDurumu,
            AmbalajUretimYardimcilari.OlculerGecerli(kayit));

    public static AmbalajBagimsizSandikDto BagimsizDtoOlustur(
        AmbalajUretimKaydi kayit,
        Proje? proje,
        AmbalajUretimKaydi? ustKayit,
        Sandik? kaynakSandik) =>
        new(
            kayit.Id,
            OzelTurId(kayit.Tur),
            OzelSandikTurMetni(kayit.Tur),
            kayit.ProjeId,
            proje?.ProjeNo,
            proje?.Musteri,
            kaynakSandik?.Id,
            kaynakSandik?.SandikNo,
            kaynakSandik?.Ad,
            ustKayit?.KaynakKayitId,
            kayit.IcSandikSablonId,
            ustKayit?.SandikNo,
            ustKayit?.Ad,
            kayit.UretimeAlindi,
            kayit.SandikNo,
            kayit.Ad ?? string.Empty,
            SandikTipiMetni(kayit.SandikCinsi, kayit.DigerSandikCinsi),
            kayit.Adet,
            kayit.Boy,
            kayit.En,
            kayit.Yukseklik,
            kayit.KullanimAmaci,
            kayit.TalimatVeren,
            kayit.Aciklama,
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(kayit.SandikCinsi) ? kayit.M3Override ?? kayit.HesaplananToplamM3 : null,
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(kayit.SandikCinsi), (int)kayit.UretimDurumu, kayit.AmbalajaDahil);

    public static AmbalajSandikCinsi SandikCinsiCoz(string? sandikTipi) => sandikTipi?.Trim() switch
    {
        "Ahşap Kapalı" => AmbalajSandikCinsi.AhsapKapali,
        "Kafes Sandık" => AmbalajSandikCinsi.Kafes,
        "Kontrplak Sandık" => AmbalajSandikCinsi.Kontrplak,
        "Katlanır Sandık" => AmbalajSandikCinsi.Katlanir,
        _ => AmbalajSandikCinsi.Diger
    };

    public static string SandikTipiMetni(AmbalajSandikCinsi cins, string? diger = null) => cins switch
    {
        AmbalajSandikCinsi.AhsapKapali => "Ahşap Kapalı",
        AmbalajSandikCinsi.Kafes => "Kafes Sandık",
        AmbalajSandikCinsi.Kontrplak => "Kontrplak Sandık",
        AmbalajSandikCinsi.Katlanir => "Katlanır Sandık",
        _ => string.IsNullOrWhiteSpace(diger) ? "Diğer" : diger
    };

    public static bool GecerliSandikTipi(string? value) => value is
        "Ahşap Kapalı" or "Kafes Sandık" or "Kontrplak Sandık" or "Katlanır Sandık";

    public static int SandikAdediHesapla(string? sandikNo)
    {
        var match = SandikAraligiRegex().Match(sandikNo ?? string.Empty);
        if (!match.Success) return 1;
        var baslangic = int.Parse(match.Groups[1].Value);
        var bitis = int.Parse(match.Groups[2].Value);
        return bitis >= baslangic ? bitis - baslangic + 1 : 1;
    }

    public static string OzelSandikTurMetni(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => "İlave",
        AmbalajSandikTuru.Ic => "İç Sandık",
        AmbalajSandikTuru.Saha => "Saha",
        AmbalajSandikTuru.Yedek => "Yedek",
        _ => "Bilinmiyor"
    };

    public static int OzelTurId(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => 2,
        AmbalajSandikTuru.Ic => 3,
        AmbalajSandikTuru.Saha => 4,
        AmbalajSandikTuru.Yedek => 5,
        _ => 0
    };

    /// <summary>Planlama grubunun (1 proje, 2 ilave, 3 iç sandık) kayıt türü.</summary>
    public static AmbalajSandikTuru GrupTuru(int grup) => grup switch
    {
        2 => AmbalajSandikTuru.Ilave,
        3 => AmbalajSandikTuru.Ic,
        _ => AmbalajSandikTuru.Normal
    };

    /// <summary>
    /// A1 tekillik anahtarı: proje + grup (Tur) + sandık no, iptal edilmemiş özel sandıklar arasında;
    /// sandık no harf duyarsız karşılaştırılır. Güncellenen kaydın kendisi hariç tutulur.
    /// </summary>
    public static bool OzelSandikTekrariVarMi(
        IQueryable<AmbalajUretimKaydi> kayitlar,
        int haricId,
        int projeId,
        AmbalajSandikTuru tur,
        string sandikNo)
    {
        var no = sandikNo.Trim().ToUpperInvariant();
        return kayitlar.Any(k => k.Id != haricId && k.ProjeId == projeId && k.BagimsizKayitMi && !k.IptalMi &&
                                 k.Tur == tur && k.SandikNo.ToUpper() == no);
    }

    public static AmbalajSandikTuru OzelTurCoz(int tur) => tur switch
    {
        2 => AmbalajSandikTuru.Ilave,
        3 => AmbalajSandikTuru.Ic,
        4 => AmbalajSandikTuru.Saha,
        5 => AmbalajSandikTuru.Yedek,
        _ => throw new ArgumentOutOfRangeException(nameof(tur))
    };

    private static AmbalajPlanlamaKalemDto KaynakKalemDtoOlustur(
        Sandik sandik,
        AmbalajUretimKaydi? kayit,
        AmbalajSandikTuru projeGrubu,
        IReadOnlySet<string> cekiSandikNolari)
    {
        AmbalajSandikTuru grup;
        bool dahil;
        if (kayit != null)
        {
            grup = AmbalajUretimPolitikasi.KaynakGrubu(projeGrubu, kayit.Tur);
            dahil = kayit.AmbalajaDahil;
        }
        else
        {
            (grup, dahil) = AmbalajUretimPolitikasi.YeniKaynakKarari(
                projeGrubu, sandik.SandikNo, sandik.Ad, sandik.AdIngilizce, cekiSandikNolari);
        }
        var tur = grup == AmbalajSandikTuru.Ilave ? 2 : 1;
        var secili = dahil && (kayit?.UretimeAlindi ?? false);
        var cins = kayit?.SandikCinsi ?? AmbalajUretimPolitikasi.KaynakCinsi(sandik.TipId);
        var hacim = !AmbalajUretimPolitikasi.M3HesaplanabilirMi(cins) ? 0 : kayit != null
            ? kayit.M3Override ?? kayit.HesaplananToplamM3 : KaynakSandikToplamHacmiHesapla(
            sandik.Ad,
            sandik.AdIngilizce,
            sandik.SandikNo,
            sandik.Boy,
            sandik.En,
            sandik.Yukseklik);
        return new AmbalajPlanlamaKalemDto(
            kayit?.Id ?? 0,
            sandik.Id,
            null,
            null,
            tur,
            tur == 2 ? "İlave Sandık" : "Proje Sandığı",
            secili,
            sandik.SandikNo,
            sandik.Ad,
            SandikTipiMetni(cins, kayit?.DigerSandikCinsi),
            SandikAdediHesapla(sandik.SandikNo),
            sandik.Boy ?? 0,
            sandik.En ?? 0,
            sandik.Yukseklik ?? 0,
            kayit?.KullanimAmaci,
            kayit?.TalimatVeren,
            kayit?.Aciklama,
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(cins) ? hacim : null,
            dahil,
            AmbalajKarariOneriliyor(sandik),
            AmbalajUretimPolitikasi.M3HesaplanabilirMi(cins), (int)(kayit?.UretimDurumu ?? AmbalajUretimDurumu.Planlandi),
            // Form ve tamamlama kaydın ölçüsünü kullanır; kaydı henüz oluşmamış sandık aynı kuralla kaynaktan okunur.
            kayit != null
                ? AmbalajUretimYardimcilari.OlculerGecerli(kayit)
                : AmbalajUretimYardimcilari.OlculerGecerli(true, cins, SandikAdediHesapla(sandik.SandikNo), sandik.Boy, sandik.En, sandik.Yukseklik));
    }

    internal static decimal KaynakSandikToplamHacmiHesapla(
        string? ad,
        string? adIngilizce,
        string? sandikNo,
        decimal? boy,
        decimal? en,
        decimal? yukseklik)
    {
        if (!AmbalajHesaplayici.KaynakDisOlculeriGecerli(boy, en, yukseklik))
            return 0;
        var profil = AmbalajAyakProfiliBelirleyici.Belirle(ad, adIngilizce);
        var ic = AmbalajHesaplayici.KaynakIcOlculeri(boy!.Value, en!.Value, yukseklik!.Value);
        var birimHacim = AmbalajHesaplayici.M3OzetiHesapla(
            ic.Boy,
            ic.En,
            ic.Yukseklik,
            1,
            ayakProfili: profil,
            ayakHesapBoyu: boy.Value).HesaplananToplamM3;
        return birimHacim * SandikAdediHesapla(sandikNo);
    }

    private static IReadOnlyList<AmbalajUretimKaydi> GrupKayitlari(
        IEnumerable<AmbalajUretimKaydi> kayitlar,
        AmbalajSandikTuru tur) => kayitlar.Where(k => k.Tur == tur).ToList();

    private static int GrupDurumu(IEnumerable<AmbalajUretimKaydi> kayitlar)
    {
        return (int)AmbalajUretimPolitikasi.ProjeDurumu(kayitlar);
    }

    private static int GrupTuruneDonustur(AmbalajSandikTuru tur) => tur switch
    {
        AmbalajSandikTuru.Ilave => 2,
        AmbalajSandikTuru.Ic => 3,
        _ => 1
    };

    private static string TurMetni(AmbalajSandikTuru tur, bool kaynakli) => tur switch
    {
        AmbalajSandikTuru.Ilave => "İlave Sandık",
        AmbalajSandikTuru.Ic => "İç Sandık",
        _ => kaynakli ? "Proje Sandığı" : "Manuel Proje Sandığı"
    };

    private static bool AmbalajKarariOneriliyor(Sandik sandik)
    {
        return AmbalajUretimPolitikasi.VarsayilanYapilmazMi(sandik.Ad, sandik.AdIngilizce);
    }

    internal static int SandikSiraAnahtari(string? sandikNo)
    {
        var match = IlkSayiRegex().Match(sandikNo ?? string.Empty);
        return match.Success && int.TryParse(match.Value, out var sayi) ? sayi : int.MaxValue;
    }

    [GeneratedRegex(@"^(\d+)\s*-\s*(\d+)$")]
    private static partial Regex SandikAraligiRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex IlkSayiRegex();
}
