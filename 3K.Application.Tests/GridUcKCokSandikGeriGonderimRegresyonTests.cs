using System.Linq.Expressions;
using _3K.Application.Common;
using _3K.Application.Features.GridIslemleri.Commands;
using _3K.Application.Features.GridIslemleri.DTOs;
using _3K.Application.Features.GridIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Application.Features.UcKIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

/// <summary>
/// PA559-01 vakası: tek çeki satırı (istenen 2) iki sandığa 1+1 dağıtılmış; Grid sevk eder,
/// 3K bir sandıktan geri gönderir, Grid yeniden sevk eder ve döngü tekrarlanır.
/// </summary>
public sealed class GridUcKCokSandikGeriGonderimRegresyonTests
{
    /// <summary>
    /// Adım adım veritabanı sayaçları ve okuma modeli. Grid/3K sayaçları satır (parti) bazlıdır;
    /// sandık satırlarına oransal bölünürse 1 adetlik iade iki sandığa 0,5 + 0,5 görünür ve
    /// iade yapılmayan sandık da iade almış gibi okunur.
    /// </summary>
    [Fact]
    public async Task IkiTahsisliSatir_SevkGeriGonderimDongusu_SatirSayaclariSandiklaraBolunmez()
    {
        using var k = new Kurgu();
        await k.AdimOk("1 adet S10 → S11 taşı", k.TasiAsync(1));
        await k.AdimOk("Grid Tam Geldi + toplu sevk", k.TopluGridSevkAsync());
        await k.SatirDogrula(gridGelen: 2, gridSevk: 2, aktifKarsilanan: 0, yenidenSevk: 0, gelen: 0, geriGonderilen: 0);

        await k.AdimOk("3K Tam Geldi S10", k.UcKAsync(UcKDurum.TamGeldi, null, Kurgu.Icerik10Id));
        await k.AdimOk("3K Tam Geldi S11", k.UcKAsync(UcKDurum.TamGeldi, null, k.Icerik11Id));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 2, aktifKarsilanan: 2, yenidenSevk: 0, gelen: 2, geriGonderilen: 0);
        k.IcerikDogrula(s10Konulan: 1, s10Aktif: 1, s11Konulan: 1, s11Aktif: 1);

