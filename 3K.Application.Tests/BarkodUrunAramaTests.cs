using ClosedXML.Excel;
using _3K.Application.Behaviors;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Barkodla ürün arama: barkod tam eşleşir (kenar boşluğu ve büyük/küçük harf önemsiz, "-01" ekli barkod ayrı üründür);
/// bütün proje tiplerindeki çeki satırları ve saha/yedek elle eklenen malzemeler gelir; eksik Eksik Ürünler raporunun
/// kalanıdır (sahada tamamlanan miktar kaynak projede eksik değildir); varsayılan yalnız eksikler, eksikler önce; tek
/// okuma izni barkod-urun-arama; PDF/Excel her satırda proje no ile aynı satırları yazar.
/// </summary>
public sealed class BarkodUrunAramaTests
{
    [Theory]
    [InlineData(" fc4324045 ", "FC4324045")]
    [InlineData("FC4324045-01", "FC4324045-01")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Kural_BarkodKirpilirBuyukHarfeCevrilir(string? girdi, string? beklenen)
    {
        Assert.Equal(beklenen, BarkodUrunAramaKurali.Normalize(girdi));
        Assert.Equal(beklenen is not null, BarkodUrunAramaKurali.Gecerli(girdi));
    }

    [Fact]
    public void Kural_UzunBarkodGecersiz_SuzgecVeSiralamaEksikleriOneAlir()
    {
        Assert.True(BarkodUrunAramaKurali.Gecerli(new string('A', BarkodUrunAramaKurali.EnFazlaUzunluk)));
        Assert.False(BarkodUrunAramaKurali.Gecerli(new string('A', BarkodUrunAramaKurali.EnFazlaUzunluk + 1)));

        BarkodUrunSatiri S(string projeNo, int tip, string sandik, int? sira, decimal eksik) =>
            new(1, projeNo, tip, "-", "-", "-", sandik, sira, null, "Ürün", "Adet", 10, 0, 10 - eksik, eksik, "-", "-", "-", false);
        var satirlar = new[]
        {
            S("PA-2", 1, "10", 1, 0), S("PA-2", 1, "2", 5, 3), S("PA-1", 2, "1", null, 0), S("PA-3", 1, "1", 2, 1), S("PA-2", 1, "2", 1, 2)
        };

        Assert.Equal([("PA-2", "2", 1), ("PA-2", "2", 5), ("PA-3", "1", 2), ("PA-1", "1", (int?)null), ("PA-2", "10", 1)],
            BarkodUrunAramaKurali.SuzVeSirala(satirlar, false, null).Select(s => (s.ProjeNo, s.SandikNo, s.SiraNo)));
        Assert.Equal(3, BarkodUrunAramaKurali.SuzVeSirala(satirlar, true, null).Count);
        Assert.Equal(["PA-1"], BarkodUrunAramaKurali.SuzVeSirala(satirlar, false, 2).Select(s => s.ProjeNo));
        Assert.Equal(4, BarkodUrunAramaKurali.SuzVeSirala(satirlar, false, 0).Count(s => s.ProjeTipiId == 1));
    }

    [Fact]
    public void Dogrulayici_BosUzunBarkoduTanimsizTipiVeBicimiReddeder()
    {
        var ara = new BarkodUrunAraQueryValidator();
        Assert.True(ara.Validate(new BarkodUrunAraQuery { Barkod = "FC1" }).IsValid);
        Assert.True(ara.Validate(new BarkodUrunAraQuery { Barkod = "FC1", ProjeTipiId = (int)ProjeTipi.Yedek }).IsValid);
        Assert.False(ara.Validate(new BarkodUrunAraQuery { Barkod = " " }).IsValid);
        Assert.False(ara.Validate(new BarkodUrunAraQuery { Barkod = new string('A', 101) }).IsValid);
        Assert.False(ara.Validate(new BarkodUrunAraQuery { Barkod = "FC1", ProjeTipiId = 9 }).IsValid);

        var rapor = new BarkodUrunAramaRaporuQueryValidator();
        Assert.True(rapor.Validate(new BarkodUrunAramaRaporuQuery { Barkod = "FC1", Format = "excel" }).IsValid);
        Assert.False(rapor.Validate(new BarkodUrunAramaRaporuQuery { Barkod = "FC1", Format = "doc" }).IsValid);
        Assert.True(new BarkodUrunAraQuery().YalnizEksikler);
    }

    [Fact]
    public async Task Yetki_YalnizBarkodUrunAramaOkumaIzniyleCalisir()
    {
        var ara = new BarkodUrunAraQuery { Barkod = "FC1" };
        var rapor = new BarkodUrunAramaRaporuQuery { Barkod = "FC1" };
        Assert.Equal((YetkiKodlari.Sandik.BarkodUrunArama, YetkiTipi.R), (ara.RequiredMenuKod, ara.RequiredYetkiTipi));
        Assert.Equal((YetkiKodlari.Sandik.BarkodUrunArama, YetkiTipi.R), (rapor.RequiredMenuKod, rapor.RequiredYetkiTipi));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(ara));
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(rapor));

