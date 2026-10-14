using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>Net, KDV ve brüt tutarın para yuvarlamasıyla hesabı.</summary>
public readonly record struct FinansTutar(decimal Net, decimal Kdv, decimal Toplam);

/// <summary>
/// Finans para hesabı, iş bedeli, dağıtım kapasitesi ve iş durumu kuralları için tek kaynak.
/// Tutarlar kuruş hassasiyetinde, yarımlar sıfırdan uzağa yuvarlanır.
/// </summary>
public static class FinansTutarKurallari
{
    public static decimal Para(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>KDV hariç birim fiyattan net, KDV ve brüt tutar.</summary>
    public static FinansTutar Hesapla(decimal miktar, decimal birimFiyat, decimal kdvOrani)
    {
        var net = Para(miktar * birimFiyat);
        var kdv = Para(net * kdvOrani / 100m);
        return new FinansTutar(net, kdv, net + kdv);
    }

    /// <summary>KDV dahil brüt tutardan net ve KDV'yi ayırır; KDV brüt ile net arasındaki farktır.</summary>
    public static FinansTutar KdvDahilHesapla(decimal brut, decimal kdvOrani)
    {
        var net = kdvOrani <= 0 ? brut : Para(brut / (1 + kdvOrani / 100m));
        return new FinansTutar(net, brut - net, brut);
    }

    /// <summary>
    /// Fatura kalemi tutarı. PO kaleminin neti bu faturayla tamamlanıyorsa KDV, PO KDV'sinden önceki
    /// faturaların KDV'si düşülerek bulunur; kuruş yuvarlama farkı son faturada kapanır.
    /// </summary>
    public static FinansTutar FaturaKalemiTutari(
        decimal net, decimal kdvOrani, decimal poNet, decimal poKdv, decimal oncekiNet, decimal oncekiKdv)
    {
        if (net + oncekiNet != poNet) return Hesapla(1, net, kdvOrani);
        var kdv = poKdv - oncekiKdv;
        return new FinansTutar(net, kdv, net + kdv);
    }

    /// <summary>Fiyatlandırma biriminin esas aldığı miktar: adet, m³ ya da sabit/toplam tutar için 1.</summary>
    public static decimal FiyatlandirmaMiktari(FinansFiyatlandirmaBirimi birim, decimal adet, decimal m3) => birim switch
    {
        FinansFiyatlandirmaBirimi.Adet => adet,
        FinansFiyatlandirmaBirimi.Metrekup => m3,
        FinansFiyatlandirmaBirimi.SabitTutar or FinansFiyatlandirmaBirimi.ManuelToplam => 1m,
        _ => 0m
    };

    /// <summary>
    /// Aylık özel/düzenli iş satırında miktar yalnız adet birimli fiyatlandırmada anlamlıdır: m³ ve sabit/toplam tutarlı
    /// satırda gösterilen miktar adetten gelmez; bileşenli hesap kendi ekranında düzenlenir.
    /// </summary>
    public static bool AylikMiktarDuzenlenebilir(FinansIsKaydi work) =>
        work.FiyatlandirmaBirimiSnapshot == FinansFiyatlandirmaBirimi.Adet &&
        work.HesaplamaYontemi != FinansHesaplamaYontemi.Bilesenli;

    /// <summary>Aylık özel/düzenli iş satırında birim fiyat (tarife) manuel toplam ve bileşenli hesap dışında düzenlenir.</summary>
    public static bool AylikBirimFiyatDuzenlenebilir(FinansIsKaydi work) =>
        work.FiyatlandirmaBirimiSnapshot != FinansFiyatlandirmaBirimi.ManuelToplam &&
        work.HesaplamaYontemi != FinansHesaplamaYontemi.Bilesenli;

    /// <summary>
    /// İşin net bedeli: elle girilmiş net tutar; malzemeli fiyatta (kontrplak/katlanır) kontra m³ × kontra tarifesi +
    /// ahşap m³ × ahşap tarifesi; diğerlerinde birim fiyat × fiyatlandırma miktarı. SQL karşılığı FinansSorgulari.IsNetIfadesi.
    /// </summary>
    public static decimal IsNet(FinansIsKaydi work) => work.ManuelNetTutar ?? (work.AhsapBirimFiyatSnapshot.HasValue
        ? MalzemeliNet(work, work.BirimFiyatSnapshot, work.AhsapBirimFiyatSnapshot.Value)
        : Para(work.BirimFiyatSnapshot * FiyatlandirmaMiktari(work.FiyatlandirmaBirimiSnapshot, work.Adet, work.ToplamM3)));

    /// <summary>
    /// Malzemeli net: kontra m³ × kontra fiyatı + ahşap m³ × ahşap fiyatı. Hacim henüz elle girilmediyse kayıt m³'ünün
    /// tamamı kontra sayılır (kontrplak/katlanır sandık; <see cref="FinansHacimKurallari"/>).
    /// </summary>
    public static decimal MalzemeliNet(FinansIsKaydi work, decimal kontraFiyati, decimal ahsapFiyati) =>
        Para(kontraFiyati * (work.KontraM3 ?? work.ToplamM3 - (work.AhsapM3 ?? 0m)) + ahsapFiyati * (work.AhsapM3 ?? 0m));

    public static decimal SiparisNet(FinansIsKaydi work) => work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).Sum(x => x.NetTutarSnapshot);

