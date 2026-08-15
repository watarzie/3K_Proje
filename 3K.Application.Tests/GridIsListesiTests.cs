using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;

namespace _3K.Application.Tests;

public sealed class GridIsListesiTests
{
    [Fact]
    public void Query_SabitGridIsListesiYetkisineBaglidir()
    {
        var query = new GetGridIsListesiQuery();

        var securedQuery = Assert.IsAssignableFrom<ISecuredRequest>(query);
        Assert.NotNull(securedQuery);
        var fixedPermission = Assert.IsAssignableFrom<IRequiresMenuPermission>(query);
        Assert.Equal("grid-is-listesi", fixedPermission.RequiredMenuKod);
    }

    [Fact]
    public void ExplicitYenidenSevk_StateVeMiktarBirlikteyse_YenidenOlur()
    {
        var sonuc = Siniflandir(
            gridSevkDurumu: GridSevkDurum.YenidenSevkGerekli,
            yenidenSevkGerekliAdet: 3,
            kalanMiktar: 3);

        Assert.NotNull(sonuc);
        Assert.Equal(GridIsListesiSiniflandirma.TipYeniden, sonuc.IsTipi);
        Assert.Equal(1, sonuc.Oncelik);
    }

    [Theory]
    [InlineData(GridSevkDurum.YenidenSevkGerekli, 0)]
    [InlineData(GridSevkDurum.SevkEdildi, 3)]
    public void ExplicitYenidenSevk_StateVeyaMiktarTekBasinaIse_PhantomIsOlusmaz(
        GridSevkDurum gridSevkDurumu,
        decimal yenidenSevkGerekliAdet)
    {
        var sonuc = Siniflandir(
            gridSevkDurumu: gridSevkDurumu,
            yenidenSevkGerekliAdet: yenidenSevkGerekliAdet,
            kalanMiktar: 3);

        Assert.Null(sonuc);
    }

    [Fact]
    public void ProjeTransferTelafisi_MevcutGridAksiyonuylaAyniSekilde_YenidenOlur()
    {
        var sonuc = Siniflandir(
            gridSevkDurumu: GridSevkDurum.SevkEdildi,
            gridSevkMiktari: 10,
            projeGonderilen: 4,
            kalanMiktar: 4);

        Assert.NotNull(sonuc);
        Assert.Equal(GridIsListesiSiniflandirma.TipYeniden, sonuc.IsTipi);
        Assert.Equal(1, sonuc.Oncelik);
    }

    [Theory]
    [InlineData(GridSevkDurum.SevkEdilmedi, 0)]
    [InlineData(GridSevkDurum.SevkEdildi, 6)]
    public void EksikGeldi_IlkVeyaParcaliSevkAsamasinda_EksikOlur(
        GridSevkDurum gridSevkDurumu,
        decimal gridSevkMiktari)
    {
        var sonuc = Siniflandir(
            gridDurumu: GridDurum.EksikGeldi,
            gridSevkDurumu: gridSevkDurumu,
            gridSevkMiktari: gridSevkMiktari,
            gridEksikMiktar: 4,
            kalanMiktar: 4);

        Assert.NotNull(sonuc);
        Assert.Equal(GridIsListesiSiniflandirma.TipEksik, sonuc.IsTipi);
        Assert.Equal(2, sonuc.Oncelik);
    }

    [Fact]
    public void YenidenVeEksikKosullariCakisiyorsa_YenidenOnceliklidir()
    {
        var sonuc = Siniflandir(
            gridDurumu: GridDurum.EksikGeldi,
            gridSevkDurumu: GridSevkDurum.YenidenSevkGerekli,
            yenidenSevkGerekliAdet: 2,
            gridEksikMiktar: 4,
            kalanMiktar: 4);

        Assert.NotNull(sonuc);
        Assert.Equal(GridIsListesiSiniflandirma.TipYeniden, sonuc.IsTipi);
        Assert.Equal(1, sonuc.Oncelik);
    }

