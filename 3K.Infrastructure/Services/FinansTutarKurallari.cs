using _3K.Core.Entities;
using _3K.Core.Enums;

namespace _3K.Infrastructure.Services;

internal static class FinansTutarKurallari
{
    internal static decimal Para(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    internal static decimal IsNet(FinansIsKaydi work) => work.ManuelNetTutar ?? Para(work.BirimFiyatSnapshot *
        (work.FiyatlandirmaBirimiSnapshot switch
        {
            FinansFiyatlandirmaBirimi.Adet => work.Adet,
            FinansFiyatlandirmaBirimi.Metrekup => work.ToplamM3,
            _ => 1m
        }));
    internal static decimal SiparisNet(FinansIsKaydi work) => work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).Sum(x => x.NetTutarSnapshot);
    internal static decimal FaturaNet(FinansSiparisKalemi line) => line.FaturaKalemleri.Where(x => !x.FinansFatura.IptalEdildi).Sum(x => x.NetTutarSnapshot);
    internal static decimal FaturaNet(FinansIsKaydi work) => work.SiparisKalemleri.Where(x => !x.FinansSiparis.IptalEdildi).Sum(FaturaNet);
    internal static void Kapasite(decimal requested, decimal used, decimal capacity, string operation)
    {
        if (requested <= 0 || requested != Para(requested))
            throw new InvalidOperationException($"{operation} net tutarı sıfırdan büyük ve en fazla iki ondalıklı olmalıdır.");
        if (used + requested > capacity)
            throw new InvalidOperationException($"{operation} net tutarı kalan kapasiteyi aşamaz. Kalan: {Math.Max(0, capacity - used):0.00}.");
    }
    internal static FinansIsDurumu Durum(FinansIsKaydi work)
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
