using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.CekiIslemleri.Validators;
using _3K.Infrastructure.Services.Cekiler;

namespace _3K.Application.Tests;

/// <summary>Ana çeki yüklemesinde istemcinin gönderdiği dosya adının sınırları ve diskte saklanan ad.</summary>
public sealed class CekiYuklemeDosyaAdiTests
{
    [Theory]
    [InlineData("ceki.xlsx", true)]
    [InlineData("CEKI.XLSX", true)]
    [InlineData("ceki.dll", false)]
    [InlineData("ceki.xlsx.exe", false)]
    [InlineData("ceki", false)]
    [InlineData("../../3K.Infrastructure.dll", false)]
    public void AnaCekiYukleme_YalnizXlsxUzantisiniKabulEder(string dosyaAdi, bool gecerli)
    {
        var sonuc = new CekiYukleCommandValidator().Validate(Komut(dosyaAdi));

        Assert.Equal(gecerli, sonuc.IsValid);
    }

    [Fact]
    public void AnaCekiYukleme_255KarakterdenUzunDosyaAdiniReddeder()
    {
        var sonuc = new CekiYukleCommandValidator().Validate(Komut(new string('a', 251) + ".xlsx"));

        Assert.False(sonuc.IsValid);
    }

    [Theory]
    [InlineData("ceki.xlsx")]
    [InlineData("../../3K.Infrastructure.dll")]
    [InlineData("..\\..\\appsettings.Secrets.json")]
    [InlineData("/etc/cron.d/kacak")]
    [InlineData("C:\\Windows\\kacak.dll")]
    [InlineData("..")]
    [InlineData("alt/klasor/")]
    [InlineData("")]
    [InlineData(null)]
    public void KaliciDosyaYolu_HerZamanProjeKlasorununIcindeVeXlsxUzantilidir(string? dosyaAdi)
    {
        var kok = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "3k-ceki-test", "Uploads", "42"));

        var yol = CekiYuklemeService.KaliciDosyaYolu(kok, dosyaAdi);

        Assert.Equal(kok, Path.GetDirectoryName(yol), ignoreCase: true);
        Assert.Equal(".xlsx", Path.GetExtension(yol));
        Assert.DoesNotContain("..", Path.GetFileName(yol));
    }

    [Fact]
    public void KaliciDosyaYolu_AyniAdIkiKezGelirseFarkliDosyaUretir()
    {
        var kok = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "3k-ceki-test", "Uploads", "42"));

        Assert.NotEqual(
            CekiYuklemeService.KaliciDosyaYolu(kok, "ceki.xlsx"),
            CekiYuklemeService.KaliciDosyaYolu(kok, "ceki.xlsx"));
    }

    [Fact]
    public void KaliciDosyaYolu_UzunAdiDosyaSistemiSinirininAltindaTutar()
    {
        var kok = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "3k-ceki-test", "Uploads", "42"));

        var ad = Path.GetFileName(CekiYuklemeService.KaliciDosyaYolu(kok, new string('a', 250) + ".xlsx"));

        Assert.True(ad.Length <= 255, $"Dosya adı {ad.Length} karakter.");
    }

    private static CekiYukleCommand Komut(string dosyaAdi) => new()
    {
        ExcelDosya = new MemoryStream([1]),
        DosyaAdi = dosyaAdi,
        KullaniciId = 1
    };
}
