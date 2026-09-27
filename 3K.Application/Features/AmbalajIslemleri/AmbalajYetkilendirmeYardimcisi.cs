using _3K.Application.Features.AmbalajIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Constants;
using _3K.Application.Features.AmbalajIslemleri.Commands;

namespace _3K.Application.Features.AmbalajIslemleri;

public readonly record struct AmbalajGorunumYetkileri(
    bool M3Gorunur,
    bool SarfGorunur,
    bool KaynakGorunur,
    bool OlcuGorunur = true);

/// <summary>
/// Ambalaj görünüm izinlerini (m³, sarf, kaynak, ölçü, üretim durumu) okur ve kayıt DTO'sunu
/// bunlara göre maskeler. Sabit işlem izinleri pipeline'da, kayda bağlı kapılar
/// <see cref="AmbalajKritikIslemDenetimi"/> içinde denetlenir.
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

        // Aynı scoped DbContext kullanıldığı için izinler sıralı okunur.
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

    /// <summary>Kullanıcının görebildiği üretim durumları; her durum ayrı bir görüntüleme iznine bağlıdır.</summary>
    public static async Task<int[]> IzinliDurumlariGetirAsync(
        IRolService rolService,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var izinli = new List<int>(DurumGorunumIzinleri.Length);
        foreach (var (durum, kod) in DurumGorunumIzinleri)
        {
            if (await YetkiliMiAsync(rolService, currentUserService, kod, cancellationToken, YetkiTipi.R))
                izinli.Add((int)durum);
        }
        return izinli.ToArray();
    }

    private static readonly (AmbalajUretimDurumu Durum, string Kod)[] DurumGorunumIzinleri =
    [
        (AmbalajUretimDurumu.Planlandi, YetkiKodlari.Ambalaj.BekleyenGoruntule),
        (AmbalajUretimDurumu.Uretimde, YetkiKodlari.Ambalaj.UretimdeGoruntule),
        (AmbalajUretimDurumu.Tamamlandi, YetkiKodlari.Ambalaj.TamamlananGoruntule)
    ];

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
