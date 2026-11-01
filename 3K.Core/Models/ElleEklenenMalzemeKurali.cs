namespace _3K.Core.Models;

/// <summary>
/// Saha/yedek sandığına çeki satırı olmadan elle eklenen malzemenin miktarları (sandık içerik ekranı ve sevkiyat
/// raporuyla aynı): miktar tahsis, yoksa girilen miktar; konulan girilen konulan adet, yoksa miktarın tamamı (elle
/// eklenen malzeme sandığa konmuş olarak eklenir).
/// </summary>
public static class ElleEklenenMalzemeKurali
{
    public static (decimal Miktar, decimal Konulan) Miktarlar(decimal tahsisMiktari, decimal miktar, decimal konulanAdet)
    {
        var sandikMiktari = tahsisMiktari > 0 ? tahsisMiktari : miktar;
        return (sandikMiktari, konulanAdet > 0 ? konulanAdet : sandikMiktari);
    }
}
