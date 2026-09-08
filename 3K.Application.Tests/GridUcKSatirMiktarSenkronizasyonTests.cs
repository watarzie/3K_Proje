using System.Linq.Expressions;
using System.Text.Json;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Application.Features.UcKIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class GridUcKSatirMiktarSenkronizasyonTests
{
    [Fact]
    public async Task OrijinalMiktarYoksa_TahsisFarkindanTarihselMiktarUretilmez()
    {
        using var kurgu = new Kurgu(3, 1);

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Null(grid.OrijinalIstenenAdet);
        Assert.Null(ucK.OrijinalIstenenAdet);
        Assert.Null(kurgu.Satir.OrijinalIstenenAdet);
        Assert.Equal(3, grid.IstenenAdet);
        Assert.Equal(1, grid.SandikMiktari);
        Assert.Equal(1, ucK.SandikMiktari);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IlkMiktaraGeriDonmusSatir_DtoOrijinalAlaniniBosaltmaz(bool degisiklikVar)
    {
        using var kurgu = new Kurgu(1, 1);
        kurgu.Satir.OrijinalIstenenAdet = degisiklikVar ? 1m : null;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(degisiklikVar ? 1m : (decimal?)null, grid.OrijinalIstenenAdet);
        Assert.Equal(degisiklikVar ? 1m : (decimal?)null, ucK.OrijinalIstenenAdet);
        Assert.Equal(1, grid.IstenenAdet);
        Assert.Equal(1, ucK.IstenenAdet);
        Assert.Equal(1, grid.SandikMiktari);
        Assert.Equal(1, ucK.SandikMiktari);
        Assert.Equal(1, grid.KalanMiktar);
        Assert.Equal(1, ucK.Kalan);
    }

    [Fact]
    public async Task SaklananOrijinalMiktar_IkiListedeDeAyricaDoner_GuncelHesaplamalaraKatilmaz()
    {
        using var kurgu = new Kurgu(5, 2);
        kurgu.Satir.OrijinalIstenenAdet = 1.2375m;
        kurgu.Satir.GridGelenAdet = 3;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(1.2375m, grid.OrijinalIstenenAdet);
        Assert.Equal(1.2375m, ucK.OrijinalIstenenAdet);
        Assert.Equal(5, grid.IstenenAdet);
        Assert.Equal(5, grid.AnaIstenenAdet);
        Assert.Equal(5, ucK.AnaIstenenAdet);
        Assert.Equal(2, grid.SandikMiktari);
        Assert.Equal(2, ucK.SandikMiktari);
        Assert.Equal(2, grid.GridEksikMiktar);
        Assert.Equal(5, grid.KalanMiktar);
        Assert.Equal(5, ucK.Kalan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CokluTahsis_OrijinalAnaMiktariPaylaraBolmez_SatirMiktarlariTahsisOlarakKalir(bool filtreli)
    {
        using var kurgu = new Kurgu(10, 4);
        kurgu.IkinciTahsisEkle(6);
        kurgu.Satir.OrijinalIstenenAdet = 3.25m;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync(filtreli ? kurgu.IlkSandik.Id : null);

        Assert.All(gridSatirlari, satir => Assert.Equal(3.25m, satir.OrijinalIstenenAdet));
        Assert.All(ucKSatirlari, satir => Assert.Equal(3.25m, satir.OrijinalIstenenAdet));
        var ilkGrid = Assert.Single(gridSatirlari, s => s.SandikIcerikId == kurgu.IlkIcerik.Id);
        var ilkUcK = Assert.Single(ucKSatirlari, s => s.SandikIcerikId == kurgu.IlkIcerik.Id);
        Assert.Equal(4, ilkGrid.IstenenAdet);
        Assert.Equal(4, ilkUcK.IstenenAdet);
        Assert.Equal(10, ilkGrid.AnaIstenenAdet);
        Assert.Equal(10, ilkUcK.AnaIstenenAdet);
        Assert.True(ilkGrid.SandikBazliDagitim);
        Assert.True(ilkUcK.SandikBazliDagitim);
    }

    [Fact]
    public async Task AnaMiktarUcEskiTekTahsisBir_IstenenEksikVeKalanGuncelMiktariKullanir()
    {
        using var kurgu = new Kurgu(3, 1);

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(3, grid.IstenenAdet);
        Assert.Equal(1, ucK.IstenenAdet);
        Assert.Equal(3, grid.AnaIstenenAdet);
        Assert.Equal(3, ucK.AnaIstenenAdet);
        Assert.Equal(1, grid.SandikMiktari);
        Assert.Equal(1, ucK.SandikMiktari);
        Assert.Equal(3, grid.GridEksikMiktar);
        Assert.Equal(3, grid.KalanMiktar);
        Assert.Equal(3, ucK.Kalan);
        Assert.False(grid.SandikBazliDagitim);
        Assert.False(ucK.SandikBazliDagitim);
        Assert.Equal(1, kurgu.IlkIcerik.TahsisMiktari);
        Assert.Equal(0, kurgu.IlkIcerik.KonulanAdet);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(3, 0)]
    public async Task GridGelenTekTahsisleKirpilmaz_EksikAnaMiktardanHesaplanir(int gelen, int eksik)
    {
        using var kurgu = new Kurgu(3, 1);
        kurgu.Satir.GridGelenAdet = gelen;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(gelen, grid.GridGelenAdet);
        Assert.Equal(eksik, grid.GridEksikMiktar);
        // Grid teslimi, 3K fiziksel teslimi yerine geçmez.
        Assert.Equal(3, grid.KalanMiktar);
        Assert.Equal(3, ucK.Kalan);
    }

    [Fact]
    public async Task TekTahsis_TeslimSevkIadeVeTransferToplamlariniEskiTahsisleSinirlamaz()
    {
        using var kurgu = new Kurgu(12, 1);
        kurgu.Satir.GridGelenAdet = 5;
        kurgu.Satir.TrafoSevkAdet = 2;
        kurgu.Satir.GridSevkMiktari = 4;
        kurgu.Satir.YenidenSevkGerekliAdet = 3;
        kurgu.Satir.GeriGonderilenMiktar = 2;
        kurgu.Satir.HataliMiktar = 2.5m;
        kurgu.Satir.ProjeGonderilen = 3;
        kurgu.Satir.GelenMiktar = 5;
        kurgu.Satir.StokKarsilanan = 2;
        kurgu.Satir.ProjeKarsilanan = 2.25m;
        kurgu.Satir.TedarikciKarsilanan = 1.25m;
        kurgu.IlkIcerik.KonulanAdet = 0.5m;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(5, grid.GridGelenAdet);
        Assert.Equal(2, grid.TrafoSevkAdet);
        Assert.Equal(4, grid.GridSevkMiktari);
        Assert.Equal(3, grid.YenidenSevkGerekliAdet);
        Assert.Equal(2, grid.GeriGonderilenMiktar);
        Assert.Equal(3, grid.ProjeGonderilen);
        Assert.Equal(2, grid.GelenMiktar);
        Assert.Equal(2, ucK.GelenMiktar);
        Assert.Equal(2, grid.StokKarsilanan);
        Assert.Equal(2, ucK.StokKarsilanan);
        Assert.Equal(2.25m, grid.ProjeKarsilanan);
        Assert.Equal(2.25m, ucK.ProjeKarsilanan);
        Assert.Equal(1.25m, grid.TedarikciKarsilanan);
        Assert.Equal(1.25m, ucK.TedarikciKarsilanan);
        Assert.Equal(5, grid.GridEksikMiktar);
        Assert.Equal(0.5m, grid.NetKullanilabilir);
        Assert.Equal(0.5m, ucK.NetKullanilabilir);
    }

    [Fact]
    public async Task OndalikMiktarlar_EksikVeSevkPaylarindaKesirKaybetmez()
    {
        using var kurgu = new Kurgu(3.625m, 0.25m);
        kurgu.Satir.GridGelenAdet = 1.375m;
        kurgu.Satir.TrafoSevkAdet = 0.625m;
        kurgu.Satir.GridSevkMiktari = 1.125m;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(3.625m, grid.IstenenAdet);
        Assert.Equal(0.25m, ucK.IstenenAdet);
        Assert.Equal(1.375m, grid.GridGelenAdet);
        Assert.Equal(0.625m, grid.TrafoSevkAdet);
        Assert.Equal(1.125m, grid.GridSevkMiktari);
        Assert.Equal(1.625m, grid.GridEksikMiktar);
        Assert.Equal(3, grid.KalanMiktar);
        Assert.Equal(3, ucK.Kalan);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CokluTahsis_SandikFiltresiTekSatirBiraksaDaDagilimDegismez(bool filtreli)
    {
        using var kurgu = new Kurgu(10, 4);
        kurgu.IkinciTahsisEkle(6);
        kurgu.Satir.GridGelenAdet = 5;
        kurgu.Satir.TrafoSevkAdet = 2;
        kurgu.Satir.GridSevkMiktari = 4;
        kurgu.Satir.GeriGonderilenMiktar = 2;
        kurgu.Satir.HataliMiktar = 2;
        kurgu.Satir.ProjeGonderilen = 2;
        kurgu.Satir.GelenMiktar = 4;
        kurgu.Satir.YenidenSevkGerekliAdet = 2;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync(filtreli ? kurgu.IlkSandik.Id : null);

        Assert.Equal(filtreli ? 1 : 2, gridSatirlari.Count);
        Assert.Equal(filtreli ? 1 : 2, ucKSatirlari.Count);
        var grid = Assert.Single(gridSatirlari, s => s.SandikIcerikId == kurgu.IlkIcerik.Id);
        var ucK = Assert.Single(ucKSatirlari, s => s.SandikIcerikId == kurgu.IlkIcerik.Id);
        Assert.True(grid.SandikBazliDagitim);
        Assert.True(ucK.SandikBazliDagitim);
        Assert.Equal(4, grid.IstenenAdet);
        Assert.Equal(4, ucK.IstenenAdet);
        Assert.Equal(10, grid.AnaIstenenAdet);
        Assert.Equal(10, ucK.AnaIstenenAdet);
        Assert.Equal(2, grid.GridGelenAdet);
        Assert.Equal(2, ucK.GridGelenAdet);
        Assert.Equal(0.8m, grid.TrafoSevkAdet);
        Assert.Equal(0.8m, ucK.TrafoSevkAdet);
        Assert.Equal(1.6m, grid.GridSevkMiktari);
        Assert.Equal(1.6m, ucK.GridSevkMiktari);
        Assert.Equal(0.8m, grid.GeriGonderilenMiktar);
        Assert.Equal(0.8m, ucK.GeriGonderilenMiktar);
        Assert.Equal(0.8m, ucK.HataliMiktar);
        Assert.Equal(0.8m, grid.ProjeGonderilen);
        Assert.Equal(0.8m, ucK.ProjeGonderilen);
        Assert.Equal(0.8m, grid.YenidenSevkGerekliAdet);
        Assert.Equal(1.2m, grid.GridEksikMiktar);
        Assert.Equal(2.4m, grid.KalanMiktar);
        Assert.Equal(2.4m, ucK.Kalan);
        if (!filtreli)
        {
            Assert.Equal(5, gridSatirlari.Sum(s => s.GridGelenAdet));
            Assert.Equal(5, ucKSatirlari.Sum(s => s.GridGelenAdet));
            Assert.Equal(6, gridSatirlari.Sum(s => s.KalanMiktar));
            Assert.Equal(6, ucKSatirlari.Sum(s => s.Kalan));
        }
    }

    [Theory]
    [InlineData((int)GridDurum.Iptal)]
    [InlineData((int)GridDurum.GridKapandi)]
    public async Task IptalVeyaGridKapandi_YeniAnaMiktarEksikVeyaKalanUretmez(int durum)
    {
        using var kurgu = new Kurgu(3, 1);
        kurgu.Satir.GridDurumuId = durum;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(0, grid.GridEksikMiktar);
        Assert.Equal(0, grid.KalanMiktar);
        Assert.Equal(0, ucK.Kalan);
        Assert.Equal(3, grid.IstenenAdet);
        Assert.Equal(1, ucK.IstenenAdet);
    }

    [Fact]
    public async Task GerceklesenSahaTamamlamasi_MerkeziKalandanDuserGridTeslimiUretmez()
    {
        using var kurgu = new Kurgu(5, 1);
        kurgu.Saha.Gerceklesen[kurgu.Satir.Id] = 1.25m;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(3.75m, grid.KalanMiktar);
        Assert.Equal(3.75m, ucK.Kalan);
        Assert.Equal(5, grid.GridEksikMiktar);
        Assert.Equal(0, grid.GridGelenAdet);
        Assert.Equal(0, ucK.GelenMiktar);
    }

    [Fact]
    public async Task TahsisKaydiYoksa_AnaMiktarlaTekSatirFallbackCalismayaDevamEder()
    {
        using var kurgu = new Kurgu(3, 1);
        kurgu.Uow.Repo<SandikIcerik>().Rows.Clear();
        kurgu.Satir.GridGelenAdet = 2;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Null(grid.SandikIcerikId);
        Assert.Null(ucK.SandikIcerikId);
        Assert.Equal(3, grid.IstenenAdet);
        Assert.Equal(3, ucK.IstenenAdet);
        Assert.Equal(3, grid.SandikMiktari);
        Assert.Equal(3, ucK.SandikMiktari);
        Assert.Equal(2, grid.GridGelenAdet);
        Assert.Equal(2, ucK.GridGelenAdet);
        Assert.Equal(1, grid.GridEksikMiktar);
        Assert.Equal(3, grid.KalanMiktar);
    }

    [Fact]
    public async Task FazlaTeslim_EskiTahsisleKirpilmazAmaNegatifEksikUretmez()
    {
        using var kurgu = new Kurgu(3, 1);
        kurgu.Satir.GridGelenAdet = 4;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        Assert.Equal(4, Assert.Single(gridSatirlari).GridGelenAdet);
        Assert.Equal(0, Assert.Single(gridSatirlari).GridEksikMiktar);
    }

    [Fact]
    public async Task UcKSeciliSandikFizikselIslemSinirlari_GridAnaMiktarDuzeltmesiyleGenislemez()
    {
        using var kurgu = new Kurgu(3, 1);
        kurgu.Satir.GridGelenAdet = 2;
        kurgu.Satir.GridSevkMiktari = 2;

        var (gridSatirlari, ucKSatirlari) = await kurgu.OkuAsync();

        var grid = Assert.Single(gridSatirlari);
        var ucK = Assert.Single(ucKSatirlari);
        Assert.Equal(3, grid.IstenenAdet);
        Assert.Equal(2, grid.GridSevkMiktari);
        Assert.Equal(1, ucK.IstenenAdet);
        Assert.Equal(1, ucK.GridSevkMiktari);
        Assert.Equal(1, ucK.GridGelenAdet);
        Assert.Equal(kurgu.IlkIcerik.Id, ucK.SandikIcerikId);
        Assert.Equal(1, kurgu.IlkIcerik.TahsisMiktari);
        Assert.Equal(0, kurgu.IlkIcerik.KonulanAdet);
    }

    private sealed class Kurgu : IDisposable
    {
        public SaltOkunurUow Uow { get; } = new();
        public SahaStub Saha { get; } = new();
        public Proje Proje { get; }
        public CekiSatiri Satir { get; }
        public Sandik IlkSandik { get; }
        public SandikIcerik IlkIcerik { get; }

        public Kurgu(decimal anaMiktar, decimal tahsis)
        {
            Proje = new Proje { Id = 1, ProjeNo = "PA699-02", ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Id = 10, ProjeId = Proje.Id, Proje = Proje };
            Satir = new CekiSatiri
            {
                Id = 100, CekiId = ceki.Id, Ceki = ceki, SiraNo = 1,
                BarkodNo = "FCT01169448", Aciklama = "Transformatör", IstenenAdet = anaMiktar,
                BirimId = (int)Birim.Adet, CekideGecenSandikNo = "1", FiiliSandikNo = "1",
                GridDurumuId = (int)GridDurum.Gelmedi, UcKDurumuId = (int)UcKDurum.Bekliyor
            };
            IlkSandik = new Sandik
            {
                Id = 20, ProjeId = Proje.Id, Proje = Proje, SandikNo = "1",
                DurumId = (int)SandikDurum.Hazirlaniyor
            };
            IlkIcerik = new SandikIcerik
            {
                Id = 200, CekiSatiriId = Satir.Id, CekiSatiri = Satir, SandikId = IlkSandik.Id,
                Sandik = IlkSandik, TahsisMiktari = tahsis, EksikAdet = tahsis
            };
            Uow.Repo<Proje>().Rows.Add(Proje);
            Uow.Repo<Ceki>().Rows.Add(ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            Uow.Repo<Sandik>().Rows.Add(IlkSandik);
            Uow.Repo<SandikIcerik>().Rows.Add(IlkIcerik);
        }

        public void IkinciTahsisEkle(decimal miktar)
        {
            var sandik = new Sandik
            {
                Id = 21, ProjeId = Proje.Id, Proje = Proje, SandikNo = "2",
                DurumId = (int)SandikDurum.Hazirlaniyor
            };
            Uow.Repo<Sandik>().Rows.Add(sandik);
            Uow.Repo<SandikIcerik>().Rows.Add(new SandikIcerik
            {
                Id = 201, CekiSatiriId = Satir.Id, CekiSatiri = Satir, SandikId = sandik.Id,
                Sandik = sandik, TahsisMiktari = miktar, EksikAdet = miktar
            });
        }

        public async Task<(List<GridUrunDto> Grid, List<UcKUrunDto> UcK)> OkuAsync(int? sandikId = null)
        {
            var once = VeriOzeti();
            var lookup = new LookupStub();
            var grid = await new GetGridUrunlerQueryHandler(Uow, lookup, Saha)
                .Handle(new GetGridUrunlerQuery { ProjeId = Proje.Id, SandikId = sandikId }, default);
            var ucK = await new GetUcKUrunlerQueryHandler(Uow, lookup, Saha)
                .Handle(new GetUcKUrunlerQuery { ProjeId = Proje.Id, SandikId = sandikId }, default);
            Assert.True(grid.IsSuccess, grid.Error?.Message);
            Assert.True(ucK.IsSuccess, ucK.Error?.Message);
            Assert.Equal(once, VeriOzeti());
            return (grid.Value!, ucK.Value!);
        }

        private string VeriOzeti() => JsonSerializer.Serialize(
            new BaseEntity[] { Proje, Satir }
                .Concat(Uow.Repo<Sandik>().Rows)
                .Concat(Uow.Repo<SandikIcerik>().Rows)
                .Select(entity => entity.GetType().GetProperties()
                    .Where(p => p.PropertyType.IsValueType || p.PropertyType == typeof(string))
                    .ToDictionary(p => p.Name, p => p.GetValue(entity))));

        public void Dispose() => Uow.Dispose();
    }

    private sealed class SaltOkunurUow : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = new();
        public bool HasActiveTransaction => false;
        public SaltOkunurRepo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repositories.TryGetValue(typeof(T), out var repository))
                _repositories[typeof(T)] = repository = new SaltOkunurRepo<T>();
            return (SaltOkunurRepo<T>)repository;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("Liste sorgusu veri kaydedemez.");
        public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Liste sorgusu yazma transaction'ı açamaz.");
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class SaltOkunurRepo<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = new();
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(r => r.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task AddAsync(T entity) => throw new InvalidOperationException("Liste sorgusu kayıt ekleyemez.");
        public void Update(T entity) => throw new InvalidOperationException("Liste sorgusu kayıt güncelleyemez.");
        public void Remove(T entity) => throw new InvalidOperationException("Liste sorgusu kayıt silemez.");
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public Dictionary<int, decimal> Gerceklesen { get; } = new();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => Task.FromResult(Gerceklesen.Where(pair => kaynakCekiSatiriIds.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => GetAktifGerceklesenTamamlamaMapAsync(kaynakCekiSatiriIds, cancellationToken);
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Planlanan miktar gerçekleşmiş teslim yerine kullanılamaz.");
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Planlanan miktar gerçekleşmiş teslim yerine kullanılamaz.");
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> kaynakSandikIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AktifTamamlamaVarMiAsync(int kaynakCekiSatiriId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> kaynakCekiSatiriIds, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Liste sorgusu kaynak veriyi değiştiremez.");
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> sahaSandikIds, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Liste sorgusu kaynak veriyi değiştiremez.");
    }
}
