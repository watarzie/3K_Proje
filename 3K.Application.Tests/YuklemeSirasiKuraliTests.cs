using _3K.Core.Models;
using _3K.YuklemeMotoru.Yerlesim;
using Xunit;
using static _3K.Application.Tests.YerlesimMotoruTests;

namespace _3K.Application.Tests;

/// <summary>
/// Yükleme sırası kuralı: yük numarası ("No") ve yükleme adımları. Rapor bu kuralı kullanır; ön yüzdeki aynası
/// (yukleme-adimlari.ts, 3B adım adım oynatma) aynı örneklerle sınanır.
/// </summary>
public sealed class YuklemeSirasiKuraliTests
{
    private static PlanYukKonumu Konum(string kod, int sira, int kat) =>
        new() { YukAnahtari = "s:" + kod, Kod = kod, IstifNo = sira, YuklemeSirasi = sira, Kat = kat };

    /// <summary>Numara: yükleme sırası, aynı sırada alt kat önce; girdi sırası sonucu değiştirmez.</summary>
    [Fact]
    public void Sirali_SiraSonraKat()
    {
        var konumlar = new[] { Konum("C", 2, 1), Konum("B-UST", 1, 2), Konum("D", 3, 1), Konum("B-ALT", 1, 1) };
        Assert.Equal(["B-ALT", "B-UST", "C", "D"], YuklemeSirasiKurali.Sirali(konumlar).Select(k => k.Kod));
    }

    /// <summary>
    /// Adım: istif grubu tek adımdır (alt kattan üste); adım numarası saklanan sıra boşluklu olsa da ardışıktır ve yük
    /// numaraları sıralı listedeki konumla aynıdır.
    /// </summary>
    [Fact]
    public void Adimlar_IstifTekAdim_NumaralarArdisik()
    {
        var konumlar = new[] { Konum("E", 7, 1), Konum("A", 1, 1), Konum("C-UST", 3, 2), Konum("C-ALT", 3, 1), Konum("C-EN-UST", 3, 3) };
        var adimlar = YuklemeSirasiKurali.Adimlar(konumlar);

        Assert.Equal([(1, 1), (2, 3), (3, 7)], adimlar.Select(a => (a.AdimNo, a.YuklemeSirasi)));
        Assert.Equal(["C-ALT", "C-UST", "C-EN-UST"], adimlar[1].Yukler.Select(y => y.Konum.Kod));
        Assert.Equal([1, 2, 3, 4, 5], adimlar.SelectMany(a => a.Yukler).Select(y => y.No));
        Assert.Equal(YuklemeSirasiKurali.Sirali(konumlar).Select(k => k.Kod), adimlar.SelectMany(a => a.Yukler).Select(y => y.Konum.Kod));
        Assert.Empty(YuklemeSirasiKurali.Adimlar([]));
    }

    /// <summary>
    /// Motorun verdiği sıra arkadan yüklemeye uygundur: adımlar önden arkaya ilerler (sonraki adımın tabanı öncekinden
    /// önde değildir) ve bir istifin bütün katları aynı adımdadır.
    /// </summary>
    [Fact]
    public void MotorSirasi_OndenArkaya_IstifAyniAdimda()
    {
        var yukler = Enumerable.Range(1, 14).Select(i => Sandik($"S{i:00}", 1_800 + i % 3 * 300, 1_100, 900, 400)).ToList();
        var sonuc = YerlesimMotoru.Hesapla(new YerlesimGirdisi(yukler, [K40], Ayar, false, false, null, null, "Konteyner"));
        var arac = Assert.Single(sonuc.Araclar);
        var konumlar = arac.Konumlar.Select(k => new PlanYukKonumu
        {
            YukAnahtari = k.YukAnahtari, Kod = k.YukAnahtari, XMm = k.XMm, YMm = k.YMm, ZMm = k.ZMm, IstifNo = k.IstifNo, Kat = k.Kat,
            YuklemeSirasi = k.YuklemeSirasi
        }).ToList();

        var adimlar = YuklemeSirasiKurali.Adimlar(konumlar);
        Assert.Contains(adimlar, a => a.Yukler.Count > 1);
        Assert.Equal(14, adimlar.Sum(a => a.Yukler.Count));
        var tabanlar = adimlar.Select(a => a.Yukler[0].Konum).ToList();
        Assert.All(tabanlar, t => Assert.Equal(1, t.Kat));
        Assert.Equal(tabanlar.Select(t => t.XMm).Order(), tabanlar.Select(t => t.XMm));
        Assert.All(adimlar, a => Assert.Single(a.Yukler.Select(y => y.Konum.IstifNo).Distinct()));
        Assert.All(adimlar, a => Assert.Equal(Enumerable.Range(1, a.Yukler.Count), a.Yukler.Select(y => y.Konum.Kat)));
    }

    /// <summary>
    /// Adım yükleme sırasından, çizim ve istif bilgisi istif numarasından okunur; motor ikisini aynı verir. Otomatik
    /// yerleşimde ve elle yerleşim denetiminde (aynı konumlar geri verilince) bu değişmez korunur.
    /// </summary>
    [Fact]
    public void IstifNoVeYuklemeSirasi_OtomatikVeElleYerlesimdeAyni()
    {
        var yukler = Enumerable.Range(1, 10).Select(i => Sandik($"S{i:00}", 2_000, 1_100, 900, 400)).ToList();
        var girdi = new YerlesimGirdisi(yukler, [K40], Ayar, false, false, null, null, "Konteyner");
        var otomatik = Assert.Single(YerlesimMotoru.Hesapla(girdi).Araclar);
        Assert.Contains(otomatik.Konumlar, k => k.Kat > 1);
        Assert.All(otomatik.Konumlar, k => Assert.Equal(k.IstifNo, k.YuklemeSirasi));

        var elle = Assert.Single(ElleYerlesimDenetimi.Denetle(girdi,
            [new ElleArac(K40.KonfigurasyonId, otomatik.Konumlar.Select(k => new ElleKonum(k.YukAnahtari, k.XMm, k.YMm, k.ZMm, k.Donduruldu)).ToList())]).Araclar);
        Assert.Equal(10, elle.Konumlar.Count);
        Assert.All(elle.Konumlar, k => Assert.Equal(k.IstifNo, k.YuklemeSirasi));
        Assert.Equal(otomatik.Konumlar.Select(k => (k.YukAnahtari, k.YuklemeSirasi, k.Kat)), elle.Konumlar.Select(k => (k.YukAnahtari, k.YuklemeSirasi, k.Kat)));
    }
}
