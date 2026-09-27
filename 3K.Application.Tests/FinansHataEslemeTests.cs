using System.Reflection;
using _3K.Application.Features.FinansIslemleri;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

/// <summary>
/// Finans istekleri geçersiz girdi için 400, iş kuralı ihlali için 409, bulunamayan kayıt için
/// 404 döner; hiçbir iş kuralı hatası 500 olarak sızmaz. Tarih sınırları tek kaynaktandır.
/// </summary>
public sealed class FinansHataEslemeTests
{
    [Theory]
    [InlineData(1999, 12, false)]
    [InlineData(2000, 1, true)]
    [InlineData(2150, 6, true)]
    [InlineData(2200, 12, true)]
    [InlineData(2201, 1, false)]
    [InlineData(2026, 13, false)]
    public void Donem_kurali_2000_2200_araligini_her_yerde_ayni_uygular(int yil, int ay, bool gecerli)
    {
        Assert.Equal(gecerli, FinansTarihKurallari.GecerliDonem(yil, ay));
        Assert.Equal(gecerli, new FinansAylikIslerQueryValidator()
            .Validate(new FinansAylikIslerQuery { Yil = yil, Ay = ay }).IsValid);
        Assert.Equal(gecerli, new FinansAylikRaporDosyaQueryValidator()
            .Validate(new FinansAylikRaporDosyaQuery { Yil = yil, Ay = ay }).IsValid);
    }

    [Fact]
    public void Tarihsiz_veya_on_yildan_uzun_panel_istegi_dogrulamada_reddedilir()
    {
        var validator = new FinansPanelQueryValidator();

        Assert.False(validator.Validate(new FinansPanelQuery()).IsValid);
        Assert.False(validator.Validate(new FinansPanelQuery { Baslangic = new(2026, 9, 30), Bitis = new(2026, 9, 1) }).IsValid);
        Assert.False(validator.Validate(new FinansPanelQuery { Baslangic = new(2010, 1, 1), Bitis = new(2026, 1, 1) }).IsValid);
        var gecerli = validator.Validate(new FinansPanelQuery { Baslangic = new(2026, 1, 1), Bitis = new(2026, 12, 31) });
        Assert.True(gecerli.IsValid);
    }

    [Fact]
    public async Task Rapor_is_kurali_hatasi_409_doner()
    {
        var handler = new FinansRaporQueryHandlers(new SinirAsanRaporServisi());

        var sonuc = await handler.Handle(new FinansIsRaporuDosyaQuery { Format = "pdf" }, default);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("20.000", sonuc.Error!.Message);
    }

    [Fact]
    public async Task Ozet_rapor_gecersiz_bicimi_400_ile_reddeder()
    {
        var handler = new FinansV2RaporQueryHandler(new SinirAsanRaporServisi());

        var sonuc = await handler.Handle(new FinansV2RaporQuery { Format = "docx" }, default);

        Assert.Equal(400, sonuc.StatusCode);
    }

    [Fact]
    public async Task Sorgu_is_kurali_hatasi_409_bulunamayan_kayit_ozel_mesajla_404_doner()
    {
        var servis = DispatchProxy.Create<IFinansService, KuralIhlalEdenFinansServisi>();
        var handler = new FinansQueryHandlers(servis);

        var secim = await handler.Handle(new FinansIsKayitlariSecimQuery { Ids = [0] }, default);
        var fatura = await handler.Handle(new FinansFaturaGetirQuery { Id = 5 }, default);

        Assert.Equal(409, secim.StatusCode);
        Assert.Equal("İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir.", secim.Error!.Message);
        Assert.Equal(404, fatura.StatusCode);
        Assert.Equal("Fatura bulunamadı.", fatura.Error!.Message);
    }

    private sealed class SinirAsanRaporServisi : IFinansRaporService
    {
        private static Task<byte[]> Sinir() =>
            throw new InvalidOperationException("Rapor en fazla 20.000 iş içerebilir; filtreyi daraltın.");
        public Task<byte[]> OzetRaporAsync(string tur, bool excel, FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikExcelAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikPdfAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikZipAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
    }

    public class KuralIhlalEdenFinansServisi : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            nameof(IFinansService.IsKayitlariSecimAsync) => throw new InvalidOperationException(
                "İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir."),
            nameof(IFinansService.FaturaGetirAsync) => Task.FromResult<FinansFaturaModel?>(null),
            _ => throw new NotSupportedException(method.Name)
        };
    }
}