    public static decimal FaturaNet(FinansSiparisKalemi line) => line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).Sum(x => x.NetTutarSnapshot);

    public static decimal FaturaNet(FinansIsKaydi work) => work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).Sum(FaturaNet);

    public static void Kapasite(decimal requested, decimal used, decimal capacity, string operation)
    {
        if (requested <= 0 || requested != Para(requested))
            throw IsKuraliException.Cakisma($"{operation} net tutarı sıfırdan büyük ve en fazla iki ondalıklı olmalıdır.");
        if (used + requested > capacity)
            throw IsKuraliException.Cakisma($"{operation} net tutarı kalan kapasiteyi aşamaz. Kalan: {Math.Max(0, capacity - used):0.00}.");
    }

    public static FinansIsDurumu Durum(FinansIsKaydi work)
        => Durum(!work.IptalEdildi && work.KaynakAktif, IsNet(work), SiparisNet(work), FaturaNet(work));

    /// <summary>
    /// İşin durumu, tutarları önceden hesaplanmış (ör. SQL projeksiyonundan gelen) kayıt için: iptal veya pasif
    /// kaynak iptal sayılır; bedeli ya da siparişi olmayan iş sipariş bekler.
    /// </summary>
    public static FinansIsDurumu Durum(bool aktif, decimal amount, decimal ordered, decimal invoiced)
    {
        if (!aktif) return FinansIsDurumu.IptalEdildi;
        if (amount <= 0 || ordered <= 0) return FinansIsDurumu.SiparisBekliyor;
        if (ordered >= amount && invoiced >= amount) return FinansIsDurumu.Faturalandi;
        if (invoiced > 0) return FinansIsDurumu.KismiFaturalandi;
        return ordered >= amount ? FinansIsDurumu.SiparisAcildi : FinansIsDurumu.KismiSiparis;
    }

    /// <summary>
    /// PO durumu satırlarından ve aktif fatura kalemlerinden türetilir, elle atanmaz: hiç fatura yoksa
    /// açık, her satır tam faturalandıysa faturalandı, arada kısmi faturalandı.
    /// </summary>
    public static FinansSiparisDurumu SiparisDurumu(FinansSiparis order)
    {
        if (order.IptalEdildi) return FinansSiparisDurumu.IptalEdildi;
        var anyInvoice = order.Kalemler.Any(line => FaturaNet(line) > 0);
        var fullyInvoiced = order.Kalemler.Count > 0 && order.Kalemler.All(line =>
            line.NetTutarSnapshot > 0 && FaturaNet(line) >= line.NetTutarSnapshot);
        return fullyInvoiced
            ? FinansSiparisDurumu.Faturalandi
            : anyInvoice ? FinansSiparisDurumu.KismiFaturalandi : FinansSiparisDurumu.Acik;
    }

    /// <summary>
    /// Bir proje veya grubun aktif kalemlerinden birleşik sipariş/fatura durumu. Gerekli kalem, net bedeli
    /// sıfırdan büyük kalemdir; fiyatı olmayan kalem ayrıca sayılır. Proje, gerekli kalemlerden biri tam
    /// siparişe bağlanmadıkça "Kısmi Sipariş"tir; aynı mantık faturada da uygulanır ve proje yalnız bütün
    /// gerekli kalemleri tamamlandığında tamamlanır. İptal edilmiş/pasif kalemler çağıran tarafça dışarıda
    /// bırakılır.
    /// </summary>
    public static FinansDurumOzeti BirlesikDurum(IEnumerable<FinansKalemTutarlari> kalemler)
    {
        ArgumentNullException.ThrowIfNull(kalemler);
        var liste = kalemler.ToList();
        var gerekli = liste.Where(x => x.Net > 0).ToList();
        var fiyatBekleyen = liste.Count - gerekli.Count;
        if (gerekli.Count == 0)
            return new FinansDurumOzeti(FinansKarsilanma.Yok, FinansKarsilanma.Yok, FinansIsDurumu.SiparisBekliyor, 0, fiyatBekleyen);

        var siparis = Karsilanma(gerekli.Select(x => (x.Net, x.Siparis)));
        var fatura = Karsilanma(gerekli.Select(x => (x.Net, x.Fatura)));
        var durum = siparis switch
        {
            FinansKarsilanma.Yok => FinansIsDurumu.SiparisBekliyor,
            FinansKarsilanma.Kismi => FinansIsDurumu.KismiSiparis,
            _ => fatura switch
            {
                FinansKarsilanma.Tam => FinansIsDurumu.Faturalandi,
                FinansKarsilanma.Kismi => FinansIsDurumu.KismiFaturalandi,
                _ => FinansIsDurumu.SiparisAcildi
            }
        };
        return new FinansDurumOzeti(siparis, fatura, durum, gerekli.Count, fiyatBekleyen);
    }

    private static FinansKarsilanma Karsilanma(IEnumerable<(decimal Gereken, decimal Karsilanan)> kalemler)
    {
        var durumlar = kalemler.Select(x => KalemKarsilanma(x.Gereken, x.Karsilanan)).ToList();
        if (durumlar.All(x => x == FinansKarsilanma.Yok)) return FinansKarsilanma.Yok;
        return durumlar.All(x => x == FinansKarsilanma.Tam) ? FinansKarsilanma.Tam : FinansKarsilanma.Kismi;
    }

    /// <summary>
    /// Tek kalemin sipariş veya fatura karşılanması: hiç tutar yoksa yok, net bedelin tamamı karşılandıysa
    /// tam, arada kısmi. SQL karşılığı <c>FinansSorgulari.KarsilanmaIfadesi</c>'dir.
    /// </summary>
    public static FinansKarsilanma KalemKarsilanma(decimal net, decimal karsilanan)
        => karsilanan <= 0 ? FinansKarsilanma.Yok
            : net > 0 && karsilanan >= net ? FinansKarsilanma.Tam
            : FinansKarsilanma.Kismi;

    /// <summary>İşin birleşik durum hesabına giren net, sipariş ve fatura tutarları.</summary>
    public static FinansKalemTutarlari KalemTutarlari(FinansIsKaydi work) => new(IsNet(work), SiparisNet(work), FaturaNet(work));

    /// <summary>
    /// Belge (fatura/PO) net + KDV = brüt denkliği. Tutarlar kuruşa yuvarlandıktan sonra kesin eşitlik
    /// aranır; doğrulayıcı ve servis aynı kuralı kullanır.
    /// </summary>
    public static bool BelgeToplamiDengeli(decimal net, decimal kdv, decimal toplam) => Para(net) + Para(kdv) == Para(toplam);
}

/// <summary>Birleşik durum hesabında tek kalemin net bedeli, siparişe bağlanan ve faturalanan net tutarı.</summary>
public readonly record struct FinansKalemTutarlari(decimal Net, decimal Siparis, decimal Fatura);

/// <summary>Proje/grup düzeyinde sipariş ve fatura karşılanması, akış durumu ve kalem sayıları.</summary>
public sealed record FinansDurumOzeti(
    FinansKarsilanma Siparis,
    FinansKarsilanma Fatura,
    FinansIsDurumu Durum,
    int GerekliKalem,
    int FiyatBekleyenKalem);
