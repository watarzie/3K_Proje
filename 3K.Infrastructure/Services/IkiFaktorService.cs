using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Services
{
    /// <summary>
    /// RFC 6238 uyumlu TOTP, şifreli authenticator secret'ı, hash'li opaque
    /// challenge ve tek kullanımlık kurtarma kodu uygulaması.
    /// </summary>
    public sealed class IkiFaktorService : IIkiFaktorService
    {
        private const int TalepSuresiDakika = 5;
        private const int AzamiBasarisizDeneme = 5;
        private const int KurtarmaKoduAdedi = 10;
        private const int TotpAdimSaniye = 30;
        private const int TotpHaneSayisi = 6;
        private const int TalepTokenBaytSayisi = 32;
        private const int TotpSecretBaytSayisi = 20;
        private const int TalepAdvisoryLockNamespace = 860566086; // ASCII: 3K2F

        private readonly AppDbContext _context;
        private readonly IDataProtector _secretProtector;
        private readonly IConfiguration _configuration;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<IkiFaktorService> _logger;

        public IkiFaktorService(
            AppDbContext context,
            IDataProtectionProvider dataProtectionProvider,
            IConfiguration configuration,
            TimeProvider timeProvider,
            ILogger<IkiFaktorService> logger)
        {
            _context = context;
            _secretProtector = dataProtectionProvider.CreateProtector(
                "3K.IkiFaktor.TotpSecret.v1");
            _configuration = configuration;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public Task<bool> AyarEtkinMiAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default)
        {
            return _context.KullaniciIkiFaktorAyarlari
                .AsNoTracking()
                .AnyAsync(
                    x => x.KullaniciId == kullaniciId && x.EtkinMi,
                    cancellationToken);
        }

        public async Task<IReadOnlyDictionary<int, IkiFaktorAyarDurumu>> AyarDurumlariniGetirAsync(
            IReadOnlyCollection<int> kullaniciIdleri,
            CancellationToken cancellationToken = default)
        {
            if (kullaniciIdleri.Count == 0)
                return new Dictionary<int, IkiFaktorAyarDurumu>();

            var idler = kullaniciIdleri.Distinct().ToArray();
            var ayarlar = await _context.KullaniciIkiFaktorAyarlari
                .AsNoTracking()
                .Where(x => idler.Contains(x.KullaniciId))
                .Select(x => new
                {
                    x.KullaniciId,
                    x.EtkinMi,
                    x.DogrulandiTarihiUtc
                })
                .ToListAsync(cancellationToken);

            return ayarlar.ToDictionary(
                x => x.KullaniciId,
                x => new IkiFaktorAyarDurumu(
                    x.EtkinMi,
                    x.DogrulandiTarihiUtc.HasValue
                        ? UtcNormalize(x.DogrulandiTarihiUtc.Value)
                        : null));
        }

        public async Task<IkiFaktorTalepSonucu> TalepOlusturAsync(
            int kullaniciId,
            IkiFaktorTalepAmaci amac,
            bool beniHatirla,
            CancellationToken cancellationToken = default)
        {
            var now = UtcNow();
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Aynı kullanıcı için iki paralel parola isteğinin iki aktif
            // challenge bırakmasını önler. Lock transaction sonunda otomatik
            // serbest kalır ve yalnız bu kullanıcı kimliği için sıralama yapar.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({TalepAdvisoryLockNamespace}, {kullaniciId})",
                cancellationToken);

            // Kısa ömürlü challenge kayıtlarının sınırsız büyümesini engelle.
            // Yedi günlük pencere güvenlik/operasyon incelemesi için yeterli
            // izi bırakırken aktif kullanıcıların eski taleplerini temizler.
            var temizlikEsigi = now.AddDays(-7);
            await _context.IkiFaktorGirisTalepleri
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.SonKullanmaTarihiUtc < temizlikEsigi)
                .ExecuteDeleteAsync(cancellationToken);

            // Aynı kullanıcı için daha önce açılmış talepler artık kullanılamaz.
            await _context.IkiFaktorGirisTalepleri
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.TuketildiTarihiUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.TuketildiTarihiUtc,
                        now),
                    cancellationToken);

            var rawToken = GuvenliTokenOlustur();
            var talep = new IkiFaktorGirisTalebi
            {
                Id = Guid.NewGuid(),
                TokenHash = Hash(rawToken),
                KullaniciId = kullaniciId,
                Amac = amac,
                SonKullanmaTarihiUtc = now.AddMinutes(TalepSuresiDakika),
                BasarisizDenemeSayisi = 0,
                BeniHatirla = beniHatirla
            };

            await _context.IkiFaktorGirisTalepleri.AddAsync(talep, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new IkiFaktorTalepSonucu(
                rawToken,
                TalepSuresiDakika * 60);
        }

        public async Task<IkiFaktorKurulumSonucu> KurulumuBaslatAsync(
            string talepTokeni,
            CancellationToken cancellationToken = default)
        {
            var kontrol = await TalebiKontrolEtAsync(
                talepTokeni,
                IkiFaktorTalepAmaci.Kurulum,
                cancellationToken);
            if (kontrol.HataKodu != IkiFaktorHataKodu.Yok)
                return new IkiFaktorKurulumSonucu(false, kontrol.HataKodu);

            var talep = kontrol.Talep!;
            var kullanici = await _context.Kullanicilar
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == talep.KullaniciId, cancellationToken);
            if (kullanici == null)
                return new IkiFaktorKurulumSonucu(false, IkiFaktorHataKodu.KullaniciBulunamadi);

            string secret;
            await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT pg_advisory_xact_lock({TalepAdvisoryLockNamespace}, {talep.KullaniciId})",
                    cancellationToken);

                // Lock beklerken yeni bir login aynı challenge'ı iptal etmiş
                // olabilir; secret üretmeden önce talebi yeniden doğrula.
                var kilitSonrasiKontrol = await TalebiKontrolEtAsync(
                    talepTokeni,
                    IkiFaktorTalepAmaci.Kurulum,
                    cancellationToken);
                if (kilitSonrasiKontrol.HataKodu != IkiFaktorHataKodu.Yok)
                {
                    return new IkiFaktorKurulumSonucu(
                        false,
                        kilitSonrasiKontrol.HataKodu);
                }

                var ayar = await _context.KullaniciIkiFaktorAyarlari
                    .FirstOrDefaultAsync(
                        x => x.KullaniciId == talep.KullaniciId,
                        cancellationToken);
                if (ayar?.EtkinMi == true)
                {
                    return new IkiFaktorKurulumSonucu(
                        false,
                        IkiFaktorHataKodu.KurulumZatenTamamlanmis);
                }

                if (ayar == null)
                {
                    secret = YeniTotpSecretOlustur();
                    ayar = new KullaniciIkiFaktorAyari
                    {
                        KullaniciId = talep.KullaniciId,
                        SifreliGizliAnahtar = SecretSifrele(secret, talep.KullaniciId),
                        EtkinMi = false
                    };
                    await _context.KullaniciIkiFaktorAyarlari.AddAsync(ayar, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    secret = SecretCoz(ayar.SifreliGizliAnahtar, ayar.KullaniciId);
                }

                await transaction.CommitAsync(cancellationToken);
            }

            var otpauthUri = OtpauthUriOlustur(kullanici.Email, secret);
            var expiresInSeconds = Math.Max(
                1,
                (int)Math.Ceiling((UtcNormalize(talep.SonKullanmaTarihiUtc) - UtcNow()).TotalSeconds));

            return new IkiFaktorKurulumSonucu(
                true,
                IkiFaktorHataKodu.Yok,
                talepTokeni,
                expiresInSeconds,
                QrKodDataUriOlustur(otpauthUri),
                secret);
        }

        public async Task<IkiFaktorDogrulamaSonucu> KurulumuDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default)
        {
            var kontrol = await TalebiKontrolEtAsync(
                talepTokeni,
                IkiFaktorTalepAmaci.Kurulum,
                cancellationToken);
            if (kontrol.HataKodu != IkiFaktorHataKodu.Yok)
                return DogrulamaHatasi(kontrol.HataKodu);

            var talep = kontrol.Talep!;
            var ayar = await _context.KullaniciIkiFaktorAyarlari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.KullaniciId == talep.KullaniciId, cancellationToken);
            if (ayar == null)
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);
            if (ayar.EtkinMi)
                return DogrulamaHatasi(IkiFaktorHataKodu.KurulumZatenTamamlanmis);

            var secret = SecretCoz(ayar.SifreliGizliAnahtar, ayar.KullaniciId);
            if (!TotpDogrula(secret, kod, out var eslesenAdim))
            {
                return await BasarisizDenemeyiKaydetAsync(
                    talep.Id,
                    IkiFaktorHataKodu.GecersizKod,
                    cancellationToken);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var now = UtcNow();
            var ayarGuncellendi = await _context.KullaniciIkiFaktorAyarlari
                .Where(x => x.KullaniciId == talep.KullaniciId && !x.EtkinMi)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.EtkinMi, true)
                        .SetProperty(x => x.DogrulandiTarihiUtc, now)
                        .SetProperty(x => x.SonKullanilanTotpAdimi, eslesenAdim),
                    cancellationToken);

            if (ayarGuncellendi != 1 ||
                !await TalebiAtomikTuketAsync(talep.Id, now, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);
            }

            await _context.IkiFaktorKurtarmaKodlari
                .Where(x => x.KullaniciId == talep.KullaniciId)
                .ExecuteDeleteAsync(cancellationToken);

            var kurtarmaKodlari = KurtarmaKodlariOlustur();
            await _context.IkiFaktorKurtarmaKodlari.AddRangeAsync(
                kurtarmaKodlari.Select(kodMetni => new IkiFaktorKurtarmaKodu
                {
                    KullaniciId = talep.KullaniciId,
                    KodHash = Hash(KurtarmaKodunuNormalizeEt(kodMetni))
                }),
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Kullanıcı {KullaniciId} iki faktörlü doğrulama kurulumunu tamamladı.",
                talep.KullaniciId);

            return new IkiFaktorDogrulamaSonucu(
                true,
                IkiFaktorHataKodu.Yok,
                talep.KullaniciId,
                talep.BeniHatirla,
                kurtarmaKodlari);
        }

        public async Task<IkiFaktorDogrulamaSonucu> GirisiDogrulaAsync(
            string talepTokeni,
            string kod,
            CancellationToken cancellationToken = default)
        {
            var kontrol = await TalebiKontrolEtAsync(
                talepTokeni,
                IkiFaktorTalepAmaci.Giris,
                cancellationToken);
            if (kontrol.HataKodu != IkiFaktorHataKodu.Yok)
                return DogrulamaHatasi(kontrol.HataKodu);

            var talep = kontrol.Talep!;
            var ayar = await _context.KullaniciIkiFaktorAyarlari
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.KullaniciId == talep.KullaniciId && x.EtkinMi,
                    cancellationToken);
            if (ayar == null)
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);

            var secret = SecretCoz(ayar.SifreliGizliAnahtar, ayar.KullaniciId);
            if (!TotpDogrula(secret, kod, out var eslesenAdim))
            {
                return await BasarisizDenemeyiKaydetAsync(
                    talep.Id,
                    IkiFaktorHataKodu.GecersizKod,
                    cancellationToken);
            }

            if (ayar.SonKullanilanTotpAdimi.HasValue &&
                eslesenAdim <= ayar.SonKullanilanTotpAdimi.Value)
            {
                return await BasarisizDenemeyiKaydetAsync(
                    talep.Id,
                    IkiFaktorHataKodu.TekrarKullanilanKod,
                    cancellationToken);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();

            // Aynı kullanıcı için iki farklı challenge eşzamanlı doğrulansa bile
            // aynı TOTP zaman adımı yalnız bir kez kabul edilir.
            var adimKaydedildi = await _context.KullaniciIkiFaktorAyarlari
                .Where(x =>
                    x.KullaniciId == talep.KullaniciId &&
                    x.EtkinMi &&
                    (x.SonKullanilanTotpAdimi == null ||
                     x.SonKullanilanTotpAdimi < eslesenAdim))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        x => x.SonKullanilanTotpAdimi,
                        eslesenAdim),
                    cancellationToken);

            if (adimKaydedildi != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                // Eşzamanlı başka bir istek aynı zaman adımını kaydetti. Bu
                // transaction dispose olmadan yeni bir SQL komutu çalıştırma;
                // istek doğrudan replay olarak reddedilir.
                return DogrulamaHatasi(IkiFaktorHataKodu.TekrarKullanilanKod);
            }

            if (!await TalebiAtomikTuketAsync(talep.Id, now, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);
            }

            await transaction.CommitAsync(cancellationToken);
            return new IkiFaktorDogrulamaSonucu(
                true,
                IkiFaktorHataKodu.Yok,
                talep.KullaniciId,
                talep.BeniHatirla);
        }

        public async Task<IkiFaktorDogrulamaSonucu> KurtarmaKoduylaGirisiDogrulaAsync(
            string talepTokeni,
            string kurtarmaKodu,
            CancellationToken cancellationToken = default)
        {
            var kontrol = await TalebiKontrolEtAsync(
                talepTokeni,
                IkiFaktorTalepAmaci.Giris,
                cancellationToken);
            if (kontrol.HataKodu != IkiFaktorHataKodu.Yok)
                return DogrulamaHatasi(kontrol.HataKodu);

            var talep = kontrol.Talep!;
            var kodHash = Hash(KurtarmaKodunuNormalizeEt(kurtarmaKodu));
            var kurtarmaKoduKaydi = await _context.IkiFaktorKurtarmaKodlari
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.KullaniciId == talep.KullaniciId &&
                         x.KodHash == kodHash &&
                         x.KullanildiTarihiUtc == null,
                    cancellationToken);

            if (kurtarmaKoduKaydi == null)
            {
                return await BasarisizDenemeyiKaydetAsync(
                    talep.Id,
                    IkiFaktorHataKodu.GecersizKod,
                    cancellationToken);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();
            var kodTuketildi = await _context.IkiFaktorKurtarmaKodlari
                .Where(x => x.Id == kurtarmaKoduKaydi.Id && x.KullanildiTarihiUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.KullanildiTarihiUtc, now),
                    cancellationToken);

            if (kodTuketildi != 1 ||
                !await TalebiAtomikTuketAsync(talep.Id, now, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogWarning(
                "Kullanıcı {KullaniciId} tek kullanımlık 2FA kurtarma koduyla giriş yaptı.",
                talep.KullaniciId);
            return new IkiFaktorDogrulamaSonucu(
                true,
                IkiFaktorHataKodu.Yok,
                talep.KullaniciId,
                talep.BeniHatirla);
        }

        public async Task<bool> SifirlaAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default)
        {
            var kullaniciVar = await _context.Kullanicilar
                .AsNoTracking()
                .AnyAsync(x => x.Id == kullaniciId, cancellationToken);
            if (!kullaniciVar)
                return false;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var now = UtcNow();

            await _context.IkiFaktorGirisTalepleri
                .Where(x => x.KullaniciId == kullaniciId && x.TuketildiTarihiUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.TuketildiTarihiUtc, now),
                    cancellationToken);
            await _context.IkiFaktorKurtarmaKodlari
                .Where(x => x.KullaniciId == kullaniciId)
                .ExecuteDeleteAsync(cancellationToken);
            await _context.KullaniciIkiFaktorAyarlari
                .Where(x => x.KullaniciId == kullaniciId)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            _logger.LogWarning(
                "Kullanıcı {KullaniciId} için iki faktörlü doğrulama ayarları sıfırlandı.",
                kullaniciId);
            return true;
        }

        private async Task<TalepKontrolSonucu> TalebiKontrolEtAsync(
            string talepTokeni,
            IkiFaktorTalepAmaci beklenenAmac,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(talepTokeni) || talepTokeni.Length > 200)
                return new(null, IkiFaktorHataKodu.GecersizTalep);

            var tokenHash = Hash(talepTokeni);
            var talep = await _context.IkiFaktorGirisTalepleri
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (talep == null || talep.Amac != beklenenAmac)
                return new(null, IkiFaktorHataKodu.GecersizTalep);

            var ikiFaktorHalaZorunlu = await _context.Kullanicilar
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == talep.KullaniciId && x.IkiFaktorZorunluMu,
                    cancellationToken);
            if (!ikiFaktorHalaZorunlu)
                return new(null, IkiFaktorHataKodu.GecersizTalep);

            if (talep.BasarisizDenemeSayisi >= AzamiBasarisizDeneme)
                return new(null, IkiFaktorHataKodu.DenemeLimitiAsildi);
            if (talep.TuketildiTarihiUtc.HasValue)
                return new(null, IkiFaktorHataKodu.GecersizTalep);
            if (UtcNormalize(talep.SonKullanmaTarihiUtc) <= UtcNow())
                return new(null, IkiFaktorHataKodu.SuresiDolmusTalep);

            return new(talep, IkiFaktorHataKodu.Yok);
        }

        private async Task<IkiFaktorDogrulamaSonucu> BasarisizDenemeyiKaydetAsync(
            Guid talepId,
            IkiFaktorHataKodu hataKodu,
            CancellationToken cancellationToken)
        {
            var now = UtcNow();
            await _context.IkiFaktorGirisTalepleri
                .Where(x =>
                    x.Id == talepId &&
                    x.TuketildiTarihiUtc == null &&
                    x.SonKullanmaTarihiUtc > now &&
                    x.BasarisizDenemeSayisi < AzamiBasarisizDeneme)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.BasarisizDenemeSayisi,
                            x => x.BasarisizDenemeSayisi + 1)
                        .SetProperty(
                            x => x.TuketildiTarihiUtc,
                            x => x.BasarisizDenemeSayisi >= AzamiBasarisizDeneme - 1
                                ? (DateTime?)now
                                : x.TuketildiTarihiUtc),
                    cancellationToken);

            var guncelTalep = await _context.IkiFaktorGirisTalepleri
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == talepId, cancellationToken);
            if (guncelTalep == null)
                return DogrulamaHatasi(IkiFaktorHataKodu.GecersizTalep);

            var kalan = Math.Max(
                0,
                AzamiBasarisizDeneme - guncelTalep.BasarisizDenemeSayisi);
            return guncelTalep.BasarisizDenemeSayisi >= AzamiBasarisizDeneme
                ? DogrulamaHatasi(IkiFaktorHataKodu.DenemeLimitiAsildi, 0)
                : DogrulamaHatasi(hataKodu, kalan);
        }

        private Task<bool> TalebiAtomikTuketAsync(
            Guid talepId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            return TalebiAtomikTuketInternalAsync(talepId, now, cancellationToken);
        }

        private async Task<bool> TalebiAtomikTuketInternalAsync(
            Guid talepId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var etkilenen = await _context.IkiFaktorGirisTalepleri
                .Where(x =>
                    x.Id == talepId &&
                    x.TuketildiTarihiUtc == null &&
                    x.SonKullanmaTarihiUtc > now &&
                    x.BasarisizDenemeSayisi < AzamiBasarisizDeneme)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.TuketildiTarihiUtc, now),
                    cancellationToken);
            return etkilenen == 1;
        }

        private bool TotpDogrula(string secret, string kod, out long eslesenAdim)
        {
            eslesenAdim = 0;
            var normalizeKod = new string(kod.Where(char.IsDigit).ToArray());
            if (normalizeKod.Length != TotpHaneSayisi)
                return false;

            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(
                secretBytes,
                step: TotpAdimSaniye,
                mode: OtpHashMode.Sha1,
                totpSize: TotpHaneSayisi,
                timeCorrection: null);
            return totp.VerifyTotp(
                UtcNow(),
                normalizeKod,
                out eslesenAdim,
                new VerificationWindow(previous: 1, future: 1));
        }

        private string SecretCoz(string sifreliSecret, int kullaniciId)
        {
            try
            {
                return KullaniciSecretProtector(kullaniciId).Unprotect(sifreliSecret);
            }
            catch (CryptographicException exception)
            {
                _logger.LogError(
                    exception,
                    "Kullanıcı {KullaniciId} için iki faktör secret'ı çözülemedi. Data Protection key ring kontrol edilmeli.",
                    kullaniciId);
                throw new InvalidOperationException(
                    "İki faktörlü doğrulama anahtarı çözülemedi.",
                    exception);
            }
        }

        private string SecretSifrele(string secret, int kullaniciId)
        {
            return KullaniciSecretProtector(kullaniciId).Protect(secret);
        }

        private IDataProtector KullaniciSecretProtector(int kullaniciId)
        {
            // Ciphertext başka bir kullanıcı satırına taşınırsa çözülememesi
            // için Data Protection purpose zincirine kullanıcı kimliği bağlanır.
            return _secretProtector.CreateProtector($"Kullanici:{kullaniciId}");
        }

        private string OtpauthUriOlustur(string email, string secret)
        {
            var issuer = _configuration["TwoFactor:Issuer"]?.Trim();
            if (string.IsNullOrWhiteSpace(issuer))
                issuer = "3K";

            var label = Uri.EscapeDataString($"{issuer}:{email}");
            return $"otpauth://totp/{label}" +
                   $"?secret={Uri.EscapeDataString(secret)}" +
                   $"&issuer={Uri.EscapeDataString(issuer)}" +
                   $"&algorithm=SHA1&digits={TotpHaneSayisi}&period={TotpAdimSaniye}";
        }

        private static string QrKodDataUriOlustur(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new SvgQRCode(data);
            var svg = qrCode.GetGraphic(5);
            return "data:image/svg+xml;base64," +
                   Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
        }

        private static string YeniTotpSecretOlustur()
        {
            return Base32Encoding.ToString(
                RandomNumberGenerator.GetBytes(TotpSecretBaytSayisi));
        }

        private static IReadOnlyList<string> KurtarmaKodlariOlustur()
        {
            var kodlar = new List<string>(KurtarmaKoduAdedi);
            for (var i = 0; i < KurtarmaKoduAdedi; i++)
            {
                var raw = Base32Encoding.ToString(RandomNumberGenerator.GetBytes(10));
                kodlar.Add(string.Join('-', raw.Chunk(4).Select(x => new string(x))));
            }

            return kodlar;
        }

        private static string KurtarmaKodunuNormalizeEt(string kod)
        {
            return new string(kod
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        private static string GuvenliTokenOlustur()
        {
            return Base64UrlEncode(RandomNumberGenerator.GetBytes(TalepTokenBaytSayisi));
        }

        private static string Base64UrlEncode(byte[] value)
        {
            return Convert.ToBase64String(value)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Hash(string value)
        {
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        }

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static DateTime UtcNormalize(DateTime value)
        {
            // Projede global Npgsql legacy timestamp modu açık. Bu modda
            // timestamptz değerleri host'un Local DateTime'ı olarak gelebilir.
            // Güvenlik sürelerini her koşulda gerçek UTC anına normalize et.
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private static IkiFaktorDogrulamaSonucu DogrulamaHatasi(
            IkiFaktorHataKodu hataKodu,
            int? kalanDenemeSayisi = null)
        {
            return new IkiFaktorDogrulamaSonucu(
                false,
                hataKodu,
                KalanDenemeSayisi: kalanDenemeSayisi);
        }

        private sealed record TalepKontrolSonucu(
            IkiFaktorGirisTalebi? Talep,
            IkiFaktorHataKodu HataKodu);
    }
}
