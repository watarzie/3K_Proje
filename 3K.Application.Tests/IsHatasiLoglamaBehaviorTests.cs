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
/// İş hatası logu: başarısız her istek tek satır Warning olur ve maskeli girdiyi taşır (parola, token,
/// doğrulama kodu "***"; dosya içeriği okunmaz). Başarı loglanmaz; 500 Error satırında girdisiyle yazılır.
/// </summary>
public sealed class IsHatasiLoglamaBehaviorTests
{
    [Fact]
    public async Task BasarisizIstek_KullaniciHataVeGirdiyiTekSatirdaYazar()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GuvenliKomut, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<GuvenliKomut, Result>(logger, new Kullanici());
        var komut = new GuvenliKomut { ProjeId = 55901, SandikIcerikId = 5591, CekiSatiriIdler = [559, 560], Aciklama = "Kira ekim açıklaması" };

        await behavior.Handle(komut, () => Task.FromResult(Result.Failure("Geri gönderilen adet büyük olamaz.", 409)), default);

        var kayit = Assert.Single(logger.Kayitlar);
        Assert.Equal(LogLevel.Warning, kayit.Seviye);
        Assert.DoesNotContain('\n', kayit.Mesaj);
        Assert.Contains("İş hatası 409", kayit.Mesaj);
        Assert.Contains(nameof(GuvenliKomut), kayit.Mesaj);
        Assert.Contains("Kullanıcı 7 (Ömer Yılmaz)", kayit.Mesaj);
        Assert.Contains("\"Geri gönderilen adet büyük olamaz.\"", kayit.Mesaj);
        Assert.Contains("Girdi: {\"projeId\":55901,\"sandikIcerikId\":5591,\"bosId\":null,\"cekiSatiriIdler\":[559,560],\"aciklama\":\"Kira ekim açıklaması\"}", kayit.Mesaj);
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
    public async Task GirisHatasi_EpostaKismenParolaTamamenMaskelenir()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GirisKomutu, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<GirisKomutu, Result>(logger, new Kullanici(oturumYok: true));

        await behavior.Handle(new GirisKomutu { Email = "deneme@gmail.com", Sifre = "Parola123!" },
            () => Task.FromResult(Result.Failure("E-posta veya şifre hatalı.", 401)), default);

