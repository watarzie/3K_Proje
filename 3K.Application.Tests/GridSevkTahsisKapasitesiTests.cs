using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Tests;

public sealed class GridSevkTahsisKapasitesiTests
{
    [Fact]
    public void KapasiteHesabi_MevcutFizikselKaynaklariVeProjeCikisiniNetOlarakDikkateAlir()
    {
        var satir = SatirOlustur(id: 1, istenenAdet: 10);
        satir.GelenMiktar = 2;
        satir.StokKarsilanan = 1;
        satir.ProjeKarsilanan = 1;
        satir.TedarikciKarsilanan = 1;
        satir.ProjeGonderilen = 1;

        var yeterli = GridUcKSevkPartisiKurali.YeniSevkTahsisKapasitesiniDogrula(
            satir,
            [IcerikOlustur(id: 11, satir.Id, tahsis: 4), IcerikOlustur(id: 12, satir.Id, tahsis: 6)],
            sevkMiktari: 6);
        var yetersiz = GridUcKSevkPartisiKurali.YeniSevkTahsisKapasitesiniDogrula(
            satir,
            [IcerikOlustur(id: 11, satir.Id, tahsis: 4), IcerikOlustur(id: 12, satir.Id, tahsis: 5)],
            sevkMiktari: 6);

        Assert.True(yeterli.IsSuccess, yeterli.Error?.Message);
        Assert.False(yetersiz.IsSuccess);
        Assert.Equal(409, yetersiz.StatusCode);
        Assert.Contains("tahsis kapasitesini", yetersiz.Error!.Message);
    }

