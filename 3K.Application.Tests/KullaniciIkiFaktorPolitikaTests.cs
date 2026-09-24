using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.KullaniciIslemleri.Commands;
using _3K.Application.Features.KullaniciIslemleri.Queries;
using _3K.Application.Features.KullaniciIslemleri.Validators;
using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class KullaniciIkiFaktorPolitikaTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ZorunlulukDegisikligi_YalnizKullaniciFlaginiDegistirir_EnrollmentiSilmez(
        bool ilkDeger,
        bool yeniDeger)
    {
        var dogrulandiTarihi = new DateTime(2026, 8, 15, 10, 30, 0, DateTimeKind.Utc);
        var kullanici = KullaniciOlustur(ilkDeger);
        var unitOfWork = new FakeUnitOfWork(kullanici);
        var ikiFaktor = new FakeIkiFaktorService(
            new IkiFaktorAyarDurumu(true, dogrulandiTarihi));
        var handler = new KullaniciIkiFaktorZorunluluguGuncelleCommandHandler(
            unitOfWork,
            ikiFaktor);

        var result = await handler.Handle(
            new KullaniciIkiFaktorZorunluluguGuncelleCommand
            {
                KullaniciId = kullanici.Id,
                ZorunluMu = yeniDeger
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(yeniDeger, kullanici.IkiFaktorZorunluMu);
        Assert.Equal(yeniDeger, result.Value!.IkiFaktorZorunluMu);
        Assert.True(result.Value.IkiFaktorEtkinMi);
        Assert.Equal(dogrulandiTarihi, result.Value.IkiFaktorDogrulandiTarihiUtc);
        Assert.Equal(1, unitOfWork.SaveChangesSayisi);
        Assert.False(unitOfWork.KullaniciRepository.UpdateCagrildi);
        Assert.False(ikiFaktor.SifirlaCagrildi);
    }

    [Fact]
    public async Task ZorunlulukDegisikligi_KullaniciYoksa404Doner()
    {
        var unitOfWork = new FakeUnitOfWork();
        var handler = new KullaniciIkiFaktorZorunluluguGuncelleCommandHandler(
            unitOfWork,
            new FakeIkiFaktorService());

        var result = await handler.Handle(
            new KullaniciIkiFaktorZorunluluguGuncelleCommand
            {
                KullaniciId = 999,
                ZorunluMu = true
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task ZorunlulukValidatoru_IdVeBodyFlaginiZorunluTutar()
    {
        var validator = new KullaniciIkiFaktorZorunluluguGuncelleCommandValidator();

        var result = await validator.ValidateAsync(
            new KullaniciIkiFaktorZorunluluguGuncelleCommand
            {
                KullaniciId = 0,
                ZorunluMu = null
            });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "KullaniciId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ZorunluMu");
    }

    [Fact]
    public void ZorunlulukKomutu_SabitKullaniciYonetimiYazmaYetkisineBaglidir()
    {
        var command = new KullaniciIkiFaktorZorunluluguGuncelleCommand();

        Assert.IsAssignableFrom<ISecuredRequest>(command);
        Assert.Equal("kullanicilar", ((IRequiresMenuPermission)command).RequiredMenuKod);
    }

    public static IEnumerable<object[]> KullaniciYonetimiIstekleri()
    {
        yield return new object[] { new KullaniciListeleQuery() };
        yield return new object[] { new RegisterCommand() };
        yield return new object[] { new KullaniciGuncelleCommand() };
        yield return new object[] { new KullaniciSilCommand() };
        yield return new object[] { new KullaniciSifreDegistirCommand() };
        yield return new object[] { new KullaniciIkiFaktorSifirlaCommand() };
        yield return new object[] { new KullaniciIkiFaktorZorunluluguGuncelleCommand() };
    }

    [Theory]
    [MemberData(nameof(KullaniciYonetimiIstekleri))]
    public void KullaniciYonetimiIstekleri_SabitMenuYetkisineBaglidir(object request)
    {
        Assert.IsAssignableFrom<ISecuredRequest>(request);
        var fixedPermission = Assert.IsAssignableFrom<IRequiresMenuPermission>(request);
        Assert.Equal("kullanicilar", fixedPermission.RequiredMenuKod);
    }

    [Fact]
    public async Task GenelKullaniciGuncelleme_TrackedAlanlarlaFlagiEzmez()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: true);
        var unitOfWork = new FakeUnitOfWork(kullanici);
        var handler = new KullaniciGuncelleCommandHandler(
            unitOfWork,
            new FakeIkiFaktorService());

        var result = await handler.Handle(
            new KullaniciGuncelleCommand
            {
                Id = kullanici.Id,
                AdSoyad = "Yeni İsim",
                RolId = kullanici.RolId
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(kullanici.IkiFaktorZorunluMu);
        Assert.True(result.Value!.IkiFaktorZorunluMu);
        Assert.False(unitOfWork.KullaniciRepository.UpdateCagrildi);
    }

    [Fact]
    public async Task RolAtamasi_CommitSonrasiYalnizHedefKullanicininYetkisiniYeniler()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: false);
        var unitOfWork = new FakeUnitOfWork(kullanici);
        await unitOfWork.AddRole(new Rol { Id = 2, Ad = "Operatör" });
        var events = new List<(int[] Idler, string Olay)>();
        var notifier = new RecordingNotifier((ids, olay) =>
        {
            Assert.False(unitOfWork.HasActiveTransaction);
            Assert.Equal(1, unitOfWork.SaveChangesSayisi);
            events.Add((ids.ToArray(), olay));
        });
        var handler = new KullaniciGuncelleCommandHandler(unitOfWork,
            new FakeIkiFaktorService(), new FakeYetkiService(), new OrtakUser(99), notifier);

        var changed = await unitOfWork.ExecuteInTransactionAsync(ct => handler.Handle(new()
        { Id = kullanici.Id, AdSoyad = kullanici.AdSoyad, RolId = 2 }, ct));
        Assert.True(changed.IsSuccess);
        var sent = Assert.Single(events);
        Assert.Equal([kullanici.Id], sent.Idler);
        Assert.Equal(SseOlaylari.YetkiGuncellendi, sent.Olay);

        var renamed = await handler.Handle(new()
        { Id = kullanici.Id, AdSoyad = "Yeni İsim", RolId = 2 }, default);
        Assert.True(renamed.IsSuccess);
        Assert.Single(events);
    }

    [Fact]
    public async Task RolAtamasi_SseHatasiKaydedilmisDegisikligiBasarisizGostermez()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: false);
        var unitOfWork = new FakeUnitOfWork(kullanici);
        await unitOfWork.AddRole(new Rol { Id = 2, Ad = "Operatör" });
        var notifier = new RecordingNotifier((_, _) => throw new IOException("Bağlantı koptu."));
        var handler = new KullaniciGuncelleCommandHandler(unitOfWork,
            new FakeIkiFaktorService(), new FakeYetkiService(), new OrtakUser(99), notifier);

        var result = await handler.Handle(new()
        { Id = kullanici.Id, AdSoyad = kullanici.AdSoyad, RolId = 2 }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, kullanici.RolId);
        Assert.Equal(1, unitOfWork.SaveChangesSayisi);
    }

    [Fact]
    public async Task RolAtamasi_RedHalindeYetkiOlayiGondermez()
    {
        var kullanici = KullaniciOlustur(ikiFaktorZorunluMu: false);
        var unitOfWork = new FakeUnitOfWork(kullanici);
        var events = new List<string>();
        var notifier = new RecordingNotifier((_, olay) => events.Add(olay));
        var handler = new KullaniciGuncelleCommandHandler(unitOfWork,
            new FakeIkiFaktorService(), new FakeYetkiService(allowed: false), new OrtakUser(99), notifier);

        var result = await handler.Handle(new()
        { Id = kullanici.Id, AdSoyad = kullanici.AdSoyad, RolId = 2 }, default);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesSayisi);
        Assert.Empty(events);
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
            IkiFaktorZorunluMu = ikiFaktorZorunluMu
        };
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeRepository<Kullanici> KullaniciRepository { get; }
        private readonly FakeRepository<Rol> _rolRepository;
        private readonly FakeRepository<YetkiDegisikligi> _yetkiDegisikligiRepository = new([]);
        private readonly List<Func<CancellationToken, Task>> _afterCommit = [];

        public FakeUnitOfWork(params Kullanici[] kullanicilar)
        {
            KullaniciRepository = new FakeRepository<Kullanici>(kullanicilar);
            _rolRepository = new FakeRepository<Rol>(
                kullanicilar.Select(x => x.Rol).DistinctBy(x => x.Id));
        }

        public int SaveChangesSayisi { get; private set; }
        public bool HasActiveTransaction { get; private set; }

        public Task AddRole(Rol rol) => _rolRepository.AddAsync(rol);

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            if (typeof(T) == typeof(Kullanici))
                return (IGenericRepository<T>)(object)KullaniciRepository;
            if (typeof(T) == typeof(Rol))
                return (IGenericRepository<T>)(object)_rolRepository;
            if (typeof(T) == typeof(YetkiDegisikligi))
                return (IGenericRepository<T>)(object)_yetkiDegisikligiRepository;

            throw new NotSupportedException(typeof(T).Name);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesSayisi++;
            return Task.FromResult(1);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            try
            {
                var result = await operation(cancellationToken);
                HasActiveTransaction = false;
                foreach (var callback in _afterCommit.ToArray()) await callback(CancellationToken.None);
                return result;
            }
            finally
            {
                HasActiveTransaction = false;
                _afterCommit.Clear();
            }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => _afterCommit.Add(callback);

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class FakeYetkiService(bool allowed = true) : IKullaniciYetkiService
    {
        public Task<IReadOnlyList<KullaniciYetkiModel>?> GetAsync(int kullaniciId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<KullaniciYetkiSonucu> UpdateAsync(int kullaniciId,
            IReadOnlyCollection<KullaniciYetkiKarari> kararlar, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<KullaniciYetkiSonucu> RolAtamayiDogrulaAsync(int? hedefKullaniciId, int rolId,
            CancellationToken cancellationToken = default) => Task.FromResult(
                allowed ? new KullaniciYetkiSonucu(true) : new KullaniciYetkiSonucu(false, "Reddedildi.", 403));
    }

    private sealed class RecordingNotifier(Action<IEnumerable<int>, string> onNotify) : ISseNotifier
    {
        public Task SubscribeAsync(object context, int kullaniciId) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh")
        {
            onNotify(kullaniciIdleri, eventName);
            return Task.CompletedTask;
        }
        public Task BroadcastApprovalUpdateAsync() => Task.CompletedTask;
    }

    private sealed class FakeRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items;

        public FakeRepository(IEnumerable<T> items) => _items = items.ToList();

        public bool UpdateCagrildi { get; private set; }

        public Task<T?> GetByIdAsync(int id) =>
            Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

        public Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(_items);

        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(
            Expression<Func<T, TProp>> include) =>
            Task.FromResult<IEnumerable<T>>(_items);

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(_items.AsQueryable().Where(predicate));

        public IQueryable<T> Queryable() => _items.AsQueryable();

        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity) => UpdateCagrildi = true;
        public void Remove(T entity) => _items.Remove(entity);
    }

    private sealed class FakeIkiFaktorService : IIkiFaktorService
    {
        private readonly IkiFaktorAyarDurumu? _durum;

        public FakeIkiFaktorService(IkiFaktorAyarDurumu? durum = null) => _durum = durum;

        public bool SifirlaCagrildi { get; private set; }

        public Task<bool> AyarEtkinMiAsync(
            int kullaniciId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_durum?.EtkinMi ?? false);

        public Task<IReadOnlyDictionary<int, IkiFaktorAyarDurumu>> AyarDurumlariniGetirAsync(
            IReadOnlyCollection<int> kullaniciIdleri,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyDictionary<int, IkiFaktorAyarDurumu> sonuc = _durum == null
                ? new Dictionary<int, IkiFaktorAyarDurumu>()
                : kullaniciIdleri.ToDictionary(x => x, _ => _durum);
            return Task.FromResult(sonuc);
        }

        public Task<IkiFaktorTalepSonucu> TalepOlusturAsync(
            int kullaniciId,
            IkiFaktorTalepAmaci amac,
            bool beniHatirla,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

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
            CancellationToken cancellationToken = default)
        {
            SifirlaCagrildi = true;
            return Task.FromResult(true);
        }
    }
}
