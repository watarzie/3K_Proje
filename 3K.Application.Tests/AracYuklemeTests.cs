using _3K.Application.Common;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Commands;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Queries;
using _3K.Application.Features.YuklemePlanlamaIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Models;
using Xunit;

namespace _3K.Application.Tests;

/// <summary>
/// Araç yükleme (saha) saf kuralları: sandık barkodundan yük anahtarı, sıradaki adım ve sıra dışı yükleme, gerekçe;
/// isteklerin izni (arac-yukleme R/W) ve doğrulaması. Veritabanı davranışı (tek açık oturum, tek etkin işaret, sandık
/// ve sevke dokunmama) <see cref="YuklemePlanlamaPostgresTests"/> içindedir.
/// </summary>
public sealed class AracYuklemeTests
{
    private static PlanYukKonumu Konum(string anahtar, int sira, int kat = 1) =>
        new() { YukAnahtari = anahtar, Kod = anahtar, IstifNo = sira, YuklemeSirasi = sira, Kat = kat };

    private static readonly List<YuklemeAdimi> Adimlar = YuklemeSirasiKurali.Adimlar(
        [Konum("s:1", 1), Konum("s:2", 2), Konum("s:3", 2, 2), Konum("m:1:1", 3)]);

    private static HashSet<string> Yuklenen(params string[] anahtarlar) => new(anahtarlar, StringComparer.Ordinal);

