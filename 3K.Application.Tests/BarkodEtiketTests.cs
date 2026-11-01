using System.Text;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Etiketler;

namespace _3K.Application.Tests;

/// <summary>
/// Barkod etiketi: Code 128 kodlaması (Godex BQ gibi alt küme seçimi), alan kuralları (sandık barkodu 12, ürün barkodu 16
/// hane; Türkçe harf ASCII'ye; açıklama TANIM/DESCRIPTION'a ayrılır), EZPL çıktısı kullanıcının Godex şablonuyla birebir,
/// PDF adet kadar sayfa; tek okuma izni barkod-etiket; liste sunucuda sayfalı, seçim projeye ait olmalı.
/// </summary>
public sealed class BarkodEtiketTests
{
    [Fact]
    public void Code128_FotograftakiSandikBarkodu_TumuCAltKumesi_404Nokta()
    {
        // Etiket fotoğrafındaki 000002060008: Start C + 6 çift + kontrol (80) + Stop.
        Assert.Equal([105, 0, 0, 2, 6, 0, 8, 80, 106], Code128.Kodlar("000002060008"));
        var (_, modul) = Code128.Cubuklar("000002060008");
        // Şablonda dar çubuk 4 nokta: 101 modül × 4 = 404 nokta (≈50,5 mm, fotoğraftaki barkod genişliği).
        Assert.Equal(404, modul * 4);
    }

    [Fact]
    public void Code128_UrunBarkodu_BasRakamlarCOrtasiBSonuC()
    {
        Assert.Equal([105, 0, 0, 100, 16, 38, 35, 52, 99, 0, 96, 74, 33, 75, 106], Code128.Kodlar("00000FCT00967433"));
        Assert.Equal([104, 38, 35, 52, 99, 0, 96, 74, 33, 62, 106], Code128.Kodlar("FCT00967433"));
        // Üç rakam C'ye geçirmez; ortadaki 4 rakam da geçirmez (en az 6 gerekir).
        Assert.Equal([104, 17, 18, 19], Code128.Kodlar("123").Take(4));
        Assert.DoesNotContain(99, Code128.Kodlar("A1234B"));
        Assert.Contains(99, Code128.Kodlar("A123456B"));
    }

    [Fact]
    public void Code128_DesenTablosu_HerSembol11ModulBitis13_CubukSayisiTutarli()
    {
        // Her yazdırılabilir karakter (B) ve her rakam çifti (C) tek sembol: toplam = 3 × 11 + 13 ve 3 × 3 + 4 çubuk.
        foreach (var c in Enumerable.Range(' ', '~' - ' ' + 1).Select(i => ((char)i).ToString()))
        {
            var (cubuklar, modul) = Code128.Cubuklar(c);
            Assert.Equal((46, 13), (modul, cubuklar.Count));
        }
        foreach (var cift in Enumerable.Range(0, 100))
        {
            var (cubuklar, modul) = Code128.Cubuklar($"{cift:00}00");
            Assert.Equal((57, 16), (modul, cubuklar.Count));
        }
        Assert.False(Code128.Kodlanabilir("ÇİFT"));
        Assert.False(Code128.Kodlanabilir(""));
        Assert.Throws<ArgumentException>(() => Code128.Kodlar("Ğ"));
    }

    [Theory]
    [InlineData("FCT00967433", "00000FCT00967433")]
    [InlineData(" fct00725463-01 ", "00FCT00725463-01")]
    [InlineData("SMC008176", "0000000SMC008176")]
    [InlineData("1234567890123456", "1234567890123456")]
    [InlineData("12345678901234567", null)]
    [InlineData("KADEME GÖSTERGESİ", null)]
    [InlineData("FC^1", null)]
    [InlineData("fcı12", null)]
    [InlineData("  ", null)]
    [InlineData(null, null)]
    public void Kural_UrunBarkodu16HaneyeSifirlaDoldurulur(string? barkod, string? beklenen) =>
        Assert.Equal(beklenen, BarkodEtiketKurali.UrunBarkodu(barkod));