        await k.AdimOk("3K S11 geri gönder 1", k.UcKAsync(UcKDurum.GeriGonderildi, 1, k.Icerik11Id));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 2, aktifKarsilanan: 1, yenidenSevk: 1, gelen: 1, geriGonderilen: 1);
        k.IcerikDogrula(s10Konulan: 1, s10Aktif: 1, s11Konulan: 0, s11Aktif: 0);
        var gridSatirlari = await k.GridUrunleriAsync();
        Assert.All(gridSatirlari, d => Assert.True(d.GridYenidenSevkEdilebilirMi));
        Assert.All(gridSatirlari, d => Assert.Equal(1, d.GridYenidenSevkUstSiniri));
        // Sandık bazlı gerçek değerler satır toplamından ayrı kalır.
        Assert.Equal([1m, 0m], gridSatirlari.Select(d => d.GelenMiktar));
        Assert.Equal([0m, 1m], gridSatirlari.Select(d => d.KalanMiktar));

        await k.AdimOk("Grid yeniden sevk 1", k.GridSevkAsync(GridDurum.TamGeldi, null, 1));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 1, aktifKarsilanan: 0, yenidenSevk: 0, gelen: 1, geriGonderilen: 1);

        await k.AdimOk("3K Tam Geldi S11 (2. parti)", k.UcKAsync(UcKDurum.TamGeldi, null, k.Icerik11Id));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 1, aktifKarsilanan: 1, yenidenSevk: 0, gelen: 2, geriGonderilen: 1);
        k.IcerikDogrula(s10Konulan: 1, s10Aktif: 0, s11Konulan: 1, s11Aktif: 1);

        await k.AdimOk("3K S11 geri gönder 1 (2.)", k.UcKAsync(UcKDurum.GeriGonderildi, 1, k.Icerik11Id));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 1, aktifKarsilanan: 0, yenidenSevk: 1, gelen: 1, geriGonderilen: 2);
        k.IcerikDogrula(s10Konulan: 1, s10Aktif: 0, s11Konulan: 0, s11Aktif: 0);

        await k.AdimOk("Grid yeniden sevk 1 (2.)", k.GridSevkAsync(GridDurum.TamGeldi, null, 1));
        await k.SatirDogrula(gridGelen: 2, gridSevk: 1, aktifKarsilanan: 0, yenidenSevk: 0, gelen: 1, geriGonderilen: 2);
    }

    internal sealed class Kurgu : IDisposable
    {
        public const int ProjeId = 55901;
        public const int SatirId = 559;
        public const int Icerik10Id = 5590;

        private readonly DurumHesaplaService _durum = new();
        private readonly CurrentUserStub _user = new();
        private readonly HareketStub _hareket = new();
        private readonly LookupStub _lookup = new();
        private readonly SahaStub _saha = new();

        public TestUnitOfWork Uow { get; } = new();
        public CekiSatiri Satir { get; }
        public Sandik Sandik10 { get; }
        public Sandik Sandik11 { get; }
        public int Icerik11Id => Uow.Repo<SandikIcerik>().Rows.Single(i => i.SandikId == Sandik11.Id).Id;

        public Kurgu()
        {
            var proje = new Proje { Id = ProjeId, ProjeNo = "PA559-01", ProjeTipiId = (int)ProjeTipi.Normal };
            var ceki = new Ceki { Id = 5591, ProjeId = proje.Id, Proje = proje };
            Satir = new CekiSatiri
            {
                Id = SatirId,
                CekiId = ceki.Id,
                Ceki = ceki,
                SiraNo = 1,
                BarkodNo = "PA559-BARKOD",
                Aciklama = "PA559 iki sandıklı ürün",
                IstenenAdet = 2,
                BirimId = (int)Birim.Adet,
                CekideGecenSandikNo = "10",
                FiiliSandikNo = "10",
                GridDurumuId = (int)GridDurum.Bekliyor,
                GridSevkDurumuId = (int)GridSevkDurum.Bekliyor,
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                DurumId = (int)UrunDurum.Bekliyor
            };
            Sandik10 = new Sandik { Id = 1010, ProjeId = proje.Id, Proje = proje, SandikNo = "10", DurumId = (int)SandikDurum.Hazirlaniyor, DepoLokasyonId = (int)DepoLokasyon.UcK };
            Sandik11 = new Sandik { Id = 1011, ProjeId = proje.Id, Proje = proje, SandikNo = "11", DurumId = (int)SandikDurum.Hazirlaniyor, DepoLokasyonId = (int)DepoLokasyon.UcK };
            var icerik = new SandikIcerik
            {
                Id = Icerik10Id,
                SandikId = Sandik10.Id,
                Sandik = Sandik10,
                CekiSatiriId = Satir.Id,
                CekiSatiri = Satir,
                TahsisMiktari = 2,
                KonulanAdet = 0,
                EksikAdet = 2,
                BirimId = (int)Birim.Adet
            };
            proje.Cekiler.Add(ceki);
            proje.Sandiklar.Add(Sandik10);
            proje.Sandiklar.Add(Sandik11);
            ceki.CekiSatirlari.Add(Satir);
            Satir.SandikIcerikleri.Add(icerik);
            Sandik10.SandikIcerikleri.Add(icerik);

            Uow.Repo<Proje>().Rows.Add(proje);
            Uow.Repo<Ceki>().Rows.Add(ceki);
            Uow.Repo<CekiSatiri>().Rows.Add(Satir);
            Uow.Repo<Sandik>().Rows.Add(Sandik10);
            Uow.Repo<Sandik>().Rows.Add(Sandik11);
            Uow.Repo<SandikIcerik>().Rows.Add(icerik);
        }

        public async Task AdimOk(string ad, Task<Result> islem)
        {
            var sonuc = await islem;
            NavigasyonlariBagla();
            Assert.True(sonuc.IsSuccess, $"{ad}: {sonuc.Error?.Message}");
        }

        /// <summary>
        /// Veritabanı sayaçlarını ve iki okuma modelinin her sandık satırını doğrular:
        /// satır (parti) bazlı sayaçlar her sandık satırında satırın gerçek değeriyle döner.
        /// </summary>
        public async Task SatirDogrula(decimal gridGelen, decimal gridSevk, decimal aktifKarsilanan,
            decimal yenidenSevk, decimal gelen, decimal geriGonderilen)
        {
            Assert.Equal(gridGelen, Satir.GridGelenAdet);
            Assert.Equal(gridSevk, Satir.GridSevkMiktari);
            Assert.Equal(aktifKarsilanan, Satir.AktifGridSevkKarsilananMiktari);
            Assert.Equal(yenidenSevk, Satir.YenidenSevkGerekliAdet);
            Assert.Equal(gelen, Satir.GelenMiktar);
            Assert.Equal(geriGonderilen, Satir.GeriGonderilenMiktar);

            var grid = await GridUrunleriAsync();
            Assert.Equal(2, grid.Count);
            foreach (var d in grid)
            {
                Assert.True(d.SandikBazliDagitim);
                Assert.Equal(gridGelen, d.GridGelenAdet);
                Assert.Equal(gridSevk, d.GridSevkMiktari);
                Assert.Equal(yenidenSevk, d.YenidenSevkGerekliAdet);
                Assert.Equal(geriGonderilen, d.GeriGonderilenMiktar);
                Assert.Equal(0, d.TrafoSevkAdet);
                Assert.Equal(Math.Max(Satir.IstenenAdet - gridGelen, 0), d.GridEksikMiktar);
            }

            var uck = await UcKUrunleriAsync();
            Assert.Equal(2, uck.Count);
            foreach (var d in uck)
            {
                Assert.Equal(gridGelen, d.GridGelenAdet);
                Assert.Equal(gridSevk, d.GridSevkMiktari);
                Assert.Equal(geriGonderilen, d.GeriGonderilenMiktar);
            }
        }

        public void IcerikDogrula(decimal s10Konulan, decimal s10Aktif, decimal s11Konulan, decimal s11Aktif)
        {
            var icerikler = Uow.Repo<SandikIcerik>().Rows.Where(i => i.CekiSatiriId == Satir.Id).OrderBy(i => i.Id).ToList();
            Assert.Equal([s10Konulan, s11Konulan], icerikler.Select(i => i.KonulanAdet));
            Assert.Equal([s10Aktif, s11Aktif], icerikler.Select(i => i.AktifGridSevkKarsilananMiktari ?? 0));
        }

        public async Task<List<_3K.Application.Features.UcKIslemleri.DTOs.UcKUrunDto>> UcKUrunleriAsync()
        {
            var sonuc = await new _3K.Application.Features.UcKIslemleri.Queries.GetUcKUrunlerQueryHandler(Uow, _lookup, _saha)
                .Handle(new _3K.Application.Features.UcKIslemleri.Queries.GetUcKUrunlerQuery { ProjeId = ProjeId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        /// <summary>Bellek içi depoda yeni eklenen içeriklerin gezinme özelliklerini EF gibi bağlar.</summary>
        private void NavigasyonlariBagla()
        {
            foreach (var i in Uow.Repo<SandikIcerik>().Rows.Where(i => i.CekiSatiriId == Satir.Id))
            {
                i.CekiSatiri ??= Satir;
                i.Sandik ??= i.SandikId == Sandik10.Id ? Sandik10 : Sandik11;
            }
        }

        public async Task<List<GridUrunDto>> GridUrunleriAsync()
        {
            var sonuc = await new GetGridUrunlerQueryHandler(Uow, _lookup, _saha)
                .Handle(new GetGridUrunlerQuery { ProjeId = ProjeId }, default);
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            return sonuc.Value!;
        }

        public Task<Result> TasiAsync(decimal miktar) =>
            new SandikUrunTasiCommandHandler(Uow, _user, _saha)
                .Handle(new SandikUrunTasiCommand
                {
                    KaynakSandikIcerikId = Icerik10Id,
                    HedefSandikId = Sandik11.Id,
                    TasinanAdet = miktar,
                    ProjeId = ProjeId,
                    IslemAnahtari = Guid.NewGuid()
                }, default);

        public Task<Result> TopluGridSevkAsync() =>
            new GridTopluSevkCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridTopluSevkCommand { ProjeId = ProjeId, CekiSatiriIdler = [Satir.Id], Aciklama = "Toplu sevk" }, default);

        public Task<Result> GridSevkAsync(GridDurum durum, decimal? gridGelen, decimal sevk) =>
            new GridDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new GridDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    YeniDurumId = (int)durum,
                    GridGelenAdet = gridGelen,
                    GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
                    SevkMiktari = sevk,
                    Aciklama = "Grid sevk"
                }, default);

        public Task<Result> UcKAsync(UcKDurum durum, decimal? adet, int? sandikIcerikId) =>
            new UcKDurumGuncelleCommandHandler(Uow, _user, _durum, _hareket, _lookup, _saha)
                .Handle(new UcKDurumGuncelleCommand
                {
                    ProjeId = ProjeId,
                    CekiSatiriId = Satir.Id,
                    SandikIcerikId = sandikIcerikId,
                    KarsilamaTipiId = (int)durum,
                    GelenAdet = adet,
                    GeriGonderilmeSebebiId = durum == UcKDurum.GeriGonderildi ? 1 : null,
                    Aciklama = "3K işlemi"
                }, default);

        public void Dispose() => Uow.Dispose();
    }

    internal sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = [];
        public bool HasActiveTransaction => false;
        public Repo<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo))
                _repos[typeof(T)] = repo = new Repo<T>();
            return (Repo<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) { }
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) { }
        public void Dispose() { }
    }

    internal sealed class Repo<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public Task<T?> GetByIdAsync(int id) => Task.FromResult(Rows.SingleOrDefault(e => e.Id == id));
        public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(Rows);
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProperty>(Expression<Func<T, TProperty>> include) => GetAllAsync();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            Task.FromResult<IEnumerable<T>>(Rows.Where(predicate.Compile()).ToList());
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task AddAsync(T entity)
        {
            if (entity.Id <= 0)
                entity.Id = Rows.Count == 0 ? 1 : Rows.Max(e => e.Id) + 1;
            Rows.Add(entity);
            return Task.CompletedTask;
        }
        public void Update(T entity) { }
        public void Remove(T entity) => Rows.Remove(entity);
    }

    private sealed class CurrentUserStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
    }

    private sealed class HareketStub : IHareketService
    {
        public Task HareketKaydetAsync(HareketGecmisi hareket) => Task.CompletedTask;
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int projeId) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string referansTipi, string referansId) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int projeId, string? searchTerm, int? islemTipiId, int pageNumber, int pageSize) => throw new NotSupportedException();
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
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new Dictionary<int, decimal>());
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.FromResult(new HashSet<int>());
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
