using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services.Raporlar;

/// <summary>Raporların ortak miktar ve metin biçimleri.</summary>
internal static class RaporBicimleri
{
    internal static string FormatAdet(decimal value)
    {
        if (decimal.Truncate(value) == value)
            return decimal.Truncate(value).ToString(System.Globalization.CultureInfo.InvariantCulture);

        return value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string RaporMetni(string? metin) => string.IsNullOrWhiteSpace(metin) ? "-" : metin.Trim();

    internal static string GetRaporSandikAdi(Sandik sandik) =>
        string.IsNullOrWhiteSpace(sandik.Ad) && string.IsNullOrWhiteSpace(sandik.AdIngilizce)
            ? "-"
            : $"{RaporMetni(sandik.Ad)} / {RaporMetni(sandik.AdIngilizce)}";

    /// <summary>
    /// SEVK sütununun yazı rengi: sevk edildi yeşil, Grid Kapandı ile sevk edildi koyu turkuaz, eksik sevk turuncu,
    /// sevk edilmedi kırmızı, trafoyla mavi.
    /// </summary>
    internal static string SevkRengi(UrunSevkDurumu? durum) => durum switch
    {
        UrunSevkDurumu.SevkEdildi => "#2E7D32",
        UrunSevkDurumu.GridKapandi => "#00796B",
        UrunSevkDurumu.EksikSevk => "#F57C00",
        UrunSevkDurumu.SevkEdilmedi => "#D32F2F",
        UrunSevkDurumu.Trafoyla => "#1565C0",
        _ => "#000000"
    };

    /// <summary>Normal rapordaki sahaya aktarılan adet ve sandık bilgisinin rengi (mor); SEVK renklerinden ayrışır.</summary>
    internal const string SahaAktarimRengi = "#6A1B9A";

    /// <summary>SEVK sütunu metni; eksik sevkte gönderilen/beklenen adet yazılır ("Eksik Sevk (20/40)").</summary>
    internal static string SevkMetni(UrunSevkSonucu? sonuc) =>
        sonuc is { Durum: UrunSevkDurumu.EksikSevk } eksik
            ? $"{UrunSevkDurumuKurali.Etiket(eksik.Durum)} ({FormatAdet(eksik.SevkEdilen)}/{FormatAdet(eksik.Beklenen)})"
            : UrunSevkDurumuKurali.Etiket(sonuc?.Durum);

    internal static string GetRaporAciklama(CekiSatiri satir)
    {
        var cekiAciklama = satir.Remarks?.Trim();
        var gridAciklama = satir.GridAciklama?.Trim();
        var ucKAciklama = satir.UcKAciklama?.Trim();
        var aciklamalar = new List<string>(3);

        if (!string.IsNullOrWhiteSpace(cekiAciklama))
            aciklamalar.Add($"Çeki: {cekiAciklama}");
        if (!string.IsNullOrWhiteSpace(gridAciklama))
            aciklamalar.Add($"Grid: {gridAciklama}");
        if (!string.IsNullOrWhiteSpace(ucKAciklama))
            aciklamalar.Add($"3K: {ucKAciklama}");

        return string.Join("\n", aciklamalar);
    }
}
