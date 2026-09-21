using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class CekiOrijinalMiktarTests
{
    [Fact]
    public void OrijinalMiktar_NullableVeOndalikHassasiyetiGuncelMiktarlaAyni()
    {
        using var context = Context();
        var property = context.Model.FindEntityType(typeof(CekiSatiri))!.FindProperty(nameof(CekiSatiri.OrijinalIstenenAdet))!;
        Assert.True(property.IsNullable);
        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(4, property.GetScale());
        Assert.Equal("numeric(18,4)", property.GetColumnType());
    }

    [Fact]
    public void OrijinalMiktar_HicbirAktifMiktarHesabiniDegistirmez()
    {
        var satir = new CekiSatiri { IstenenAdet = 3.5m, GridGelenAdet = 1, GelenMiktar = 1 };
        var once = (satir.KalanMiktar, satir.EksikMiktar, satir.GridEksikMiktar, satir.KumulatifToplam);
        satir.OrijinalIstenenAdet = 1000.125m;
        Assert.Equal(once, (satir.KalanMiktar, satir.EksikMiktar, satir.GridEksikMiktar, satir.KumulatifToplam));
        Assert.Equal(2.5m, satir.KalanMiktar);
    }

    [Fact]
    public void AktifGridSevkKarsilamaSayaclari_NullableVeOndalikHassasiyetlidir()
    {
        using var context = Context();
        var satirProperty = context.Model.FindEntityType(typeof(CekiSatiri))!
            .FindProperty(nameof(CekiSatiri.AktifGridSevkKarsilananMiktari))!;
        var icerikProperty = context.Model.FindEntityType(typeof(SandikIcerik))!
            .FindProperty(nameof(SandikIcerik.AktifGridSevkKarsilananMiktari))!;

        Assert.True(satirProperty.IsNullable);
        Assert.Equal(18, satirProperty.GetPrecision());
        Assert.Equal(4, satirProperty.GetScale());
        Assert.True(icerikProperty.IsNullable);
        Assert.Equal(18, icerikProperty.GetPrecision());
        Assert.Equal(4, icerikProperty.GetScale());
    }

    [Theory]
    [InlineData(null, 2, 3, 2d)]
    [InlineData(1d, 2, 3, 1d)]
    [InlineData(1d, 3, 2, 1d)]
    [InlineData(2d, 3, 2, 2d)]
    [InlineData(null, 2, 2, null)]
    [InlineData(1d, 2, 2, 1d)]
    [InlineData(null, 1.125, 2.25, 1.125)]
    [InlineData(0d, 2, 3, 0d)]
    public async Task RevizyonU_IlkMiktarDegisikligindeOncekiDegeriKorur_SonraEzmez(
        double? orijinal, decimal onceki, decimal yeni, double? beklenen)
    {
        await using var context = Context();
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);
        var satir = new CekiSatiri
        {
            Id = 20, CekiId = 15, SiraNo = 1, BarkodNo = "TEST", Aciklama = "TEST",
            CekideGecenSandikNo = "1", FiiliSandikNo = "1", IstenenAdet = onceki,
            OrijinalIstenenAdet = orijinal.HasValue ? (decimal)orijinal.Value : null,
            GelenMiktar = .5m, GridGelenAdet = 1
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(yeni), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        Assert.Equal(beklenen.HasValue ? (decimal)beklenen.Value : null, satir.OrijinalIstenenAdet);
        Assert.Equal(yeni, satir.IstenenAdet);
        Assert.Equal(.5m, satir.GelenMiktar);
        Assert.Equal(1, satir.GridGelenAdet);
    }

    [Fact]
    public async Task RevizyonA_MiktarDegisikligiOlmadigiIcinOrijinalMiktarBosKalir()
    {
        await using var context = Context();
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);

        await InvokeAsync(Service(context), "RevizyonSatiriEkleAsync", 10, 15,
            ImportSatiri(2.125m), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        var satir = Assert.Single(context.ChangeTracker.Entries<CekiSatiri>()).Entity;
        Assert.Equal(2.125m, satir.IstenenAdet);
        Assert.Null(satir.OrijinalIstenenAdet);
        Assert.Equal(EntityState.Added, context.Entry(satir).State);
    }

    [Fact]
    public async Task PA702Revizyonu_TekTamTahsisAnaMiktarlaBirlikteGuncellenir()
    {
        var tahsisVerisi = new TahsisSatiri(
            Id: 40,
            CekiSatiriId: 20,
            SandikId: 30,
            ProjeId: 10,
            SandikNo: "1",
            Tahsis: 2,
            Konulan: 0);
        await using var context = Context(new TahsisSelectSonucu(tahsisVerisi));
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);
        var satir = new CekiSatiri
        {
            Id = 20,
            CekiId = 15,
            SiraNo = 1,
            BarkodNo = "FCT01331267",
            Aciklama = "LASTİK TİTREŞİM ÖNLEYİCİ PLAKA",
            CekideGecenSandikNo = "1",
            FiiliSandikNo = "1",
            IstenenAdet = 2
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(4), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        var icerik = Assert.Single(context.ChangeTracker.Entries<SandikIcerik>()).Entity;
        Assert.Equal(4, satir.IstenenAdet);
        Assert.Equal(2, satir.OrijinalIstenenAdet);
        Assert.Equal(4, icerik.TahsisMiktari);
        Assert.Equal(0, icerik.KonulanAdet);
        Assert.Equal(4, icerik.EksikAdet);
    }

    [Fact]
    public async Task RevizyonU_CokluTahsisDagiliminiOtomatikYenidenPaylastirmaz()
    {
        var ilk = new TahsisSatiri(40, 20, 30, 10, "1", Tahsis: 2, Konulan: 1);
        var ikinci = new TahsisSatiri(41, 20, 31, 10, "2", Tahsis: 2, Konulan: 1);
        await using var context = Context(new TahsisSelectSonucu(ilk, ikinci));
        var sandik = new Sandik { Id = 30, ProjeId = 10, SandikNo = "1" };
        context.Sandiklar.Attach(sandik);
        var satir = new CekiSatiri
        {
            Id = 20,
            CekiId = 15,
            SiraNo = 1,
            BarkodNo = "TEST",
            Aciklama = "TEST",
            CekideGecenSandikNo = "1",
            FiiliSandikNo = "1",
            IstenenAdet = 4
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(6), new Dictionary<string, Sandik> { ["1"] = sandik }, SandikBilgileri(), 7);

        var icerikler = context.ChangeTracker.Entries<SandikIcerik>()
            .Select(e => e.Entity)
            .OrderBy(i => i.Id)
            .ToList();
        Assert.Equal(6, satir.IstenenAdet);
        Assert.Equal(4, satir.OrijinalIstenenAdet);
        Assert.Collection(icerikler,
            i =>
            {
                Assert.Equal(2, i.TahsisMiktari);
                Assert.Equal(1, i.KonulanAdet);
                Assert.Equal(1, i.EksikAdet);
            },
            i =>
            {
                Assert.Equal(2, i.TahsisMiktari);
                Assert.Equal(1, i.KonulanAdet);
                Assert.Equal(1, i.EksikAdet);
            });
    }

    [Fact]
    public async Task RevizyonOncesiOtomatikGeriAl_AktifPartiSayaclariniAnaSatirVeTahsislerdeTemizler()
    {
        var tahsisVerisi = new TahsisSatiri(
            Id: 40,
            CekiSatiriId: 20,
            SandikId: 30,
            ProjeId: 10,
            SandikNo: "1",
            Tahsis: 4,
            Konulan: 2,
            AktifPartideKarsilanan: 1);
        await using var context = Context(new TahsisSelectSonucu(tahsisVerisi));
        var satir = new CekiSatiri
        {
            Id = 20,
            CekiId = 15,
            SiraNo = 1,
            BarkodNo = "REVIZYON-RESET",
            Aciklama = "Aktif parti takip sayacı testi",
            CekideGecenSandikNo = "1",
            FiiliSandikNo = "1",
            IstenenAdet = 4,
            GridDurumuId = (int)GridDurum.TamGeldi,
            GridGelenAdet = 4,
            GridSevkDurumuId = (int)GridSevkDurum.SevkEdildi,
            GridSevkMiktari = 2,
            AktifGridSevkKarsilananMiktari = 1,
            UcKDurumuId = (int)UcKDurum.EksikGeldi,
            UcKKarsilamaTipiId = (int)UcKDurum.EksikGeldi,
            GelenMiktar = 2
        };

        await InvokeAsync(
            Service(context),
            "RevizyonSatiriIslemleriniGeriAlAsync",
            10,
            satir,
            7,
            "Revizyon regresyon testi");

        var icerik = Assert.Single(context.ChangeTracker.Entries<SandikIcerik>()).Entity;
        Assert.Null(satir.AktifGridSevkKarsilananMiktari);
        Assert.Null(icerik.AktifGridSevkKarsilananMiktari);
        Assert.Equal(0, satir.GridSevkMiktari);
        Assert.Equal(0, satir.GelenMiktar);
        Assert.Equal(0, icerik.KonulanAdet);
    }

    [Fact]
    public async Task IlkNormalCekiYuklemesi_MiktarDegisikligiOlmadigiIcinOrijinalMiktarBosKalir()
    {
        await using var context = Context();
        var uow = new UowStub();
        var dosyaAdi = $"orijinal-miktar-test-{Guid.NewGuid():N}.xlsx";
        var projeId = Random.Shared.Next(1_800_000_000, int.MaxValue);
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", projeId.ToString());
        Assert.False(Directory.Exists(uploads));
        uow.Repo<Proje>().YeniId = projeId;
        try
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("ÇIKTI SAYFASI");
            sheet.Cell(1, 1).Value = "FB NO";
            sheet.Cell(1, 2).Value = "TEST-ORIGINAL";
            sheet.Cell(6, 1).Value = 1;
            sheet.Cell(6, 3).Value = "TEST-BARKOD";
            sheet.Cell(6, 4).Value = "Test ürün";
            sheet.Cell(6, 5).Value = "1";
            sheet.Cell(6, 6).Value = 4;
            sheet.Cell(6, 7).Value = "Adet";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            await Service(context, uow).CekiYukleAsync(stream, dosyaAdi);

            var satir = Assert.Single(uow.Repo<CekiSatiri>().Rows);
            Assert.Equal(4, satir.IstenenAdet);
            Assert.Null(satir.OrijinalIstenenAdet);
        }
        finally
        {
            var testDosyasi = Path.Combine(uploads, dosyaAdi);
            if (File.Exists(testDosyasi)) File.Delete(testDosyasi);
            if (Directory.Exists(uploads)) Directory.Delete(uploads); // yalnızca bu teste ait boş dizin
        }
    }

    [Theory]
    [InlineData(true, 10, 0, 0)]
    [InlineData(false, 10, 0, 0)]
    [InlineData(true, 15, 0, 0)]
    [InlineData(false, 15, 0, 0)]
    [InlineData(false, 10, 1, 0)]
    [InlineData(false, 10, 0, 1)]
    public async Task RevizyonU_SandikDegisikligi_TasimaUygunlugunuYeniEksikHesabindanOnceBelirler(
        bool yeniSandik, decimal yeniMiktar, decimal oncekiEksik, decimal konulan)
    {
        var tahsisVerisi = new TahsisSatiri(
            40, 20, 30, 10, "19", Tahsis: 10, Konulan: konulan, Eksik: oncekiEksik);
        await using var context = Context(new TahsisSelectSonucu(tahsisVerisi));
        var kaynak = new Sandik { Id = 30, ProjeId = 10, SandikNo = "19" };
        context.Sandiklar.Attach(kaynak);
        var sandikCache = new Dictionary<string, Sandik> { ["19"] = kaynak };
        if (!yeniSandik)
        {
            var mevcutHedef = new Sandik { Id = 31, ProjeId = 10, SandikNo = "20" };
            context.Sandiklar.Attach(mevcutHedef);
            sandikCache.Add("20", mevcutHedef);
        }

        var satir = new CekiSatiri
        {
            Id = 20, CekiId = 15, SiraNo = 1, BarkodNo = "TEST", Aciklama = "TEST",
            CekideGecenSandikNo = "19", FiiliSandikNo = "19", IstenenAdet = 10
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(yeniMiktar, "20"), sandikCache, SandikBilgileri(), 7);
        context.ChangeTracker.DetectChanges();

        var icerik = Assert.Single(context.ChangeTracker.Entries<SandikIcerik>()).Entity;
        var hedef = sandikCache["20"];
        Assert.Equal("20", satir.CekideGecenSandikNo);
        Assert.Equal("20", satir.FiiliSandikNo);
        Assert.Equal(yeniMiktar, satir.IstenenAdet);
        Assert.Equal(yeniMiktar, icerik.TahsisMiktari);
        Assert.Equal(konulan, icerik.KonulanAdet);
        Assert.Equal(yeniMiktar - konulan, icerik.EksikAdet);
        Assert.Equal(0, satir.GelenMiktar);
        Assert.Equal(0, icerik.StokKarsilanan + icerik.ProjeKarsilanan + icerik.TedarikciKarsilanan);

        if (oncekiEksik == 0 && konulan == 0)
        {
            Assert.Same(hedef, icerik.Sandik);
            Assert.Equal(context.Entry(hedef).Property(s => s.Id).CurrentValue,
                context.Entry(icerik).Property(i => i.SandikId).CurrentValue);
            Assert.Empty(kaynak.SandikIcerikleri);
            // Yeni hedef boş değildir; revizyon sonundaki boş sandık temizliğine aday olmaz.
            Assert.Same(icerik, Assert.Single(hedef.SandikIcerikleri));
            if (yeniSandik)
                Assert.Equal(EntityState.Added, context.Entry(hedef).State);
        }
        else
        {
            // Güncelleme öncesinden kalan gerçek operasyon izi taşıma engeli olmaya devam eder.
            Assert.Same(kaynak, icerik.Sandik);
            Assert.Equal(kaynak.Id, icerik.SandikId);
            Assert.Empty(hedef.SandikIcerikleri);
        }
    }

    [Theory]
    [InlineData("20", 10, false, 10, true)]
    [InlineData("", 10, false, 10, true)]
    [InlineData("20", 0, false, 10, true)]
    [InlineData("19", 10, false, 10, false)]
    [InlineData("20", 5, false, 10, false)]
    [InlineData("20", 10, true, 10, false)]
    [InlineData("20", 10, false, 99, false)]
    public async Task RevizyonU_AyniPlanliTekTahsisDuzeltmesi_BilincliDagilimiKorur(
        string fiiliSandikNo, decimal tahsis, bool bolunmus, int kaynakProjeId, bool tasinmali)
    {
        var veriler = new List<TahsisSatiri>
        {
            new(40, 20, 30, kaynakProjeId, "19", Tahsis: tahsis, Konulan: 0, Eksik: 0)
        };
        if (bolunmus)
            veriler.Add(new(41, 20, 32, 10, "21", Tahsis: 5, Konulan: 0, Eksik: 0));
        await using var context = Context(new TahsisSelectSonucu(veriler.ToArray()));
        var kaynak = new Sandik { Id = 30, ProjeId = kaynakProjeId, SandikNo = "19" };
        context.Sandiklar.Attach(kaynak);
        var cache = new Dictionary<string, Sandik> { ["19"] = kaynak };
        var satir = new CekiSatiri
        {
            Id = 20, CekiId = 15, SiraNo = 1, BarkodNo = "TEST", Aciklama = "TEST",
            CekideGecenSandikNo = "20", FiiliSandikNo = fiiliSandikNo, IstenenAdet = 10
        };
        context.CekiSatirlari.Attach(satir);

        await InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
            ImportSatiri(10, "20"), cache, SandikBilgileri(), 7);
        context.ChangeTracker.DetectChanges();

        var icerik = context.ChangeTracker.Entries<SandikIcerik>().Single(e => e.Entity.Id == 40).Entity;
        Assert.Equal("20", satir.CekideGecenSandikNo);
        Assert.Equal(fiiliSandikNo == "19" ? "19" : "20", satir.FiiliSandikNo);
        Assert.Equal(10, satir.IstenenAdet);
        Assert.Null(satir.OrijinalIstenenAdet);
        Assert.Equal(0, icerik.KonulanAdet);
        Assert.Equal(tasinmali ? 10 : tahsis, icerik.TahsisMiktari);
        Assert.Same(tasinmali ? cache["20"] : kaynak, icerik.Sandik);
        if (tasinmali)
            Assert.Same(icerik, Assert.Single(cache["20"].SandikIcerikleri));
        else
            Assert.Empty(cache["20"].SandikIcerikleri);
        if (bolunmus)
        {
            var diger = context.ChangeTracker.Entries<SandikIcerik>().Single(e => e.Entity.Id == 41).Entity;
            Assert.Equal(32, diger.SandikId);
            Assert.Equal(5, diger.TahsisMiktari);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RevizyonU_AyniPlanliTahsisDuzeltmesi_KaynakVeHedefSevkKilidiniAsmaz(bool kaynakKilitli)
    {
        await using var context = Context(new TahsisSelectSonucu(
            new TahsisSatiri(40, 20, 30, 10, "19", Tahsis: 10, Konulan: 0, Eksik: 0)));
        var kaynak = new Sandik { Id = 30, ProjeId = 10, SandikNo = "19",
            DurumId = (int)(kaynakKilitli ? SandikDurum.Sevkedildi : SandikDurum.Hazirlaniyor) };
        var hedef = new Sandik { Id = 31, ProjeId = 10, SandikNo = "20",
            DurumId = (int)(kaynakKilitli ? SandikDurum.Hazirlaniyor : SandikDurum.Sevkedildi) };
        context.Sandiklar.AttachRange(kaynak, hedef);
        var satir = new CekiSatiri
        {
            Id = 20, CekiId = 15, SiraNo = 1, BarkodNo = "TEST", Aciklama = "TEST",
            CekideGecenSandikNo = "20", FiiliSandikNo = "20", IstenenAdet = 10
        };
        context.CekiSatirlari.Attach(satir);

        await Assert.ThrowsAsync<_3K.Core.Exceptions.CekiRevizyonConflictException>(() =>
            InvokeAsync(Service(context), "RevizyonSatiriniGuncelleAsync", 10, satir,
                ImportSatiri(10, "20"), new Dictionary<string, Sandik> { ["19"] = kaynak, ["20"] = hedef }, SandikBilgileri(), 7));

        var icerik = Assert.Single(context.ChangeTracker.Entries<SandikIcerik>()).Entity;
        Assert.Same(kaynak, icerik.Sandik);
        Assert.Equal(10, icerik.TahsisMiktari);
        Assert.Equal(0, icerik.EksikAdet);
        Assert.Equal(0, icerik.KonulanAdet);
        Assert.Empty(hedef.SandikIcerikleri);
    }

    private static AppDbContext Context(DbCommandInterceptor? selectSonucu = null) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=database-must-not-be-contacted.invalid;Database=test;Username=test;Password=test")
        .AddInterceptors(new BaglantiyiEngelle(), selectSonucu ?? new BosSelectSonucu()).Options);

    private static CekiService Service(AppDbContext context, IUnitOfWork? uow = null) =>
        new(uow!, context, null!, new DurumStub(), null!, NullLogger<CekiService>.Instance);

    private static object ImportSatiri(decimal miktar, string koliNo = "1")
    {
        var type = typeof(CekiService).GetNestedType("CiktiSatirImportBilgisi", BindingFlags.NonPublic)!;
        var satir = Activator.CreateInstance(type)!;
        foreach (var (property, value) in new Dictionary<string, object>
        {
            ["ExcelSatirNo"] = 6, ["SiraNo"] = 1, ["BarkodNo"] = "TEST", ["Aciklama"] = "TEST",
            ["KoliNo"] = koliNo, ["IstenenAdet"] = miktar, ["BirimId"] = (int)Birim.Adet
        }) type.GetProperty(property)!.SetValue(satir, value);
        return satir;
    }

    private static object SandikBilgileri() => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
        typeof(string), typeof(CekiService).GetNestedType("SandikImportBilgisi", BindingFlags.NonPublic)!))!;

    private static Task InvokeAsync(CekiService servis, string name, params object[] args) =>
        Assert.IsAssignableFrom<Task>(typeof(CekiService).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(servis, args));

    private sealed class BaglantiyiEngelle : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class BosSelectSonucu : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT ", command.CommandText);
            Assert.True(command.CommandText.Contains("FROM \"SandikIcerikleri\"") || command.CommandText.Contains("FROM \"Projeler\""));
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader()));
        }
    }

    private sealed record TahsisSatiri(
        int Id,
        int CekiSatiriId,
        int SandikId,
        int ProjeId,
        string SandikNo,
        decimal Tahsis,
        decimal Konulan,
        decimal? AktifPartideKarsilanan = null,
        decimal? Eksik = null);

    /// <summary>
    /// Revizyon testlerinin gerçek PostgreSQL'e bağlanmadan EF materialization yolunu
    /// kullanmasını sağlar. Sütun sırası CekiService'in SandikIcerik + Sandik sorgusuyla aynıdır.
    /// </summary>
    private sealed class TahsisSelectSonucu(params TahsisSatiri[] satirlar) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT ", command.CommandText);
            if (command.CommandText.Contains("EXISTS", StringComparison.OrdinalIgnoreCase))
            {
                var exists = new DataTable();
                exists.Columns.Add("c0", typeof(bool));
                exists.Rows.Add(false);
                return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(exists.CreateDataReader()));
            }

            if (!command.CommandText.Contains("FROM \"SandikIcerikleri\""))
                return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(
                    new DataTable().CreateDataReader()));

            var table = TahsisTablosuOlustur();
            foreach (var satir in satirlar)
                TahsisSatiriEkle(table, satir);

            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }

        private static DataTable TahsisTablosuOlustur()
        {
            var table = new DataTable();
            var tipler = new[]
            {
                typeof(int), typeof(string), typeof(decimal), typeof(string), typeof(int),
                typeof(int), typeof(string), typeof(DateTime), typeof(decimal), typeof(string),
                typeof(string), typeof(decimal), typeof(decimal), typeof(decimal), typeof(int),
                typeof(decimal), typeof(decimal), typeof(decimal), typeof(string), typeof(DateTime), typeof(uint),
                typeof(int), typeof(string), typeof(string), typeof(decimal), typeof(string),
                typeof(DateTime), typeof(int), typeof(int), typeof(decimal), typeof(decimal),
                typeof(decimal), typeof(int), typeof(string), typeof(int), typeof(bool),
                typeof(int), typeof(string), typeof(DateTime), typeof(uint), typeof(decimal)
            };
            for (var i = 0; i < tipler.Length; i++)
                table.Columns.Add($"c{i}", tipler[i]);
            return table;
        }

        private static void TahsisSatiriEkle(DataTable table, TahsisSatiri veri)
        {
            var bos = DBNull.Value;
            table.Rows.Add(
                veri.Id, bos, veri.AktifPartideKarsilanan.HasValue ? veri.AktifPartideKarsilanan.Value : bos, bos, (int)Birim.Adet,
                veri.CekiSatiriId, bos, DateTime.UtcNow, veri.Eksik ?? Math.Max(veri.Tahsis - veri.Konulan, 0), bos,
                bos, veri.Konulan, 0m, 0m, veri.SandikId,
                0m, veri.Tahsis, 0m, bos, bos, 1u,
                veri.SandikId, bos, bos, bos, bos,
                DateTime.UtcNow, (int)DepoLokasyon.Belirsiz, (int)SandikDurum.Hazirlaniyor, bos, bos,
                bos, veri.ProjeId, veri.SandikNo, bos, false,
                (int)SandikTipi.AhsapKapali, bos, bos, 1u, bos);
        }
    }

    private sealed class DurumStub : IDurumHesaplaService
    {
        public void HesaplaKalanVeDurum(CekiSatiri satir) { }
        public int HesaplaGenelDurum(int grid, int uck) => (int)UrunDurum.Bekliyor;
    }

    private sealed class UowStub : IUnitOfWork
    {
        private readonly Dictionary<Type, object> _repos = new();
        public RepoStub<T> Repo<T>() where T : BaseEntity
        {
            if (!_repos.TryGetValue(typeof(T), out var repo)) _repos[typeof(T)] = repo = new RepoStub<T>();
            return (RepoStub<T>)repo;
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity => Repo<T>();
        public bool HasActiveTransaction => false;
        public Task<int> SaveChangesAsync(CancellationToken token = default) => Task.FromResult(1);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken token = default) => operation(token);
        public void RegisterAfterCommit(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void RegisterAfterRollback(Func<CancellationToken, Task> callback) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private sealed class RepoStub<T> : IGenericRepository<T> where T : BaseEntity
    {
        public List<T> Rows { get; } = [];
        public int YeniId { get; set; } = 100;
        public Task AddAsync(T entity) { entity.Id = YeniId++; Rows.Add(entity); return Task.CompletedTask; }
        public void Update(T entity) { }
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<T?> GetByIdAsync(int id) => throw new NotSupportedException();
        public IQueryable<T> Queryable() => Rows.AsQueryable();
        public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllAsync() => throw new NotSupportedException();
        public Task<IEnumerable<T>> GetAllWithIncludeAsync<TProp>(Expression<Func<T, TProp>> include) => throw new NotSupportedException();
    }
}
