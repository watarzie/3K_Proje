using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// Projeden karşılama transferi geri alındığında donör (veren) satırın Grid yeniden sevk borcunu düzeltir.
/// Donör ürünü verince kalanı açılır ve Grid'de sevk edilmiş satıra kalan kadar "yeniden sevk gerekli" borcu
/// yazılır. Transfer geri alınınca <see cref="CekiSatiri.ProjeGonderilen"/> düşer ve kalan kapanır; borç kalana
/// indirilmezse karşılanmış satır "Yeniden Sevk Gerekli" görünmeye devam eder ve Grid'in sonraki sevki
/// eski borcu kapatamaz. Başka nedenle (geri gönderim, eksik/gelmedi) açılmış ve hâlâ kalanı olan borç korunur.
/// </summary>
public static class ProjeTransferIadeKurali
{
    /// <summary>
    /// Donörün <see cref="CekiSatiri.ProjeGonderilen"/> değeri düşürüldükten sonra çağrılır: borç kalan miktarı
    /// aşamaz; borç kalmadıysa "Yeniden Sevk Gerekli" durumu, aktif sevk partisi varsa "Sevk Edildi"ye, yoksa
    /// "Bekliyor"a döner (3K sıfırlamasındaki Grid sevk durumu senkronuyla aynı karar).
    /// </summary>
    public static void DonorYenidenSevkBorcunuDuzelt(CekiSatiri donor)
    {
        ArgumentNullException.ThrowIfNull(donor);

        var kalan = donor.KalanMiktar;
        if (donor.YenidenSevkGerekliAdet > kalan)
            donor.YenidenSevkGerekliAdet = kalan;

        if (donor.YenidenSevkGerekliAdet <= 0 &&
            donor.GridSevkDurumuId == (int)GridSevkDurum.YenidenSevkGerekli)
        {
            donor.GridSevkDurumuId = (donor.GridSevkMiktari ?? 0) > 0
                ? (int)GridSevkDurum.SevkEdildi
                : (int)GridSevkDurum.Bekliyor;
        }
    }
}
