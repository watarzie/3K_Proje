using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K_API.Extensions;
using _3K_API.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Dışa aktarım sınırı: yanıtı <c>Result&lt;DosyaIcerigi&gt;</c> olan her istek kullanıcı başına dakikada 20 ile sınırlanır;
/// kota bitince handler çalışmaz ve 429 döner. Dosya dışı istek ve kullanıcısız (sunucu içi) çağrı etkilenmez.
/// </summary>
public sealed class DisaAktarimHizSiniriTests
{
    [Fact]
    public async Task DosyaIstegi_KullaniciBasina20_SonrasindaHandlerCalismadan429()
    {
        using var hizSiniri = new DisaAktarimHizSiniri();
        var handlerCagrisi = 0;
        Task<Result<DosyaIcerigi>> Handler()
        {
            handlerCagrisi++;
            return Task.FromResult(Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf([1], "rapor.pdf")));
        }

        var ali = Davranis<Result<DosyaIcerigi>>(hizSiniri, kullaniciId: 1);
        for (var i = 0; i < HizSiniriBolumleri.DisaAktarimDakikaLimiti; i++)
            Assert.True((await ali.Handle(new Istek(), Handler, CancellationToken.None)).IsSuccess);

        var ret = await ali.Handle(new Istek(), Handler, CancellationToken.None);
        Assert.Equal((false, 429, DisaAktarimHizSiniriBehavior<Istek, Result<DosyaIcerigi>>.Mesaj), (ret.IsSuccess, ret.StatusCode, ret.Error?.Message));
        Assert.Equal(HizSiniriBolumleri.DisaAktarimDakikaLimiti, handlerCagrisi);

        // Başka kullanıcının kotası ayrıdır.
        Assert.True((await Davranis<Result<DosyaIcerigi>>(hizSiniri, kullaniciId: 2).Handle(new Istek(), Handler, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task DosyaDisiIstekVeKullanicisizCagriSinirlanmaz()
    {
        using var hizSiniri = new DisaAktarimHizSiniri();
        for (var i = 0; i < HizSiniriBolumleri.DisaAktarimDakikaLimiti; i++)
            Assert.True(hizSiniri.IzinVer(1));

        // Kotası biten kullanıcının dosya dışı isteği (aynı Result ailesinden olsa da) geçer.
        var liste = await Davranis<Result<int>>(hizSiniri, kullaniciId: 1)
            .Handle(new Istek(), () => Task.FromResult(Result<int>.Success(5)), CancellationToken.None);
        Assert.True(liste.IsSuccess);

        // Sunucu içi çağrı (oturum yok) dosya üretse de sınırlanmaz.
        var icCagri = Davranis<Result<DosyaIcerigi>>(hizSiniri, kullaniciId: null);
        for (var i = 0; i < HizSiniriBolumleri.DisaAktarimDakikaLimiti + 5; i++)
        {
            var sonuc = await icCagri.Handle(new Istek(),
                () => Task.FromResult(Result<DosyaIcerigi>.Success(DosyaIcerigi.Pdf([1], "x.pdf"))), CancellationToken.None);
            Assert.True(sonuc.IsSuccess);
        }
    }

    /// <summary>Hat sırası: yetki → doğrulama → dışa aktarım sınırı → onay; sınırlayıcı tek örnektir (sayaç paylaşılır).</summary>
    [Fact]
    public void Hatta_YetkiVeDogrulamadanSonra_OnaydanOnce_TekOrnek()
    {
        var services = new ServiceCollection().AddApplication();
        var davranislar = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType!)
            .ToList();

        var sira = davranislar.IndexOf(typeof(DisaAktarimHizSiniriBehavior<,>));
        Assert.True(sira > davranislar.IndexOf(typeof(AuthorizationBehavior<,>)));
        Assert.True(sira > davranislar.IndexOf(typeof(ValidationBehavior<,>)));
        Assert.True(sira < davranislar.IndexOf(typeof(ApprovalBehavior<,>)));

        // Sayaç uygulama boyunca paylaşılır: API tek örnek kaydeder.
        var api = new ServiceCollection().AddApiRateLimiting();
        var kayit = api.Single(d => d.ServiceType == typeof(IDisaAktarimHizSiniri));
        Assert.Equal((ServiceLifetime.Singleton, typeof(DisaAktarimHizSiniri)), (kayit.Lifetime, kayit.ImplementationType));
    }

    /// <summary>Hattın 429'u HTTP'de 429 ve <c>{ message }</c> gövdesi olur (ön yüz mesajı olduğu gibi gösterir).</summary>
    [Fact]
    public void Ret_DosyaUcundaHttp429VeMesajGovdesiOlur()
    {
        var mesaj = DisaAktarimHizSiniriBehavior<Istek, Result<DosyaIcerigi>>.Mesaj;
        var yanit = Assert.IsAssignableFrom<ObjectResult>(Result<DosyaIcerigi>.Failure(mesaj, 429).ToFileResult());

        Assert.Equal(429, yanit.StatusCode);
        Assert.Equal(mesaj, yanit.Value!.GetType().GetProperty("message")!.GetValue(yanit.Value));
    }

    /// <summary>
    /// Sınırın kapsamı yanıt tipine dayanır: dosya yalnız <c>ToFileResult()</c> (yalnız <c>Result&lt;DosyaIcerigi&gt;</c>
    /// kabul eder) ile döner. Controller doğrudan dosya sonucu üretmez, hiçbir istek ham bayt (<c>Result&lt;byte[]&gt;</c>)
    /// döndürmez; böylece yeni dosya ucu sınırın dışında kalamaz.
    /// </summary>
    [Fact]
    public void DosyaYalnizToFileResultIleDoner_SinirDisindaDosyaUcuYok()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin != null && !Directory.Exists(Path.Combine(dizin.FullName, "3K_API", "Controllers")))
            dizin = dizin.Parent;
        Assert.NotNull(dizin);

        var dogrudanDosya = new Regex(@"\b(File|PhysicalFile)\s*\(|\bnew\s+(FileContentResult|FileStreamResult|PhysicalFileResult)\b");
        var ihlaller = Directory.GetFiles(Path.Combine(dizin.FullName, "3K_API", "Controllers"), "*.cs")
            .Where(dosya => dogrudanDosya.IsMatch(File.ReadAllText(dosya)))
            .Select(Path.GetFileName);
        Assert.Empty(ihlaller);

        var hamBayt = typeof(DisaAktarimHizSiniriBehavior<,>).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.GetInterfaces().Any(i => i == typeof(IRequest<Result<byte[]>>)))
            .Select(t => t.Name);
        Assert.Empty(hamBayt);
    }

    private static DisaAktarimHizSiniriBehavior<Istek, TResponse> Davranis<TResponse>(DisaAktarimHizSiniri hizSiniri, int? kullaniciId) =>
        new(hizSiniri, new SabitKullanici(kullaniciId));

    private sealed class Istek;

    private sealed class SabitKullanici(int? kullaniciId) : ICurrentUserService
    {
        public int? UserId => kullaniciId;
        public bool IsAuthenticated => kullaniciId != null;
    }
}
