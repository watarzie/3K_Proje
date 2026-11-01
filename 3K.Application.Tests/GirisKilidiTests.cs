using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.Features.KullaniciIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// Hesap bazlı giriş kilidi: art arda 5 hatalı parola → 15 dakika kilit; kilitliyken doğru parola da reddedilir;
/// başarılı giriş ve yöneticinin parola değiştirmesi sıfırlar; yanıt hesabın varlığını ya da kilidini sızdırmaz.
/// </summary>
public sealed class GirisKilidiTests
{
    private const string Parola = "Dogru-Parola-1";
    private static readonly DateTime Simdi = new(2026, 10, 25, 10, 0, 0);

    [Fact]
    public void Kural_BesinciHataliDenemedeKilitlerSuresiDolanKilitSayaciBastanBaslatir()
    {
        Assert.Equal((4, (DateTime?)null), GirisKilidiKurali.HataliDenemeSonrasi(3, null, Simdi));
        Assert.Equal((5, Simdi.AddMinutes(15)), GirisKilidiKurali.HataliDenemeSonrasi(4, null, Simdi));
        // Süresi dolan kilit: sayaç 1'den başlar, tek hata yeniden kilitlemez.
        Assert.Equal((1, (DateTime?)null), GirisKilidiKurali.HataliDenemeSonrasi(5, Simdi.AddSeconds(-1), Simdi));
        // Aktif kilit: deneme sayılmaz, kilit uzamaz.
        Assert.Equal((5, Simdi.AddMinutes(3)), GirisKilidiKurali.HataliDenemeSonrasi(5, Simdi.AddMinutes(3), Simdi));

        Assert.True(GirisKilidiKurali.KilitliMi(Simdi.AddSeconds(1), Simdi));
        Assert.False(GirisKilidiKurali.KilitliMi(Simdi, Simdi));
        Assert.False(GirisKilidiKurali.KilitliMi(null, Simdi));
    }

    [Fact]
    public async Task Handler_KilitliVeGecersizAyniYanitiAlir_TokenVeTalepUretilmez()
    {
        var gecersiz = await GirisYap(GirisDogrulamaSonucu.Gecersiz);
        var kilitli = await GirisYap(GirisDogrulamaSonucu.Kilitli);

        Assert.Equal((false, 401, GirisKilidiKurali.GenelHataMesaji), (gecersiz.IsSuccess, gecersiz.StatusCode, gecersiz.Error?.Message));
        Assert.Equal((gecersiz.IsSuccess, gecersiz.StatusCode, gecersiz.Error), (kilitli.IsSuccess, kilitli.StatusCode, kilitli.Error));
        Assert.DoesNotContain("kilit", GirisKilidiKurali.GenelHataMesaji, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_BesHatalidaKilitlenir_KilitliykenDogruParolaReddedilir_BilinmeyenEpostaAyniSonucuAlir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var id = await KullaniciEkleAsync(options, "kilit@example.com");

            for (var i = 1; i <= 4; i++)
                Assert.Equal(GirisDogrulamaDurumu.Gecersiz, await DogrulaAsync(options, "kilit@example.com", "yanlis"));
            Assert.Equal((4, (DateTime?)null), await DurumAsync(options, id));

            var once = TurkeyTime.Now;
            Assert.Equal(GirisDogrulamaDurumu.Gecersiz, await DogrulaAsync(options, "kilit@example.com", "yanlis"));
            var (sayac, kilit) = await DurumAsync(options, id);
            Assert.Equal(5, sayac);
            Assert.InRange(kilit!.Value, once.AddMinutes(15).AddSeconds(-1), TurkeyTime.Now.AddMinutes(15).AddSeconds(1));

            // Kilitliyken doğru parola da reddedilir; reddedilen deneme sayacı ve kilidi değiştirmez.
            Assert.Equal(GirisDogrulamaDurumu.Kilitli, await DogrulaAsync(options, "kilit@example.com", Parola));
            Assert.Equal(GirisDogrulamaDurumu.Kilitli, await DogrulaAsync(options, "kilit@example.com", "yanlis"));
            Assert.Equal((5, kilit), await DurumAsync(options, id));

            Assert.Equal(GirisDogrulamaDurumu.Gecersiz, await DogrulaAsync(options, "yok@example.com", Parola));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_BasariliGirisSayaciSifirlar_SuresiDolanKilitKalkar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var id = await KullaniciEkleAsync(options, "sifirla@example.com");
            for (var i = 0; i < 3; i++)
                await DogrulaAsync(options, "sifirla@example.com", "yanlis");
            Assert.Equal(GirisDogrulamaDurumu.Basarili, await DogrulaAsync(options, "sifirla@example.com", Parola));
            Assert.Equal((0, (DateTime?)null), await DurumAsync(options, id));

            // Süresi dolmuş kilit: hatalı deneme sayacı 1'den başlatır (yeniden kilitlemez).
            await KilitKurAsync(options, id, 5, TurkeyTime.Now.AddMinutes(-1));
            Assert.Equal(GirisDogrulamaDurumu.Gecersiz, await DogrulaAsync(options, "sifirla@example.com", "yanlis"));
            Assert.Equal((1, (DateTime?)null), await DurumAsync(options, id));

            // Süresi dolmuş kilitte doğru parola girer ve izleri temizler.
            await KilitKurAsync(options, id, 5, TurkeyTime.Now.AddMinutes(-1));
            Assert.Equal(GirisDogrulamaDurumu.Basarili, await DogrulaAsync(options, "sifirla@example.com", Parola));
            Assert.Equal((0, (DateTime?)null), await DurumAsync(options, id));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_EszamanliHataliDenemelerBirbiriniEzmez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var id = await KullaniciEkleAsync(options, "paralel@example.com");

            // Farklı IP'lerden aynı anda gelen denemeler: her biri ayrı bağlamda; artışlar kaybolmaz.
            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => DogrulaAsync(options, "paralel@example.com", "yanlis")));
            Assert.Equal((4, (DateTime?)null), await DurumAsync(options, id));

