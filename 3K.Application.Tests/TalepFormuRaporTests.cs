using ClosedXML.Excel;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>G4: toplu talep formu Excel'i — seçim doğrulaması, kapsam ve GridEksikMiktar satırları.</summary>
public sealed class TalepFormuRaporTests
{
    [Theory]
    [InlineData(new int[0], "En az bir proje")]
    [InlineData(new[] { 1, 1 }, "birden fazla")]
    [InlineData(new[] { 0 }, "pozitif")]
    public async Task Gecersiz_secim_rapor_uretmeden_400_doner(int[] ids, string mesaj)
    {
        var servis = new SahteTalepFormu();
        var sonuc = await new GetTopluTalepFormuQueryHandler(new OrtakMemoryUow(), servis)
            .Handle(new GetTopluTalepFormuQuery { ProjeIds = ids }, default);
        Assert.Equal(400, sonuc.StatusCode);
        Assert.Contains(mesaj, sonuc.Error!.Message);
        Assert.Equal(0, servis.Cagri);
    }

    [Fact]
    public async Task Bulunamayan_ve_kapsam_disi_proje_reddedilir_gecerli_secim_tek_excel_doner()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.AddRange([
            new() { Id = 1, ProjeNo = "PA-1", ProjeTipiId = (int)ProjeTipi.Normal },
            new() { Id = 2, ProjeNo = "PA-2", ProjeTipiId = (int)ProjeTipi.Normal },
            new() { Id = 3, ProjeNo = "SH-3", ProjeTipiId = (int)ProjeTipi.Saha }]);
        var servis = new SahteTalepFormu();
        var handler = new GetTopluTalepFormuQueryHandler(uow, servis);

        Assert.Equal(404, (await handler.Handle(new GetTopluTalepFormuQuery { ProjeIds = [1, 9] }, default)).StatusCode);
        Assert.Equal(403, (await handler.Handle(new GetTopluTalepFormuQuery { ProjeIds = [1, 3] }, default)).StatusCode);
        Assert.Equal(0, servis.Cagri);

        var sonuc = await handler.Handle(new GetTopluTalepFormuQuery { ProjeIds = [2, 1] }, default);
        Assert.True(sonuc.IsSuccess);
        Assert.Equal([2, 1], servis.SonProjeler);
        Assert.StartsWith("Talep_Formu_", sonuc.Value!.DosyaAdi);
        Assert.EndsWith(".xlsx", sonuc.Value.DosyaAdi);
    }

    /// <summary>Yalnız GridEksikMiktar &gt; 0 satırlar, seçim sırasıyla tek sayfada; İstenen Miktar Grid eksiğidir.</summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Excel_grid_eksigi_olan_satirlari_secim_sirasiyla_tek_sayfada_listeler()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var pa1 = Proje("PA-1"); var pa2 = Proje("PA-2");
            Satir(pa1, "2", 1, "B-EKSIK", "Kablo", "Kırmızı", istenen: 10, gridGelen: 4, trafo: 1, Birim.Metre);
            Satir(pa1, "1", 2, "B-TAM", "Tam gelen", null, istenen: 5, gridGelen: 5);
            Satir(pa1, "1", 1, "B-IPTAL", "İptal", null, istenen: 5, gridDurum: GridDurum.Iptal);
            Satir(pa1, "1", 3, "B-KAPALI", "Kapandı", null, istenen: 5, gridDurum: GridDurum.GridKapandi);
            Satir(pa1, "1", 4, "B-FAZLA", "Fazla gelen", null, istenen: 2, gridGelen: 3);
            Satir(pa2, "10", 1, "B-PA2", "Takım", "Açıklama", istenen: 2, birim: Birim.Takim);
            db.AddRange(pa1, pa2); await db.SaveChangesAsync();

            var excel = await new TalepFormuRaporService(db).TopluTalepFormuExcelAsync([pa2.Id, pa1.Id]);
            using var workbook = new XLWorkbook(new MemoryStream(excel));
            var sayfa = Assert.Single(workbook.Worksheets);
            Assert.Equal("TALEP FORMU", sayfa.Cell(1, 1).GetString());
            Assert.Equal("PA-2, PA-1", sayfa.Cell(3, 2).GetString());
            Assert.Equal("2 kalem", sayfa.Cell(3, 4).GetString());
            Assert.Equal(["#", "Proje No", "Barkod", "Ürün Adı", "Ürün Açıklaması", "İstenen Miktar", "Ölçü Birimi"],
                Enumerable.Range(1, 7).Select(k => sayfa.Cell(5, k).GetString()));
            Assert.Equal(("PA-2", "B-PA2", "Takım", "Açıklama", 2m, "Takım"), (sayfa.Cell(6, 2).GetString(), sayfa.Cell(6, 3).GetString(),
                sayfa.Cell(6, 4).GetString(), sayfa.Cell(6, 5).GetString(), sayfa.Cell(6, 6).GetValue<decimal>(), sayfa.Cell(6, 7).GetString()));
            Assert.Equal(("PA-1", "B-EKSIK", "Kırmızı", 5m, "Metre"), (sayfa.Cell(7, 2).GetString(), sayfa.Cell(7, 3).GetString(),
                sayfa.Cell(7, 5).GetString(), sayfa.Cell(7, 6).GetValue<decimal>(), sayfa.Cell(7, 7).GetString()));
            Assert.Equal(2, sayfa.Cell(7, 1).GetValue<int>());
            Assert.True(sayfa.Cell(8, 1).IsEmpty());
            Assert.Equal("Bu form sistem tarafından otomatik oluşturulmuştur.", sayfa.Cell(9, 1).GetString());
        });
    }

    private static Proje Proje(string no) => new() { ProjeNo = no, Musteri = "QA", ProjeTipiId = (int)ProjeTipi.Normal };

    private static void Satir(Proje proje, string sandikNo, int sira, string barkod, string aciklama, string? remarks,
        decimal istenen, decimal gridGelen = 0, decimal trafo = 0, Birim birim = Birim.Adet, GridDurum? gridDurum = null)
    {
        var ceki = proje.Cekiler.FirstOrDefault() ?? new Ceki();
        if (proje.Cekiler.Count == 0) proje.Cekiler.Add(ceki);
        ceki.CekiSatirlari.Add(new CekiSatiri
        {
            SiraNo = sira, BarkodNo = barkod, Aciklama = aciklama, Remarks = remarks, CekideGecenSandikNo = sandikNo,
            IstenenAdet = istenen, GridGelenAdet = gridGelen, TrafoSevkAdet = trafo, BirimId = (int)birim,
            GridDurumuId = (int)(gridDurum ?? GridDurum.Gelmedi)
        });
    }

    private sealed class SahteTalepFormu : ITalepFormuRaporService
    {
        public int Cagri { get; private set; }
        public IReadOnlyList<int> SonProjeler { get; private set; } = [];
        public Task<byte[]> TopluTalepFormuExcelAsync(IReadOnlyList<int> projeIds, CancellationToken cancellationToken = default)
        {
            Cagri++;
            SonProjeler = projeIds.ToArray();
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }
    }
}
