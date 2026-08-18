using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.KullaniciIslemleri.Commands;
using _3K.Application.Features.KullaniciIslemleri.Queries;
using _3K.Application.Features.KullaniciIslemleri.Validators;
using _3K.Application.Features.AuthIslemleri.Commands;
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

        public FakeUnitOfWork(params Kullanici[] kullanicilar)
        {
            KullaniciRepository = new FakeRepository<Kullanici>(kullanicilar);
            _rolRepository = new FakeRepository<Rol>(
                kullanicilar.Select(x => x.Rol).DistinctBy(x => x.Id));
        }

        public int SaveChangesSayisi { get; private set; }
        public bool HasActiveTransaction => false;

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            if (typeof(T) == typeof(Kullanici))
                return (IGenericRepository<T>)(object)KullaniciRepository;
            if (typeof(T) == typeof(Rol))
                return (IGenericRepository<T>)(object)_rolRepository;

            throw new NotSupportedException(typeof(T).Name);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesSayisi++;
            return Task.FromResult(1);
        }

        public Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) =>
            throw new NotSupportedException();

        public void Dispose()
        {
        }
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