            await DogrulaAsync(options, "paralel@example.com", "yanlis");
            Assert.True(GirisKilidiKurali.KilitliMi((await DurumAsync(options, id)).Kilit, TurkeyTime.Now));
        });
    }

    /// <summary>
    /// Aynı hesaba paralel denemeler: bu istek kullanıcıyı kilitsiz okuduktan sonra başka bir istek kilidi kurar.
    /// Doğru parola kilidi silemez, hatalı parola sayılmaz ve kilidi uzatmaz.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_OkumaIleKararArasindaKurulanKilidiDogruParolaSilemez()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var kilit = SaniyeyeYuvarla(TurkeyTime.Now.AddMinutes(10));
            foreach (var (email, sifre) in new[] { ("dogru@example.com", Parola), ("hatali@example.com", "yanlis") })
            {
                var id = await KullaniciEkleAsync(options, email);
                var araya = new OkumadanSonraKilitKuran(options, id, kilit);

                Assert.Equal(GirisDogrulamaDurumu.Kilitli, await DogrulaAsync(options, email, sifre, araya));
                Assert.True(araya.KilitKuruldu);
                Assert.Equal((5, (DateTime?)kilit), await DurumAsync(options, id));
            }
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_YoneticiParolayiDegistirinceKilitKalkar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var id = await KullaniciEkleAsync(options, "yonetici@example.com");
            await KilitKurAsync(options, id, 5, TurkeyTime.Now.AddMinutes(10));

            await using (var db = new AppDbContext(options))
            {
                var unitOfWork = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var handler = new KullaniciSifreDegistirCommandHandler(unitOfWork, Servis(db));
                var sonuc = await handler.Handle(
                    new KullaniciSifreDegistirCommand { KullaniciId = id, YeniSifre = "Yeni-Parola-2" },
                    CancellationToken.None);
                Assert.True(sonuc.IsSuccess);
            }

            Assert.Equal((0, (DateTime?)null), await DurumAsync(options, id));
            Assert.Equal(GirisDogrulamaDurumu.Basarili, await DogrulaAsync(options, "yonetici@example.com", "Yeni-Parola-2"));
        });
    }

    private static async Task<Common.Result<Features.AuthIslemleri.DTOs.LoginResultDto>> GirisYap(GirisDogrulamaSonucu sonuc)
    {
        var auth = new SabitAuthService(sonuc);
        var handler = new LoginCommandHandler(auth, DispatchProxy.Create<IIkiFaktorService, KullanilmayanServis>());
        var result = await handler.Handle(new LoginCommand { Email = "x@example.com", Sifre = "x" }, CancellationToken.None);
        Assert.False(auth.AccessTokenUretildi);
        return result;
    }

    private static AuthService Servis(AppDbContext db) =>
        new(new UnitOfWork(db, NullLogger<UnitOfWork>.Instance), db, new ConfigurationBuilder().Build(), NullLogger<AuthService>.Instance);

    private static async Task<GirisDogrulamaDurumu> DogrulaAsync(
        DbContextOptions<AppDbContext> options, string email, string sifre, IInterceptor? araya = null)
    {
        if (araya != null)
            options = new DbContextOptionsBuilder<AppDbContext>(options).AddInterceptors(araya).Options;
        await using var db = new AppDbContext(options);
        return (await Servis(db).ValidateCredentialsAsync(email, sifre)).Durum;
    }

    private static async Task<int> KullaniciEkleAsync(DbContextOptions<AppDbContext> options, string email)
    {
        await using var db = new AppDbContext(options);
        var kullanici = new Kullanici
        {
            AdSoyad = "Kilit Test", BasHarf = "KT", Email = email,
            SifreHash = BCrypt.Net.BCrypt.HashPassword(Parola, workFactor: 4), RolId = SistemRolleri.AdminId
        };
        db.Kullanicilar.Add(kullanici);
        await db.SaveChangesAsync();
        return kullanici.Id;
    }

    private static async Task KilitKurAsync(DbContextOptions<AppDbContext> options, int id, int sayac, DateTime kilit)
    {
        await using var db = new AppDbContext(options);
        await db.Kullanicilar.Where(k => k.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(k => k.BasarisizGirisSayisi, sayac)
            .SetProperty(k => k.GirisKilitBitisTarihi, kilit));
    }

    private static DateTime SaniyeyeYuvarla(DateTime zaman) => new(zaman.Ticks - zaman.Ticks % TimeSpan.TicksPerSecond);

    /// <summary>Kullanıcı okunduktan hemen sonra, ayrı bağlantıdan, başka bir isteğin kurduğu kilidi taklit eder.</summary>
    private sealed class OkumadanSonraKilitKuran(DbContextOptions<AppDbContext> options, int kullaniciId, DateTime kilit)
        : DbCommandInterceptor
    {
        public bool KilitKuruldu { get; private set; }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!KilitKuruldu && command.CommandText.Contains("FROM \"Kullanicilar\""))
            {
                KilitKuruldu = true;
                await KilitKurAsync(options, kullaniciId, GirisKilidiKurali.EsikDenemeSayisi, kilit);
            }
            return result;
        }
    }

    private static async Task<(int Sayac, DateTime? Kilit)> DurumAsync(DbContextOptions<AppDbContext> options, int id)
    {
        await using var db = new AppDbContext(options);
        var k = await db.Kullanicilar.AsNoTracking().SingleAsync(x => x.Id == id);
        return (k.BasarisizGirisSayisi, k.GirisKilitBitisTarihi);
    }

    private sealed class SabitAuthService(GirisDogrulamaSonucu sonuc) : IAuthService
    {
        public bool AccessTokenUretildi { get; private set; }

        public Task<GirisDogrulamaSonucu> ValidateCredentialsAsync(string email, string sifre, CancellationToken cancellationToken = default) =>
            Task.FromResult(sonuc);

        public string GenerateAccessToken(Kullanici kullanici, bool ikiFaktorDogrulandi)
        {
            AccessTokenUretildi = true;
            return "access-token";
        }

        public Task<Kullanici?> GetKullaniciByIdAsync(int kullaniciId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Kullanici> RegisterAsync(string adSoyad, string email, string sifre, int rolId) => throw new NotSupportedException();
        public Task<Kullanici?> GetKullaniciByEmailAsync(string email) => throw new NotSupportedException();
        public string GenerateBasHarf(string adSoyad) => throw new NotSupportedException();
        public string HashPassword(string plainPassword) => throw new NotSupportedException();
        public Task<string> RefreshTokenAsync(int userId, bool ikiFaktorDogrulandi, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>Başarısız girişte 2FA servisine hiç gidilmez; her çağrı testi düşürür.</summary>
    public class KullanilmayanServis : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"Başarısız girişte {targetMethod?.Name} çağrılmamalı.");
    }
}
