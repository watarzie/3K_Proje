using _3K.Core.Exceptions;

namespace _3K.Core.Models;

/// <summary>Grup dağıtımına giren kalem ve kalemin henüz dağıtılmamış net kapasitesi.</summary>
public readonly record struct FinansDagitimKapasitesi(int Id, decimal Kalan);

/// <summary>Grup tutarından kaleme düşen net pay.</summary>
public readonly record struct FinansDagitimPayi(int Id, decimal Tutar);

/// <summary>
/// Proje × kategori (veya düzenli iş) grubuna girilen PO/fatura net tutarının kalemlere dağıtımı için tek
/// kaynak. Tutar kalemlere verilen sırayla (eski kayıt önce) ve her kalemin kalan kapasitesini aşmadan
/// dağıtılır; grup toplam kalanını aşan tutar reddedilir. Böylece kullanıcı sandıkları tek tek seçmeden
/// grup düzeyinde kısmi sipariş veya fatura girebilir, kalem kapasitesi kuralı
/// (<see cref="FinansTutarKurallari.Kapasite"/>) de her kalemde korunur.
/// </summary>
public static class FinansDagitimKurallari
{
    public static IReadOnlyList<FinansDagitimPayi> Dagit(
        decimal tutar,
        IReadOnlyList<FinansDagitimKapasitesi> kalemler,
        string islemAdi)
    {
        ArgumentNullException.ThrowIfNull(kalemler);
        if (tutar <= 0 || tutar != FinansTutarKurallari.Para(tutar))
            throw IsKuraliException.Cakisma($"{islemAdi} net tutarı sıfırdan büyük ve en fazla iki ondalıklı olmalıdır.");
        if (kalemler.Count == 0)
            throw IsKuraliException.Cakisma($"{islemAdi} grubu en az bir kalem içermelidir.");
        if (kalemler.Select(x => x.Id).Distinct().Count() != kalemler.Count)
            throw IsKuraliException.Cakisma($"{islemAdi} grubunda aynı kalem birden fazla kez bulunamaz.");

        var toplamKalan = kalemler.Sum(x => Math.Max(0m, x.Kalan));
        if (tutar > toplamKalan)
            throw IsKuraliException.Cakisma($"{islemAdi} net tutarı grubun kalan kapasitesini aşamaz. Kalan: {toplamKalan:0.00}.");

        var paylar = new List<FinansDagitimPayi>();
        var dagitilacak = tutar;
        foreach (var kalem in kalemler)
        {
            if (dagitilacak <= 0) break;
            var kapasite = Math.Max(0m, kalem.Kalan);
            if (kapasite <= 0) continue;
            var pay = Math.Min(kapasite, dagitilacak);
            paylar.Add(new FinansDagitimPayi(kalem.Id, pay));
            dagitilacak -= pay;
        }
        return paylar;
    }
}
