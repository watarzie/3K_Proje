using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Application.Features.AmbalajIslemleri.Commands;

namespace _3K.Application.Features.AmbalajIslemleri;

internal readonly record struct AmbalajGorunumYetkileri(
    bool M3Gorunur,
    bool SarfGorunur,
    bool KaynakGorunur);

/// <summary>
/// Handler içindeki kayda/diff'e bağlı yetkileri ve hassas alan maskelemesini
/// tek yerde tutar. Pipeline sabit ve istek gövdesinden belirlenebilen yetkileri
/// kontrol eder; bu sınıf ise yalnız DB kaydı okunduktan sonra bilinen kuralları
/// uygular.
/// </summary>
internal static class AmbalajYetkilendirmeYardimcisi
{
    private static readonly HashSet<string> M3Alanlari = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AmbalajUretimKaydi.HesaplananBirimM3),
        nameof(AmbalajUretimKaydi.HesaplananToplamM3),
        nameof(AmbalajUretimKaydi.M3Override),
        nameof(AmbalajUretimKaydi.M3OverrideNedeni),
        nameof(AmbalajUretimKaydi.M3HesaplamaVersiyonu),
        nameof(AmbalajUretimKaydi.ToplamM3)
    };

    private static readonly HashSet<string> SarfAlanlari = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AmbalajUretimKaydi.SarfOrani),
        nameof(AmbalajUretimKaydi.SarfM3),
        nameof(AmbalajUretimKaydi.ToplamM3)
    };

    public static bool KaynakliKayitMi(AmbalajUretimKaydi kayit) => kayit.KaynakKayitId.HasValue;

    public static async Task<AmbalajGorunumYetkileri> GorunumYetkileriniGetirAsync(
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (!userId.HasValue)
            return new AmbalajGorunumYetkileri(false, false, false);

        // Konsolide rol modelinde üç görünüm alias'ı aynı ana liste yetkisine bağlıdır.
        // Aynı kod için üç ayrı DB sorgusu üretmeyelim.
        if (string.Equals(AmbalajMenuKodlari.M3Goruntule, AmbalajMenuKodlari.SarfGoruntule, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(AmbalajMenuKodlari.M3Goruntule, AmbalajMenuKodlari.KaynakGoruntule, StringComparison.OrdinalIgnoreCase))
        {
            var gorunur = await rolService.HasUserPermissionAsync(
                userId.Value, AmbalajMenuKodlari.M3Goruntule, YetkiTipi.R, cancellationToken);
            return new AmbalajGorunumYetkileri(gorunur, gorunur, gorunur);
        }

        // Farklı yetki kodlarına dönülürse aynı scoped DbContext nedeniyle sıralı çalışır.
        var m3 = await rolService.HasUserPermissionAsync(
            userId.Value, AmbalajMenuKodlari.M3Goruntule, YetkiTipi.R, cancellationToken);
        var sarf = await rolService.HasUserPermissionAsync(
            userId.Value, AmbalajMenuKodlari.SarfGoruntule, YetkiTipi.R, cancellationToken);
        var kaynak = await rolService.HasUserPermissionAsync(
            userId.Value, AmbalajMenuKodlari.KaynakGoruntule, YetkiTipi.R, cancellationToken);
        return new AmbalajGorunumYetkileri(m3, sarf, kaynak);
    }

    public static async Task<bool> YetkiliMiAsync(
        IRolService rolService,
        ICurrentUserService currentUserService,
        string menuKodu,
        CancellationToken cancellationToken,
        YetkiTipi yetkiTipi = YetkiTipi.W)
    {
        var userId = currentUserService.UserId;
        return userId.HasValue && await rolService.HasUserPermissionAsync(
            userId.Value, menuKodu, yetkiTipi, cancellationToken);
    }

    public static async Task<string?> EksikYetkiKodunuGetirAsync(
        IEnumerable<string> menuKodlari,
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        foreach (var menuKodu in menuKodlari.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!await YetkiliMiAsync(rolService, currentUserService, menuKodu, cancellationToken))
                return menuKodu;
        }

        return null;
    }

    public static async Task<bool> KaynakMudahalesineYetkiliMiAsync(
        AmbalajUretimKaydi kayit,
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken) =>
        !KaynakliKayitMi(kayit) || await YetkiliMiAsync(
            rolService,
            currentUserService,
            AmbalajMenuKodlari.KaynakMudahalesi,
            cancellationToken);

    public static IReadOnlyList<string> GuncellemeEkYetkiKodlariniBelirle(
        AmbalajUretimKaydi mevcut,
        AmbalajUretimKaydiGuncelleCommand yeni)
    {
        var kodlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (mevcut.Tur != yeni.Tur)
        {
            kodlar.Add(AmbalajMenuKodlari.TurDuzenle);
            var turOlusturmaKodu = AmbalajMenuKodlari.TurOlusturmaKodu(yeni.Tur);
            if (turOlusturmaKodu != null)
                kodlar.Add(turOlusturmaKodu);
        }

        if (mevcut.ProjeId != yeni.ProjeId ||
            Farkli(mevcut.ManuelProjeNo, yeni.ManuelProjeNo) ||
            Farkli(mevcut.ManuelProjeAdi, yeni.ManuelProjeAdi) ||
            mevcut.UstKayitId != yeni.UstKayitId)
        {
            kodlar.Add(AmbalajMenuKodlari.ProjeDuzenle);
            if (!yeni.ProjeId.HasValue)
                kodlar.Add(AmbalajMenuKodlari.ManuelProje);
        }

        if (mevcut.SandikCinsi != yeni.SandikCinsi ||
            Farkli(mevcut.DigerSandikCinsi, yeni.DigerSandikCinsi))
            kodlar.Add(AmbalajMenuKodlari.CinsDuzenle);

        if (mevcut.Adet != yeni.Adet || mevcut.Boy != yeni.Boy ||
            mevcut.En != yeni.En || mevcut.Yukseklik != yeni.Yukseklik)
            kodlar.Add(AmbalajMenuKodlari.OlcuDuzenle);

        if (Farkli(mevcut.KullanimAmaci, yeni.KullanimAmaci) ||
            Farkli(mevcut.TalepEdenKisi, yeni.TalepEdenKisi) ||
            Farkli(mevcut.TalepEdenBolum, yeni.TalepEdenBolum) ||
            Farkli(mevcut.TalimatVeren, yeni.TalimatVeren))
            kodlar.Add(AmbalajMenuKodlari.TalepBilgileriDuzenle);

        if (mevcut.UretimTarihi != yeni.UretimTarihi ||
            Farkli(mevcut.FirinPartiNo, yeni.FirinPartiNo))
            kodlar.Add(AmbalajMenuKodlari.DurumDuzenle);

        return kodlar.ToArray();
    }

    public static IReadOnlyList<string> SecimGecisYetkiKodlariniBelirle(
        AmbalajUretimKaydi mevcut,
        bool ambalajaDahil,
        bool uretimeAlindi)
    {
        var kodlar = new List<string>(2);
        if (mevcut.AmbalajaDahil != ambalajaDahil)
            kodlar.Add(ambalajaDahil ? AmbalajMenuKodlari.DahilEt : AmbalajMenuKodlari.HaricTut);
        if (mevcut.UretimeAlindi != uretimeAlindi)
            kodlar.Add(uretimeAlindi ? AmbalajMenuKodlari.UretimeAl : AmbalajMenuKodlari.UretimdenCikar);
        return kodlar;
    }

    private static bool Farkli(string? sol, string? sag) => !string.Equals(
        AmbalajUretimYardimcilari.Temizle(sol),
        AmbalajUretimYardimcilari.Temizle(sag),
        StringComparison.OrdinalIgnoreCase);

    public static void DtoyuMaskele(AmbalajUretimKaydiDto dto, AmbalajGorunumYetkileri yetkiler)
    {
        dto.M3BilgisiGorunurMu = yetkiler.M3Gorunur;
        dto.SarfBilgisiGorunurMu = yetkiler.SarfGorunur;
        dto.KaynakBilgisiGorunurMu = yetkiler.KaynakGorunur;

        if (!yetkiler.M3Gorunur)
        {
            dto.HesaplananBirimM3 = 0;
            dto.HesaplananToplamM3 = 0;
            dto.M3Override = null;
            dto.M3OverrideNedeni = null;
            dto.NetM3 = 0;
            dto.M3HesaplamaVersiyonu = string.Empty;
        }

        if (!yetkiler.SarfGorunur)
        {
            dto.SarfOrani = 0;
            dto.SarfM3 = 0;
        }

        // Toplam m3 hem net hem sarf bilgisini içerir; iki izin birlikte yoksa
        // kısmi değeri toplam gibi göstermeyip alanı tamamen maskeleriz.
        if (!yetkiler.M3Gorunur || !yetkiler.SarfGorunur)
            dto.ToplamM3 = 0;

        if (!yetkiler.KaynakGorunur)
        {
            dto.KaynakModul = AmbalajKaynakModulu.Manuel;
            dto.KaynakModulMetni = string.Empty;
            dto.KaynakKayitId = null;
            dto.KaynakSenkronizasyonuKilitliMi = false;
            dto.KaynakSonSenkronizasyonTarihi = null;
            if (string.Equals(
                    dto.IptalNedeni,
                    Commands.AmbalajKaynakSenkronizasyonPolitikasi.SistemKaynakEksigiNedeni,
                    StringComparison.Ordinal))
                dto.IptalNedeni = null;
        }
    }

    public static async Task<AmbalajUretimKaydiDto> DtoyuYetkiyeGoreMaskeleAsync(
        AmbalajUretimKaydiDto dto,
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var yetkiler = await GorunumYetkileriniGetirAsync(
            rolService, currentUserService, cancellationToken);
        DtoyuMaskele(dto, yetkiler);
        return dto;
    }

    public static AmbalajUretimHareketiDto HareketiMaskele(
        AmbalajUretimHareketiDto hareket,
        AmbalajGorunumYetkileri yetkiler)
    {
        var m3Gizli = !yetkiler.M3Gorunur && M3Alanlari.Contains(hareket.AlanAdi);
        var sarfGizli = !yetkiler.SarfGorunur && SarfAlanlari.Contains(hareket.AlanAdi);
        var kaynakGizli = !yetkiler.KaynakGorunur &&
                          (hareket.AlanAdi.StartsWith("Kaynak", StringComparison.OrdinalIgnoreCase) ||
                           hareket.Islem.Contains("Kaynak", StringComparison.OrdinalIgnoreCase));
        if (!m3Gizli && !sarfGizli && !kaynakGizli)
            return hareket;

        hareket.EskiDeger = null;
        hareket.YeniDeger = null;
        hareket.Aciklama = null;
        hareket.DegerlerGizliMi = true;
        if (kaynakGizli)
        {
            hareket.AlanAdi = "KisitliKaynakAlani";
            hareket.Islem = "Kayıt güncellendi";
        }

        return hareket;
    }
}
