using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Features.ProjeIslemleri.Commands;
using _3K.Application.Features.SandikIslemleri.Commands;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;
using _3K.Infrastructure.Repositories;

namespace _3K.Application.Tests;

public class SevkiyatKilidiAcRegresyonTests
{
    [Theory]
    [InlineData(true, ProjeTipi.Normal)]
    [InlineData(false, ProjeTipi.Normal)]
    [InlineData(true, ProjeTipi.Saha)]
    [InlineData(false, ProjeTipi.Saha)]
    [InlineData(true, ProjeTipi.Yedek)]
    [InlineData(false, ProjeTipi.Yedek)]
    public async Task KaydiKoruyarakAc_SevkiyatVeProjeDegismez_YalnizDuzeltmeBayragiAcilir(
        bool projeKilidi, ProjeTipi tip)
    {
        using var kurgu = new Kurgu(tip);

        var sonuc = await kurgu.KilidiAcAsync(projeKilidi, SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var proje = Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>());
        Assert.Equal(EntityState.Unchanged, proje.State);
        Assert.Equal((int)ProjeDurum.SevkEdildi, proje.Entity.DurumId);
        Assert.Equal(kurgu.SevkTarihi, proje.Entity.GerceklesenSevkTarihi);
        var sandik = Assert.Single(kurgu.Context.ChangeTracker.Entries<Sandik>());
        Assert.True(sandik.Entity.SevkiyatDuzeltmeAcikMi);
        Assert.Equal((int)SandikDurum.Sevkedildi, sandik.Entity.DurumId);
        Assert.Equal((int)SandikDurum.Kapandi, sandik.Entity.SevkOncesiDurumId);
        Assert.All(sandik.Properties.Where(p => !Equals(p.OriginalValue, p.CurrentValue)),
            p => Assert.Contains(p.Metadata.Name, new[] { "SevkiyatDuzeltmeAcikMi", "UpdatedDate", "UpdatedBy" }));
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<SevkiyatSandik>());
        Assert.Equal(tip == ProjeTipi.Saha ? 1 : 0, kurgu.Saha.SenkronizasyonSayisi);
        Assert.Single(kurgu.Hareket.Kayitlar);
        Assert.Equal(1, kurgu.SaveDenetimi.CagriSayisi);
    }

    [Theory]
    [InlineData(true, ProjeTipi.Normal)]
    [InlineData(false, ProjeTipi.Normal)]
    [InlineData(true, ProjeTipi.Saha)]
    [InlineData(false, ProjeTipi.Saha)]
    [InlineData(true, ProjeTipi.Yedek)]
    [InlineData(false, ProjeTipi.Yedek)]
    public async Task SevkiyatiGeriAl_EskiDurumVeBagKaldirmaAkisiKorunur(bool projeKilidi, ProjeTipi tip)
    {
        using var kurgu = new Kurgu(tip);

        var sonuc = await kurgu.KilidiAcAsync(projeKilidi, SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var proje = Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>()).Entity;
        Assert.Equal((int)ProjeDurum.Hazirlaniyor, proje.DurumId);
        Assert.Null(proje.GerceklesenSevkTarihi);
        var sandik = Assert.Single(kurgu.Context.ChangeTracker.Entries<Sandik>()).Entity;
        Assert.Equal((int)SandikDurum.Kapandi, sandik.DurumId);
        Assert.False(sandik.SevkiyatDuzeltmeAcikMi);
        Assert.Null(sandik.SevkOncesiDurumId);
        Assert.Equal(EntityState.Deleted, Assert.Single(kurgu.Context.ChangeTracker.Entries<SevkiyatSandik>()).State);
        Assert.Equal(tip == ProjeTipi.Saha ? 1 : 0, kurgu.Saha.SenkronizasyonSayisi);
    }

    [Theory]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc)]
    [InlineData(SevkiyatKilitAcmaTipi.SevkiyatGeriAlinarakAc)]
    public async Task YalnizSahadanSevkEdilenNormalProje_AnaProjeKilidiAcilamaz(SevkiyatKilitAcmaTipi tip)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal, fizikselSevk: false);
        kurgu.Saha.Durum = new KaynakSandikSahaAktarimDurumu
        {
            SahaUzerindenSevkEdilenSandikIds = new HashSet<int> { 20 }
        };

        var sonuc = await kurgu.KilidiAcAsync(true, tip);

        Assert.False(sonuc.IsSuccess);
        Assert.Contains("yalnızca saha projesi", sonuc.Error!.Message);
        Assert.Equal(0, kurgu.SaveDenetimi.CagriSayisi);
    }

    [Fact]
    public async Task EskiTakipsizSandikGuncellemesi_InterceptorTarafindanEngellenir()
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal);
        var repo = kurgu.UnitOfWork.GetRepository<Sandik>();
        var sandik = Assert.Single(await repo.FindAsync(s => s.ProjeId == 10));
        Assert.Equal(EntityState.Detached, kurgu.Context.Entry(sandik).State);
        sandik.SevkiyatDuzeltmeAcikMi = true;
        repo.Update(sandik);

        await Assert.ThrowsAsync<ProjectLockedException>(() => kurgu.UnitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task DegismeyenProjeyeUpdate_Cagirmak_KorunanSevkiyatKilidiniYenidenTetikler()
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal);
        var sandik = await kurgu.UnitOfWork.GetRepository<Sandik>().GetByIdAsync(20);
        sandik!.SevkiyatDuzeltmeAcikMi = true;
        kurgu.UnitOfWork.GetRepository<Sandik>().Update(sandik);
        var proje = await kurgu.UnitOfWork.GetRepository<Proje>().GetByIdAsync(10);
        kurgu.UnitOfWork.GetRepository<Proje>().Update(proje!);

        await Assert.ThrowsAsync<ProjectLockedException>(() => kurgu.UnitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task DuzeltmeBayragi_IzinsizProjeVeyaSandikDegisiklikleriniMuafTutmaz()
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal);
        var sandik = await kurgu.UnitOfWork.GetRepository<Sandik>().GetByIdAsync(20);
        sandik!.SevkiyatDuzeltmeAcikMi = true;
        sandik.Boy = 9999;
        kurgu.UnitOfWork.GetRepository<Sandik>().Update(sandik);

        await Assert.ThrowsAsync<ProjectLockedException>(() => kurgu.UnitOfWork.SaveChangesAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ZatenAcikDuzeltme_TekrarTalepteGereksizGuncellemeOlusturmaz(bool projeKilidi)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal, zatenAcik: true);

        var sonuc = await kurgu.KilidiAcAsync(projeKilidi, SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.All(kurgu.Context.ChangeTracker.Entries(), e => Assert.Equal(EntityState.Unchanged, e.State));
    }

    [Fact]
    public async Task ProjedeBirSandikZatenAciksa_YalnizDigerSandikDuzeltmeyeAcilir()
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal, zatenAcik: true, ikinciSandik: true);

        var sonuc = await kurgu.KilidiAcAsync(true, SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var sandiklar = kurgu.Context.ChangeTracker.Entries<Sandik>().ToList();
        Assert.Equal(2, sandiklar.Count);
        Assert.All(sandiklar, s => Assert.True(s.Entity.SevkiyatDuzeltmeAcikMi));
        Assert.Equal(EntityState.Unchanged, sandiklar.Single(s => s.Entity.Id == 20).State);
        Assert.Equal(EntityState.Modified, sandiklar.Single(s => s.Entity.Id == 21).State);
        Assert.Equal(EntityState.Unchanged, Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>()).State);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EksigiOlanNormalProje_KaydiKoruyarakAc_DurumuEksikSevkYapar_SevkiyatiKorur(bool projeKilidi)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal, eksikUrun: true);

        var sonuc = await kurgu.KilidiAcAsync(projeKilidi, SevkiyatKilitAcmaTipi.SevkiyatKaydiKorunarakAc);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var proje = Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>()).Entity;
        Assert.Equal((int)ProjeDurum.EksikSevkEdildi, proje.DurumId);
        Assert.Equal(kurgu.SevkTarihi, proje.GerceklesenSevkTarihi);
        var sandik = Assert.Single(kurgu.Context.ChangeTracker.Entries<Sandik>()).Entity;
        Assert.Equal((int)SandikDurum.Sevkedildi, sandik.DurumId);
        Assert.Equal((int)SandikDurum.Kapandi, sandik.SevkOncesiDurumId);
        Assert.True(sandik.SevkiyatDuzeltmeAcikMi);
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<SevkiyatSandik>());
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<CekiSatiri>());
        Assert.Equal(0, kurgu.Saha.SenkronizasyonSayisi);
    }

    [Theory]
    [InlineData(ProjeDurum.SevkEdildi, true, ProjeDurum.EksikSevkEdildi)]
    [InlineData(ProjeDurum.EksikSevkEdildi, false, ProjeDurum.SevkEdildi)]
    [InlineData(ProjeDurum.SevkEdildi, false, ProjeDurum.SevkEdildi)]
    [InlineData(ProjeDurum.EksikSevkEdildi, true, ProjeDurum.EksikSevkEdildi)]
    public async Task DuzeltmeTamamla_NormalProjeDurumuGuncelUrunlerleUyumlu_SevkiyatKorunur(
        ProjeDurum oncekiDurum, bool eksikUrun, ProjeDurum beklenenDurum)
    {
        using var kurgu = new Kurgu(ProjeTipi.Normal, zatenAcik: true, eksikUrun: eksikUrun,
            projeDurumu: oncekiDurum);

        var sonuc = await kurgu.DuzeltmeyiTamamlaAsync();

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var proje = Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>());
        Assert.Equal((int)beklenenDurum, proje.Entity.DurumId);
        Assert.Equal(kurgu.SevkTarihi, proje.Entity.GerceklesenSevkTarihi);
        if (oncekiDurum == beklenenDurum)
            Assert.Equal(EntityState.Unchanged, proje.State);
        var sandik = Assert.Single(kurgu.Context.ChangeTracker.Entries<Sandik>()).Entity;
        Assert.False(sandik.SevkiyatDuzeltmeAcikMi);
        Assert.Equal((int)SandikDurum.Sevkedildi, sandik.DurumId);
        Assert.Equal((int)SandikDurum.Kapandi, sandik.SevkOncesiDurumId);
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<SevkiyatSandik>());
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<CekiSatiri>());
        Assert.Equal(0, kurgu.Saha.SenkronizasyonSayisi);
    }

    [Theory]
    [InlineData(ProjeTipi.Saha)]
    [InlineData(ProjeTipi.Yedek)]
    public async Task DuzeltmeTamamla_SahaYedekProjeDurumunuDegistirmez(ProjeTipi tip)
    {
        using var kurgu = new Kurgu(tip, zatenAcik: true, eksikUrun: true);

        var sonuc = await kurgu.DuzeltmeyiTamamlaAsync();

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        var proje = Assert.Single(kurgu.Context.ChangeTracker.Entries<Proje>());
        Assert.Equal(EntityState.Unchanged, proje.State);
        Assert.Equal((int)ProjeDurum.SevkEdildi, proje.Entity.DurumId);
        Assert.Equal(kurgu.SevkTarihi, proje.Entity.GerceklesenSevkTarihi);
        Assert.False(Assert.Single(kurgu.Context.ChangeTracker.Entries<Sandik>()).Entity.SevkiyatDuzeltmeAcikMi);
        Assert.Empty(kurgu.Context.ChangeTracker.Entries<SevkiyatSandik>());
        Assert.Equal(tip == ProjeTipi.Saha ? 1 : 0, kurgu.Saha.SenkronizasyonSayisi);
    }

    // Gerçek EF change tracker, GenericRepository ve interceptor kullanılır. Yalnız SQL okuma
    // cevapları sabittir; bağlantı açılması ve SaveChanges yazımı bastırılır, hiçbir DB'ye erişilmez.
    private sealed class Kurgu : IDisposable
    {
        public AppDbContext Context { get; }
        public UnitOfWork UnitOfWork { get; }
        public SahaStub Saha { get; } = new();
        public HareketStub Hareket { get; } = new();
        public YazmayiEngelle SaveDenetimi { get; } = new();
        public DateTime SevkTarihi { get; } = new(2026, 9, 1);

        public Kurgu(ProjeTipi tip, bool fizikselSevk = true, bool zatenAcik = false, bool ikinciSandik = false,
            bool eksikUrun = false, ProjeDurum projeDurumu = ProjeDurum.SevkEdildi)
        {
            var proje = new Proje { Id = 10, ProjeTipiId = (int)tip, DurumId = (int)projeDurumu,
                GerceklesenSevkTarihi = SevkTarihi, ProjeNo = "TEST-10" };
            var sandik = new Sandik { Id = 20, ProjeId = 10, SandikNo = "1", Boy = 1000,
                DurumId = (int)(fizikselSevk ? SandikDurum.Sevkedildi : SandikDurum.Kapandi),
                SevkOncesiDurumId = (int)SandikDurum.Kapandi, SevkiyatDuzeltmeAcikMi = zatenAcik };
            var sandiklar = new List<Sandik> { sandik };
            if (ikinciSandik)
                sandiklar.Add(new Sandik { Id = 21, ProjeId = 10, SandikNo = "2",
                    DurumId = (int)SandikDurum.Sevkedildi, SevkOncesiDurumId = (int)SandikDurum.Kapandi });
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
                .AddInterceptors(new BaglantiyiEngelle(), new OkumaSonuclari(proje, sandiklar, eksikUrun),
                    new AuditInterceptor(new KullaniciStub()), new ProjectLockInterceptor(), SaveDenetimi)
                .Options;
            Context = new AppDbContext(options);
            UnitOfWork = new UnitOfWork(Context, NullLogger<UnitOfWork>.Instance);
        }

        public Task<_3K.Application.Common.Result> KilidiAcAsync(bool projeKilidi, SevkiyatKilitAcmaTipi tip) =>
            projeKilidi
                ? new ProjeKilidiAcCommandHandler(UnitOfWork, Hareket, new KullaniciStub(), Saha, new EntityFrameworkReadQueryExecutor()).Handle(
                    new ProjeKilidiAcCommand { ProjeId = 10, KilitAcmaTipiId = (int)tip }, default)
                : new SandikKilidiAcCommandHandler(UnitOfWork, Hareket, new KullaniciStub(), Saha, new EntityFrameworkReadQueryExecutor()).Handle(
                    new SandikKilidiAcCommand { ProjeId = 10, SandikId = 20, KilitAcmaTipiId = (int)tip }, default);

        public Task<_3K.Application.Common.Result> DuzeltmeyiTamamlaAsync() =>
            new SandikSevkiyatDuzeltmeTamamlaCommandHandler(UnitOfWork, Hareket, new KullaniciStub(),
                Saha, new EntityFrameworkReadQueryExecutor()).Handle(
                new SandikSevkiyatDuzeltmeTamamlaCommand { ProjeId = 10, SandikId = 20 }, default);

        public void Dispose() => Context.Dispose();
    }

    private sealed class BaglantiyiEngelle : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class YazmayiEngelle : SaveChangesInterceptor
    {
        public int CagriSayisi { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            CagriSayisi++;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }

    private sealed class OkumaSonuclari(Proje proje, List<Sandik> sandiklar, bool eksikUrun) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT ", command.CommandText);
            var tableName = Regex.Match(command.CommandText, "FROM \"([^\"]+)\"").Groups[1].Value;
            IEnumerable<BaseEntity> entities = tableName switch
            {
                "Projeler" => command.CommandText.Contains("\"DurumId\" = 5") &&
                    proje.DurumId != (int)ProjeDurum.SevkEdildi ? [] : new[] { proje },
                "Sandiklar" => sandiklar,
                "CekiSatirlari" => new[] { new CekiSatiri { Id = 50, CekiId = 60,
                    IstenenAdet = 4, GelenMiktar = eksikUrun ? 3 : 4 } },
                "SevkiyatSandiklari" => new[] { new SevkiyatSandik { Id = 30, SandikId = 20, SevkiyatId = 40 } },
                "SandikIcerikleri" => [],
                "SahaAktarimKalemleri" => [],
                "Cekiler" => [],
                _ => throw new InvalidOperationException($"Beklenmeyen sorgu: {command.CommandText}")
            };
            var projection = command.CommandText[..command.CommandText.IndexOf("FROM", StringComparison.Ordinal)];
            var columns = Regex.Matches(projection, "[a-z][a-z0-9]*\\.(?:\"([^\"]+)\"|(xmin))");
            var table = new DataTable();
            var entityType = tableName switch
            {
                "Projeler" => typeof(Proje),
                "Sandiklar" => typeof(Sandik),
                "CekiSatirlari" => typeof(CekiSatiri),
                "SevkiyatSandiklari" => typeof(SevkiyatSandik),
                "SandikIcerikleri" => typeof(SandikIcerik),
                "SahaAktarimKalemleri" => typeof(SahaAktarimKalemi),
                "Cekiler" => typeof(Ceki),
                _ => throw new InvalidOperationException(tableName)
            };
            var properties = columns.Select(column =>
            {
                var name = column.Groups[1].Success ? column.Groups[1].Value : column.Groups[2].Value;
                var property = entityType.GetProperty(name == "xmin" ? "Version" : name)!;
                table.Columns.Add(name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
                return property;
            }).ToList();
            foreach (var entity in entities)
            {
                table.Rows.Add(properties.Select(property => property.GetValue(entity) ?? DBNull.Value).ToArray());
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private sealed class KullaniciStub : ICurrentUserService
    {
        public int? UserId => 7;
        public bool IsAuthenticated => true;
        public string? MenuKod => null;
    }

    private sealed class HareketStub : IHareketService
    {
        public List<HareketGecmisi> Kayitlar { get; } = new();
        public Task HareketKaydetAsync(HareketGecmisi hareket) { Kayitlar.Add(hareket); return Task.CompletedTask; }
        public Task<IEnumerable<HareketGecmisi>> GetProjeHareketleriAsync(int id) => throw new NotSupportedException();
        public Task<IEnumerable<HareketGecmisi>> GetUrunHareketleriAsync(string tip, string id) => throw new NotSupportedException();
        public Task<(IEnumerable<HareketGecmisi> Items, int TotalCount)> GetPaginatedProjeHareketleriAsync(
            int id, string? arama, int? tip, int sayfa, int boyut) => throw new NotSupportedException();
    }

    private sealed class SahaStub : ISahaTamamlamaService
    {
        public KaynakSandikSahaAktarimDurumu Durum { get; set; } = new();
        public int SenkronizasyonSayisi { get; private set; }
        public Task<KaynakSandikSahaAktarimDurumu> GetKaynakSandikSahaAktarimDurumuAsync(
            IEnumerable<int> ids, CancellationToken token = default) => Task.FromResult(Durum);
        public Task SenkronizeKaynakProjelerBySahaSandikIdsAsync(IEnumerable<int> ids, CancellationToken token = default)
        { Assert.Equal(new[] { 20 }, ids); SenkronizasyonSayisi++; return Task.CompletedTask; }
        public Task<Dictionary<int, decimal>> GetSevkEdilenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<Dictionary<int, decimal>> GetAktifIsTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default) => GetAktifGerceklesenTamamlamaMapAsync(ids, token);
        public Task<Dictionary<int, decimal>> GetSevkEdilenGerceklesenTamamlamaMapAsync(IEnumerable<int> ids, CancellationToken token = default)
        { Assert.Equal(new[] { 50 }, ids); return Task.FromResult(new Dictionary<int, decimal>()); }
        public Task<HashSet<int>> GetAktifSandikBazliAktarimSatirIdsAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> AktifTamamlamaVarMiAsync(int id, CancellationToken token = default) => throw new NotSupportedException();
        public Task SenkronizeKaynakProjelerAsync(IEnumerable<int> ids, CancellationToken token = default) => throw new NotSupportedException();
    }
}