    /// <summary>Sandık barkodu sandık kimliğinin 12 haneli hâlidir; başka biçim (ürün barkodu, harf, taşma) yük tanımlamaz.</summary>
    [Theory]
    [InlineData("000002060008", 2060008)]
    [InlineData(" 000000000723 ", 723)]
    [InlineData("000000000000", null)]
    [InlineData("00000FCT00967433", null)]
    [InlineData("00000000723", null)]
    [InlineData("0000000007230", null)]
    [InlineData("00000000072A", null)]
    [InlineData("999999999999", null)]
    [InlineData("٠٠٠٠٠٠٠٠٠٧٢٣", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void SandikBarkodu_Cozulur(string? barkod, int? sandikId)
    {
        Assert.Equal(sandikId, BarkodEtiketKurali.SandikBarkoduCoz(barkod));
        Assert.Equal(sandikId == null ? null : $"s:{sandikId}", YuklemeOturumuKurali.BarkoddanYukAnahtari(barkod));
        if (sandikId is { } id)
            Assert.Equal(id, BarkodEtiketKurali.SandikBarkoduCoz(BarkodEtiketKurali.SandikBarkodu(id)));
    }

    /// <summary>"Barkodla okutuldu" kaydı için okutulan barkod o yükü göstermelidir; elle eklenen yükün barkodu yoktur.</summary>
    [Theory]
    [InlineData("000000000042", "s:42", true)]
    [InlineData("000000000042", "s:43", false)]
    [InlineData("000000000042", "m:1:1", false)]
    [InlineData("42", "s:42", false)]
    [InlineData("", "s:42", false)]
    [InlineData(null, "s:42", false)]
    public void BarkodKaynagi_OkutulanBarkodYukuGostermeli(string? barkod, string anahtar, bool beklenen) =>
        Assert.Equal(beklenen, YuklemeOturumuKurali.BarkodYukleEslesir(barkod, anahtar));

    /// <summary>Sıradaki adım yüklenmemiş yükü olan ilk adımdır; istif grubunun bütün katları yüklenmeden adım bitmez.</summary>
    [Fact]
    public void SiradakiAdim_IlkEksikAdim()
    {
        Assert.Equal(1, YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen()));
        Assert.Equal(2, YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen("s:1")));
        Assert.Equal(2, YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen("s:1", "s:3")));
        Assert.Equal(3, YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen("s:1", "s:2", "s:3")));
        Assert.Null(YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen("s:1", "s:2", "s:3", "m:1:1")));
        // Sıra dışı yüklenmiş sonraki adım, önceki eksik adımı sıradaki olmaktan çıkarmaz.
        Assert.Equal(1, YuklemeOturumuKurali.SiradakiAdimNo(Adimlar, Yuklenen("m:1:1")));
    }

    /// <summary>Sıra dışı: adımı sıradaki adımdan sonra olan yük. Aynı istif grubundaki katlar kendi arasında serbesttir.</summary>
    [Fact]
    public void SiraDisi_SiradakiAdimdanSonrakiYuk()
    {
        Assert.False(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen(), "s:1"));
        Assert.True(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen(), "s:2"));
        Assert.True(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen(), "m:1:1"));
        Assert.False(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen("s:1"), "s:3"));
        Assert.False(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen("s:1", "s:3"), "s:2"));
        Assert.True(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen("s:1", "s:3"), "m:1:1"));
        // Yerleşimde olmayan yük sıra dışı sayılmaz (ayrıca reddedilir).
        Assert.False(YuklemeOturumuKurali.SiraDisiMi(Adimlar, Yuklenen(), "s:99"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("    ", false)]
    [InlineData("abcd", false)]
    [InlineData("  abcd  ", false)]
    [InlineData("yanlış araca okutuldu", true)]
    public void Gerekce_BesIleBesYuzKarakter(string? gerekce, bool gecerli)
    {
        Assert.Equal(gecerli, YuklemeOturumuKurali.GerekceGecerli(gerekce));
        Assert.True(YuklemeOturumuKurali.GerekceGecerli(new string('a', 500)));
        Assert.False(YuklemeOturumuKurali.GerekceGecerli(new string('a', 501)));
    }

    /// <summary>İzleme uçları R, oturum ve işaretleme uçları W ister; hepsi arac-yukleme koduyla.</summary>
    [Fact]
    public void Istekler_AracYuklemeIzniyleKorunur()
    {
        (object Istek, YetkiTipi Tip)[] beklenen =
        [
            (new AracYuklemePlanlariQuery(), YetkiTipi.R),
            (new AracYuklemeDurumuQuery(), YetkiTipi.R),
            (new AracYuklemeAracQuery(), YetkiTipi.R),
            (new AracYuklemeCozumleQuery(), YetkiTipi.R),
            (new YuklemeOturumuBaslatCommand(), YetkiTipi.W),
            (new YukuYuklendiIsaretleCommand(), YetkiTipi.W),
            (new YukIsaretiGeriAlCommand(), YetkiTipi.W),
            (new YuklemeOturumuKapatCommand(), YetkiTipi.W)
        ];
        Assert.All(beklenen, b =>
        {
            Assert.IsAssignableFrom<ISecuredRequest>(b.Istek);
            var izin = Assert.IsAssignableFrom<IRequiresMenuPermission>(b.Istek);
            Assert.Equal((YetkiKodlari.YuklemePlanlama.AracYukleme, b.Tip), (izin.RequiredMenuKod, izin.RequiredYetkiTipi));
        });
    }

    [Fact]
    public void Dogrulama_EksikVeGecersizGirdiReddedilir()
    {
        var cozumle = new AracYuklemeCozumleQueryValidator();
        Assert.True(cozumle.Validate(new AracYuklemeCozumleQuery { OturumId = 1, AracNo = 1, Barkod = "000000000723" }).IsValid);
        Assert.False(cozumle.Validate(new AracYuklemeCozumleQuery { OturumId = 1, AracNo = 1, Barkod = " " }).IsValid);
        Assert.False(cozumle.Validate(new AracYuklemeCozumleQuery { OturumId = 1, AracNo = 1, Barkod = new string('1', 65) }).IsValid);
        Assert.False(cozumle.Validate(new AracYuklemeCozumleQuery { OturumId = 1, AracNo = 0, Barkod = "000000000723" }).IsValid);

        Assert.False(new YuklemeOturumuBaslatCommandValidator().Validate(new YuklemeOturumuBaslatCommand { PlanId = 1 }).IsValid);

        var isaretle = new YukuYuklendiIsaretleCommandValidator();
        YukuYuklendiIsaretleCommand Isaret(string anahtar, YuklemeIsaretKaynagi kaynak = YuklemeIsaretKaynagi.Barkod, string? barkod = "000000000001") =>
            new() { OturumId = 1, AracNo = 1, YukAnahtari = anahtar, Kaynak = kaynak, Barkod = barkod };
        Assert.True(isaretle.Validate(Isaret("s:1")).IsValid);
        Assert.False(isaretle.Validate(Isaret("")).IsValid);
        Assert.False(isaretle.Validate(Isaret(new string('s', YuklemeOturumuKurali.YukAnahtariEnFazla + 1))).IsValid);
        Assert.False(isaretle.Validate(Isaret("s:1", (YuklemeIsaretKaynagi)9)).IsValid);
        // Barkod kaynağı okutulan metni taşımalıdır; elle işarette barkod beklenmez.
        Assert.False(isaretle.Validate(Isaret("s:1", barkod: null)).IsValid);
        Assert.False(isaretle.Validate(Isaret("s:1", barkod: new string('1', YuklemeOturumuKurali.BarkodEnFazla + 1))).IsValid);
        Assert.True(isaretle.Validate(Isaret("m:1:1", YuklemeIsaretKaynagi.Elle, barkod: null)).IsValid);

        var planlar = new AracYuklemePlanlariQueryValidator();
        Assert.True(planlar.Validate(new AracYuklemePlanlariQuery { Arama = new string('a', 100) }).IsValid);
        Assert.False(planlar.Validate(new AracYuklemePlanlariQuery { Arama = new string('a', 101) }).IsValid);

        var geriAl = new YukIsaretiGeriAlCommandValidator();
        Assert.True(geriAl.Validate(new YukIsaretiGeriAlCommand { OturumId = 1, IsaretId = 1, Gerekce = "yanlış sandık" }).IsValid);
        Assert.False(geriAl.Validate(new YukIsaretiGeriAlCommand { OturumId = 1, IsaretId = 1, Gerekce = "x" }).IsValid);

        // İptal gerekçe ister; tamamlamada gerekçe boş olabilir (eksik yükte zorunluluk serviste), yazıldıysa geçerli olmalı.
        var kapat = new YuklemeOturumuKapatCommandValidator();
        Assert.False(kapat.Validate(new YuklemeOturumuKapatCommand { OturumId = 1, Iptal = true }).IsValid);
        Assert.True(kapat.Validate(new YuklemeOturumuKapatCommand { OturumId = 1, Iptal = true, Gerekce = "araç gelmedi" }).IsValid);
        Assert.True(kapat.Validate(new YuklemeOturumuKapatCommand { OturumId = 1 }).IsValid);
        Assert.False(kapat.Validate(new YuklemeOturumuKapatCommand { OturumId = 1, Gerekce = "x" }).IsValid);
    }
}
