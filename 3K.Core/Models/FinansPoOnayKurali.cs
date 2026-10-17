using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Core.Models;

/// <summary>
/// PO öncesi iş onayı (Onay Merkezi kodu <c>FINANS_PO_IS_ONAYI</c>). Onay kalem (iş kaydı) üzerinde tutulur; onaya
/// PO seçicisindeki grup (proje × iş türü × para birimi) tek talep olarak sunulur. Yalnız üretimden gelen proje
/// kategorileri onay ister; özel ve düzenli iş onaysız PO'ya bağlanır. Onaydan sonra net tutar veya para birimi
/// değişirse onay düşer. Kural kapalıyken PO onay aramaz.
/// </summary>
public static class FinansPoOnayKurali
{
    /// <summary>Onay yalnız üretim kategorilerinde aranır (<see cref="FinansKategoriKurali.UretimKategorisi"/>).</summary>
    public static bool KapsamdaMi(FinansIsTuru isTuru, int? duzenliIsId)
        => FinansKategoriKurali.UretimKategorisi(FinansKategoriKurali.Kategori(isTuru, duzenliIsId));

    public static bool KapsamdaMi(FinansIsKaydi work) => KapsamdaMi(work.IsTuru, work.DuzenliIsId);

    /// <summary>
    /// Onay Merkezi'ndeki "Onay gerekli" anahtarı. Kural kaydı yoksa onay kuyruğuyla aynı güvenli varsayılan
    /// (açık) geçerlidir.
    /// </summary>
    public static bool KuralAcik(bool? onayGerektirirMi) => onayGerektirirMi ?? true;

    /// <summary>Verilmiş onay hâlâ geçerli mi: onaylanan net tutar ve para birimi güncel değerlerle aynı.</summary>
    public static bool OnayGecerli(FinansIsKaydi work)
        => work.PoOnayNetTutar.HasValue &&
           work.PoOnayNetTutar.Value == FinansTutarKurallari.IsNet(work) &&
           string.Equals(work.PoOnayParaBirimi, work.ParaBirimiSnapshot, StringComparison.Ordinal);

    /// <summary>
    /// Kalemin onay durumu. <paramref name="talepKarari"/> ve <paramref name="talepCalistirma"/>, kalemin son onay
    /// talebinin Onay Merkezi kaydından gelir; talep yoksa boştur. Onaylanmış kalemde onay alanları kararın önüne geçer.
    /// </summary>
    public static FinansPoOnayDurumu Durum(
        FinansIsKaydi work,
        OnayDurumu? talepKarari,
        OnayCalistirmaDurumu? talepCalistirma)
    {
        if (!KapsamdaMi(work) || work.IptalEdildi || !work.KaynakAktif)
            return FinansPoOnayDurumu.Gerekmiyor;
        if (work.PoOnayNetTutar.HasValue)
            return OnayGecerli(work) ? FinansPoOnayDurumu.Onaylandi : FinansPoOnayDurumu.OnayDustu;

        return talepKarari switch
        {
            OnayDurumu.Bekliyor => FinansPoOnayDurumu.OnayBekliyor,
            OnayDurumu.Reddedildi => FinansPoOnayDurumu.Reddedildi,
            // Onaylandı fakat uygulanamadı: kalem talepten sonra değişti; yeniden onaya sunulur.
            OnayDurumu.Onaylandi => talepCalistirma switch
            {
                OnayCalistirmaDurumu.Basarisiz or OnayCalistirmaDurumu.Atlandi => FinansPoOnayDurumu.OnayDustu,
                OnayCalistirmaDurumu.Basarili => FinansPoOnayDurumu.OnayaSunulmadi,
                _ => FinansPoOnayDurumu.OnayBekliyor
            },
            _ => FinansPoOnayDurumu.OnayaSunulmadi
        };
    }

