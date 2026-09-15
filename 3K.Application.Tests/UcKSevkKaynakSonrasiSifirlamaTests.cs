using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class UcKSevkKaynakSonrasiSifirlamaTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EksikTeslimSonrasiTedarikciVeSandikSifirlama_YenidenTeslimdeSahteSevkBorcuBirakmaz(bool toplu)
    {
        using var kurgu = new Kurgu(istenen: 4, sevk: 4);

        var eksik = await kurgu.KarsilaAsync(UcKDurum.EksikGeldi, 2);
        Assert.True(eksik.IsSuccess, eksik.Error?.Message);
        Assert.Equal(2, kurgu.Satir.YenidenSevkGerekliAdet);

        var tedarikci = await kurgu.KarsilaAsync(UcKDurum.TedarikcidenGeldi, 1);
        Assert.True(tedarikci.IsSuccess, tedarikci.Error?.Message);
        Assert.Equal((int)UcKDurum.TedarikcidenGeldi, kurgu.Satir.UcKKarsilamaTipiId);
        Assert.True(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(3, kurgu.Icerik.KonulanAdet);

        var sifirla = await kurgu.SifirlaAsync(toplu);
        Assert.True(sifirla.IsSuccess, sifirla.Error?.Message);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
        Assert.Equal(0, kurgu.Satir.TedarikciKarsilanan);
        Assert.Equal(0, kurgu.Satir.KarsilananMiktar);
        Assert.Equal(0, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal(0, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Icerik.AktifGridSevkKarsilananMiktari);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);

        var yenidenTeslim = await kurgu.KarsilaAsync(UcKDurum.TamGeldi);
        Assert.True(yenidenTeslim.IsSuccess, yenidenTeslim.Error?.Message);
        Assert.Equal(4, kurgu.Satir.GelenMiktar);
        Assert.Equal(4, kurgu.Icerik.KonulanAdet);
        Assert.Equal(0, kurgu.Satir.KalanMiktar);
        Assert.Equal(0, kurgu.Icerik.EksikAdet);
        Assert.Equal(0, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.False(GridUcKSevkPartisiKurali.DevamSevkiniDegerlendir(kurgu.Satir).YeniPartiMi);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EksikPartiyiSifirlama_DahaOnceBekleyenSevkBorcunuSilmez(bool toplu)
    {
        using var kurgu = new Kurgu(istenen: 6, sevk: 4);
        kurgu.Satir.YenidenSevkGerekliAdet = 1;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;

        var eksik = await kurgu.KarsilaAsync(UcKDurum.EksikGeldi, 2);
        Assert.True(eksik.IsSuccess, eksik.Error?.Message);
        Assert.Equal(3, kurgu.Satir.YenidenSevkGerekliAdet);

        var sifirla = await kurgu.SifirlaAsync(toplu);
        Assert.True(sifirla.IsSuccess, sifirla.Error?.Message);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.Equal((int)GridSevkDurum.YenidenSevkGerekli, kurgu.Satir.GridSevkDurumuId);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);

        var tekrar = await kurgu.SifirlaAsync(toplu);
        Assert.False(tekrar.IsSuccess);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcikSevkteTuretilmisEksikDurumu_FinalizasyonVarsayipEskiBorcuDusurmez(bool toplu)
    {
        using var kurgu = new Kurgu(istenen: 6, sevk: 4);
        kurgu.Satir.GelenMiktar = 2;
        kurgu.Satir.AktifGridSevkKarsilananMiktari = 2;
        kurgu.Satir.UcKDurumuId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi;
        kurgu.Satir.YenidenSevkGerekliAdet = 1;
        kurgu.Satir.GridSevkDurumuId = (int)GridSevkDurum.YenidenSevkGerekli;
        kurgu.Icerik.KonulanAdet = 2;
        kurgu.Icerik.EksikAdet = 4;
        kurgu.Icerik.AktifGridSevkKarsilananMiktari = 2;

        var sifirla = await kurgu.SifirlaAsync(toplu);

        Assert.True(sifirla.IsSuccess, sifirla.Error?.Message);
        Assert.Equal(1, kurgu.Satir.YenidenSevkGerekliAdet);
        Assert.False(kurgu.Satir.AktifGridSevkPartisiErkenSonuclandirildiMi);
        Assert.Equal(0, kurgu.Satir.GelenMiktar);
    }

    private sealed class Kurgu : IDisposable
    {
        private readonly TestUnitOfWork _uow = new();
        private readonly CurrentUserStub _user = new();
        private readonly DurumHesaplaService _durum = new();
        private readonly HareketStub _hareket = new();
        private readonly LookupStub _lookup = new();
        private readonly SahaStub _saha = new();

        public CekiSatiri Satir { get; }
        public SandikIcerik Icerik { get; }

        public Kurgu(decimal istenen, decimal sevk)
        {
            var proje = new Proje { Id = 10, ProjeNo = "PA699-02", ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Id = 20, ProjeId = proje.Id, Proje = proje };
            Satir = new CekiSatiri
            {
                Id = 23, CekiId = ceki.Id, Ceki = ceki, SiraNo = 23,
                BarkodNo = "FCT01181927", Aciklama = "Sevk sıfırlama regresyonu",
                IstenenAdet = istenen, BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "1", FiiliSandikNo = "1",
                GridDurumuId = (int)GridDurum.TamGeldi, GridGelenAdet = istenen,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi, GridSevkMiktari = sevk,
                AktifGridSevkKarsilananMiktari = 0, AktifGridSevkPartisiErkenSonuclandirildiMi = false,
                UcKDurumuId = (int)UcKDurum.Bekliyor, UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor
            };
            var sandik = new Sandik
            {
                Id = 30, ProjeId = proje.Id, Proje = proje, SandikNo = "1",
                DurumId = (int)SandikDurum.Hazirlaniyor, DepoLokasyonId = (int)DepoLokasyon.UcK
            };
            Icerik = new SandikIcerik
            {
                Id = 40, CekiSatiriId = Satir.Id, CekiSatiri = Satir,
                SandikId = sandik.Id, Sandik = sandik, TahsisMiktari = istenen,
                EksikAdet = istenen, AktifGridSevkKarsilananMiktari = 0, BirimId = (int)Birim.Adet
            };
            proje.Cekiler.Add(ceki);
            proje.Sandiklar.Add(sandik);
            ceki.CekiSatirlari.Add(Satir);
            Satir.SandikIcerikleri.Add(Icerik);
            sandik.SandikIcerikleri.Add(Icerik);
            _uow.Repo<Proje>().Rows.Add(proje);
            _uow.Repo<Ceki>().Rows.Add(ceki);
            _uow.Repo<CekiSatiri>().Rows.Add(Satir);
            _uow.Repo<Sandik>().Rows.Add(sandik);
            _uow.Repo<SandikIcerik>().Rows.Add(Icerik);
        }

        public Task<Result> KarsilaAsync(UcKDurum durum, decimal? adet = null) =>
            new UcKDurumGuncelleCommandHandler(_uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new UcKDurumGuncelleCommand
                {
                    ProjeId = 10, CekiSatiriId = Satir.Id, SandikIcerikId = Icerik.Id,
                    KarsilamaTipiId = (int)durum, GelenAdet = adet, Aciklama = "Sıfırlama regresyonu"
                }, default);

        public Task<Result> SifirlaAsync(bool toplu) => toplu
            ? new UcKTopluSifirlaCommandHandler(_uow, _user, _durum, _hareket, _saha)
                .Handle(new UcKTopluSifirlaCommand
                {
                    ProjeId = 10, Aciklama = "Sıfırlama regresyonu",
                    Secimler = [new UcKSandikSecimDto { CekiSatiriId = Satir.Id, SandikIcerikId = Icerik.Id }]
                }, default)
            : new UcKDurumSifirlaCommandHandler(_uow, _user, _durum, _hareket, _saha)
                .Handle(new UcKDurumSifirlaCommand
                {
                    ProjeId = 10, CekiSatiriId = Satir.Id, SandikIcerikId = Icerik.Id,
                    Aciklama = "Sıfırlama regresyonu"
                }, default);

        public void Dispose() => _uow.Dispose();
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = [];
        public bool HasActiveTransaction { get; private set; }
        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo))
                _repos[typeof(T)] = repo = new Repo<T>();
            return (Repo<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
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
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(e => e.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(Expression<Func<T, TProperty>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task AddAsync(T entity) { Rows.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) { }
        public void Remove(T entity) => Rows.Remove(entity);
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize) => throw new NotSupportedException();
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
