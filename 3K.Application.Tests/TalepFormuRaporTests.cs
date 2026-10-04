using ClosedXML.Excel;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>G4 / G-110: toplu talep formu Excel'i — seçim doğrulaması, kapsam ve Grid eksiği olup 3K kalanı süren satırlar.</summary>
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

    /// <summary>
    /// Grid eksiği olan ve 3K kalanı süren satırlar seçim sırasıyla tek sayfada; İstenen Miktar 3K kalanıdır. 3K'nın
    /// stoktan tamamen karşıladığı satır Grid'de eksik görünse de forma girmez; kısmen karşılanan satır kalan kadar,
    /// hatalı ürünü olan karşılanmış satır kalan kuralı gereği en az 1 olarak yazılır. Ürün Açıklaması sütunu yoktur.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Excel_grid_eksigi_olup_3k_kalani_suren_satirlari_kalan_miktariyla_listeler()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var pa1 = Proje("PA-1"); var pa2 = Proje("PA-2");
            Satir(pa1, "2", 1, "B-EKSIK", "Kablo", istenen: 10, gridGelen: 4, trafo: 1, birim: Birim.Metre);
            Satir(pa1, "1", 2, "B-TAM", "Tam gelen", istenen: 5, gridGelen: 5);
            Satir(pa1, "1", 1, "B-IPTAL", "İptal", istenen: 5, gridDurum: GridDurum.Iptal);
            Satir(pa1, "1", 3, "B-KAPALI", "Kapandı", istenen: 5, gridDurum: GridDurum.GridKapandi);
            Satir(pa1, "1", 4, "B-FAZLA", "Fazla gelen", istenen: 2, gridGelen: 3);
            Satir(pa1, "1", 5, "B-STOK", "Stoktan tamamı", istenen: 16, stok: 16);
            Satir(pa1, "1", 6, "B-TEDARIK", "Tedarikçiden kısmi", istenen: 16, tedarikci: 10);
            Satir(pa1, "1", 7, "B-HATALI", "Hatalı ürünlü", istenen: 5, stok: 5, hatali: 1);
            Satir(pa2, "10", 1, "B-PA2", "Takım", istenen: 2, birim: Birim.Takim);
            db.AddRange(pa1, pa2); await db.SaveChangesAsync();

            var excel = await Servis(db).TopluTalepFormuExcelAsync([pa2.Id, pa1.Id]);
            using var workbook = new XLWorkbook(new MemoryStream(excel));
            var sayfa = Assert.Single(workbook.Worksheets);
            Assert.Equal("TALEP FORMU", sayfa.Cell(1, 1).GetString());
            Assert.Equal("PA-2, PA-1", sayfa.Cell(3, 2).GetString());
            Assert.Equal("4 kalem", sayfa.Cell(3, 4).GetString());
            Assert.Equal(["#", "Proje No", "Barkod", "Ürün Adı", "İstenen Miktar", "Ölçü Birimi"],
                Enumerable.Range(1, 6).Select(k => sayfa.Cell(5, k).GetString()));
            Assert.True(sayfa.Cell(5, 7).IsEmpty());

            (int Sira, string Proje, string Barkod, string Ad, decimal Miktar, string Birim) Satiri(int r) =>
                (sayfa.Cell(r, 1).GetValue<int>(), sayfa.Cell(r, 2).GetString(), sayfa.Cell(r, 3).GetString(),
                 sayfa.Cell(r, 4).GetString(), sayfa.Cell(r, 5).GetValue<decimal>(), sayfa.Cell(r, 6).GetString());
            Assert.Equal((1, "PA-2", "B-PA2", "Takım", 2m, "Takım"), Satiri(6));
            Assert.Equal((2, "PA-1", "B-TEDARIK", "Tedarikçiden kısmi", 6m, "Adet"), Satiri(7));
            Assert.Equal((3, "PA-1", "B-HATALI", "Hatalı ürünlü", 1m, "Adet"), Satiri(8));
            Assert.Equal((4, "PA-1", "B-EKSIK", "Kablo", 9m, "Metre"), Satiri(9));
            Assert.True(sayfa.Cell(10, 1).IsEmpty());
            Assert.DoesNotContain(Enumerable.Range(6, 4), r => sayfa.Cell(r, 3).GetString() == "B-STOK");
        });
    }

    /// <summary>
    /// S-01: aktif saha aktarımına bağlı normal kaynak satır normal projenin formuna girmez; işi sahada sürer ve
    /// ihtiyaç saha projesinin formunda çıkar. Geri alınmış aktarım satırı normal projeye döndürür.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Sahaya_aktarilan_kaynak_satir_normal_projede_degil_saha_projesinde_talep_edilir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var normal = Proje("PA-N");
            var saha = new Proje { ProjeNo = "SH-1", Musteri = "QA", ProjeTipiId = (int)ProjeTipi.Saha };
            Satir(normal, "1", 1, "B-SAHADA", "Sahaya aktarılan", istenen: 4);
            Satir(normal, "1", 2, "B-GERI", "Aktarımı geri alınan", istenen: 3);
            Satir(normal, "1", 3, "B-NORMAL", "Aktarılmayan", istenen: 2);
            db.AddRange(normal, saha);
            await db.SaveChangesAsync();
            var kaynakSatirlar = normal.Cekiler.Single().CekiSatirlari.ToDictionary(x => x.BarkodNo);

            Satir(saha, "1", 1, "B-SAHADA", "Sahaya aktarılan", istenen: 4);
            var sahaSatiri = saha.Cekiler.Single().CekiSatirlari.Single();
            sahaSatiri.KaynakCekiSatiriId = kaynakSatirlar["B-SAHADA"].Id;
            await db.SaveChangesAsync();

            var aktarim = new SahaAktarim { SahaProjeId = saha.Id, KaynakProjeId = normal.Id };
            SahaAktarimKalemi Kalem(CekiSatiri kaynak, SahaAktarimDurum durum, int? sahaSatirId) => new()
            {
                SahaAktarim = aktarim, KaynakProjeId = normal.Id, SahaProjeId = saha.Id, KaynakCekiSatiriId = kaynak.Id,
                SahaCekiSatiriId = sahaSatirId, Miktar = kaynak.IstenenAdet, DurumId = (int)durum
            };
            db.AddRange(
                Kalem(kaynakSatirlar["B-SAHADA"], SahaAktarimDurum.Planlandi, sahaSatiri.Id),
                Kalem(kaynakSatirlar["B-GERI"], SahaAktarimDurum.GeriAlindi, null));
            await db.SaveChangesAsync();

            var servis = Servis(db);
            using var normalForm = new XLWorkbook(new MemoryStream(await servis.TopluTalepFormuExcelAsync([normal.Id])));
            var normalSayfa = Assert.Single(normalForm.Worksheets);
            Assert.Equal("2 kalem", normalSayfa.Cell(3, 4).GetString());
            Assert.Equal(["B-GERI", "B-NORMAL"], new[] { 6, 7 }.Select(r => normalSayfa.Cell(r, 3).GetString()));
            Assert.True(normalSayfa.Cell(8, 1).IsEmpty());

            using var sahaForm = new XLWorkbook(new MemoryStream(await servis.TopluTalepFormuExcelAsync([saha.Id])));
            var sahaSayfa = Assert.Single(sahaForm.Worksheets);
            Assert.Equal("1 kalem", sahaSayfa.Cell(3, 4).GetString());
            Assert.Equal(("SH-1", "B-SAHADA", 4m), (sahaSayfa.Cell(6, 2).GetString(), sahaSayfa.Cell(6, 3).GetString(),
                sahaSayfa.Cell(6, 5).GetValue<decimal>()));
        });
    }

    private static TalepFormuRaporService Servis(AppDbContext db) => new(db, new SahaTamamlamaService(db));

    private static Proje Proje(string no) => new() { ProjeNo = no, Musteri = "QA", ProjeTipiId = (int)ProjeTipi.Normal };

    private static void Satir(Proje proje, string sandikNo, int sira, string barkod, string aciklama,
        decimal istenen, decimal gridGelen = 0, decimal trafo = 0, Birim birim = Birim.Adet, GridDurum? gridDurum = null,
        decimal stok = 0, decimal tedarikci = 0, decimal hatali = 0)
    {
        var ceki = proje.Cekiler.FirstOrDefault() ?? new Ceki();
        if (proje.Cekiler.Count == 0) proje.Cekiler.Add(ceki);
        ceki.CekiSatirlari.Add(new CekiSatiri
        {
            SiraNo = sira, BarkodNo = barkod, Aciklama = aciklama, Remarks = "Kırmızı", CekideGecenSandikNo = sandikNo,
            IstenenAdet = istenen, GridGelenAdet = gridGelen, TrafoSevkAdet = trafo, BirimId = (int)birim,
            StokKarsilanan = stok, TedarikciKarsilanan = tedarikci, HataliMiktar = hatali,
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