    /// <summary>Kalem yeniden onaya gönderilebilir mi: hiç sunulmamış, reddedilmiş veya onayı düşmüş.</summary>
    public static bool OnayaGonderilebilir(FinansPoOnayDurumu durum)
        => durum is FinansPoOnayDurumu.OnayaSunulmadi or FinansPoOnayDurumu.Reddedildi or FinansPoOnayDurumu.OnayDustu;

    /// <summary>Kalem PO'ya bağlanabilir mi: kural kapalı, kalem onay kapsamı dışında veya onayı geçerli.</summary>
    public static bool PoyaBaglanabilir(FinansIsKaydi work, bool kuralAcik)
        => !kuralAcik || !KapsamdaMi(work) || OnayGecerli(work);

    /// <summary>
    /// Bir satırın (birden çok kalem) gösterilecek durumu: kapsam dışı kalemler sayılmaz; bütün kalemler onaylıysa
    /// onaylı, değilse en çok iş bekleyen durum (ret, düşme, sunulmamış, bekleyen sırasıyla).
    /// </summary>
    public static FinansPoOnayDurumu Birlesik(IEnumerable<FinansPoOnayDurumu> durumlar)
    {
        ArgumentNullException.ThrowIfNull(durumlar);
        var kapsam = durumlar.Where(x => x != FinansPoOnayDurumu.Gerekmiyor).ToHashSet();
        if (kapsam.Count == 0) return FinansPoOnayDurumu.Gerekmiyor;
        foreach (var durum in OncelikSirasi)
            if (kapsam.Contains(durum))
                return durum;
        return FinansPoOnayDurumu.Onaylandi;
    }

    private static readonly FinansPoOnayDurumu[] OncelikSirasi =
    [
        FinansPoOnayDurumu.Reddedildi,
        FinansPoOnayDurumu.OnayDustu,
        FinansPoOnayDurumu.OnayaSunulmadi,
        FinansPoOnayDurumu.OnayBekliyor
    ];

    /// <summary>
    /// Onay talebinin grubu: PO seçicisinin üretim grubuyla aynı (proje × iş türü × para birimi). Projesiz kayıt
    /// proje numarası ve müşteriyle ayrılır.
    /// </summary>
    public static string GrupAnahtari(FinansIsKaydi work)
    {
        var proje = work.ProjeId.HasValue ? $"P:{work.ProjeId}" : $"M:{work.ProjeNo}\u001f{work.Musteri}";
        return $"{proje}:{(int)work.IsTuru}:{work.ParaBirimiSnapshot}";
    }

    public static string Etiket(FinansPoOnayDurumu durum) => durum switch
    {
        FinansPoOnayDurumu.OnayaSunulmadi => "Onaya sunulmadı",
        FinansPoOnayDurumu.OnayBekliyor => "Onay bekliyor",
        FinansPoOnayDurumu.Onaylandi => "Onaylandı",
        FinansPoOnayDurumu.Reddedildi => "Reddedildi",
        FinansPoOnayDurumu.OnayDustu => "Onay düştü",
        _ => "Onay gerekmiyor"
    };

    /// <summary>PO'ya bağlanamayan kalemler için kullanıcıya dönen ret mesajı (ilk beş kalem adıyla).</summary>
    public static string PoEngelMesaji(IReadOnlyCollection<(FinansIsKaydi Work, FinansPoOnayDurumu Durum)> engeller)
    {
        ArgumentNullException.ThrowIfNull(engeller);
        var adlar = engeller.Take(5).Select(x => $"{x.Work.ProjeNo} · {x.Work.IsAdi} ({Etiket(x.Durum)})");
        var kalan = engeller.Count > 5 ? $" ve {engeller.Count - 5} kalem daha" : string.Empty;
        return $"PO'ya bağlanmadan önce onaylanması gereken kalemler var: {string.Join(", ", adlar)}{kalan}. " +
               "Kalemleri \"Onaya gönder\" ile onaya sunun; Onay Merkezi'nde onaylandıktan sonra PO'ya bağlanabilir.";
    }
}