    [Fact]
    public void Kural_SandikBarkoduMetinAciklamaTarihVeAdetler()
    {
        Assert.Equal("000002060008", BarkodEtiketKurali.SandikBarkodu(2060008));
        Assert.Equal("000000000723", BarkodEtiketKurali.SandikBarkodu(723));
        Assert.Throws<ArgumentOutOfRangeException>(() => BarkodEtiketKurali.SandikBarkodu(0));

        // Yazıcı Türkçe harf basamaz: fotoğraftaki "CAP 200 CIFT"; büyük/küçük harf korunur.
        Assert.Equal("TEKERLEK DUZ.-RAY CAP 200 CIFT TEKERLEK", BarkodEtiketKurali.Metin("TEKERLEK DUZ.-RAY ÇAP 200 ÇİFT TEKERLEK"));
        Assert.Equal("ALGERIA", BarkodEtiketKurali.Metin(" ALGERİA "));
        Assert.Equal("Isik gostergesi sogutucu", BarkodEtiketKurali.Metin("Işık göstergesi\n soğutucu"));
        Assert.Equal("Cafe a b c ?", BarkodEtiketKurali.Metin("Café a^b~c 中"));
        // BMP dışı karakter (emoji) tek "?" olur; vekil yarımlar hata vermez.
        Assert.Equal("Kutu ? 1", BarkodEtiketKurali.Metin("Kutu 😀 1"));
        Assert.Equal("", BarkodEtiketKurali.Metin(null));
        Assert.Equal("-", BarkodEtiketKurali.Remarks("  "));
        Assert.Equal("38.39", BarkodEtiketKurali.Remarks("38.39"));

        Assert.Equal(("TEKERLEK DUZ.-RAY ÇAP 200 ÇİFT TEKERLEK", "Wheel Assembly"),
            BarkodEtiketKurali.AciklamaAyir("TEKERLEK DUZ.-RAY ÇAP 200 ÇİFT TEKERLEK\n\\Wheel Assembly"));
        Assert.Equal(("Buchholz Rölesi", "Buchholz Relay"), BarkodEtiketKurali.AciklamaAyir("Buchholz Rölesi \r\n\\Buchholz Relay"));
        Assert.Equal(("Silikagel", ""), BarkodEtiketKurali.AciklamaAyir("Silikagel"));
        Assert.Equal(("", ""), BarkodEtiketKurali.AciklamaAyir(null));

        Assert.Equal("PA600-22", BarkodEtiketKurali.SeriNo(" PA600-22 ", "PA600-22-23-24-SAHA"));
        Assert.Equal("PA532-02-SAHA", BarkodEtiketKurali.SeriNo(null, "PA532-02-SAHA"));
        Assert.Equal("09/26", BarkodEtiketKurali.Tarih(2026, 9));
        Assert.False(BarkodEtiketKurali.TarihGecerli(2026, 13));
        Assert.Throws<ArgumentOutOfRangeException>(() => BarkodEtiketKurali.Tarih(1999, 1));

        Assert.True(BarkodEtiketKurali.AdetlerGecerli([1, BarkodEtiketKurali.EnFazlaAdet]));
        Assert.False(BarkodEtiketKurali.AdetlerGecerli([]));
        Assert.False(BarkodEtiketKurali.AdetlerGecerli([0]));
        Assert.False(BarkodEtiketKurali.AdetlerGecerli([BarkodEtiketKurali.EnFazlaAdet + 1]));
        // Seçim sayısının ayrı sınırı yok: 253 satır birer etiketle geçer; yalnız toplam etiket sınırı aşılamaz.
        Assert.True(BarkodEtiketKurali.AdetlerGecerli(Enumerable.Repeat(1, 253).ToList()));
        Assert.True(BarkodEtiketKurali.AdetlerGecerli(Enumerable.Repeat(1, BarkodEtiketKurali.EnFazlaToplamEtiket).ToList()));
        Assert.False(BarkodEtiketKurali.AdetlerGecerli(Enumerable.Repeat(1, BarkodEtiketKurali.EnFazlaToplamEtiket + 1).ToList()));
        Assert.False(BarkodEtiketKurali.AdetlerGecerli(Enumerable.Repeat(BarkodEtiketKurali.EnFazlaAdet, 11).ToList()));
    }

