using _3K.Core.Entities;

namespace _3K.Core.Models;

public enum FinansTarifeIslemi
{
    DegisiklikYok = 0,
    YeniSurum = 1,
    SurumuKapatVeYeniAc = 2,
    AyniGunSurumunuDuzelt = 3
}

/// <summary>
/// Fiyat değişikliğinin tarife sürümlerine etkisi: yeni sürümün başlangıç/bitişi ve kapatılacak ya da
/// düzeltilecek mevcut sürüm.
/// </summary>
public sealed record FinansTarifePlani(
    FinansTarifeIslemi Islem,
    FinansFiyatTarifesi? Mevcut,
    DateTime YeniBaslangic,
    DateTime YeniBitis);

/// <summary>
/// Birim fiyat tarifesi sürümleme kuralı (R1 §11). Fiyat değişikliği geçmişi düzenlemez: değişikliğin
/// geçerlilik tarihini kapsayan sürüm bir gün önce kapatılır ve aynı bitişle yeni sürüm açılır. Tarihi
/// kapsayan sürüm yoksa, sonraki sürüme kadar (yoksa açık uçlu) yeni sürüm açılır; böylece geriye dönük
/// girilen fiyat, önceden fiyatsız kalan dönemi de kapsar. Aynı gün başlayan sürüm yalnız hiçbir kayıt
/// onunla fiyatlanmadıysa düzeltilebilir; bu kontrol çağıran servistedir.
/// </summary>
public static class FinansTarifeKurallari
{
    public static readonly DateTime AcikUcluBitis = new(FinansTarihKurallari.EnBuyukYil, 12, 31);

    public static FinansTarifePlani Planla(
        IEnumerable<FinansFiyatTarifesi> surumler,
        DateTime gecerlilikBaslangici,
        decimal birimFiyat,
        string paraBirimi,
        decimal kdvOrani)
    {
        ArgumentNullException.ThrowIfNull(surumler);
        var tarih = gecerlilikBaslangici.Date;
        var aktif = surumler.Where(x => x.Aktif).ToList();
        var mevcut = aktif
            .Where(x => x.GecerlilikBaslangici.Date <= tarih && x.GecerlilikBitisi.Date >= tarih)
            .OrderByDescending(x => x.GecerlilikBaslangici)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
        if (mevcut is null)
        {
            var sonraki = aktif
                .Where(x => x.GecerlilikBaslangici.Date > tarih)
                .OrderBy(x => x.GecerlilikBaslangici)
                .FirstOrDefault();
            return new FinansTarifePlani(FinansTarifeIslemi.YeniSurum, null, tarih,
                sonraki?.GecerlilikBaslangici.Date.AddDays(-1) ?? AcikUcluBitis);
        }
        if (mevcut.BirimFiyat == birimFiyat &&
            string.Equals(mevcut.ParaBirimi, paraBirimi, StringComparison.OrdinalIgnoreCase) &&
            mevcut.KdvOrani == kdvOrani)
            return new FinansTarifePlani(FinansTarifeIslemi.DegisiklikYok, mevcut,
                mevcut.GecerlilikBaslangici.Date, mevcut.GecerlilikBitisi.Date);
        return mevcut.GecerlilikBaslangici.Date == tarih
            ? new FinansTarifePlani(FinansTarifeIslemi.AyniGunSurumunuDuzelt, mevcut, tarih, mevcut.GecerlilikBitisi.Date)
            : new FinansTarifePlani(FinansTarifeIslemi.SurumuKapatVeYeniAc, mevcut, tarih, mevcut.GecerlilikBitisi.Date);
    }
}
