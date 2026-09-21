using _3K.Core.Constants;
using _3K.Core.Enums;

namespace _3K.Core.Models;

public sealed record RolSablonu(string Kod, string Ad, int ModulMenuId, IReadOnlyList<string> IzinKodlari);

/// <summary>Şablon seçimi açık kullanıcı işlemidir; mevcut rollere otomatik kritik izin vermez.</summary>
public static class RolSablonlari
{
    private static string[] Sec(int parent, Func<YetkiTanimi, bool> predicate) =>
        YetkiKatalogu.Tum.Where(x => x.ParentId == parent && predicate(x)).Select(x => x.Kod).ToArray();
    public static IReadOnlyList<RolSablonu> Tum { get; } =
    [
        new("uretim-yonetici", "Üretim — Yönetici", 46, Sec(46, _ => true)),
        new("uretim-sorumlu", "Üretim — Üretim Sorumlusu", 46, Sec(46, x => !x.Kritik)),
        new("uretim-personel", "Üretim — Üretim Personeli", 46,
        [
            YetkiKodlari.Ambalaj.BekleyenGoruntule, YetkiKodlari.Ambalaj.UretimdeGoruntule,
            YetkiKodlari.Ambalaj.TamamlananGoruntule, YetkiKodlari.Ambalaj.ProjeDetayGoruntule,
            YetkiKodlari.Ambalaj.SandikListesiGoruntule, YetkiKodlari.Ambalaj.OlcuGoruntule,
            YetkiKodlari.Ambalaj.FormGoruntule, YetkiKodlari.Ambalaj.PlanGoruntule,
            YetkiKodlari.Ambalaj.DurumDuzenle, YetkiKodlari.Ambalaj.UretimiTamamla
        ]),
        new("uretim-goruntuleme", "Üretim — Görüntüleme", 46, Sec(46, x => x.GerekenYetki == YetkiTipi.R)),
        new("finans-yoneticisi", "Finans — Finans Yöneticisi", 47, Sec(47, x => x.Kod != YetkiKodlari.Finans.KaliciSil)),
        new("finans-fatura", "Finans — Fatura Kullanıcısı", 47,
        [
            YetkiKodlari.Finans.KayitGoruntule, YetkiKodlari.Finans.ParasalVeriGoruntule,
            YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiKodlari.Finans.TutarGoruntule,
            YetkiKodlari.Finans.GelirGoruntule, YetkiKodlari.Finans.SiparisOperasyonGoruntule,
            YetkiKodlari.Finans.FaturaYonet, YetkiKodlari.Finans.FaturaGir,
            YetkiKodlari.Finans.BelgeYukle, YetkiKodlari.Finans.BelgeIndir
        ]),
        new("finans-siparis", "Finans — Sipariş Kullanıcısı", 47,
        [
            YetkiKodlari.Finans.KayitGoruntule, YetkiKodlari.Finans.ParasalVeriGoruntule,
            YetkiKodlari.Finans.BirimFiyatGoruntule, YetkiKodlari.Finans.TutarGoruntule,
            YetkiKodlari.Finans.GelirGoruntule, YetkiKodlari.Finans.SiparisOperasyonGoruntule,
            YetkiKodlari.Finans.PoGir, YetkiKodlari.Finans.BelgeYukle, YetkiKodlari.Finans.BelgeIndir
        ]),
        new("finans-rapor", "Finans — Rapor Görüntüleyici", 47, Sec(47, x => x.GerekenYetki == YetkiTipi.R)),
        new("finans-yonetici", "Finans — Yönetici", 47, Sec(47, _ => true)),
        new("finans-okuma", "Finans — Sadece Okuma", 47, Sec(47, x => x.GerekenYetki == YetkiTipi.R &&
            x.Kod != YetkiKodlari.Finans.ExcelAktar && x.Kod != YetkiKodlari.Finans.PdfAktar &&
            x.Kod != YetkiKodlari.Finans.BelgeIndir))
    ];
}