    /// <summary>
    /// Fotoğraftaki sandık etiketi (PA600-22, sandık 8, SONELGAZ, ALGERIA, 09/26): EZPL kullanıcının Label_Sandik.txt
    /// şablonuyla satır satır aynıdır; yalnız #Alan# yerleri dolar ve adet ^P ile yazılır.
    /// </summary>
    [Fact]
    public void Ezpl_SandikEtiketi_SablonlaBirebir()
    {
        var ezpl = Encoding.ASCII.GetString(GodexEtiketSablonu.Ezpl(
            [GodexEtiketSablonu.Sandik(new SandikEtiketi("000002060008", "PA600-22", "8", "SONELGAZ", "ALGERIA", "09/26", 2))]));

        Assert.Equal("""
            ^Q75,3
            ^W100
            ^H5
            ^P2
            ^S2
            ^AD
            ^C1
            ^R0
            ~Q+0
            ^O0
            ^D0
            ^E12
            ~R200
            ^XSET,ROTATION,0
            ^L
            Dy2-me-dd
            Th:m:s
            BQ,134,140,4,10,174,0,3,000002060008
            AD,20,496,1,1,0,0,FB NO:
            AD,15,542,1,1,0,0,CASE NO:
            AD,171,494,1,1,0,0,PA600-22
            AD,169,542,1,1,0,0,8
            AD,228,401,1,1,0,0,Case Label / Koli Etiketi
            AB,423,493,1,1,0,0,CLIENT NAME: SONELGAZ
            Lo,14,385,776,396
            AD,242,48,1,1,0,0,Grid Solutions ATT / Gebze
            Lo,15,452,777,463
            AC,651,99,1,1,0,0,09/26
            AB,424,526,1,1,0,0,DISPATCH DESTINATION:
            AB,424,552,1,1,0,0,ALGERIA
            E

            """.ReplaceLineEndings("\r\n"), ezpl);
    }

    /// <summary>
    /// Fotoğraftaki ürün etiketi (FCT00967433, PA682-42, MOE POC-IRAQ): Label_Urun.txt şablonunun öğeleri; başlık
    /// "Grid Solutions ATT / Gebze". Her etiket kendi kurulumuyla, art arda.
    /// </summary>
    [Fact]
    public void Ezpl_UrunEtiketi_SablonOgeleriVeYeniBaslik()
    {
        var urun = new UrunEtiketi("00000FCT00967433", "PA682-42", "MOE POC-IRAQ", "IRAQ",
            "TEKERLEK DUZ.-RAY CAP 200 CIFT TEKERLEK", "Wheel Assembly", "-", 3);
        var satirlar = Encoding.ASCII.GetString(GodexEtiketSablonu.Ezpl(
            [GodexEtiketSablonu.Urun(urun), GodexEtiketSablonu.Urun(urun with { Adet = 1 })])).Split("\r\n");

        Assert.Equal(["^Q40,3", "^W60", "^H5", "^P3"], satirlar[..4]);
        Assert.Contains("^R16", satirlar);
        Assert.Equal(2, satirlar.Count(s => s == "E"));
        Assert.Contains("^P1", satirlar);
        foreach (var beklenen in new[]
        {
            "AC,51,16,1,1,0,0,Grid Solutions ATT / Gebze",
            "BQ,20,68,2,5,62,0,3,00000FCT00967433",
            "AB,13,163,1,1,0,0,FB NO:",
            "AA,12,221,1,1,0,0,TANIM:",
            "AA,12,241,1,1,0,0,DESCRIPTION:",
            "AB,12,191,1,1,0,0,CLIENT:",
            "AB,106,189,1,1,0,0,MOE POC-IRAQ",
            "AA,12,269,1,1,0,0,REMARKS:",
            "AB,95,164,1,1,0,0,PA682-42",
            "AA,127,219,1,1,0,0,TEKERLEK DUZ.-RAY CAP 200 CIFT TEKERLEK",
            "AA,102,269,1,1,0,0,-",
            "Lo,2,216,438,217",
            "Lo,0,262,436,263",
            "AA,264,170,1,1,0,0,DESTINATION:",
            "AA,264,189,1,1,0,0,IRAQ",
            "AA,128,240,1,1,0,0,Wheel Assembly",
        })
            Assert.Contains(beklenen, satirlar);
        Assert.DoesNotContain(satirlar, s => s.Contains("ALSTOM"));
    }

