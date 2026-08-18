using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.Features.AuthIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class IkiFaktorLoginAkisiTests
{
    [Fact]
    public async Task FlagKapaliyken_MevcutGirisGibiTamTokenDoner()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: false);
        var auth = new FakeAuthService(kullanici);
        var ikiFaktor = new FakeIkiFaktorService(ayarEtkin: false);
        var handler = new LoginCommandHandler(auth, ikiFaktor);

        var result = await handler.Handle(
            new LoginCommand { Email = kullanici.Email, Sifre = "secret" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoginNextSteps.Authenticated, result.Value!.NextStep);
        Assert.Equal("access-token", result.Value.Token);
        Assert.NotNull(result.Value.Kullanici);
        Assert.False(result.Value.Kullanici.IkiFaktorZorunluMu);
        Assert.Null(result.Value.ChallengeToken);
        Assert.True(auth.AccessTokenUretildi);
        Assert.False(ikiFaktor.TalepOlusturuldu);
    }

    [Fact]
    public async Task FlagAcikVeKuruluysa_TotpOlmadanTamTokenDonmez()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: true);
        var auth = new FakeAuthService(kullanici);
        var ikiFaktor = new FakeIkiFaktorService(ayarEtkin: true);
        var handler = new LoginCommandHandler(auth, ikiFaktor);

        var result = await handler.Handle(
            new LoginCommand
            {
                Email = kullanici.Email,
                Sifre = "secret",
                BeniHatirla = true
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoginNextSteps.TwoFactorRequired, result.Value!.NextStep);
        Assert.Null(result.Value.Token);
        Assert.Null(result.Value.Kullanici);
        Assert.Equal("opaque-challenge", result.Value.ChallengeToken);
        Assert.False(auth.AccessTokenUretildi);
        Assert.Equal(IkiFaktorTalepAmaci.Giris, ikiFaktor.SonTalepAmaci);
        Assert.True(ikiFaktor.SonBeniHatirla);
    }

    [Fact]
    public async Task FlagAcikVeKurulmamissa_QrKurulumTalebiDoner()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: true);
        var auth = new FakeAuthService(kullanici);
        var ikiFaktor = new FakeIkiFaktorService(ayarEtkin: false);
        var handler = new LoginCommandHandler(auth, ikiFaktor);

        var result = await handler.Handle(
            new LoginCommand { Email = kullanici.Email, Sifre = "secret" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoginNextSteps.TwoFactorSetupRequired, result.Value!.NextStep);
        Assert.Null(result.Value.Token);
        Assert.Equal(IkiFaktorTalepAmaci.Kurulum, ikiFaktor.SonTalepAmaci);
        Assert.False(auth.AccessTokenUretildi);
    }

    [Fact]
    public async Task HataliParolada_TalepVeTokenUretilmez()
    {
        var auth = new FakeAuthService(kullanici: null);
        var ikiFaktor = new FakeIkiFaktorService(ayarEtkin: false);
        var handler = new LoginCommandHandler(auth, ikiFaktor);

        var result = await handler.Handle(
            new LoginCommand { Email = "x@example.com", Sifre = "wrong" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.StatusCode);
        Assert.False(auth.AccessTokenUretildi);
        Assert.False(ikiFaktor.TalepOlusturuldu);
    }

    private static Kullanici KullaniciOlustur(bool ikiFaktorZorunluMu)
    {
        var rol = new Rol { Id = 1, Ad = "Admin" };
        return new Kullanici
        {
            Id = 7,
            AdSoyad = "Test Kullanıcı",
            BasHarf = "TK",
            Email = "test@example.com",
            RolId = rol.Id,
            Rol = rol,
            IkiFaktorZorunluMu = ikiFaktorZorunluMu,
            SifreHash = "unused"
        };
    }

    private sealed class FakeAuthService : IAuthService
    {
        private readonly Kullanici? _kullanici;

        public FakeAuthService(Kullanici? kullanici) => _kullanici = kullanici;

        public bool AccessTokenUretildi { get; private set; }

        public Task<Kullanici?> ValidateCredentialsAsync(
            string email,
            string sifre,
            CancellationToken cancellationToken = default) => Task.FromResult(_kullanici);

        public Task<Kullanici?> GetKullaniciByIdAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default) => Task.FromResult(_kullanici);

        public string GenerateAccessToken(Kullanici kullanici, bool ikiFaktorDogrulandi)
        {
            AccessTokenUretildi = true;
            return "access-token";
        }

        public Task<Kullanici> RegisterAsync(string adSoyad, string email, string sifre, int rolId) =>
            throw new NotSupportedException();

        public Task<Kullanici?> GetKullaniciByEmailAsync(string email) => Task.FromResult(_kullanici);
        public string GenerateBasHarf(string adSoyad) => "TK";
        public string HashPassword(string plainPassword) => plainPassword;

        public Task<string> RefreshTokenAsync(
            int userId,
            bool ikiFaktorDogrulandi,
            CancellationToken cancellationToken = default) => Task.FromResult("access-token");
    }

    private sealed class FakeIkiFaktorService : IIkiFaktorService
    {
        private readonly bool _ayarEtkin;

        public FakeIkiFaktorService(bool ayarEtkin) => _ayarEtkin = ayarEtkin;

        public bool TalepOlusturuldu { get; private set; }
        public IkiFaktorTalepAmaci? SonTalepAmaci { get; private set; }
        public bool SonBeniHatirla { get; private set; }

        public Task<bool> AyarEtkinMiAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default) => Task.FromResult(_ayarEtkin);

        public Task<IReadOnlyDictionary<int, IkiFaktorAyarDurumu>> AyarDurumlariniGetirAsync(
            IReadOnlyCollection<int> kullaniciIdleri,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<int, IkiFaktorAyarDurumu> sonuc = kullaniciIdleri
                .ToDictionary(
                    id => id,
                    _ => new IkiFaktorAyarDurumu(_ayarEtkin, _ayarEtkin ? DateTime.UtcNow : null));
            return Task.FromResult(sonuc);
        }

        public Task<IkiFaktorTalepSonucu> TalepOlusturAsync(
            int kullaniciId,
            IkiFaktorTalepAmaci amac,
            bool beniHatirla,
            CancellationToken cancellationToken = default)
        {
            TalepOlusturuldu = true;
            SonTalepAmaci = amac;
            SonBeniHatirla = beniHatirla;
            return Task.FromResult(new IkiFaktorTalepSonucu("opaque-challenge", 300));
        }

        public Task<IkiFaktorKurulumSonucu> KurulumuBaslatAsync(
            string talepTokeni,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IkiFaktorDogrulamaSonucu> KurulumuDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IkiFaktorDogrulamaSonucu> GirisiDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IkiFaktorDogrulamaSonucu> KurtarmaKoduylaGirisiDogrulaAsync(
            string talepTokeni,
            string kurtarmaKodu,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> SifirlaAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
