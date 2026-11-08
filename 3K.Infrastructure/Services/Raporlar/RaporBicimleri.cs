using _3K.Core.Common;
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
    /// SEVK sütununun yazı rengi: sevk edildi yeşil, eksik sevk turuncu, sevk edilmedi kırmızı, trafoyla mavi.
    /// </summary>
    internal static string SevkRengi(UrunSevkDurumu? durum) => durum switch
    {
        UrunSevkDurumu.SevkEdildi => "#2E7D32",
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

    /// <summary>
    /// Gerçekleşen çeki AÇIKLAMA hücresi: çeki/Grid/3K açıklamalarının altında sandık parçalarının ürün bilgisi
    /// (<see cref="SandikIcerik.UrunBilgisi"/>: seri no, SKT, lot). Ayrı sütun açılmaz.
    /// </summary>
    internal static string GetGerceklesenRaporAciklama(CekiSatiri satir)
    {
        var satirlar = new List<string>();
        var aciklama = GetRaporAciklama(satir);
        if (!string.IsNullOrEmpty(aciklama))
            satirlar.Add(aciklama);
        satirlar.AddRange(UrunBilgisiSatirlari(satir.SandikIcerikleri));
        return string.Join("\n", satirlar);
    }

    /// <summary>
    /// Ürün bilgisi satırları. Bilgi tek (ya da bütün parçalarda aynı) ise "Ürün bilgisi: …"; bölünmüş üründe
    /// parçalar farklı bilgi taşıyorsa her biri sandık numarasıyla ayrı satır: "Ürün bilgisi (Sandık 17): …".
    /// Kullanıcının alt satırlara yazdığı bilgi tek satırda virgülle birleştirilir.
    /// </summary>
    internal static IReadOnlyList<string> UrunBilgisiSatirlari(IEnumerable<SandikIcerik> icerikler)
    {
        var bilgiler = icerikler
            .Select(i => (SandikNo: i.Sandik?.SandikNo?.Trim(), Id: i.Id, Bilgi: UrunBilgisiTekSatir(i.UrunBilgisi)))
            .Where(x => x.Bilgi.Length > 0)
            .OrderBy(x => x.SandikNo ?? string.Empty, SandikNumarasiComparer.Instance)
            .ThenBy(x => x.Id)
            .ToList();

        if (bilgiler.Count == 0)
            return [];

        var farkliBilgiler = bilgiler.Select(x => x.Bilgi).Distinct(StringComparer.Ordinal).ToList();
        if (farkliBilgiler.Count == 1)
            return [$"Ürün bilgisi: {farkliBilgiler[0]}"];

        return bilgiler
            .GroupBy(x => (x.SandikNo, x.Bilgi))
            .Select(g => string.IsNullOrEmpty(g.Key.SandikNo)
                ? $"Ürün bilgisi: {g.Key.Bilgi}"
                : $"Ürün bilgisi (Sandık {g.Key.SandikNo}): {g.Key.Bilgi}")
            .ToList();
    }

    private static string UrunBilgisiTekSatir(string? urunBilgisi)
    {
        if (string.IsNullOrWhiteSpace(urunBilgisi))
            return string.Empty;

        var parcalar = urunBilgisi
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().TrimEnd(',', ';').TrimEnd())
            .Where(p => p.Length > 0);
        return string.Join(", ", parcalar);
    }
}