    [Fact]
    public void YaziOlcusu_KelimeSinirindanKaydirir_SatirYetmezseKalaniAtar()
    {
        // Helvetica: "SONELGAZ" B fontunda (8 pt × 0,93) ≈ 114 nokta.
        Assert.InRange(GodexYaziOlcusu.Genislik('B', "SONELGAZ"), 110, 118);
        Assert.Equal(["GCA ENERGY -", "ESCRAVOS"], GodexYaziOlcusu.Sar('B', "GCA ENERGY - ESCRAVOS", 160, 2));
        Assert.Equal(["MINOOKA HV SUBSTATION SITE 1 -", "GSI - EQUINIX"],
            GodexYaziOlcusu.Sar('B', "MINOOKA HV SUBSTATION SITE 1 - GSI - EQUINIX", GodexEtiketSablonu.SandikSagSutunGenisligi, 2));
        // Satırlar yetmezse kalan atılır; sığmayan tek kelime harf harf bölünür.
        Assert.Equal(["AAAA BBBB"], GodexYaziOlcusu.Sar('B', "AAAA BBBB CCCC DDDD", 125, 1));
        Assert.All(GodexYaziOlcusu.Sar('B', new string('W', 40), 100, 5), s => Assert.True(GodexYaziOlcusu.Genislik('B', s) <= 100));
        Assert.Equal([""], GodexYaziOlcusu.Sar('B', "", 100, 2));
    }

    /// <summary>
    /// Uzun müşteri adı (PA727-03 "GCA ENERGY - ESCRAVOS"; veride en uzunu 44 karakter) etiketin kenarından taşmaz:
    /// sandıkta "CLIENT NAME:" kendi satırına iner ve ad iki satıra kayar, DISPATCH DESTINATION aşağı kayar; üründe ad
    /// küçük fontla kayar. Kısa adda şablon değişmez.
    /// </summary>
    [Theory]
    [InlineData("GCA ENERGY - ESCRAVOS")]
    [InlineData("MINOOKA HV SUBSTATION SITE 1 - GSI - EQUINIX")]
    [InlineData("PRZEDSIEBIORSTWO BADAWCZO WDROZENIO")]
    public void Sablon_UzunMusteriEtiketKenarindanTasmaz(string musteri)
    {
        var sandik = GodexEtiketSablonu.Sandik(new SandikEtiketi("000000006507", "PA727-03", "1", musteri, "WOJTOWO / POLAND", "10/26", 1));
        // Sağ sütun: kalın çizginin (y 452–463) altındaki müşteri ve varış yeri bloğu.
        var sagSutun = sandik.Ogeler.OfType<MetinOgesi>().Where(m => m.X >= 423 && m.Y > 463).ToList();
        Assert.All(sagSutun, m => Assert.True(m.X + GodexYaziOlcusu.Genislik(m.Font, m.Metin) <= 790, m.Metin));
        var dispatchY = sagSutun.Single(m => m.Metin == "DISPATCH DESTINATION:").Y;
        Assert.Equal(musteri, string.Join(" ", sagSutun.Where(m => m.Y > 470 && m.Y < dispatchY).Select(m => m.Metin)));
        Assert.Equal(new[] { "CLIENT NAME:", "DISPATCH DESTINATION:", "WOJTOWO / POLAND" },
            sagSutun.Where(m => m.Metin is "CLIENT NAME:" or "DISPATCH DESTINATION:" or "WOJTOWO / POLAND").Select(m => m.Metin));
        // Alt satır etiketin içinde (75 mm = 600 nokta), satırlar üst üste binmez.
        var yler = sagSutun.Select(m => m.Y).ToList();
        Assert.True(yler.Max() + 22 <= 600);
        Assert.All(yler.Zip(yler.Skip(1)), c => Assert.True(c.Second - c.First >= 22));

        var urun = GodexEtiketSablonu.Urun(new UrunEtiketi("00000FCT00967433", "PA727-03", musteri, "NIGERIA", "TANIM", "Description", "-", 1));
        var musteriSatirlari = urun.Ogeler.OfType<MetinOgesi>().Where(m => m.X == 106).ToList();
        Assert.All(musteriSatirlari, m => Assert.Equal('A', m.Font));
        Assert.All(musteriSatirlari, m => Assert.True(m.X + GodexYaziOlcusu.Genislik('A', m.Metin) <= 264, m.Metin));
        Assert.All(musteriSatirlari, m => Assert.True(m.Y + 16 <= 216, "çizginin üstünde kalır"));
        // Sütuna sığmayan tek kelime (PRZEDSIEBIORSTWO) harf harf bölünür; harfler sırasıyla korunur.
        Assert.StartsWith(string.Concat(musteriSatirlari.Select(m => m.Metin)).Replace(" ", ""), musteri.Replace(" ", ""));
    }

