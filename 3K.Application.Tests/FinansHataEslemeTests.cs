using System.Reflection;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Queries;
using _3K.Application.Features.FinansIslemleri.Validators;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

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

    [Theory]
    [InlineData(null, false)]
    [InlineData("   iptal ", true)]
    [InlineData("kısa", false)]
    [InlineData("Hatalı giriş", true)]
    public void Gerekce_kurali_servisle_ayni_sinirla_dogrulamada_uygulanir(string? gerekce, bool gecerli)
    {
        Assert.Equal(gecerli, FinansGerekceKurali.Gecerli(gerekce));
        Assert.Equal(gecerli, new FinansIsKaydiIptalCommandValidator()
            .Validate(new FinansIsKaydiIptalCommand { Id = 1, Aciklama = gerekce! }).IsValid);
        Assert.Equal(gecerli, new FinansSiparisIptalCommandValidator()
            .Validate(new FinansSiparisIptalCommand { Id = 1, Aciklama = gerekce! }).IsValid);
        Assert.Equal(gecerli, new FinansKaliciSilCommandValidator()
            .Validate(new FinansKaliciSilCommand { Model = new("Gider", 1, "v1", true, gerekce!) }).IsValid);
    }

    [Fact]
    public void Tutar_revizyonu_gerekce_ister_baslik_duzeltmesi_istemez()
    {
        var validator = new FinansSiparisGuncelleModelValidator();
        var kalem = new FinansSiparisDagitimModel(1, 0, 0, NetTutar: 50);

        Assert.True(validator.Validate(new FinansSiparisGuncelleModel("PO-1", new(2026, 9, 1), null)).IsValid);
        Assert.False(validator.Validate(new FinansSiparisGuncelleModel("PO-1", new(2026, 9, 1), null, [kalem])).IsValid);
        Assert.True(validator.Validate(new FinansSiparisGuncelleModel("PO-1", new(2026, 9, 1), null, [kalem], "Müşteri revizyonu")).IsValid);
    }

    [Fact]
    public async Task Rapor_is_kurali_hatasi_409_doner()
    {
        var handler = new FinansRaporQueryHandlers(new SinirAsanRaporServisi());
        var istek = new FinansIsRaporuDosyaQuery { Format = "pdf" };

        var sonuc = await Pipeline(istek, () => handler.Handle(istek, default));

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("20.000", sonuc.Error!.Message);
    }

    [Fact]
    public async Task Ozet_rapor_gecersiz_bicimi_400_ile_reddeder()
    {
        var handler = new FinansRaporQueryHandlers(new SinirAsanRaporServisi());

        var sonuc = await handler.Handle(new FinansOzetRaporuDosyaQuery { Format = "docx" }, default);

        Assert.Equal(400, sonuc.StatusCode);
    }

    [Fact]
    public async Task Sorgu_is_kurali_hatasi_409_bulunamayan_kayit_ozel_mesajla_404_doner()
    {
        var isHandler = new FinansIsKaydiQueryHandlers(
            DispatchProxy.Create<IFinansIsKaydiService, KuralIhlalEdenFinansServisi>(),
            DispatchProxy.Create<IFinansAylikOzetService, KuralIhlalEdenFinansServisi>());
        var faturaHandler = new FinansSiparisFaturaQueryHandlers(
            DispatchProxy.Create<IFinansSiparisFaturaService, KuralIhlalEdenFinansServisi>());

        var secimIstegi = new FinansIsKayitlariSecimQuery { Ids = [0] };
        var secim = await Pipeline(secimIstegi, () => isHandler.Handle(secimIstegi, default));
        var fatura = await faturaHandler.Handle(new FinansFaturaGetirQuery { Id = 5 }, default);

        Assert.Equal(409, secim.StatusCode);
        Assert.Equal("İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir.", secim.Error!.Message);
        Assert.Equal(404, fatura.StatusCode);
        Assert.Equal("Fatura bulunamadı.", fatura.Error!.Message);
    }

    [Fact]
    public async Task Beklenmeyen_framework_hatasi_ic_mesaji_sizdirmadan_500_doner()
    {
        var istek = new FinansIsRaporuDosyaQuery { Format = "pdf" };

        var sonuc = await Pipeline<FinansIsRaporuDosyaQuery, Result<DosyaIcerigi>>(istek,
            () => throw new InvalidOperationException("A second operation was started on this context instance."));

        Assert.Equal(500, sonuc.StatusCode);
        Assert.DoesNotContain("context", sonuc.Error!.Message);
    }

    private static Task<TResponse> Pipeline<TRequest, TResponse>(TRequest istek, Func<Task<TResponse>> handler)
        where TRequest : notnull
        => new UnhandledExceptionBehavior<TRequest, TResponse>(NullLogger<UnhandledExceptionBehavior<TRequest, TResponse>>.Instance)
            .Handle(istek, () => handler(), CancellationToken.None);

    private sealed class SinirAsanRaporServisi : IFinansRaporService
    {
        private static Task<byte[]> Sinir() =>
            throw IsKuraliException.Cakisma("Rapor en fazla 20.000 iş içerebilir; filtreyi daraltın.");
        public Task<byte[]> OzetRaporAsync(string tur, bool excel, FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<FinansRaporOnizlemeModel> OnizlemeAsync(string tur, FinansListeFiltre filtre, FinansRaporSecenekleri secenekler, CancellationToken ct) =>
            throw IsKuraliException.Cakisma("Rapor en fazla 20.000 iş içerebilir; filtreyi daraltın.");
        public Task<byte[]> IslerExcelAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken ct) => Sinir();
        public Task<byte[]> IslerPdfAsync(FinansListeFiltre filtre, FinansIsRaporuKapsami kapsam, CancellationToken ct) => Sinir();
        public Task<byte[]> GiderlerExcelAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken ct) => Sinir();
        public Task<byte[]> GiderlerPdfAsync(FinansListeFiltre filtre, FinansGiderRaporuGruplamasi gruplama, CancellationToken ct) => Sinir();
        public Task<byte[]> FinansalDurumExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> FinansalDurumPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> SiparisDurumExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> SiparisDurumPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> AlacakExcelAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> AlacakPdfAsync(FinansListeFiltre filtre, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikExcelAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikPdfAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
        public Task<byte[]> AylikZipAsync(int yil, int ay, IReadOnlyCollection<string>? gruplar, CancellationToken ct) => Sinir();
    }

    public class KuralIhlalEdenFinansServisi : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            nameof(IFinansIsKaydiService.IsKayitlariSecimAsync) => throw IsKuraliException.Cakisma(
                "İş kaydı seçiminde yalnız pozitif kimlikler kullanılabilir."),
            nameof(IFinansSiparisFaturaService.FaturaGetirAsync) => Task.FromResult<FinansFaturaModel?>(null),
            _ => throw new NotSupportedException(method.Name)
        };
    }
}