        async Task<bool> GecerMi(OrtakRoller roller)
        {
            var gecti = false;
            var sonuc = await new AuthorizationBehavior<BarkodUrunAraQuery, Result<BarkodUrunAramaSonucu>>(new OrtakUser(), roller)
                .Handle(ara, () => { gecti = true; return Task.FromResult(Result<BarkodUrunAramaSonucu>.Success(new())); }, CancellationToken.None);
            if (!gecti) Assert.Equal(403, sonuc.StatusCode);
            return gecti;
        }
        Assert.True(await GecerMi(OrtakRoller.Yalniz(YetkiKodlari.Sandik.BarkodUrunArama)));
        // Sandık yönetimi ve eksik raporu izinleri ekranı açmaz.
        Assert.False(await GecerMi(OrtakRoller.Haric(YetkiKodlari.Sandik.BarkodUrunArama)));
    }

    [Fact]
    public async Task Rapor_BicimiDosyaAdiniVeIcerigiSecer_GecersizBicimReddedilir()
    {
        var servis = new SahteServis();
        var handler = new BarkodUrunAramaQueryHandlers(servis);

        var pdf = await handler.Handle(new BarkodUrunAramaRaporuQuery { Barkod = " fc1/2 ", Format = "pdf" }, default);
        Assert.Matches(@"^BarkodArama_FC1_2_\d{8}_\d{6}\.pdf$", pdf.Value!.DosyaAdi);
        Assert.Equal(" fc1/2 ", servis.SonBarkod);
        var excel = await handler.Handle(new BarkodUrunAramaRaporuQuery { Barkod = "FC1", Format = "xlsx", YalnizEksikler = false, ProjeTipiId = 2 }, default);
        Assert.EndsWith(".xlsx", excel.Value!.DosyaAdi);
        Assert.Equal((false, (int?)2), (servis.SonYalnizEksik, servis.SonTip));
        Assert.Equal(400, (await handler.Handle(new BarkodUrunAramaRaporuQuery { Barkod = "FC1", Format = "doc" }, default)).StatusCode);
    }

    /// <summary>
    /// Gerçek SQL: tam eşleşme (küçük harf ve boşlukla yazılmış satır dahil, "-01" ekli hariç), üç proje tipi, sahada
    /// tamamlanan kaynak satırın eksiği düşer, saha elle eklenen malzeme gelir; özet süzgeçten bağımsızdır. PDF ve Excel
    /// gerçek veriyle üretilir ve Excel'de her satırda proje no vardır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_BarkodBiitunProjelerdeTamEslesir_EksikRaporKuraliylaHesaplanir()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            await using (var db = new AppDbContext(options))
            {
                var a = new Proje { ProjeNo = "PA-A", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
                var b = new Proje { ProjeNo = "PA-B", Musteri = "GE", ProjeTipiId = (int)ProjeTipi.Normal, DurumId = (int)ProjeDurum.Hazirlaniyor };
                var saha = new Proje { ProjeNo = "PA-A-SAHA", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Saha, DurumId = (int)ProjeDurum.Hazirlaniyor };
                var yedek = new Proje { ProjeNo = "PA-Y", Musteri = "GE", ProjeTipiId = (int)ProjeTipi.Yedek, DurumId = (int)ProjeDurum.Hazirlaniyor };
                Sandik S(Proje p, string no) => new() { Proje = p, SandikNo = no, Ad = "Sandık " + no, DurumId = (int)SandikDurum.Hazirlaniyor, TipId = 3, DepoLokasyonId = 2 };
                var a3 = S(a, "3"); var a10 = S(a, "10"); var b1 = S(b, "1"); var y1 = S(yedek, "Y-1");
                // PA-B'nin tahsissiz satırı fiili sandık no "7" ile bu sevk edilmiş sandıkta sayılır (Eksik raporu, R-07).
                var b7 = S(b, "7"); b7.DurumId = (int)SandikDurum.Sevkedildi;
                var cekiA = new Ceki { Proje = a, OrijinalDosyaYolu = "a.xlsx" };
                // PA-A: 10 istenen, 4 geldi → 6 eksik; iki sandığa bölünmüş.
                var kok = new CekiSatiri { Ceki = cekiA, SiraNo = 2, BarkodNo = "FC1", Aciklama = "Kablo Bağı\n\\Cable Holder", IstenenAdet = 10, GelenMiktar = 4, BirimId = 1 };
                // PA-A: 5 istenen, 0 geldi; 2 adedi sahada tamamlandı → kaynakta 3 eksik.
                var aktarilan = new CekiSatiri { Ceki = cekiA, SiraNo = 1, BarkodNo = " fc1 ", Aciklama = "Kablo Bağı", IstenenAdet = 5, BirimId = 1 };
                var farkli = new CekiSatiri { Ceki = cekiA, SiraNo = 3, BarkodNo = "FC1-01", Aciklama = "Başka ürün", IstenenAdet = 9, BirimId = 1 };
                // PA-B: tamamlanmış (eksik 0), tahsissiz: sandık fiili sandık no'dan.
                var tamam = new CekiSatiri { Ceki = new Ceki { Proje = b, OrijinalDosyaYolu = "b.xlsx" }, SiraNo = 1, BarkodNo = "FC1",
                    Aciklama = "Kablo Bağı", IstenenAdet = 2, GelenMiktar = 2, FiiliSandikNo = "7", BirimId = 1 };
                db.CekiSatirlari.AddRange(kok, aktarilan, farkli, tamam);
                db.Sandiklar.AddRange(a3, a10, b1, b7, y1);
                db.SandikIcerikleri.AddRange(
                    new SandikIcerik { Sandik = a10, CekiSatiri = kok, TahsisMiktari = 6, KonulanAdet = 2 },
                    new SandikIcerik { Sandik = a3, CekiSatiri = kok, TahsisMiktari = 4, KonulanAdet = 2 },
                    new SandikIcerik { Sandik = y1, BarkodNo = "Fc1", Isim = "Yedek kablo bağı", Miktar = 3, KonulanAdet = 1, BirimId = 1 });
                await db.SaveChangesAsync();
                // Saha projesinde kaynak satırın 2 adedi tamamlandı (Eksik Ürünler raporunun tamamlama planı).
                db.CekiSatirlari.Add(new CekiSatiri { Ceki = new Ceki { Proje = saha, OrijinalDosyaYolu = "s.xlsx" }, SiraNo = 1,
                    BarkodNo = "FC1", Aciklama = "Kablo Bağı", IstenenAdet = 2, GelenMiktar = 2, KaynakCekiSatiriId = aktarilan.Id, BirimId = 1 });
                await db.SaveChangesAsync();
            }

            await using var okuma = new AppDbContext(options);
            IBarkodUrunAramaService servis = new BarkodUrunAramaService(okuma);

            var eksikler = await servis.AraAsync("  fc1", yalnizEksikler: true, projeTipiId: null, default);
            Assert.Equal("FC1", eksikler.Barkod);
            Assert.Equal([("PA-A", "3, 10", 2, 6m, 4m), ("PA-A", "-", 1, 3m, 0m), ("PA-Y", "Y-1", (int?)null, 2m, 1m)],
                eksikler.Satirlar.Select(s => (s.ProjeNo, s.SandikNo, s.SiraNo, s.Eksik, s.UcKGelen)));
            Assert.True(eksikler.Satirlar[2].ElleEklenen);
            Assert.Equal(("Yedek kablo bağı", "Elle eklenen"), (eksikler.Satirlar[2].Aciklama, eksikler.Satirlar[2].GridDurumu));
            // Özet süzgeçten bağımsız: 5 satır (4 proje), 3 eksik satır (2 proje), toplam 11 eksik; "-01" ekli ürün yok.
            Assert.Equal((5, 4, 3, 2, 11m, false), (eksikler.ToplamSatir, eksikler.ProjeSayisi, eksikler.EksikSatir,
                eksikler.EksikProjeSayisi, eksikler.ToplamEksik, eksikler.Sinirli));
            Assert.StartsWith("Kablo Bağı", eksikler.Aciklama);

            var tumu = await servis.AraAsync("FC1", yalnizEksikler: false, projeTipiId: null, default);
            Assert.Equal(["PA-A", "PA-A", "PA-Y", "PA-A-SAHA", "PA-B"], tumu.Satirlar.Select(s => s.ProjeNo));
            Assert.Equal(("7", 0m, "Sevk Edildi"), (tumu.Satirlar[4].SandikNo, tumu.Satirlar[4].Eksik, tumu.Satirlar[4].SevkDurumu));
            Assert.Equal("Sevk Edilmedi", tumu.Satirlar[0].SevkDurumu);
            // Proje tipi veritabanında süzülür; özet o tipin satırlarıdır.
            var sahaSonucu = await servis.AraAsync("FC1", false, (int)ProjeTipi.Saha, default);
            Assert.Equal(["PA-A-SAHA"], sahaSonucu.Satirlar.Select(s => s.ProjeNo));
            Assert.Equal((1, 1), (sahaSonucu.ToplamSatir, sahaSonucu.ProjeSayisi));
            Assert.Empty((await servis.AraAsync("YOK-1", false, null, default)).Satirlar);

            var pdf = servis.PdfOlustur(tumu);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
            using var wb = new XLWorkbook(new MemoryStream(servis.ExcelOlustur(tumu)));
            var ws = wb.Worksheet("Barkod Arama");
            Assert.StartsWith("Barkodla Ürün Arama Raporu – FC1", ws.Cell(1, 1).GetString());
            Assert.Equal(("Proje No", "Eksik"), (ws.Cell(5, 1).GetString(), ws.Cell(5, 12).GetString()));
            Assert.Equal(["PA-A", "PA-A", "PA-Y", "PA-A-SAHA", "PA-B"], Enumerable.Range(6, 5).Select(r => ws.Cell(r, 1).GetString()));
            Assert.Equal(6m, ws.Cell(6, 12).GetValue<decimal>());
        });
    }

    private sealed class SahteServis : IBarkodUrunAramaService
    {
        public string? SonBarkod { get; private set; }
        public bool SonYalnizEksik { get; private set; }
        public int? SonTip { get; private set; }

        public Task<BarkodUrunAramaSonucu> AraAsync(string barkod, bool yalnizEksikler, int? projeTipiId, CancellationToken cancellationToken)
        {
            (SonBarkod, SonYalnizEksik, SonTip) = (barkod, yalnizEksikler, projeTipiId);
            return Task.FromResult(new BarkodUrunAramaSonucu { Barkod = BarkodUrunAramaKurali.Normalize(barkod)! });
        }

        public byte[] PdfOlustur(BarkodUrunAramaSonucu sonuc) => [1];
        public byte[] ExcelOlustur(BarkodUrunAramaSonucu sonuc) => [2];
    }
}