    [Fact]
    public void Pdf_HerEtiketAdediKadarSayfa_EtiketBoyunda()
    {
        var pdf = BarkodEtiketPdf.Olustur(
        [
            GodexEtiketSablonu.Sandik(new SandikEtiketi("000002060008", "PA600-22", "8", "SONELGAZ", "ALGERIA", "09/26", 2)),
            GodexEtiketSablonu.Urun(new UrunEtiketi("00000FCT00967433", "PA682-42", "MOE POC-IRAQ", "IRAQ", "TEKERLEK", "Wheel Assembly", "-", 1)),
        ]);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        // QuestPDF sayfa kökünde toplam sayfayı ve her sayfanın kutusunu (pt, yarım noktaya yuvarlı) yazar: 100×75 mm
        // (283,46×212,6 pt) ve 60×40 mm (170,08×113,39 pt).
        var metin = Encoding.Latin1.GetString(pdf);
        Assert.Contains("/Count 3", metin);
        Assert.Contains("/MediaBox [0 0 283.5 212.5]", metin);
        Assert.Contains("/MediaBox [0 0 170 113.5]", metin);
    }

    [Fact]
    public void Dogrulayici_SecimAdetTarihBicimVeTekrarlariDenetler()
    {
        var sandik = new SandikEtiketiOlusturQueryValidator();
        SandikEtiketiOlusturQuery S(params BarkodEtiketSandikSecimi[] secimler) =>
            new() { ProjeId = 1, Yil = 2026, Ay = 9, Bicim = BarkodEtiketBicimi.Ezpl, Secimler = [.. secimler] };
        Assert.True(sandik.Validate(S(new BarkodEtiketSandikSecimi(1, 2), new BarkodEtiketSandikSecimi(2, 1))).IsValid);
        Assert.False(sandik.Validate(S()).IsValid);
        Assert.False(sandik.Validate(S(new BarkodEtiketSandikSecimi(1, 1), new BarkodEtiketSandikSecimi(1, 3))).IsValid);
        Assert.False(sandik.Validate(S(new BarkodEtiketSandikSecimi(1, 0))).IsValid);
        Assert.False(sandik.Validate(S(new BarkodEtiketSandikSecimi(0, 1))).IsValid);
        // Gövdede null eleman 500 değil, doğrulama hatasıdır.
        Assert.False(sandik.Validate(new SandikEtiketiOlusturQuery { ProjeId = 1, Yil = 2026, Ay = 9, Secimler = [null!] }).IsValid);
        Assert.False(sandik.Validate(new SandikEtiketiOlusturQuery { ProjeId = 1, Yil = 2026, Ay = 9, Secimler = null! }).IsValid);
        Assert.False(sandik.Validate(new SandikEtiketiOlusturQuery { ProjeId = 1, Yil = 2026, Ay = 0, Secimler = [new(1, 1)] }).IsValid);
        Assert.False(sandik.Validate(new SandikEtiketiOlusturQuery { ProjeId = 1, Yil = 2026, Ay = 9, Bicim = (BarkodEtiketBicimi)9, Secimler = [new(1, 1)] }).IsValid);

        var urun = new UrunEtiketiOlusturQueryValidator();
        UrunEtiketiOlusturQuery U(params BarkodEtiketUrunSecimi[] secimler) => new() { ProjeId = 1, Secimler = [.. secimler] };
        // Aynı kimlik farklı kaynaktan iki ayrı üründür.
        Assert.True(urun.Validate(U(new BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi.CekiSatiri, 5, 1, null), new BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi.SandikIcerik, 5, 1, "Not"))).IsValid);
        Assert.False(urun.Validate(U(new BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi.CekiSatiri, 5, 1, null), new BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi.CekiSatiri, 5, 2, null))).IsValid);
        Assert.False(urun.Validate(U(new BarkodEtiketUrunSecimi((BarkodEtiketUrunKaynagi)7, 5, 1, null))).IsValid);
        Assert.False(urun.Validate(new UrunEtiketiOlusturQuery { ProjeId = 1, Secimler = [null!] }).IsValid);
        Assert.False(urun.Validate(U(new BarkodEtiketUrunSecimi(BarkodEtiketUrunKaynagi.CekiSatiri, 5, 1, new string('x', BarkodEtiketKurali.RemarksEnFazlaUzunluk + 1)))).IsValid);

        var liste = new BarkodEtiketUrunleriQueryValidator();
        Assert.True(liste.Validate(new BarkodEtiketUrunleriQuery { ProjeId = 1, SandikId = 3, Arama = "FC", Sayfa = 2, Boyut = 100 }).IsValid);
        Assert.False(liste.Validate(new BarkodEtiketUrunleriQuery { ProjeId = 1, Boyut = 101 }).IsValid);
        Assert.False(liste.Validate(new BarkodEtiketUrunleriQuery { ProjeId = 0 }).IsValid);
        Assert.False(new BarkodEtiketProjeleriQueryValidator().Validate(new BarkodEtiketProjeleriQuery { Arama = new string('A', 101) }).IsValid);
    }

    [Fact]
    public async Task Yetki_HerIstekYalnizBarkodEtiketOkumaIzniIster()
    {
        object[] istekler =
        [
            new BarkodEtiketProjeleriQuery(), new BarkodEtiketProjeQuery(), new BarkodEtiketSandiklariQuery(),
            new BarkodEtiketUrunleriQuery(), new SandikEtiketiOlusturQuery(), new UrunEtiketiOlusturQuery()
        ];
        foreach (var istek in istekler)
        {
            var izin = Assert.IsAssignableFrom<IRequiresMenuPermission>(istek);
            Assert.Equal((YetkiKodlari.Sandik.BarkodEtiket, YetkiTipi.R), (izin.RequiredMenuKod, izin.RequiredYetkiTipi));
            Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
        }

        async Task<bool> GecerMi(OrtakRoller roller)
        {
            var gecti = false;
            var sonuc = await new AuthorizationBehavior<UrunEtiketiOlusturQuery, Result<DosyaIcerigi>>(new OrtakUser(), roller)
                .Handle(new UrunEtiketiOlusturQuery(), () => { gecti = true; return Task.FromResult(Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf([], "x.pdf"))); }, CancellationToken.None);
            if (!gecti) Assert.Equal(403, sonuc.StatusCode);
            return gecti;
        }
        Assert.True(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.Sandik.BarkodEtiket)));
        // Sandık yönetimi ve barkod arama izinleri etiket basmaya yetmez.
        Assert.False(await GecerMi(OrtakRoller.Haric(YetkiKodlari.Sandik.BarkodEtiket)));
    }

    /// <summary>
    /// Gerçek SQL: sandıklar doğal sırada ve sayfalı; ürün listesi çeki satırları + elle eklenen malzemeler (UNION ALL)
    /// sandıkla süzülür; başka projenin sandığı/ürünü seçilemez (404); barkodu basılamayan ürün 400; dosyalar fotoğraftaki
    /// alanlarla üretilir ve hiçbir kayıt değişmez.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_ListelerSayfaliSecimProjeyeAitDosyaUretilir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            int projeId, digerProjeId, s8, s10, digerSandik, tekerlek, gecersiz, elle, tahsis;
            await using (var db = new AppDbContext(options))
            {
                var proje = new Proje { ProjeNo = "PA682-42", FBNo = "PA682-42", Musteri = "MOE POC-IRAQ", Lokasyon = "IRAQ",
                    ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
                var saha = new Proje { ProjeNo = "PA600-22-23-24-SAHA", FBNo = "PA600-22", Musteri = "SONELGAZ", Lokasyon = "ALGERİA",
                    ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
                Sandik S(Proje p, string no) => new() { Proje = p, SandikNo = no, Ad = "Sandık " + no, DurumId = (int)SandikDurum.Hazirlaniyor, TipId = 3, DepoLokasyonId = 2 };
                var sandiklar = new[] { S(proje, "10"), S(proje, "8"), S(proje, "2"), S(saha, "1") };
                var ceki = new Ceki { Proje = proje, OrijinalDosyaYolu = "a.xlsx" };
                var tekerlekSatiri = new CekiSatiri { Ceki = ceki, SiraNo = 2, BarkodNo = "FCT00967433",
                    Aciklama = "TEKERLEK DUZ.-RAY ÇAP 200 ÇİFT TEKERLEK\n\\Wheel Assembly", Remarks = "38.39", IstenenAdet = 4, BirimId = 1 };
                var gecersizSatir = new CekiSatiri { Ceki = ceki, SiraNo = 1, BarkodNo = "KADEME GÖSTERGESİ", Aciklama = "Kademe", IstenenAdet = 1, BirimId = 1 };
                var tahsissiz = new CekiSatiri { Ceki = ceki, SiraNo = 3, BarkodNo = "SMC008176", Aciklama = "Merdiven", IstenenAdet = 1,
                    CekideGecenSandikNo = "14", BirimId = 1 };
                db.CekiSatirlari.AddRange(tekerlekSatiri, gecersizSatir, tahsissiz);
                db.Sandiklar.AddRange(sandiklar);
                db.SandikIcerikleri.AddRange(
                    new SandikIcerik { Sandik = sandiklar[1], CekiSatiri = tekerlekSatiri, TahsisMiktari = 2 },
                    new SandikIcerik { Sandik = sandiklar[0], CekiSatiri = tekerlekSatiri, TahsisMiktari = 2 },
                    new SandikIcerik { Sandik = sandiklar[1], BarkodNo = "fc4324042", Isim = "Silikagel", Miktar = 3, BirimId = 1 },
                    new SandikIcerik { Sandik = sandiklar[3], BarkodNo = "FC1", Isim = "Saha malzemesi", Miktar = 1, BirimId = 1 });
                await db.SaveChangesAsync();
                (projeId, digerProjeId) = (proje.Id, saha.Id);
                (s10, s8, digerSandik) = (sandiklar[0].Id, sandiklar[1].Id, sandiklar[3].Id);
                (tekerlek, gecersiz) = (tekerlekSatiri.Id, gecersizSatir.Id);
                elle = db.SandikIcerikleri.Single(si => si.Isim == "Silikagel").Id;
                tahsis = db.SandikIcerikleri.First(si => si.CekiSatiriId != null).Id;
            }

            await using var okuma = new AppDbContext(options);
            var servis = new BarkodEtiketService(okuma);

            var sayfa1 = await servis.SandiklariListeleAsync(projeId, null, 1, 2, default);
            Assert.Equal((3, 2), (sayfa1.TotalCount, sayfa1.TotalPages));
            Assert.Equal(["2", "8"], sayfa1.Items.Select(s => s.SandikNo));
            var sayfa2 = await servis.SandiklariListeleAsync(projeId, null, 9, 2, default);
            Assert.Equal((2, "10", BarkodEtiketKurali.SandikBarkodu(s10), 1), (sayfa2.PageNumber, sayfa2.Items[0].SandikNo, sayfa2.Items[0].Barkod, sayfa2.Items[0].UrunSayisi));
            Assert.Equal(["8"], (await servis.SandiklariListeleAsync(projeId, " 8", null, null, default)).Items.Select(s => s.SandikNo));

            var urunler = await servis.UrunleriListeleAsync(projeId, null, null, null, null, default);
            Assert.Equal([gecersiz, tekerlek, 0, elle], urunler.Items.Select(u => u.BarkodNo == "SMC008176" ? 0 : u.Id));
            var tekerlekSatiri2 = urunler.Items[1];
            Assert.Equal(("00000FCT00967433", "TEKERLEK DUZ.-RAY ÇAP 200 ÇİFT TEKERLEK", "Wheel Assembly", "38.39", "8, 10", 4m),
                (tekerlekSatiri2.EtiketBarkodu, tekerlekSatiri2.Tanim, tekerlekSatiri2.Aciklama, tekerlekSatiri2.Remarks, tekerlekSatiri2.SandikNolari, tekerlekSatiri2.Miktar));
            Assert.Null(urunler.Items[0].EtiketBarkodu);
            Assert.Equal("14", urunler.Items[2].SandikNolari);
            Assert.Equal((BarkodEtiketUrunKaynagi.SandikIcerik, "0000000FC4324042", "Silikagel", "8"),
                (urunler.Items[3].Kaynak, urunler.Items[3].EtiketBarkodu, urunler.Items[3].Tanim, urunler.Items[3].SandikNolari));
            Assert.All(urunler.Items, u => Assert.Null(u.SandiktakiMiktar));
            var sandikta = await servis.UrunleriListeleAsync(projeId, s8, null, 1, 1, default);
            Assert.Equal((2, tekerlek), (sandikta.TotalCount, sandikta.Items.Single().Id));
            // Sandıkla süzülünce bölünmüş satırın o sandıktaki tahsisi (2) ayrıca gelir; istenen adet (4) bütün sandıklarındır.
            Assert.Equal((4m, (decimal?)2m), (sandikta.Items[0].Miktar, sandikta.Items[0].SandiktakiMiktar));
            var sandiktakiElle = (await servis.UrunleriListeleAsync(projeId, s8, null, 2, 1, default)).Items.Single();
            Assert.Equal((elle, (decimal?)3m), (sandiktakiElle.Id, sandiktakiElle.SandiktakiMiktar));
            Assert.Equal([elle], (await servis.UrunleriListeleAsync(projeId, null, "silika", null, null, default)).Items.Select(u => u.Id));
            await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.UrunleriListeleAsync(projeId, digerSandik, null, null, null, default));
            await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.SandiklariListeleAsync(int.MaxValue, null, null, null, default));

            var sahaProjesi = await servis.ProjeAsync(digerProjeId, default);
            Assert.Equal(("PA600-22", "SONELGAZ", "ALGERİA"), (sahaProjesi.SeriNo, sahaProjesi.Musteri, sahaProjesi.Lokasyon));
            Assert.Contains(sahaProjesi, (await servis.ProjeleriListeleAsync("pa600-22", null, null, default)).Items);

            var ezpl = await servis.SandikEtiketleriAsync(digerProjeId, [new(digerSandik, 2)], 2026, 9, BarkodEtiketBicimi.Ezpl, default);
            Assert.Matches(@"^Sandik_Etiketleri_PA600-22-23-24-SAHA_\d{8}_\d{6}\.prn$", ezpl.DosyaAdi);
            var komut = Encoding.ASCII.GetString(ezpl.Icerik);
            Assert.Contains($"BQ,134,140,4,10,174,0,3,{BarkodEtiketKurali.SandikBarkodu(digerSandik)}", komut);
            Assert.Contains("AD,171,494,1,1,0,0,PA600-22", komut);
            Assert.Contains("AB,424,552,1,1,0,0,ALGERIA", komut);
            Assert.Contains("AC,651,99,1,1,0,0,09/26", komut);
            Assert.Contains("^P2", komut);
            // Başka projenin sandığı seçilemez.
            await Assert.ThrowsAsync<KayitBulunamadiException>(() =>
                servis.SandikEtiketleriAsync(projeId, [new(s8, 1), new(digerSandik, 1)], 2026, 9, BarkodEtiketBicimi.Pdf, default));

            var urunPdf = await servis.UrunEtiketleriAsync(projeId,
                [new(BarkodEtiketUrunKaynagi.CekiSatiri, tekerlek, 2, null), new(BarkodEtiketUrunKaynagi.SandikIcerik, elle, 1, "Kutu 1")],
                BarkodEtiketBicimi.Pdf, default);
            Assert.Equal(DosyaIcerigi.PdfTuru, urunPdf.IcerikTuru);
            Assert.Contains("/Count 3", Encoding.Latin1.GetString(urunPdf.Icerik));
            var urunEzpl = Encoding.ASCII.GetString((await servis.UrunEtiketleriAsync(projeId,
                [new(BarkodEtiketUrunKaynagi.CekiSatiri, tekerlek, 1, null)], BarkodEtiketBicimi.Ezpl, default)).Icerik);
            Assert.Contains("AA,127,219,1,1,0,0,TEKERLEK DUZ.-RAY CAP 200 CIFT TEKERLEK", urunEzpl);
            Assert.Contains("AA,102,269,1,1,0,0,-", urunEzpl);
            var hata = await Assert.ThrowsAsync<IsKuraliException>(() => servis.UrunEtiketleriAsync(projeId,
                [new(BarkodEtiketUrunKaynagi.CekiSatiri, gecersiz, 1, null)], BarkodEtiketBicimi.Pdf, default));
            Assert.Equal(400, hata.StatusCode);
            // Çeki satırına bağlı tahsis elle eklenen malzeme olarak seçilemez; başka projenin malzemesi de seçilemez.
            await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.UrunEtiketleriAsync(projeId,
                [new(BarkodEtiketUrunKaynagi.SandikIcerik, tahsis, 1, null)], BarkodEtiketBicimi.Pdf, default));
            var sahaMalzemesi = okuma.SandikIcerikleri.Single(si => si.Isim == "Saha malzemesi").Id;
            await Assert.ThrowsAsync<KayitBulunamadiException>(() => servis.UrunEtiketleriAsync(projeId,
                [new(BarkodEtiketUrunKaynagi.SandikIcerik, sahaMalzemesi, 1, null)], BarkodEtiketBicimi.Pdf, default));
        });
    }
}
