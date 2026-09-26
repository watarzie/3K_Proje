using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using _3K.Core.Enums;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class AmbalajPdfYerlesimTests
{
    [Fact]
    public void UretimFormuPdf_PA727_03_IkiGrubuBesSayfadaLatoIleUretirVeGirdiyiKorur()
    {
        // Canlı rapordaki dış ölçüler; 2 ve 3 aynı üretim tarifinde birleştirilir.
        var kalemler = new[]
        {
            KalemOlustur(2, 1400, 1000, 1375, "Radyatör"),
            KalemOlustur(3, 1400, 1000, 1375, "Radyatör"),
            KalemOlustur(4, 1700, 1600, 1500, "Havalı Kablo Kutusu")
        };
        var form = FormOlustur("PA727-03", kalemler);
        form.ProjeAdi = "GCA ENERGY - ESCRAVOS";
        form.FBNo = "PA727-03";
        var oncekiModel = JsonSerializer.Serialize(form);
        var oncekiParcalar = kalemler.Select(kalem => kalem.Parcalar).ToArray();

        var pdf = new AmbalajRaporDosyaService().UretimFormuPdfOlustur(form);

        Assert.Equal(5, PdfSayfaSayisi(pdf)); // Bir liste ve her grup için iki teknik sayfa.
        GomuluLatoFontlariniDogrula(pdf);
        Assert.Equal(oncekiModel, JsonSerializer.Serialize(form));
        Assert.Same(kalemler, form.Kalemler);
        for (var i = 0; i < kalemler.Length; i++)
            Assert.Same(oncekiParcalar[i], kalemler[i].Parcalar);
    }

    [Theory]
    [InlineData(1400, 2)]
    [InlineData(2592, 3)]
    [InlineData(4092, 4)]
    [InlineData(5092, 5)]
    [InlineData(5990, 6)]
    [InlineData(7000, 7)]
    public void UretimFormuPdf_IkiIleYediAyakVaryantiTekGrupIcinUcSayfadaKalir(
        decimal disBoy,
        int beklenenAyak)
    {
        var kalem = KalemOlustur(1, disBoy, 1600, 1800, "Radyatör");
        var form = FormOlustur("PA-PDF-VARYANT", [kalem]);
        Assert.Equal(beklenenAyak, kalem.AyakAdedi);

        var pdf = new AmbalajRaporDosyaService().UretimFormuPdfOlustur(form);

        Assert.Equal(3, PdfSayfaSayisi(pdf));
        GomuluLatoFontlariniDogrula(pdf);
    }

    [Fact]
    public void UretimFormuPdf_KesintiliSekizSandikNumarasiTekGrupIcinUcSayfadaKalir()
    {
        var kalemler = Enumerable.Range(1, 8)
            .Select(sira => KalemOlustur(sira * 2, 1400, 1000, 1375, "Radyatör"))
            .ToArray();
        var form = FormOlustur("PA727-03", kalemler);
        form.ProjeAdi = "GCA ENERGY - ESCRAVOS";
        form.FBNo = "PA727-03";
        var grup = Assert.Single(AmbalajUretimFormuGruplayici.Grupla(kalemler));
        Assert.Equal("2, 4, 6, 8, 10, 12, 14, 16", grup.SandikNo);

        var pdf = new AmbalajRaporDosyaService().UretimFormuPdfOlustur(form);

        // Uzayan teknik sayfa başlığı, çizimleri ek sayfaya taşırmamalı.
        Assert.Equal(3, PdfSayfaSayisi(pdf));
        GomuluLatoFontlariniDogrula(pdf);
    }

    [Fact]
    public void AmbalajPdf_FontlarVeLisansInfrastructureAssemblysineGomuludur()
    {
        var assembly = typeof(AmbalajRaporDosyaService).Assembly;
        var gomuluKaynaklar = assembly.GetManifestResourceNames();
        string[] dosyalar =
        [
            "Lato-Regular.ttf", "Lato-Bold.ttf", "Lato-Italic.ttf",
            "Lato-BoldItalic.ttf", "Lato-ExtraBold.ttf", "Lato-ExtraBoldItalic.ttf", "OFL.txt"
        ];

        foreach (var dosya in dosyalar)
        {
            var kaynakAdi = $"_3K.Infrastructure.Assets.AmbalajFonts.{dosya}";
            Assert.Contains(kaynakAdi, gomuluKaynaklar);
            using var stream = assembly.GetManifestResourceStream(kaynakAdi);
            Assert.NotNull(stream);
            Assert.True(stream.Length > 0, $"Gömülü kaynak boş: {dosya}");
        }
    }

    private static AmbalajUretimFormuModel FormOlustur(
        string projeNo,
        IReadOnlyList<AmbalajUretimFormuKalemiModel> kalemler) => new()
    {
        ProjeId = 1,
        ProjeNo = projeNo,
        ProjeAdi = "Test Müşteri",
        FBNo = "FB-TEST",
        Kalemler = kalemler,
        NetM3 = kalemler.Sum(kalem => kalem.NetM3),
        SarfM3 = kalemler.Sum(kalem => kalem.SarfM3),
        ToplamM3 = kalemler.Sum(kalem => kalem.ToplamM3)
    };

    private static AmbalajUretimFormuKalemiModel KalemOlustur(
        int sandikNo,
        decimal disBoy,
        decimal disEn,
        decimal disYukseklik,
        string kullanimAmaci)
    {
        var profil = AmbalajAyakProfiliBelirleyici.Belirle(kullanimAmaci);
        var hesap = AmbalajHesaplayici.Hesapla(
            disBoy - 92m, disEn - 92m, disYukseklik - 255m, profil, disBoy);
        var netM3 = Math.Round(hesap.ToplamHacimM3, 6, MidpointRounding.AwayFromZero);
        var sarfM3 = Math.Round(netM3 * AmbalajHesaplayici.VarsayilanSarfOrani, 6, MidpointRounding.AwayFromZero);
        return new AmbalajUretimFormuKalemiModel
        {
            Tur = AmbalajSandikTuru.Normal,
            KayitId = sandikNo,
            IsAkisKimligi = Guid.NewGuid(),
            SandikNo = sandikNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SandikAdi = kullanimAmaci,
            SandikTuru = "Normal",
            SandikCinsi = "Ahşap Kapalı",
            Adet = 1,
            BrutKg = 1250,
            KullanimAmaci = kullanimAmaci,
            IcOlculer = hesap.IcOlculer,
            DisOlculer = hesap.DisOlculer,
            UstKizakAdedi = hesap.UstKizakAdedi,
            AyakAdedi = hesap.AyakAdedi,
            YanKusakAdedi = hesap.YanKusakAdedi,
            OnDuvarYuksekligi = hesap.OnDuvarYuksekligi,
            FormulVersiyonu = AmbalajHesaplayici.FormulVersiyonu,
            HesaplananNetM3 = netM3,
            NetM3 = netM3,
            SarfOrani = AmbalajHesaplayici.VarsayilanSarfOrani,
            SarfM3 = sarfM3,
            ToplamM3 = netM3 + sarfM3,
            Parcalar = hesap.Parcalar.Select(parca => new AmbalajUretimFormuParcasiModel
            {
                Kod = parca.Kod,
                Grup = parca.Grup,
                Aciklama = parca.Aciklama,
                Malzeme = parca.Malzeme,
                KesitEn = parca.KesitEn,
                KesitYukseklik = parca.KesitYukseklik,
                Uzunluk = parca.Uzunluk,
                TeorikAdet = parca.Adet,
                KesimAdedi = parca.UretimAdedi,
                HacimM3 = Math.Round(parca.HacimM3, 6, MidpointRounding.AwayFromZero)
            }).ToList()
        };
    }

    private static int PdfSayfaSayisi(byte[] pdf) => Regex.Matches(
        Encoding.Latin1.GetString(pdf),
        @"/Type\s*/Page(?!s)\b",
        RegexOptions.CultureInvariant).Count;

    private static void GomuluLatoFontlariniDogrula(byte[] pdf)
    {
        var metin = Encoding.Latin1.GetString(pdf);
        var fontlar = Regex.Matches(
                metin,
                @"/BaseFont\s*/([^\s/<>\[\](){}%]+)",
                RegexOptions.CultureInvariant)
            .Select(eslesme => eslesme.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(fontlar);
        Assert.All(fontlar, font => Assert.Contains("Lato", font, StringComparison.Ordinal));
        Assert.DoesNotContain("DejaVuSerif", metin, StringComparison.Ordinal);
        Assert.Matches(@"/FontFile2\s+\d+\s+0\s+R", metin);
    }
}
