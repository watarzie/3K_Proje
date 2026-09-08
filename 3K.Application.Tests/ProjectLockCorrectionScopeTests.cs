using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Data.Interceptors;

namespace _3K.Application.Tests;

public class ProjectLockCorrectionScopeTests
{
    [Theory]
    [InlineData("sandik")]
    [InlineData("icerik")]
    [InlineData("satir")]
    public async Task KaliciDuzeltmeOnayi_YalnizBagliKaydinDuzenlenmesineIzinVerir(string tur)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        if (tur == "sandik") db.Load<Sandik>(20).Boy = 2000;
        if (tur == "icerik") db.Load<SandikIcerik>(30).KonulanAdet = 2;
        if (tur == "satir") db.Load<CekiSatiri>(40).GridGelenAdet = 2;

        await db.Context.SaveChangesAsync();

        Assert.Equal(1, db.Writes.SuppressedSaves);
        Assert.InRange(db.Reads.CommandCount, 1, 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AyniProjedeBaskaSandiginAcikOlmasi_KilitliKomsuyuAcmaz(bool ayniSave)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = !ayniSave;
        db.Crates.Add(new Sandik { Id = 21, ProjeId = 1, DurumId = 4 });
        if (ayniSave) db.Load<Sandik>(20).SevkiyatDuzeltmeAcikMi = true;
        db.Load<Sandik>(21).Boy = 9999;

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("sandik")]
    [InlineData("icerik")]
    public async Task AyniSaveIcindekiBayrakAcma_YeniDegisikliklereOnaySayilmaz(string tur)
    {
        using var db = new Fixture();
        db.Load<Sandik>(20).SevkiyatDuzeltmeAcikMi = true;
        if (tur == "sandik") db.Load<Sandik>(20).Boy = 9999;
        else db.Load<SandikIcerik>(30).KonulanAdet = 2;

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrtakCekiSatiri_TumSevkEdilmisTahsislerAcikOlmalidir(bool hepsiAcik)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Crates.Add(new Sandik { Id = 21, ProjeId = 1, DurumId = 4, SevkiyatDuzeltmeAcikMi = hepsiAcik });
        db.Contents.Add(new SandikIcerik { Id = 31, SandikId = 21, CekiSatiriId = 40 });
        db.Load<CekiSatiri>(40).GridGelenAdet = 2;

        if (hepsiAcik) await db.Context.SaveChangesAsync();
        else await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("sandik")]
    [InlineData("icerik")]
    [InlineData("satir")]
    public async Task DetachedSahiplikDegisimi_OrijinalKilitliProjedenKacamaz(string tur)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Projects.Add(new Proje { Id = 2, DurumId = 1 });
        db.PackingLists.Add(new Ceki { Id = 11, ProjeId = 2 });
        db.Crates.Add(new Sandik { Id = 22, ProjeId = 2, DurumId = 1 });
        if (tur == "sandik") db.Context.Update(new Sandik { Id = 20, ProjeId = 2, DurumId = 4, SevkiyatDuzeltmeAcikMi = true });
        if (tur == "icerik") db.Context.Update(new SandikIcerik { Id = 30, SandikId = 22, CekiSatiriId = 40 });
        if (tur == "satir") db.Context.Update(new CekiSatiri { Id = 40, CekiId = 11, IstenenAdet = 99 });

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task IceriginCekiReferansiniDegistirme_DuzeltmeYetkisiniGenisletemez()
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Load<SandikIcerik>(30).CekiSatiriId = null;

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("proje")]
    [InlineData("ceki")]
    [InlineData("yeni-sandik")]
    [InlineData("sil-sandik")]
    [InlineData("yeni-satir")]
    public async Task SandikDuzeltmesi_ProjeCekiVeyaYeniSandikYetkisiDegildir(string tur)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        if (tur == "proje") db.Load<Proje>(1).Musteri = "Yeni";
        if (tur == "ceki") db.Load<Ceki>(10).Aciklama = "Yeni";
        if (tur == "yeni-sandik") db.Context.Add(new Sandik { ProjeId = 1 });
        if (tur == "sil-sandik") db.Context.Remove(db.Load<Sandik>(20));
        if (tur == "yeni-satir") db.Context.Add(new CekiSatiri { CekiId = 10, IstenenAdet = 1 });

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcikHedefSahaSandiginaManuelMalzemeEklenebilir_AktifKaynakKorunur(bool kaynak)
    {
        using var db = new Fixture();
        db.Projects[0].ProjeTipiId = (int)ProjeTipi.Saha;
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Transfers.Add(new SahaAktarimKalemi
        {
            Id = 50, KaynakCekiSatiriId = kaynak ? 40 : 99,
            KaynakSandikId = kaynak ? 20 : 99, SahaSandikId = kaynak ? 99 : 20,
            SahaCekiSatiriId = kaynak ? 99 : 40, DurumId = (int)SahaAktarimDurum.SevkiyatDuzeltmede
        });
        db.Context.Add(new SandikIcerik { SandikId = 20, Isim = "Manuel", Miktar = 1 });

        if (kaynak) await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
        else await db.Context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(SahaAktarimDurum.Planlandi)]
    [InlineData(SahaAktarimDurum.SevkEdildi)]
    [InlineData(SahaAktarimDurum.SevkiyatDuzeltmede)]
    public async Task AktifSahaKaynakSatiri_DuzeltmeBayragiylaSerbestKalmaz(SahaAktarimDurum durum)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Transfers.Add(new SahaAktarimKalemi { Id = 50, KaynakCekiSatiriId = 40, DurumId = (int)durum });
        db.Load<CekiSatiri>(40).GridGelenAdet = 2;

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task EskiDeftersizSahaBaglantisi_KaynakSatiriKorumayaDevamEder()
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.LegacySourceRows.Add(40);
        db.Load<CekiSatiri>(40).GridGelenAdet = 2;

        await Assert.ThrowsAsync<ProjectLockedException>(() => db.Context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(SahaAktarimDurum.GeriAlindi)]
    [InlineData(SahaAktarimDurum.Iptal)]
    public async Task GeriAlinmisVeyaIptalSahaBaglantisi_YeniAktifBagGibiEngellemez(SahaAktarimDurum durum)
    {
        using var db = new Fixture();
        db.Crates[0].SevkiyatDuzeltmeAcikMi = true;
        db.Transfers.Add(new SahaAktarimKalemi { Id = 50, KaynakCekiSatiriId = 40, DurumId = (int)durum });
        db.Load<CekiSatiri>(40).GridGelenAdet = 2;

        await db.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task SevkEdilmemisProje_EkDuzeltmeOnayiGerektirmez()
    {
        using var db = new Fixture();
        db.Projects[0].DurumId = (int)ProjeDurum.Hazirlaniyor;
        db.Load<SandikIcerik>(30).KonulanAdet = 2;

        await db.Context.SaveChangesAsync();
    }

    // EF gerçek tracker ve üretimdeki interceptor çalışır. Bağlantı açılmaz; SQL yazımları
    // bastırılır. SELECT cevapları yalnız bu testteki kalıcı durum örneğinden üretilir.
    private sealed class Fixture : IDisposable
    {
        public List<Proje> Projects { get; } = [new() { Id = 1, DurumId = 5 }];
        public List<Ceki> PackingLists { get; } = [new() { Id = 10, ProjeId = 1 }];
        public List<CekiSatiri> Rows { get; } = [new() { Id = 40, CekiId = 10, IstenenAdet = 2 }];
        public List<Sandik> Crates { get; } = [new() { Id = 20, ProjeId = 1, DurumId = 4, Boy = 1000 }];
        public List<SandikIcerik> Contents { get; } = [new() { Id = 30, SandikId = 20, CekiSatiriId = 40 }];
        public List<SahaAktarimKalemi> Transfers { get; } = [];
        public List<int> LegacySourceRows { get; } = [];
        public AppDbContext Context { get; }
        public ReadStub Reads { get; }
        public WriteStub Writes { get; } = new();

        public Fixture()
        {
            Reads = new ReadStub(this);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
                .AddInterceptors(new ConnectionStub(), Reads, new ProjectLockInterceptor(), Writes).Options;
            Context = new AppDbContext(options);
        }

        public T Load<T>(int id) where T : BaseEntity, new()
        {
            var tracked = Context.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity.Id == id)?.Entity;
            if (tracked != null) return tracked;
            var original = All().OfType<T>().Single(x => x.Id == id);
            var entity = new T();
            foreach (var property in Context.Model.FindEntityType(typeof(T))!.GetProperties())
                property.PropertyInfo!.SetValue(entity, property.PropertyInfo.GetValue(original));
            Context.Attach(entity);
            return entity;
        }
        public IEnumerable<BaseEntity> All() => Projects.Cast<BaseEntity>().Concat(PackingLists).Concat(Rows)
            .Concat(Crates).Concat(Contents).Concat(Transfers);
        public void Dispose() => Context.Dispose();
    }

    private sealed class ConnectionStub : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
    private sealed class WriteStub : SaveChangesInterceptor
    {
        public int SuppressedSaves { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        { SuppressedSaves++; return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1)); }
    }
    private sealed class ReadStub(Fixture db) : DbCommandInterceptor
    {
        public int CommandCount { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            CommandCount++;
            Assert.StartsWith("SELECT ", command.CommandText);
            var sql = command.CommandText;
            var tableName = Regex.Match(sql, "FROM \"([^\"]+)\"").Groups[1].Value;
            var rowIds = Ids(command, "rowIds");
            var crateIds = Ids(command, "crateIds");
            IEnumerable<BaseEntity> entities = tableName switch
            {
                "Projeler" => db.Projects.Where(p => Ids(command, "projectIds").Contains(p.Id) && p.DurumId == 5),
                "Cekiler" => db.PackingLists.Where(c => Ids(command, "packingListIds").Contains(c.Id)),
                "Sandiklar" => db.Crates.Where(c => crateIds.Contains(c.Id)),
                "SandikIcerikleri" => db.Contents.Where(c => Ids(command, "contentIds").Contains(c.Id) ||
                    crateIds.Contains(c.SandikId) || (c.CekiSatiriId.HasValue && rowIds.Contains(c.CekiSatiriId.Value))),
                "CekiSatirlari" when sql.Contains("NOT EXISTS") => db.LegacySourceRows.Where(rowIds.Contains)
                    .Select(id => new CekiSatiri { KaynakCekiSatiriId = id }),
                "CekiSatirlari" => db.Rows.Where(r => rowIds.Contains(r.Id)),
                "SahaAktarimKalemleri" => db.Transfers.Where(t => t.DurumId != 6 && t.DurumId != 7 &&
                    ((t.KaynakSandikId.HasValue && crateIds.Contains(t.KaynakSandikId.Value)) || rowIds.Contains(t.KaynakCekiSatiriId))),
                _ => throw new InvalidOperationException(sql)
            };
            var entityType = tableName switch
            {
                "Projeler" => typeof(Proje), "Cekiler" => typeof(Ceki), "Sandiklar" => typeof(Sandik),
                "SandikIcerikleri" => typeof(SandikIcerik), "CekiSatirlari" => typeof(CekiSatiri),
                _ => typeof(SahaAktarimKalemi)
            };
            var projection = sql[..sql.IndexOf("FROM", StringComparison.Ordinal)];
            var columns = Regex.Matches(projection, "[a-z][a-z0-9]*\\.(?:\"([^\"]+)\"|(xmin))");
            var table = new DataTable();
            var properties = columns.Select(m => entityType.GetProperty(m.Groups[1].Success ? m.Groups[1].Value : "Version")!).ToList();
            foreach (var property in properties)
                table.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            foreach (var entity in entities)
                table.Rows.Add(properties.Select(p => p.GetValue(entity) ?? DBNull.Value).ToArray());
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
        private static HashSet<int> Ids(DbCommand command, string name) => command.Parameters.Cast<DbParameter>()
            .Where(p => p.ParameterName.TrimStart('@') == name || p.ParameterName.Contains($"__{name}_", StringComparison.Ordinal))
            .SelectMany(p => p.Value is IEnumerable<int> ids ? ids : [])
            .ToHashSet();
    }
}
