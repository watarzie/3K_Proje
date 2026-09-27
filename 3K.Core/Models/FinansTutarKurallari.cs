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

    public static decimal IsNet(FinansIsKaydi work) => work.ManuelNetTutar ?? Para(work.BirimFiyatSnapshot *
        FiyatlandirmaMiktari(work.FiyatlandirmaBirimiSnapshot, work.Adet, work.ToplamM3));

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
    {
        if (work.IptalEdildi || !work.KaynakAktif) return FinansIsDurumu.IptalEdildi;
        var amount = IsNet(work);
        var ordered = SiparisNet(work);
        var invoiced = FaturaNet(work);
        if (amount <= 0 || ordered <= 0) return FinansIsDurumu.SiparisBekliyor;
        if (ordered >= amount && invoiced >= amount) return FinansIsDurumu.Faturalandi;
        if (invoiced > 0) return FinansIsDurumu.KismiFaturalandi;
        return ordered >= amount ? FinansIsDurumu.SiparisAcildi : FinansIsDurumu.KismiSiparis;
    }
}
