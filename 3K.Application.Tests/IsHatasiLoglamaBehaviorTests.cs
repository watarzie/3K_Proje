using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

/// <summary>
/// İş hatası logu: başarısız her istek tek satır Warning olur; istek içeriği (metin alanları) yazılmaz,
/// yalnız sayısal kimlikler yazılır. Başarı ve 500 (zaten Error loglanır) yazılmaz.
/// </summary>
public sealed class IsHatasiLoglamaBehaviorTests
{
    [Fact]
    public async Task BasarisizIstek_KimlikleriYazar_MetinAlanlariniYazmaz()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GuvenliKomut, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<GuvenliKomut, Result>(logger, new Kullanici());
        var komut = new GuvenliKomut { ProjeId = 55901, SandikIcerikId = 5591, CekiSatiriIdler = [559, 560], Aciklama = "gizli açıklama" };

        await behavior.Handle(komut, () => Task.FromResult(Result.Failure("Geri gönderilen adet büyük olamaz.", 409)), default);

        var kayit = Assert.Single(logger.Kayitlar);
        Assert.Equal(LogLevel.Warning, kayit.Seviye);
        Assert.Contains("İş hatası 409", kayit.Mesaj);
        Assert.Contains(nameof(GuvenliKomut), kayit.Mesaj);
        Assert.Contains("Kullanıcı 7 (Ömer Yılmaz)", kayit.Mesaj);
        Assert.Contains("ProjeId=55901", kayit.Mesaj);
        Assert.Contains("SandikIcerikId=5591", kayit.Mesaj);
        Assert.Contains("CekiSatiriIdler=[559,560]", kayit.Mesaj);
        Assert.Contains("\"Geri gönderilen adet büyük olamaz.\"", kayit.Mesaj);
        Assert.DoesNotContain("gizli", kayit.Mesaj);
        Assert.DoesNotContain("BosId", kayit.Mesaj);
    }

    [Fact]
    public async Task BasariVe500_LoglanmazGenericResultBasarisizligiLoglanir()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GuvenliKomut, Result<int>>>();
        var behavior = new IsHatasiLoglamaBehavior<GuvenliKomut, Result<int>>(logger, new Kullanici());

        await behavior.Handle(new GuvenliKomut(), () => Task.FromResult(Result<int>.Success(1)), default);
        await behavior.Handle(new GuvenliKomut(), () => Task.FromResult(Result<int>.Failure("Beklenmeyen", 500)), default);
        Assert.Empty(logger.Kayitlar);

        await behavior.Handle(new GuvenliKomut(), () => Task.FromResult(Result<int>.Failure("Bulunamadı.", 404)), default);
        Assert.Contains("İş hatası 404", Assert.Single(logger.Kayitlar).Mesaj);
    }

    [Fact]
    public async Task PublicIstek_KimlikYazmaz_ParolaYazmaz()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GirisKomutu, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<GirisKomutu, Result>(logger, new Kullanici(oturumYok: true));

        await behavior.Handle(new GirisKomutu { Email = "a@b.c", Sifre = "Parola123!", DenemeId = 3 },
            () => Task.FromResult(Result.Failure("E-posta veya şifre hatalı.", 401)), default);

        var mesaj = Assert.Single(logger.Kayitlar).Mesaj;
        Assert.Contains("İş hatası 401", mesaj);
        Assert.Contains("Kullanıcı - (-)", mesaj);
        Assert.DoesNotContain("Parola123", mesaj);
        Assert.DoesNotContain("a@b.c", mesaj);
        Assert.DoesNotContain("DenemeId", mesaj);
    }

    [Fact]
    public async Task IstisnadanCevrilenIsKurali_DisKatmandaLoglanir()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GuvenliKomut, Result>>();
        var dis = new IsHatasiLoglamaBehavior<GuvenliKomut, Result>(logger, new Kullanici());
        var ic = new UnhandledExceptionBehavior<GuvenliKomut, Result>(NullLogger<UnhandledExceptionBehavior<GuvenliKomut, Result>>.Instance);

        var sonuc = await dis.Handle(new GuvenliKomut { ProjeId = 1 },
            () => ic.Handle(new GuvenliKomut { ProjeId = 1 }, () => throw IsKuraliException.Cakisma("Sandık kilitli."), default),
            default);

        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("\"Sandık kilitli.\"", Assert.Single(logger.Kayitlar).Mesaj);
    }

    [Fact]
    public void Pipeline_IsHatasiLogu_EnDistaKayitlidir()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        var davranislar = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();

        Assert.Equal(typeof(IsHatasiLoglamaBehavior<,>), davranislar[0]);
        Assert.Equal(typeof(UnhandledExceptionBehavior<,>), davranislar[1]);
    }

    public sealed class GuvenliKomut : IRequest<Result>, ISecuredRequest
    {
        public int ProjeId { get; set; }
        public int? SandikIcerikId { get; set; }
        public int? BosId { get; set; }
        public List<int> CekiSatiriIdler { get; set; } = [];
        public string? Aciklama { get; set; }
    }

    public sealed class GirisKomutu : IRequest<Result>
    {
        public string Email { get; set; } = "";
        public string Sifre { get; set; } = "";
        public int DenemeId { get; set; }
    }

    private sealed class Kullanici(bool oturumYok = false) : ICurrentUserService
    {
        public int? UserId => oturumYok ? null : 7;
        public bool IsAuthenticated => !oturumYok;
        public string? KullaniciAdi => oturumYok ? null : "Ömer Yılmaz";
    }

    private sealed class ToplayanLogger<T> : ILogger<T>
    {
        public List<(LogLevel Seviye, string Mesaj)> Kayitlar { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Kayitlar.Add((logLevel, formatter(state, exception)));
    }
}
