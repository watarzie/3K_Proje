using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Constants;
using _3K.Application.Features.AmbalajIslemleri.Commands;

namespace _3K.Application.Features.AmbalajIslemleri;

internal readonly record struct AmbalajGorunumYetkileri(
    bool M3Gorunur,
    bool SarfGorunur,
    bool KaynakGorunur,
    bool OlcuGorunur = true);

/// <summary>
/// Handler içindeki kayda/diff'e bağlı yetkileri ve hassas alan maskelemesini
/// tek yerde tutar. Pipeline sabit ve istek gövdesinden belirlenebilen yetkileri
/// kontrol eder; bu sınıf ise yalnız DB kaydı okunduktan sonra bilinen kuralları
/// uygular.
/// </summary>
internal static class AmbalajYetkilendirmeYardimcisi
{
    public static bool KaynakliKayitMi(AmbalajUretimKaydi kayit) => kayit.KaynakKayitId.HasValue;

    public static async Task<AmbalajGorunumYetkileri> GorunumYetkileriniGetirAsync(
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.IslemKullaniciId;
        if (!userId.HasValue)
            return new AmbalajGorunumYetkileri(false, false, false, false);

        // Konsolide rol modelinde üç görünüm alias'ı aynı ana liste yetkisine bağlıdır.
        // Aynı kod için üç ayrı DB sorgusu üretmeyelim.
        if (string.Equals(YetkiKodlari.Ambalaj.M3Goruntule, YetkiKodlari.Ambalaj.SarfGoruntule, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(YetkiKodlari.Ambalaj.M3Goruntule, YetkiKodlari.Ambalaj.KaynakGoruntule, StringComparison.OrdinalIgnoreCase))
        {
            var gorunur = await rolService.HasUserPermissionAsync(
                userId.Value, YetkiKodlari.Ambalaj.M3Goruntule, YetkiTipi.R, cancellationToken);
            return new AmbalajGorunumYetkileri(gorunur, gorunur, gorunur);
        }

        // Farklı yetki kodlarına dönülürse aynı scoped DbContext nedeniyle sıralı çalışır.
        var m3 = await rolService.HasUserPermissionAsync(
            userId.Value, YetkiKodlari.Ambalaj.M3Goruntule, YetkiTipi.R, cancellationToken);
        var sarf = await rolService.HasUserPermissionAsync(
            userId.Value, YetkiKodlari.Ambalaj.SarfGoruntule, YetkiTipi.R, cancellationToken);
        var kaynak = await rolService.HasUserPermissionAsync(
            userId.Value, YetkiKodlari.Ambalaj.KaynakGoruntule, YetkiTipi.R, cancellationToken);
        var olcu = await rolService.HasUserPermissionAsync(
            userId.Value, YetkiKodlari.Ambalaj.OlcuGoruntule, YetkiTipi.R, cancellationToken);
        return new AmbalajGorunumYetkileri(m3, m3 && sarf, kaynak, olcu);
    }

    public static async Task<bool> YetkiliMiAsync(
        IRolService rolService,
        ICurrentUserService currentUserService,
        string menuKodu,
        CancellationToken cancellationToken,
        YetkiTipi yetkiTipi = YetkiTipi.W)
    {
        var userId = currentUserService.IslemKullaniciId;
        return userId.HasValue && await rolService.HasUserPermissionAsync(
            userId.Value, menuKodu, yetkiTipi, cancellationToken);
    }

    public static async Task<bool> KaynakMudahalesineYetkiliMiAsync(
        AmbalajUretimKaydi kayit,
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken) =>
        !KaynakliKayitMi(kayit) || await YetkiliMiAsync(
            rolService,
            currentUserService,
            YetkiKodlari.Ambalaj.KaynakMudahalesi,
            cancellationToken);

    public static void DtoyuMaskele(AmbalajUretimKaydiDto dto, AmbalajGorunumYetkileri yetkiler)
    {
        dto.M3BilgisiGorunurMu = yetkiler.M3Gorunur;
        dto.SarfBilgisiGorunurMu = yetkiler.SarfGorunur;
        dto.KaynakBilgisiGorunurMu = yetkiler.KaynakGorunur;
        dto.OlcuBilgisiGorunurMu = yetkiler.OlcuGorunur;
        if (!yetkiler.OlcuGorunur)
            dto.Boy = dto.En = dto.Yukseklik = null;

        if (!yetkiler.M3Gorunur)
        {
            dto.HesaplananBirimM3 = null;
            dto.HesaplananToplamM3 = null;
            dto.M3Override = null;
            dto.M3OverrideNedeni = null;
            dto.NetM3 = null;
            dto.M3HesaplamaVersiyonu = string.Empty;
        }

        if (!yetkiler.SarfGorunur)
        {
            dto.SarfOrani = null;
            dto.SarfM3 = null;
        }

        // Toplam m3 hem net hem sarf bilgisini içerir; iki izin birlikte yoksa
        // kısmi değeri toplam gibi göstermeyip alanı tamamen maskeleriz.
        if (!yetkiler.M3Gorunur || !yetkiler.SarfGorunur)
            dto.ToplamM3 = null;

        if (!yetkiler.KaynakGorunur)
        {
            dto.KaynakModul = AmbalajKaynakModulu.Manuel;
            dto.KaynakModulMetni = string.Empty;
            dto.KaynakKayitId = null;
            dto.KaynakSenkronizasyonuKilitliMi = false;
            dto.KaynakSonSenkronizasyonTarihi = null;
            if (string.Equals(
                    dto.IptalNedeni,
                    AmbalajKaynakSenkronizasyonPolitikasi.SistemKaynakEksigiNedeni,
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


}
