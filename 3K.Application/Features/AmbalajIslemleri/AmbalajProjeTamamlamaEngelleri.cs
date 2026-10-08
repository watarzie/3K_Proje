using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Models;

namespace _3K.Application.Features.AmbalajIslemleri;

/// <summary>
/// Proje bazlı tamamlamayı engelleyen sandıkları tek geçişte toplar: üretim formu olmayan, üretime alınmamış ve
/// ölçüsü/manuel m³ değeri eksik kayıtlar. Mesaj her sandığı numarası ve grubuyla söyler; aynı liste 409 yanıtının
/// <c>issues</c> alanında döner ki ekran engelleyen satırları vurgulayabilsin.
/// </summary>
internal static class AmbalajProjeTamamlamaEngelleri
{
    public const string FormYok = "FormYok";
    public const string UretimeAlinmadi = "UretimeAlinmadi";
    public const string OlcuEksik = "OlcuEksik";

    /// <summary>Uzun projelerde mesaj okunur kalsın diye grup başına yazılan sandık numarası sınırı.</summary>
    private const int GrupBasinaEnFazlaNo = 15;

    private static readonly (string Neden, string Baslik, string Aciklama)[] Nedenler =
    [
        (FormYok, "Üretim formu olmayan", "üretim formu yok"),
        (UretimeAlinmadi, "Üretime alınmamış", "üretime alınmamış"),
        (OlcuEksik, "Ölçüsü veya manuel m³ değeri eksik", "ölçüsü veya manuel m³ değeri eksik")
    ];

    /// <summary>Her kayıt için değişiklik öncesi hâliyle ilk engel; engeli olmayan kayıt listeye girmez.</summary>
    public static IReadOnlyList<AmbalajTamamlamaEngeliDto> Belirle(IEnumerable<AmbalajKayitDegisikligi> degisiklikler) =>
        degisiklikler
            .Select(d => (d.Kayit, Neden: Neden(d)))
            .Where(x => x.Neden != null)
            .OrderBy(x => AmbalajPlanlamaYardimcisi.SandikSiraAnahtari(x.Kayit.SandikNo))
            .ThenBy(x => x.Kayit.SandikNo, StringComparer.OrdinalIgnoreCase)
            .Select(x => Engel(x.Kayit, x.Neden!))
            .ToList();

    /// <summary>"Üretim tamamlanamıyor. Üretim formu olmayan: 12, 14 (İlave). Üretime alınmamış: 7 (Normal). …"</summary>
    public static string Mesaj(IReadOnlyList<AmbalajTamamlamaEngeliDto> engeller)
    {
        var bolumler = Nedenler
            .Select(n => (n.Baslik, Engeller: engeller.Where(e => e.Neden == n.Neden).ToList()))
            .Where(n => n.Engeller.Count > 0)
            .Select(n => $"{n.Baslik}: {GrupListesi(n.Engeller)}");
        return $"Üretim tamamlanamıyor. {string.Join(". ", bolumler)}. Bu sandıkları üretim planında forma alın ve " +
               "eksiklerini tamamlayın; ambalaj yapılmayacak kaynak sandığı \"Yapılmaz\" olarak işaretleyin.";
    }

    private static string? Neden(AmbalajKayitDegisikligi degisiklik) =>
        !degisiklik.FormuVar ? FormYok
        : !degisiklik.Kayit.UretimeAlindi ? UretimeAlinmadi
        : !AmbalajUretimYardimcilari.UretimMiktariGecerli(degisiklik.Kayit) ? OlcuEksik
        : null;

    private static AmbalajTamamlamaEngeliDto Engel(AmbalajUretimKaydi kayit, string neden)
    {
        var grup = GrupMetni(kayit);
        var aciklama = Nedenler.First(n => n.Neden == neden).Aciklama;
        return new AmbalajTamamlamaEngeliDto(
            kayit.Id,
            kayit.BagimsizKayitMi ? null : kayit.KaynakKayitId,
            kayit.SandikNo,
            grup,
            kayit.BagimsizKayitMi,
            neden,
            $"{kayit.SandikNo} ({grup}): {aciklama}");
    }

    /// <summary>Plan grubu (Normal, İlave, İç); Özel Sandıklar listesindeki bağımsız kayıt ayrıca belirtilir.</summary>
    private static string GrupMetni(AmbalajUretimKaydi kayit)
    {
        var tur = AmbalajUretimYardimcilari.TurMetni(kayit.Tur);
        return kayit.BagimsizKayitMi ? $"{tur} özel sandık" : tur;
    }

    private static string GrupListesi(IReadOnlyList<AmbalajTamamlamaEngeliDto> engeller) =>
        string.Join("; ", engeller
            .GroupBy(e => e.GrupMetni)
            .Select(g =>
            {
                var numaralar = g.Select(e => e.SandikNo).ToList();
                var gorunen = string.Join(", ", numaralar.Take(GrupBasinaEnFazlaNo));
                var kalan = numaralar.Count - GrupBasinaEnFazlaNo;
                return kalan > 0 ? $"{gorunen} ve {kalan} sandık daha ({g.Key})" : $"{gorunen} ({g.Key})";
            }));
}