    [Fact]
    public async Task TekliIlkSevk_RevizyonSonrasiEskiTahsisYetersizsePartiBaslatmaz()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [1]);

        var sonuc = await kurgu.TekliSevkEtAsync(satir, sevkMiktari: 3);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("tahsis kapasitesini", sonuc.Error!.Message);
        Assert.Null(satir.GridSevkMiktari);
        Assert.Null(satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.DoesNotContain(satir, kurgu.Uow.Repo<CekiSatiri>().Updated);
    }

    [Fact]
    public async Task TekliDevamSevki_MevcutFizikselMiktarTahsisleriDolduruyorsaYeniPartiyiEngeller()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [2]);
        var icerik = Assert.Single(kurgu.Uow.Repo<SandikIcerik>().Rows);
        satir.GridDurumuId = (int)GridDurum.TamGeldi;
        satir.GridGelenAdet = 3;
        satir.GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi;
        satir.GridSevkMiktari = 2;
        satir.AktifGridSevkKarsilananMiktari = 2;
        satir.AktifGridSevkPartisiErkenSonuclandirildiMi = false;
        satir.UcKDurumuId = (int)UcKDurum.TamGeldi;
        satir.UcKKarsilamaTipiId = (int)UcKDurum.TamGeldi;
        satir.GelenMiktar = 2;
        icerik.KonulanAdet = 2;
        icerik.EksikAdet = 0;
        icerik.AktifGridSevkKarsilananMiktari = 2;

        var sonuc = await kurgu.TekliSevkEtAsync(satir, sevkMiktari: 1);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Equal(2, satir.GridSevkMiktari);
        Assert.Equal(2, satir.AktifGridSevkKarsilananMiktari);
        Assert.Equal(2, icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task TopluSevk_TumSatirlarinTahsisKapasitesiYetersizse409Doner()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [1]);

        var sonuc = await kurgu.TopluSevkEtAsync([satir.Id]);

        Assert.False(sonuc.IsSuccess);
        Assert.Equal(409, sonuc.StatusCode);
        Assert.Contains("tahsis kapasitesi", sonuc.Error!.Message);
        Assert.Null(satir.GridSevkMiktari);
        Assert.Equal((int)GridDurum.Bekliyor, satir.GridDurumuId);
        Assert.Equal(0, satir.GridGelenAdet);
        Assert.Equal(0, kurgu.Uow.SaveCount);
        Assert.DoesNotContain(satir, kurgu.Uow.Repo<CekiSatiri>().Updated);
    }

    [Fact]
    public async Task TopluSevk_YetersizSatiriAtlarYeterliSatiriSevkEderVeAtlamayiKaydeder()
    {
        using var kurgu = new Kurgu();
        var yetersiz = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [1]);
        var yeterli = kurgu.SatirEkle(id: 2, istenenAdet: 3, tahsisler: [1, 2]);

        var sonuc = await kurgu.TopluSevkEtAsync([yetersiz.Id, yeterli.Id]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Null(yetersiz.GridSevkMiktari);
        Assert.Equal((int)GridDurum.Bekliyor, yetersiz.GridDurumuId);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, yetersiz.GridSevkDurumuId);
        Assert.Equal(0, yetersiz.GridGelenAdet);
        Assert.Equal(0, yetersiz.TrafoSevkAdet);
        Assert.Null(yetersiz.AktifGridSevkKarsilananMiktari);
        Assert.Null(yetersiz.GridPersonelId);
        Assert.Null(yetersiz.GridSevkTarihi);
        Assert.Null(yetersiz.GridAciklama);
        Assert.Equal(3, yeterli.GridSevkMiktari);
        Assert.Equal(0, yeterli.AktifGridSevkKarsilananMiktari);
        Assert.DoesNotContain(yetersiz, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Contains(yeterli, kurgu.Uow.Repo<CekiSatiri>().Updated);
        Assert.Equal(1, kurgu.Uow.SaveCount);
        var hareket = Assert.Single(kurgu.Hareketler);
        Assert.Contains("Atlanan (1)", hareket.Aciklama);
        Assert.Contains("tahsis kapasitesini", hareket.Aciklama);
    }

    [Theory]
    [InlineData((int)GridDurum.Iptal)]
    [InlineData((int)GridDurum.GridKapandi)]
    [InlineData((int)GridDurum.EksikGeldi)]
    public async Task TopluSevk_TahsisKontroluYeniGridDurumunaGoreYapilir_ReddedilenSatirAynenKalir(int eskiGridDurumu)
    {
        using var kurgu = new Kurgu();
        var yetersiz = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [1]);
        var yeterli = kurgu.SatirEkle(id: 2, istenenAdet: 3, tahsisler: [3]);
        yetersiz.GridDurumuId = eskiGridDurumu;
        yetersiz.GridGelenAdet = 1;
        yetersiz.TrafoSevkAdet = 2;

        var sonuc = await kurgu.TopluSevkEtAsync([yetersiz.Id, yeterli.Id]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(eskiGridDurumu, yetersiz.GridDurumuId);
        Assert.Equal(1, yetersiz.GridGelenAdet);
        Assert.Equal(2, yetersiz.TrafoSevkAdet);
        Assert.Null(yetersiz.GridSevkMiktari);
        Assert.Equal((int)GridSevkDurum.SevkEdilmedi, yetersiz.GridSevkDurumuId);
        Assert.Equal(3, yeterli.GridSevkMiktari);
        Assert.Equal(1, kurgu.Uow.SaveCount);
    }

    [Fact]
    public async Task TopluTrafoSevk_TrafoPayiniKorumayaDevamEder()
    {
        using var kurgu = new Kurgu();
        var satir = kurgu.SatirEkle(id: 1, istenenAdet: 3, tahsisler: [1]);
        satir.GridDurumuId = (int)GridDurum.TrafoSevk;
        satir.GridGelenAdet = 1;
        satir.TrafoSevkAdet = 2;

        var sonuc = await kurgu.TopluSevkEtAsync([satir.Id]);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal((int)GridDurum.TrafoSevk, satir.GridDurumuId);
        Assert.Equal(1, satir.GridSevkMiktari);
        Assert.Equal(2, satir.TrafoSevkAdet);
    }

    private static CekiSatiri SatirOlustur(int id, decimal istenenAdet) => new()
    {
        Id = id,
        CekiId = id,
        SiraNo = id,
        BarkodNo = $"BARKOD-{id}",
        Aciklama = $"Ürün {id}",
        IstenenAdet = istenenAdet,
        GridDurumuId = (int)GridDurum.Bekliyor,
        GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
        UcKDurumuId = (int)UcKDurum.Bekliyor,
        UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
        CekideGecenSandikNo = string.Empty
    };

    private static SandikIcerik IcerikOlustur(
        int id,
        int cekiSatiriId,
        decimal tahsis,
        int sandikId = 0) => new()
    {
        Id = id,
        CekiSatiriId = cekiSatiriId,
        SandikId = sandikId > 0 ? sandikId : id,
        TahsisMiktari = tahsis,
        EksikAdet = tahsis
    };

    private sealed class Kurgu : IDisposable
    {
        private readonly CurrentUserStub _currentUser = new();
        private readonly DurumStub _durum = new();
        private readonly LookupStub _lookup = new();
        private readonly SahaStub _saha = new();
        private int _icerikId = 100;
        private int _sandikId = 200;

        public TestUnitOfWork Uow { get; } = new();
        public List<HareketGecmisi> Hareketler { get; } = [];

        public CekiSatiri SatirEkle(int id, decimal istenenAdet, decimal[] tahsisler)
        {
            var satir = SatirOlustur(id, istenenAdet);
            Uow.Repo<CekiSatiri>().Rows.Add(satir);

            foreach (var tahsis in tahsisler)
            {
                var sandik = new Sandik
                {
                    Id = _sandikId++,
                    ProjeId = 10,
                    SandikNo = $"S-{_sandikId}",
                    DurumId = (int)SandikDurum.Hazirlaniyor
                };
                Uow.Repo<Sandik>().Rows.Add(sandik);
                Uow.Repo<SandikIcerik>().Rows.Add(IcerikOlustur(
                    _icerikId++,
                    satir.Id,
                    tahsis,
                    sandik.Id));
            }

            return satir;
        }

        public Task<Result> TekliSevkEtAsync(CekiSatiri satir, decimal sevkMiktari) =>
            new GridDurumGuncelleCommandHandler(
                    Uow,
                    _currentUser,
                    _durum,
                    new HareketStub(Hareketler),
                    _lookup,
                    _saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    CekiSatiriId = satir.Id,
                    ProjeId = 10,
                    YeniDurumId = (int)GridDurum.TamGeldi,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                    SevkMiktari = sevkMiktari
                }, default);

        public Task<Result> TopluSevkEtAsync(List<int> satirIdleri) =>
            new GridTopluSevkCommandHandler(
                    Uow,
                    _currentUser,
                    _durum,
                    new HareketStub(Hareketler),
                    _lookup,
                    _saha)
                .Handle(new GridTopluSevkCommand
                {
                    ProjeId = 10,
                    CekiSatiriIdler = satirIdleri,
                    Aciklama = "Tahsis kapasitesi testi"
                }, default);

        public void Dispose() => Uow.Dispose();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repositories = [];
        public bool HasActiveTransaction { get; private set; }
        public int SaveCount { get; private set; }

        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repositories.TryGetValue(typeof(T), out var repository))
                _repositories[typeof(T)] = repository = new Repo<T>();
            return (Repo<T>)repository;
        }

        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            HasActiveTransaction = true;
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                HasActiveTransaction = false;
            }
        }

        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class Repo<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public List<T> Updated { get; } = [];

        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(entity => entity.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(Expression<Func<T, TProperty>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task AddAsync(T entity)
        {
            Rows.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(T entity) => Updated.Add(entity);
        public void Remove(T entity) => Rows.Remove(entity);
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class DurumStub : IDurumHesaplaService
    {
        public int HesaplaGenelDurum(int gridDurumuId, int ucKDurumuId) => (int)UrunDurum.Bekliyor;
        public void HesaplaKalanVeDurum(CekiSatiri satir) { }
    }

    private sealed class HareketStub(List<HareketGecmisi> hareketler) : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket)
        {
            hareketler.Add(hareket);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId,
            string? searchTerm,
            int? islemTipiId,
            int pageNumber,
            int pageSize) => throw new NotSupportedException();
    }

    private sealed class LookupStub : ILookupCacheService
    {
        public string GetDeger<TLookup>(int id) where TLookup : LookupBase => id.ToString();
        public Task WarmupAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RefreshAsync<TLookup>(CancellationToken ct = default) where TLookup : LookupBase => Task.CompletedTask;
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public Task<bool> AktifTamamlamaVarMiAsync(int kaynakCekiSatiriId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