    [Fact]
    public void EksikGeldi_FarkliKaynaklaTamamlanipKalanSifirsa_PhantomIsOlusmaz()
    {
        var sonuc = Siniflandir(
            gridDurumu: GridDurum.EksikGeldi,
            gridSevkDurumu: GridSevkDurum.SevkEdildi,
            gridSevkMiktari: 6,
            gridEksikMiktar: 4,
            kalanMiktar: 0);

        Assert.Null(sonuc);
    }

    [Theory]
    [InlineData(GridDurum.TamGeldi)]
    [InlineData(GridDurum.Gelmedi)]
    [InlineData(GridDurum.TrafoSevk)]
    [InlineData(GridDurum.Iptal)]
    [InlineData(GridDurum.GridKapandi)]
    public void ExplicitYenidenVeyaTransferYokken_DigerGridDurumlariListeyeGirmez(GridDurum gridDurumu)
    {
        var sonuc = Siniflandir(
            gridDurumu: gridDurumu,
            gridSevkDurumu: GridSevkDurum.SevkEdilmedi,
            gridEksikMiktar: 4,
            kalanMiktar: 4);

        Assert.Null(sonuc);
    }

    [Fact]
    public async Task Sayfalama_SatirDegilProjeBazindaYapilir()
    {
        var enYeni = DateTime.UtcNow.AddMinutes(-1);
        var eski = DateTime.UtcNow.AddDays(-1);
        var satirlar = new[]
        {
            YeniEksikSatir(101, 1, "PA-001", ProjeTipi.Normal, enYeni),
            YeniEksikSatir(102, 1, "PA-001", ProjeTipi.Normal, enYeni.AddMinutes(-1)),
            YeniEksikSatir(201, 2, "PA-002-SAHA", ProjeTipi.Saha, eski)
        };
        var handler = new GetGridIsListesiQueryHandler(
            new TestUnitOfWork(satirlar),
            new TestLookupCacheService());

        var ilkSayfa = await handler.Handle(
            new GetGridIsListesiQuery { Page = 1, PageSize = 1 },
            CancellationToken.None);
        var ikinciSayfa = await handler.Handle(
            new GetGridIsListesiQuery { Page = 2, PageSize = 1 },
            CancellationToken.None);

        Assert.True(ilkSayfa.IsSuccess);
        Assert.NotNull(ilkSayfa.Value);
        Assert.Equal(2, ilkSayfa.Value.Liste.TotalCount);
        Assert.True(ilkSayfa.Value.Liste.HasMore);
        Assert.Equal(2, ilkSayfa.Value.Liste.Items.Count);
        Assert.All(ilkSayfa.Value.Liste.Items, item => Assert.Equal(1, item.ProjeId));
        Assert.All(ilkSayfa.Value.Liste.Items, item => Assert.Equal((int)ProjeTipi.Normal, item.ProjeTipiId));

        Assert.True(ikinciSayfa.IsSuccess);
        Assert.NotNull(ikinciSayfa.Value);
        var ikinciSayfaSatiri = Assert.Single(ikinciSayfa.Value.Liste.Items);
        Assert.Equal(2, ikinciSayfaSatiri.ProjeId);
        Assert.Equal((int)ProjeTipi.Saha, ikinciSayfaSatiri.ProjeTipiId);
        Assert.False(ikinciSayfa.Value.Liste.HasMore);
    }

