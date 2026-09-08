using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class SahaIsTamamlamaServiceTests
{
    [Theory]
    [InlineData(GridDurum.Iptal, 0, 0, 0, 0, 2)]
    [InlineData(GridDurum.GridKapandi, 0, 0, 0, 0, 2)]
    [InlineData(GridDurum.Gelmedi, 0, 0, 0, 0, 0)]
    [InlineData(GridDurum.Sipariste, 0, 0, 0, 0, 0)]
    [InlineData(GridDurum.TamGeldi, 0, 0, 0, 0, 0)]
    [InlineData(GridDurum.TamGeldi, 1, 0, 0, 0, 1)]
    [InlineData(GridDurum.TamGeldi, 2, 0, 0, 1, 1)]
    [InlineData(GridDurum.TamGeldi, 2, 0.5, 0, 0, 1.5)]
    [InlineData(GridDurum.TrafoSevk, 0, 0, 2, 0, 2)]
    [InlineData(GridDurum.TamGeldi, 3, 0, 0, 0, 2)]
    [InlineData(GridDurum.Iptal, 2, 0, 0, 1, 2)]
    public void IsTamamlanmasi_NormalProjeninKalanKuraliniKullanir(
        GridDurum grid, decimal gelen, decimal gonderilen, decimal trafo, decimal hatali, decimal beklenen)
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 2, GridDurumuId = (int)grid, GelenMiktar = gelen,
            GridGelenAdet = 2, ProjeGonderilen = gonderilen, TrafoSevkAdet = trafo, HataliMiktar = hatali
        };

        Assert.Equal(beklenen, HesaplaIs(satir));
        Assert.Equal(Math.Max(satir.IstenenAdet - satir.KalanMiktar, 0), HesaplaIs(satir));
        Assert.Equal(gelen, satir.GelenMiktar);
        Assert.Equal(2, satir.GridGelenAdet);
    }

    [Fact]
    public void IptalGeriAlininca_IsYenidenAcilir_FizikselTamamlanmaArtmaz()
    {
        var satir = new CekiSatiri { IstenenAdet = 2, GridDurumuId = (int)GridDurum.Iptal };
        Assert.Equal(2, HesaplaIs(satir));
        Assert.Equal(0, HesaplaFiziksel(istenen: 2, konulan: 0));

        satir.GridDurumuId = (int)GridDurum.Gelmedi;
        Assert.Equal(0, HesaplaIs(satir));
        Assert.Equal(0, HesaplaFiziksel(istenen: 2, konulan: 0));
        Assert.Empty(satir.SandikIcerikleri);
    }

    [Fact]
    public void HataliDurumVeKarsilamaKaynaklari_KalanIleAyniHesaplanir()
    {
        var satir = new CekiSatiri
        {
            IstenenAdet = 2, StokKarsilanan = .5m, ProjeKarsilanan = .5m,
            TedarikciKarsilanan = 1, DurumId = (int)UrunDurum.HataliUyumsuzGonderim
        };
        Assert.Equal(1, HesaplaIs(satir));
        satir.DurumId = (int)UrunDurum.Tamamlandi;
        Assert.Equal(2, HesaplaIs(satir));
    }

    [Fact]
    public async Task YeniVeLegacyIsTamamlanmasi_HedefVeAktarimMiktariylaSinirlanir()
    {
        var iptal = Satir(10, 101, 2, GridDurum.Iptal);
        var parcali = Satir(10, 102, 2, GridDurum.TamGeldi, gelen: .75m);
        var legacy = Satir(10, 103, 1, GridDurum.GridKapandi);
        var okuyucu = new SecimOkuyucu(
            yeni: [new(iptal, 2), new(iptal, 2), new(parcali, .5m)],
            legacy: [new(legacy, 1)]);
        await using var context = Context(okuyucu);
        var servis = new SahaTamamlamaService(context);

        var map = await servis.GetAktifIsTamamlamaMapAsync([10, 10, 0, -1]);

        Assert.Equal(3.5m, Assert.Single(map).Value); // iptal 2 + kısmi aktarım .5 + legacy 1
        Assert.Equal(10, Assert.Single(map).Key);
        Assert.Equal(2, okuyucu.Sorgular.Count);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(0, iptal.GelenMiktar);
        Assert.Equal(.75m, parcali.GelenMiktar);
    }

    [Fact]
    public async Task AktifHedefYoksa_VeyaDefterIptalGeriAlindiIse_LegacyIleCanlandirilmaz()
    {
        var okuyucu = new SecimOkuyucu(yeni: [], legacy: []);
        await using var context = Context(okuyucu);
        Assert.Empty(await new SahaTamamlamaService(context).GetAktifIsTamamlamaMapAsync([10]));
        Assert.Equal(2, okuyucu.Sorgular.Count);
    }

    [Fact]
    public async Task GecersizKaynakListesi_VeritabaniSorgusuYapmaz()
    {
        var okuyucu = new SecimOkuyucu([], []);
        await using var context = Context(okuyucu);
        Assert.Empty(await new SahaTamamlamaService(context).GetAktifIsTamamlamaMapAsync([0, -1]));
        Assert.Empty(okuyucu.Sorgular);
    }

    private static CekiSatiri Satir(int kaynak, int id, decimal istenen, GridDurum grid, decimal gelen = 0) => new()
    {
        Id = id, KaynakCekiSatiriId = kaynak, IstenenAdet = istenen,
        GridDurumuId = (int)grid, GelenMiktar = gelen
    };

    private static decimal HesaplaIs(CekiSatiri satir)
    {
        var tip = typeof(SahaTamamlamaService).GetNestedType("SahaIsTamamlamaSatirRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(tip)!;
        foreach (var property in tip.GetProperties())
        {
            var source = typeof(CekiSatiri).GetProperty(property.Name);
            if (source != null && source.PropertyType == property.PropertyType)
                property.SetValue(row, source.GetValue(satir));
        }
        return (decimal)typeof(SahaTamamlamaService)
            .GetMethod("HesaplaIsTamamlama", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [row])!;
    }

    private static decimal HesaplaFiziksel(decimal istenen, decimal konulan)
    {
        var tip = typeof(SahaTamamlamaService).GetNestedType("SahaTamamlamaSatirRow", BindingFlags.NonPublic)!;
        var row = Activator.CreateInstance(tip)!;
        tip.GetProperty("IstenenAdet")!.SetValue(row, istenen);
        tip.GetProperty("KonulanAdet")!.SetValue(row, konulan);
        return (decimal)typeof(SahaTamamlamaService)
            .GetMethod("HesaplaGerceklesenTamamlama", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [row])!;
    }

    private static AppDbContext Context(SecimOkuyucu okuyucu) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
        .AddInterceptors(new BaglantiyiEngelle(), okuyucu).Options);

    private sealed record SecimSatiri(CekiSatiri Satir, decimal Aktarilan);

    // Gerçek EF SQL çevirisi ve servis hesapları çalışır. Sonuç satırları taklittir;
    // bağlantı açılmaz, SELECT dışı komutlar kabul edilmez, gerçek veri değişmez.
    private sealed class SecimOkuyucu(SecimSatiri[] yeni, SecimSatiri[] legacy) : DbCommandInterceptor
    {
        public List<string> Sorgular { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            var sql = command.CommandText;
            Assert.StartsWith("SELECT", sql);
            Sorgular.Add(sql);
            if (Sorgular.Count == 1)
            {
                Assert.Contains("NOT EXISTS", sql); // farklı kaynakla aynı hedefe çift kredi yok
                Assert.Contains($"\"DurumId\" <> {(int)SahaAktarimDurum.GeriAlindi}", sql);
                Assert.Contains($"\"DurumId\" <> {(int)SahaAktarimDurum.Iptal}", sql);
                Assert.Contains("\"ProjeTipiId\" = 2", sql);
                Assert.Contains("\"Miktar\" > 0", sql);
                Assert.DoesNotContain("\"KonulanAdet\"", sql);
            }
            else
            {
                Assert.Equal(2, Sorgular.Count);
                Assert.Contains("\"ProjeTipiId\" = 2", sql);
                Assert.Contains("\"KaynakCekiSatiriId\"", sql);
                Assert.Contains("NOT EXISTS", sql);
                var defterFiltresi = sql[sql.IndexOf("FROM \"SahaAktarimKalemleri\"", StringComparison.Ordinal)..];
                Assert.Contains("\"SahaCekiSatiriId\"", defterFiltresi);
                Assert.DoesNotContain("\"DurumId\"", defterFiltresi); // pasif hedefler de dışlanır
                Assert.DoesNotContain("\"KaynakCekiSatiriId\"", defterFiltresi); // başka kaynak pointer'ı da dışlanır
            }

            var projection = Regex.Match(sql, @"^SELECT (.*?)\s+FROM ", RegexOptions.Singleline).Groups[1].Value;
            Assert.NotEmpty(projection);
            var columns = projection.Split(", ").Select(column => Regex.Match(column, "\\.\"([^\"]+)\"").Groups[1].Value).ToArray();
            Assert.All(columns, column => Assert.NotEmpty(column));
            var rows = Sorgular.Count == 1 ? yeni : legacy;
            var values = rows.Select(row => columns.Select(column => column switch
            {
                "SahaCekiSatiriId" or "Id" => (object)row.Satir.Id,
                "Miktar" => row.Aktarilan,
                _ => typeof(CekiSatiri).GetProperty(column)!.GetValue(row.Satir)!
            }).ToArray()).ToArray();
            var cevap = new DataTable();
            for (var index = 0; index < columns.Length; index++)
            {
                var type = columns[index] is "SahaCekiSatiriId" or "Id" or "KaynakCekiSatiriId" or "DurumId" or "GridDurumuId"
                    ? typeof(int) : typeof(decimal);
                cevap.Columns.Add($"c{index}", type);
            }
            foreach (var row in values) cevap.Rows.Add(row);
            return Cevap(cevap);
        }

        private static ValueTask<InterceptionResult<DbDataReader>> Cevap(DataTable table) =>
            ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
    }

    private sealed class BaglantiyiEngelle : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }
}