        var mesaj = Assert.Single(logger.Kayitlar).Mesaj;
        Assert.Contains("İş hatası 401", mesaj);
        Assert.Contains("Kullanıcı - (-)", mesaj);
        Assert.Contains("\"email\":\"de***@gmail.com\"", mesaj);
        Assert.Contains("\"sifre\":\"***\"", mesaj);
        Assert.DoesNotContain("deneme@", mesaj);
        Assert.DoesNotContain("Parola123", mesaj);
    }

    /// <summary>
    /// E-posta biçiminde değilse tamamen gizlenir: kullanıcı şifresini yanlışlıkla e-posta kutusuna yazarsa şifre loga
    /// düşmez. Biçimliyse yerel kısmın en fazla ilk iki harfi görünür, hiçbir zaman tamamı görünmez.
    /// </summary>
    [Theory]
    [InlineData("deneme@gmail.com", "de***@gmail.com")]
    [InlineData("  ali.veli@3kambalaj.com ", "al***@3kambalaj.com")]
    [InlineData("ab@x.co", "a***@x.co")]
    [InlineData("a@b.c", "***@b.c")]
    [InlineData("GercekParolam!2026", "***")]
    [InlineData("kullanici@sunucu", "***")]
    [InlineData("iki@at@ornek.com", "***")]
    [InlineData("bosluk li@ornek.com", "***")]
    public async Task Eposta_alani_bicimine_gore_maskelenir(string girilen, string yazilan)
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<GirisKomutu, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<GirisKomutu, Result>(logger, new Kullanici(oturumYok: true));

        await behavior.Handle(new GirisKomutu { Email = girilen, Sifre = "x" },
            () => Task.FromResult(Result.Failure("E-posta veya şifre hatalı.", 401)), default);

        var mesaj = Assert.Single(logger.Kayitlar).Mesaj;
        Assert.Contains($"\"email\":\"{yazilan}\"", mesaj);
        if (yazilan == "***") Assert.DoesNotContain(girilen.Trim(), mesaj);
    }

    [Fact]
    public async Task Girdi_GizliAlanlarIcIceDahilMaskelenir_DosyaOkunmaz_UzunDegerlerKisaltilir()
    {
        var logger = new ToplayanLogger<IsHatasiLoglamaBehavior<HassasKomut, Result>>();
        var behavior = new IsHatasiLoglamaBehavior<HassasKomut, Result>(logger, new Kullanici());
        var dosya = new MemoryStream([1, 2, 3, 4, 5]);
        var komut = new HassasKomut
        {
            ChallengeToken = "challenge-gizli",
            Kod = "123456",
            KurtarmaKodu = "KURTAR-GIZLI",
            YeniSifre = "YeniParola!",
            ApiAnahtari = "anahtar-gizli",
            Barkod = "3K048214",
            Model = new IcModel { Sifre = "IcParola!", Not = "görünür not", KisiEposta = "ayse.kaya@3kambalaj.com" },
            ExcelDosya = dosya,
            Icerik = [9, 9, 9],
            Uzun = new string('x', 500),
            Liste = Enumerable.Range(1, 30).ToList(),
        };

        await behavior.Handle(komut, () => Task.FromResult(Result.Failure("Doğrulama hatası.", 400)), default);

        var mesaj = Assert.Single(logger.Kayitlar).Mesaj;
        foreach (var gizli in new[] { "challenge-gizli", "123456", "KURTAR-GIZLI", "YeniParola", "IcParola", "anahtar-gizli", "ayse.kaya" })
            Assert.DoesNotContain(gizli, mesaj);
        Assert.Contains("\"challengeToken\":\"***\"", mesaj);
        Assert.Contains("\"kod\":\"***\"", mesaj);
        Assert.Contains("\"kurtarmaKodu\":\"***\"", mesaj);
        Assert.Contains("\"yeniSifre\":\"***\"", mesaj);
        Assert.Contains("\"apiAnahtari\":\"***\"", mesaj);
        Assert.Contains("\"model\":{\"sifre\":\"***\",\"not\":\"görünür not\",\"kisiEposta\":\"ay***@3kambalaj.com\"}", mesaj);
        // "Kod" tam adla gizlenir; barkod gibi alanlar görünür kalır.
        Assert.Contains("\"barkod\":\"3K048214\"", mesaj);
        Assert.Contains("\"excelDosya\":\"<dosya akışı, 5 bayt>\"", mesaj);
        Assert.Equal(0, dosya.Position);
        Assert.Contains("\"icerik\":\"<3 bayt>\"", mesaj);
        Assert.Contains(new string('x', 200) + "…", mesaj);
        Assert.DoesNotContain(new string('x', 201), mesaj);
        Assert.Contains("\"liste\":[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,\"…(+10)\"]", mesaj);
        Assert.DoesNotContain("requiredMenuKod", mesaj);
    }

    [Fact]
    public async Task BeklenmeyenHata_ErrorSatiriMaskeliGirdiyiTasir()
    {
        var logger = new ToplayanLogger<UnhandledExceptionBehavior<GirisKomutu, Result>>();
        var behavior = new UnhandledExceptionBehavior<GirisKomutu, Result>(logger);

        var sonuc = await behavior.Handle(new GirisKomutu { Email = "deneme@gmail.com", Sifre = "Parola123!" },
            () => throw new InvalidOperationException("veritabanı bağlantısı koptu"), default);

        Assert.Equal(500, sonuc.StatusCode);
        var kayit = Assert.Single(logger.Kayitlar);
        Assert.Equal(LogLevel.Error, kayit.Seviye);
        Assert.Contains("Girdi: {\"email\":\"de***@gmail.com\",\"sifre\":\"***\"}", kayit.Mesaj);
        Assert.DoesNotContain("Parola123", kayit.Mesaj);
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
    }

    public sealed class HassasKomut : IRequest<Result>, ISecuredRequest
    {
        public string RequiredMenuKod => "gizli-menu";
        public string ChallengeToken { get; set; } = "";
        public string Kod { get; set; } = "";
        public string KurtarmaKodu { get; set; } = "";
        public string YeniSifre { get; set; } = "";
        public string ApiAnahtari { get; set; } = "";
        public string Barkod { get; set; } = "";
        public IcModel? Model { get; set; }
        public Stream? ExcelDosya { get; set; }
        public byte[]? Icerik { get; set; }
        public string Uzun { get; set; } = "";
        public List<int> Liste { get; set; } = [];
    }

    public sealed class IcModel
    {
        public string Sifre { get; set; } = "";
        public string Not { get; set; } = "";
        public string KisiEposta { get; set; } = "";
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