    [Theory]
    [InlineData(ProjeTipi.Normal)]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task NormalSahaVeYedekProjeleri_AyniListeKuralinaDahildir(ProjeTipi projeTipi)
    {
        var satir = YeniEksikSatir(101, 1, $"P-{(int)projeTipi}", projeTipi, DateTime.UtcNow);
        var handler = new GetGridIsListesiQueryHandler(
            new TestUnitOfWork(new[] { satir }),
            new TestLookupCacheService());

        var sonuc = await handler.Handle(new GetGridIsListesiQuery(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.NotNull(sonuc.Value);
        var item = Assert.Single(sonuc.Value.Liste.Items);
        Assert.Equal((int)projeTipi, item.ProjeTipiId);
        Assert.Equal(GridIsListesiSiniflandirma.TipEksik, item.IsTipi);
    }

    [Fact]
    public async Task FiiliSandikNoBosluksa_CekidekiSandikNoIleKilitKontroluYapilir()
    {
        var satir = YeniEksikSatir(101, 1, "PA-001", ProjeTipi.Normal, DateTime.UtcNow);
        satir.FiiliSandikNo = "   ";
        satir.Ceki.Proje.Sandiklar.Add(new Sandik
        {
            Id = 10,
            ProjeId = satir.Ceki.ProjeId,
            SandikNo = satir.CekideGecenSandikNo,
            DurumId = (int)SandikDurum.Sevkedildi,
            SevkiyatDuzeltmeAcikMi = false
        });
        var handler = new GetGridIsListesiQueryHandler(
            new TestUnitOfWork(new[] { satir }),
            new TestLookupCacheService());

        var sonuc = await handler.Handle(new GetGridIsListesiQuery(), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.NotNull(sonuc.Value);
        Assert.Empty(sonuc.Value.Liste.Items);
    }

    private static GridIsListesiSiniflandirmaSonucu? Siniflandir(
        GridDurum gridDurumu = GridDurum.TamGeldi,
        GridSevkDurum gridSevkDurumu = GridSevkDurum.SevkEdilmedi,
        decimal gridSevkMiktari = 0,
        decimal yenidenSevkGerekliAdet = 0,
        decimal projeGonderilen = 0,
        decimal gridEksikMiktar = 0,
        decimal kalanMiktar = 0)
    {
        return GridIsListesiSiniflandirma.Belirle(
            (int)gridDurumu,
            (int)gridSevkDurumu,
            gridSevkMiktari,
            yenidenSevkGerekliAdet,
            projeGonderilen,
            gridEksikMiktar,
            kalanMiktar);
    }

    private static CekiSatiri YeniEksikSatir(
        int satirId,
        int projeId,
        string projeNo,
        ProjeTipi projeTipi,
        DateTime updatedDate)
    {
        var proje = new Proje
        {
            Id = projeId,
            ProjeNo = projeNo,
            Musteri = "Test Musteri",
            ProjeTipiId = (int)projeTipi,
            DurumId = (int)ProjeDurum.Hazirlaniyor
        };
        var ceki = new Ceki
        {
            Id = projeId * 10,
            ProjeId = projeId,
            Proje = proje
        };

        return new CekiSatiri
        {
            Id = satirId,
            CekiId = ceki.Id,
            Ceki = ceki,
            SiraNo = satirId,
            BarkodNo = $"B-{satirId}",
            Aciklama = $"Urun {satirId}",
            CekideGecenSandikNo = "1",
            IstenenAdet = 10,
            BirimId = (int)Birim.Adet,
            GridDurumuId = (int)GridDurum.EksikGeldi,
            GridGelenAdet = 6,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
            UcKDurumuId = (int)UcKDurum.Bekliyor,
            UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
            DurumId = (int)UrunDurum.Bekliyor,
            UpdatedDate = updatedDate
        };
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly IReadOnlyList<CekiSatiri> _satirlar;

        public TestUnitOfWork(IReadOnlyList<CekiSatiri> satirlar)
        {
            _satirlar = satirlar;
        }

        public bool HasActiveTransaction => false;

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            var items = typeof(T) == typeof(CekiSatiri)
                ? _satirlar.Cast<T>()
                : Enumerable.Empty<T>();
            return new TestRepository<T>(items);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

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

    private sealed class TestRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly List<T> _items;

        public TestRepository(IEnumerable<T> items)
        {
            _items = items.ToList();
        }

        public Task<T?> GetByIdAsync(int id) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<IEnumerable<T>> GetAllAsync() =>
            Task.FromResult<IEnumerable<T>>(_items);

        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) =>
            Task.FromResult<IEnumerable<T>>(_items);

        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(_items.Where(predicate.Compile()));

        public IQueryable<T> Queryable() => _items.AsQueryable();

        public Task AddAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
        }

        public void Remove(T entity) => _items.Remove(entity);
    }

    private sealed class TestLookupCacheService : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();

        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase =>
            Task.CompletedTask;
    }
}
