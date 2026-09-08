using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class GridSahaIsTamamlamaSenkronizasyonTests
{
    [Theory]
    [InlineData(GridDurum.Gelmedi, GridDurum.Iptal)]
    [InlineData(GridDurum.Iptal, GridDurum.Gelmedi)]
    public async Task TekliIptalVeGeriAlma_KayitSonrasiKaynakProjeyiSenkronizeEder(
        GridDurum eskiDurum, GridDurum yeniDurum)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 101, eskiDurum);

        var sonuc = await kurgu.CalistirAsync(Islem.TekliGuncelle, yeniDurum);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)yeniDurum, satir.GridDurumuId);
        Assert.Equal(new[] { 101 }, Assert.Single(kurgu.Saha.Senkronizasyonlar));
        Assert.Equal(new[] { "save", "sync" }, kurgu.Uow.Olaylar);
        Assert.Equal((int)yeniDurum, Assert.Single(kurgu.Saha.SenkronizasyondaDurumlar));
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Fact]
    public async Task TekliSifirla_IptaliKaldirirVeKaynakProjeyiTekrarHesaplatir()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, 101, GridDurum.Iptal);

        var sonuc = await kurgu.CalistirAsync(Islem.TekliSifirla);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.Gelmedi, satir.GridDurumuId);
        Assert.Equal(3, satir.KalanMiktar);
        Assert.Equal(new[] { 101 }, Assert.Single(kurgu.Saha.Senkronizasyonlar));
        Assert.Equal(new[] { "save", "sync" }, kurgu.Uow.Olaylar);
        Assert.Equal((int)GridDurum.Gelmedi, Assert.Single(kurgu.Saha.SenkronizasyondaDurumlar));
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Fact]
    public async Task TopluIptal_KaynakIdleriniTekillestirirKokSatiriSenkronizasyonaKatmaz()
    {
        using var kurgu = new Kurgu();
        kurgu.SatirEkle(1, 101);
        kurgu.SatirEkle(2, 101);
        kurgu.SatirEkle(3, 102);
        kurgu.SatirEkle(4, null);

        var sonuc = await kurgu.CalistirAsync(Islem.TopluGuncelle);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(new[] { 101, 102 }, Assert.Single(kurgu.Saha.Senkronizasyonlar));
        Assert.Equal(new[] { "save", "sync" }, kurgu.Uow.Olaylar);
        Assert.All(kurgu.Uow.Repo<CekiSatiri>().Rows, s =>
        {
            Assert.Equal((int)GridDurum.Iptal, s.GridDurumuId);
            Assert.Equal(0, s.KalanMiktar);
        });
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Fact]
    public async Task TopluSifirla_YalnizcaDegisenSatirlarinKaynaklariniSenkronizeEder()
    {
        using var kurgu = new Kurgu();
        kurgu.SatirEkle(1, 101, GridDurum.Iptal);
        kurgu.SatirEkle(2, 101, GridDurum.Iptal);
        var zatenSifir = kurgu.SatirEkle(3, 102, GridDurum.Gelmedi);
        var ucKIslemli = kurgu.SatirEkle(4, 103, GridDurum.Iptal);
        ucKIslemli.GelenMiktar = 1;
        kurgu.SatirEkle(5, null, GridDurum.Iptal);

        var sonuc = await kurgu.CalistirAsync(Islem.TopluSifirla);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(new[] { 101 }, Assert.Single(kurgu.Saha.Senkronizasyonlar));
        Assert.Equal(new[] { "save", "sync" }, kurgu.Uow.Olaylar);
        Assert.DoesNotContain(zatenSifir.Id, kurgu.Uow.Repo<CekiSatiri>().GuncellenenIdler);
        Assert.DoesNotContain(ucKIslemli.Id, kurgu.Uow.Repo<CekiSatiri>().GuncellenenIdler);
        Assert.Equal((int)GridDurum.Iptal, ucKIslemli.GridDurumuId);
        Assert.Equal(1, ucKIslemli.GelenMiktar);
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Fact]
    public async Task TopluTamGeldi_UcKBlokajiNedeniyleAtlananKaynakSenkronizeEdilmez()
    {
        using var kurgu = new Kurgu();
        var degisen = kurgu.SatirEkle(1, 101);
        var bloke = kurgu.SatirEkle(2, 102);
        bloke.UcKDurumuId = (int)UcKDurum.TamGeldi;

        var sonuc = await kurgu.CalistirAsync(Islem.TopluGuncelle, GridDurum.TamGeldi);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(new[] { 101 }, Assert.Single(kurgu.Saha.Senkronizasyonlar));
        Assert.Equal((int)GridDurum.TamGeldi, degisen.GridDurumuId);
        Assert.Equal((int)GridDurum.Gelmedi, bloke.GridDurumuId);
        Assert.DoesNotContain(bloke.Id, kurgu.Uow.Repo<CekiSatiri>().GuncellenenIdler);
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Theory]
    [InlineData(Islem.TekliGuncelle)]
    [InlineData(Islem.TekliSifirla)]
    [InlineData(Islem.TopluGuncelle)]
    [InlineData(Islem.TopluSifirla)]
    public async Task KaynagiOlmayanNormalSatir_GereksizSenkronizasyonBaslatmaz(Islem islem)
    {
        using var kurgu = new Kurgu();
        kurgu.SatirEkle(1, null, GridDurum.Iptal);

        var sonuc = await kurgu.CalistirAsync(islem);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Empty(kurgu.Saha.Senkronizasyonlar);
        Assert.Equal(new[] { "save" }, kurgu.Uow.Olaylar);
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Theory]
    [InlineData(Islem.TekliGuncelle, false)]
    [InlineData(Islem.TekliSifirla, false)]
    [InlineData(Islem.TopluGuncelle, false)]
    [InlineData(Islem.TopluSifirla, false)]
    [InlineData(Islem.TekliGuncelle, true)]
    [InlineData(Islem.TekliSifirla, true)]
    [InlineData(Islem.TopluGuncelle, true)]
    [InlineData(Islem.TopluSifirla, true)]
    public async Task SevkKilidiVeyaAktifSahaKaynakBlokaji_SenkronizasyonIcinAsilmaz(
        Islem islem, bool aktifSahaKaynak)
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(1, aktifSahaKaynak ? null : 101, GridDurum.Iptal);
        if (aktifSahaKaynak)
            kurgu.Saha.AktifKaynakIdler.Add(satir.Id);
        else
            kurgu.Uow.Repo<Sandik>().Rows.Single().DurumId = (int)SandikDurum.Sevkedildi;

        var sonuc = await kurgu.CalistirAsync(islem);

        Assert.False(sonuc.IsSuccess);
        Assert.Empty(kurgu.Saha.Senkronizasyonlar);
        Assert.Empty(kurgu.Uow.Olaylar);
        Assert.Empty(kurgu.Uow.Repo<CekiSatiri>().GuncellenenIdler);
        Assert.Equal((int)GridDurum.Iptal, satir.GridDurumuId);
        kurgu.FizikselMiktarlarKorunduMu();
    }

    [Theory]
    [InlineData(Islem.TekliSifirla)]
    [InlineData(Islem.TopluSifirla)]
    public async Task ZatenSifirlanmisSatir_KaynakProjeyiGereksizSenkronizeEtmez(Islem islem)
    {
        using var kurgu = new Kurgu();
        kurgu.SatirEkle(1, 101, GridDurum.Gelmedi);

        var sonuc = await kurgu.CalistirAsync(islem);

        Assert.False(sonuc.IsSuccess);
        Assert.Empty(kurgu.Saha.Senkronizasyonlar);
        Assert.Empty(kurgu.Uow.Olaylar);
        kurgu.FizikselMiktarlarKorunduMu();
    }

    public enum Islem { TekliGuncelle, TekliSifirla, TopluGuncelle, TopluSifirla }

    private sealed class Kurgu : IDisposable
    {
        public TestUnitOfWork Uow { get; } = new();
        public SahaStub Saha { get; }
        private readonly CancellationTokenSource _cts = new();
        private readonly DurumHesaplaService _durum = new();
        private readonly CurrentUserStub _user = new();
        private readonly HareketStub _hareket = new();

        public Kurgu()
        {
            Saha = new SahaStub(Uow, _cts.Token);
            Uow.Repo<Ceki>().Rows.Add(new Ceki { Id = 10, ProjeId = 20 });
            Uow.Repo<Sandik>().Rows.Add(new Sandik
            {
                Id = 30, ProjeId = 20, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor
            });
        }

        public CekiSatiri SatirEkle(int id, int? kaynakId, GridDurum durum = GridDurum.Gelmedi)
        {
            var satir = new CekiSatiri
            {
                Id = id, SiraNo = id, CekiId = 10, KaynakCekiSatiriId = kaynakId,
                IstenenAdet = 3, GridDurumuId = (int)durum, CekideGecenSandikNo = "1"
            };
            Uow.Repo<CekiSatiri>().Rows.Add(satir);
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = 1000 + id, SandikId = 30, CekiSatiriId = id,
                TahsisMiktari = 3, KonulanAdet = 1.25m, EksikAdet = 1.75m
            });
            return satir;
        }

        public Task<Result> CalistirAsync(Islem islem, GridDurum yeniDurum = GridDurum.Iptal)
        {
            var idler = Uow.Repo<CekiSatiri>().Rows.Select(s => s.Id).ToList();
            return islem switch
            {
                Islem.TekliGuncelle => new GridDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, new LookupStub(), Saha)
                    .Handle(new GridDurumGuncelleCommand { ProjeId = 20, CekiSatiriId = idler[0], YeniDurumId = (int)yeniDurum }, _cts.Token),
                Islem.TekliSifirla => new GridDurumSifirlaCommandHandler(Uow, _user, _durum, _hareket, Saha)
                    .Handle(new GridDurumSifirlaCommand { ProjeId = 20, CekiSatiriId = idler[0] }, _cts.Token),
                Islem.TopluGuncelle => new GridTopluDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, Saha)
                    .Handle(new GridTopluDurumGuncelleCommand { ProjeId = 20, CekiSatiriIdler = idler, HedefDurumId = (int)yeniDurum }, _cts.Token),
                Islem.TopluSifirla => new GridTopluSifirlaCommandHandler(Uow, _user, _durum, _hareket, Saha)
                    .Handle(new GridTopluSifirlaCommand { ProjeId = 20, CekiSatiriIdler = idler }, _cts.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(islem))
            };
        }

        public void FizikselMiktarlarKorunduMu()
        {
            Assert.All(Uow.Repo<SandikIcerik>().Rows, i =>
            {
                Assert.Equal(3, i.TahsisMiktari);
                Assert.Equal(1.25m, i.KonulanAdet);
                Assert.Equal(1.75m, i.EksikAdet);
            });
            Assert.Empty(Uow.Repo<SandikIcerik>().GuncellenenIdler);
        }

        public void Dispose() { _cts.Dispose(); Uow.Dispose(); }
    }

    private sealed class SahaStub(TestUnitOfWork uow, CancellationToken beklenenToken) : ISahaTamamlamaService
    {
        public HashSet<int> AktifKaynakIdler { get; } = [];
        public List<int[]> Senkronizasyonlar { get; } = [];
        public List<int> SenkronizasyondaDurumlar { get; } = [];
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            Assert.Equal(beklenenToken, cancellationToken);
            Assert.True(uow.HasActiveTransaction);
            Assert.Equal("save", uow.Olaylar.Last());
            var kaynakIdler = ids.ToArray();
            Assert.Equal(kaynakIdler.Length, kaynakIdler.Distinct().Count());
            Senkronizasyonlar.Add(kaynakIdler.Order().ToArray());
            SenkronizasyondaDurumlar.AddRange(uow.Repo<CekiSatiri>().Rows
                .Where(s => s.KaynakCekiSatiriId.HasValue && kaynakIdler.Contains(s.KaynakCekiSatiriId.Value))
                .Select(s => s.GridDurumuId));
            uow.Olaylar.Add("sync");
            return Task.CompletedTask;
        }
        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(AktifKaynakIdler.Contains(id));
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(ids.Where(AktifKaynakIdler.Contains).ToDictionary(id => id, _ => 1m));
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = new();
        public List<string> Olaylar { get; } = [];
        public bool HasActiveTransaction { get; private set; }
        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo)) _repos.Add(typeof(T), repo = new Repo<T>());
            return (Repo<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Assert.True(HasActiveTransaction);
            Olaylar.Add("save");
            return Task.FromResult(1);
        }
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            try { return await operation(cancellationToken); }
            finally { HasActiveTransaction = false; }
        }
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public List<int> GuncellenenIdler { get; } = [];
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(r => r.Id == id));
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Rows.Where(predicate.Compile()));
        public void Update(T entity) => GuncellenenIdler.Add(entity.Id);
        public Task AddAsync(T entity) => throw new NotSupportedException();
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string tip, string id) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(int id, string? arama, int? tip, int sayfa, int boyut) => throw new NotSupportedException();
    }
}
