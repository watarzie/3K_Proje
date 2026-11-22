using _3K.Core.Models;
using _3K.YuklemeMotoru.Ekipman;

namespace _3K.Infrastructure.Services.YuklemePlanlama;

/// <summary>Ekrandan gelen teknik verinin okunmuş hali ya da okunamama/sınır sorunları.</summary>
internal sealed record EkipmanTeknikVeriGirdiSonucu(EkipmanTeknikVerisi? Veri, IReadOnlyList<string> Sorunlar);

/// <summary>
/// Ekrandan gelen teknik veri JSON'unu Excel'in alt sayfalarıyla aynı kurallarla okur: zorunlu sütunların karşılığı
/// zorunlu alandır, metinler hücre gibi kırpılır (boş metin "yok"), metin sınırları aynıdır ve listeler kanonik sıraya
/// dizilir. Böylece aynı bilgi iki yoldan da aynı teknik veriyi (ve aynı revizyon kararını) üretir. Yapısal denetim
/// (<see cref="EkipmanTeknikVeriDenetimi"/>) çağıranındır.
/// </summary>
internal static class EkipmanTeknikVeriGirdisi
{
    public const string OkunamadiMesaji = "Teknik veri okunamadı; geçerli bir JSON nesnesi gönderin.";
    public const string BosKayitMesaji = "Teknik veri listelerinde boş kayıt olamaz.";

    // Hata yolu istekten gelen alan adını taşıyabilir; mesaja sınırlı uzunlukta yazılır.
    private const int YolEnFazla = 120;

    public static EkipmanTeknikVeriGirdiSonucu Oku(string? json)
    {
        var okuma = EkipmanTeknikVeriSerilestirici.KatiOku(json ?? string.Empty);
        if (okuma.Veri is not { } ham)
            return new EkipmanTeknikVeriGirdiSonucu(null, [OkumaSorunu(okuma.HataYolu)]);

        // Liste ögesi olarak gönderilen null (ör. "yuzeyler":[null]) şema dışıdır; denetime ulaşmadan reddedilir.
        if (ham.Yuzeyler.Any(y => y is null) || ham.Acikliklar.Any(a => a is null) || ham.Engeller.Any(e => e is null) ||
            ham.Dingiller.Any(d => d is null) || ham.BaglamaNoktalari.Any(b => b is null))
            return new EkipmanTeknikVeriGirdiSonucu(null, [BosKayitMesaji]);

        var veri = ham with
        {
            Yuzeyler = ham.Yuzeyler.Select(y => y with { Kod = y.Kod.Trim(), Ad = EkipmanKitapligiKurali.MetinNormalize(y.Ad) }).ToList(),
            Engeller = ham.Engeller.Select(e => e with { Aciklama = EkipmanKitapligiKurali.MetinNormalize(e.Aciklama) }).ToList()
        };

        var sorunlar = new List<string>();
        foreach (var yuzey in veri.Yuzeyler)
        {
            if (yuzey.Kod.Length > EkipmanKitapligiKurali.KodEnFazla)
                sorunlar.Add($"Yüzey kodu en fazla {EkipmanKitapligiKurali.KodEnFazla} karakter olabilir.");
            if (yuzey.Ad is { Length: > EkipmanKitapligiKurali.AdEnFazla })
                sorunlar.Add($"Yüzey '{Kisalt(yuzey.Kod)}': ad en fazla {EkipmanKitapligiKurali.AdEnFazla} karakter olabilir.");
        }
        if (veri.Engeller.Any(e => e.Aciklama is { Length: > EkipmanKitapligiKurali.AciklamaEnFazla }))
            sorunlar.Add($"Engel açıklaması en fazla {EkipmanKitapligiKurali.AciklamaEnFazla} karakter olabilir.");

        return new EkipmanTeknikVeriGirdiSonucu(EkipmanTeknikVeriSerilestirici.KanonikSira(veri), sorunlar);
    }

    private static string OkumaSorunu(string? yol) => string.IsNullOrEmpty(yol)
        ? OkunamadiMesaji
        : $"Teknik veri okunamadı: '{Kisalt(yol)}' alanı bilinmiyor, eksik ya da hatalı.";

    private static string Kisalt(string metin) => metin.Length > YolEnFazla ? metin[..YolEnFazla] + "…" : metin;
}
