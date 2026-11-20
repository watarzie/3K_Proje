namespace _3K.YuklemeMotoru.Yerlesim;

/// <summary>Ağırlık merkezi hesabına giren yük: ağırlığı ve ekipman koordinatlarındaki kutusu (mm).</summary>
public readonly record struct AgirlikliKutu(decimal Kg, int XMm, int YMm, int ZMm, int BoyMm, int EnMm, int YukseklikMm);

/// <summary>Ağırlık merkezi (ekipman koordinatları, mm): X ön uçtan arkaya, Y sol kenardan sağa, Z yerden yukarı.</summary>
public readonly record struct AgirlikMerkeziNoktasi(int XMm, int YMm, int ZMm);

/// <summary>
/// Yüklerin ortak ağırlık merkezi. Her yükün ağırlığı kendi kutusunun ortasında sayılır (sandığın iç dağılımı
/// bilinmez); sonuç ağırlıkla tartılı ortalamadır ve tam milimetreye yuvarlanır. Aracın sakladığı boyuna ağırlık
/// merkezi ile raporda yazılan enine ve düşey değerler bu tek hesaptan gelir.
/// </summary>
public static class AgirlikMerkeziHesabi
{
    /// <summary>Toplam ağırlık sıfırsa (boş araç ya da ağırlığı bilinmeyen yükler) ağırlık merkezi yoktur.</summary>
    public static AgirlikMerkeziNoktasi? Hesapla(IEnumerable<AgirlikliKutu> kutular)
    {
        decimal toplam = 0, x = 0, y = 0, z = 0;
        foreach (var k in kutular)
        {
            toplam += k.Kg;
            x += k.Kg * (k.XMm + k.BoyMm / 2m);
            y += k.Kg * (k.YMm + k.EnMm / 2m);
            z += k.Kg * (k.ZMm + k.YukseklikMm / 2m);
        }
        return toplam > 0
            ? new AgirlikMerkeziNoktasi((int)Math.Round(x / toplam), (int)Math.Round(y / toplam), (int)Math.Round(z / toplam))
            : null;
    }
}
