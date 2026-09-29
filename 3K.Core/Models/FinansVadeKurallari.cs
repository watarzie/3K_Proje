using _3K.Core.Enums;
using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>Finans parametre kodları; değerler FinansParametreleri tablosundadır, yoksa varsayılan kullanılır.</summary>
public static class FinansParametreKodlari
{
    public const string VarsayilanVadeGunu = "VarsayilanVadeGunu";
    public const string VadeUyariGunu = "VadeUyariGunu";
}

/// <summary>
/// Vade ve tahsilat kuralı (D3). Tahsilat bilgisi dışarıdan gelir: kullanıcı vadesi gelen faturayı ödendi
/// olarak işaretler. Durum vade tarihi, ödeme işareti ve bugünden türetilir; elle atanmaz.
/// </summary>
public static class FinansVadeKurallari
{
    public const int VarsayilanVadeGunuDegeri = 30;
    public const int VarsayilanUyariGunu = 7;
    public const int EnFazlaVadeGunu = 365;
    public const int EnFazlaUyariGunu = 90;

    public static DateTime VadeTarihi(DateTime faturaTarihi, int vadeGunu) => faturaTarihi.Date.AddDays(vadeGunu);

    public static void VadeGunuDogrula(int vadeGunu)
    {
        if (vadeGunu is < 0 or > EnFazlaVadeGunu)
            throw IsKuraliException.Cakisma($"Varsayılan vade günü 0-{EnFazlaVadeGunu} aralığında olmalıdır.");
    }

    public static void UyariGunuDogrula(int uyariGunu)
    {
        if (uyariGunu is < 0 or > EnFazlaUyariGunu)
            throw IsKuraliException.Cakisma($"Vade uyarı günü 0-{EnFazlaUyariGunu} aralığında olmalıdır.");
    }

    /// <summary>Vade tarihi fatura tarihinden önce olamaz.</summary>
    public static DateTime VadeDogrula(DateTime faturaTarihi, DateTime vadeTarihi)
    {
        if (vadeTarihi.Date < faturaTarihi.Date)
            throw IsKuraliException.Cakisma("Vade tarihi fatura tarihinden önce olamaz.");
        return vadeTarihi.Date;
    }

    public static FinansOdemeDurumu Durum(bool odendi, DateTime vadeTarihi, DateTime? odemeTarihi, DateTime bugun, int uyariGunu)
    {
        if (odendi)
            return (odemeTarihi ?? bugun).Date <= vadeTarihi.Date ? FinansOdemeDurumu.ZamanindaOdendi : FinansOdemeDurumu.GecOdendi;
        if (vadeTarihi.Date < bugun.Date) return FinansOdemeDurumu.VadesiGecti;
        return vadeTarihi.Date <= bugun.Date.AddDays(uyariGunu) ? FinansOdemeDurumu.VadesiYaklasiyor : FinansOdemeDurumu.Bekliyor;
    }

    /// <summary>Ödenmemişte vadeye kalan gün (negatif: gecikme); ödenende ödeme ile vade arasındaki gün farkı.</summary>
    public static int VadeFarkiGun(bool odendi, DateTime vadeTarihi, DateTime? odemeTarihi, DateTime bugun)
        => odendi
            ? ((odemeTarihi ?? bugun).Date - vadeTarihi.Date).Days
            : (vadeTarihi.Date - bugun.Date).Days;

    /// <summary>Ödeme durumunun rapor metni; ön yüz etiketleriyle aynıdır.</summary>
    public static string DurumMetni(FinansOdemeDurumu durum) => durum switch
    {
        FinansOdemeDurumu.Bekliyor => "Bekliyor",
        FinansOdemeDurumu.VadesiYaklasiyor => "Vadesi yaklaşıyor",
        FinansOdemeDurumu.VadesiGecti => "Vadesi geçti",
        FinansOdemeDurumu.ZamanindaOdendi => "Zamanında ödendi",
        FinansOdemeDurumu.GecOdendi => "Geç ödendi",
        _ => durum.ToString()
    };

    /// <summary>Alacak yaşlandırma kovaları (gecikme günü); vadesi geçmemiş alacak "Vadesi gelmedi" grubundadır.</summary>
    public static readonly IReadOnlyList<string> GecikmeGruplari = ["Vadesi gelmedi", "1–30", "31–60", "61–90", "90+"];

    /// <summary>Ödenmemiş faturanın gecikme grubu; <paramref name="vadeFarkiGun"/> negatifse vade o kadar gün geçmiştir.</summary>
    public static string GecikmeGrubu(int vadeFarkiGun)
    {
        var gecikme = -vadeFarkiGun;
        return gecikme <= 0 ? GecikmeGruplari[0] : gecikme <= 30 ? GecikmeGruplari[1] : gecikme <= 60 ? GecikmeGruplari[2] : gecikme <= 90 ? GecikmeGruplari[3] : GecikmeGruplari[4];
    }

    /// <summary>Ödeme işareti: ödendi ise ödeme tarihi zorunlu ve bugünden ileri olamaz.</summary>
    public static DateTime? OdemeTarihiDogrula(bool odendi, DateTime? odemeTarihi, DateTime bugun)
    {
        if (!odendi) return null;
        if (!odemeTarihi.HasValue) throw IsKuraliException.Cakisma("Ödeme tarihi zorunludur.");
        if (odemeTarihi.Value.Date > bugun.Date) throw IsKuraliException.Cakisma("Ödeme tarihi bugünden ileri olamaz.");
        return odemeTarihi.Value.Date;
    }
}
