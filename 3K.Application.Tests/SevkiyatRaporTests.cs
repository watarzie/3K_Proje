using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using _3K.Application.Common;
using _3K.Application.Features.SevkiyatIslemleri.Queries;
using _3K.Application.Features.SevkiyatIslemleri.Validators;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Sevkiyat raporları (Sevkiyatlar sayfası): dönem raporu sayfanın süzgeçleriyle aynı seferleri seçer; her seferin
/// projeleri, doğal sırada sandıkları ve sandık içerikleri (tahsis miktarı) rapora girer; tek sefer ve eski kayıt PDF'i;
/// PDF ve Excel dosyaları üretilir; yetki sevkiyatlar R'dir.
/// </summary>
public sealed class SevkiyatRaporTests
{
    [Fact]
    public void Model_AracOzetiPlakayaGoreToplar_AgirligiOlmayanSandikToplamaKatilmaz()
    {
        var rapor = OrnekRapor();

        Assert.Equal((3, 4, 5, 330m, 290m), (rapor.Seferler.Count, rapor.ProjeSayisi, rapor.SandikSayisi, rapor.BrutKg, rapor.NetKg));
        var araclar = rapor.AracOzetleri();
        Assert.Equal(["34 ABC 123", SevkiyatRaporKurali.PlakasizEtiketi], araclar.Select(a => a.Plaka));
        var arac = araclar[0];
        Assert.Equal((2, 3, 4, 330m), (arac.SeferSayisi, arac.ProjeSayisi, arac.SandikSayisi, arac.BrutKg));
        Assert.Equal((new DateTime(2026, 10, 1, 9, 0, 0), new DateTime(2026, 10, 7, 14, 30, 0)), (arac.IlkSevk, arac.SonSevk));
        Assert.Equal(["PA698-08", "PA727-03", "PA727-03-SAHA"], arac.Projeler);
        Assert.Equal(SevkiyatRaporKurali.EskiKayitEtiketi, rapor.Seferler[2].Etiket);
        Assert.True(rapor.Seferler[0].DuzeltmeAcikSandikVar);
        Assert.False(rapor.Seferler[1].DuzeltmeAcikSandikVar);
    }

    [Fact]
    public void Excel_BesSayfaliDokum_SeferProjeSandikVeUrunSatirlariDogruDoner()
    {
        var bytes = new SevkiyatRaporDosyaService().DonemExcelOlustur(OrnekRapor());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(["Seferler", "Araçlar", "Projeler", "Sandıklar", "Ürünler"], wb.Worksheets.Select(w => w.Name));
        var seferler = wb.Worksheet("Seferler");
        Assert.Equal("Sevkiyat Raporu – Seferler", seferler.Cell(1, 1).GetString());
        Assert.Contains("Plaka: 34 ABC", seferler.Cell(2, 1).GetString());
        Assert.Equal("Sefer no", seferler.Cell(5, 1).GetString());
        Assert.Equal("2026-000005", seferler.Cell(6, 1).GetString());
        Assert.Equal(new DateTime(2026, 10, 7, 14, 30, 0), seferler.Cell(6, 3).GetDateTime());
        Assert.Equal(("34 ABC 123", "34 XYZ 99", "Ahmet"), (seferler.Cell(6, 4).GetString(), seferler.Cell(6, 5).GetString(), seferler.Cell(6, 6).GetString()));
        Assert.Equal((3, 250m), (seferler.Cell(6, 9).GetValue<int>(), seferler.Cell(6, 11).GetValue<decimal>()));
        Assert.Equal("Evet", seferler.Cell(6, 13).GetString());
        Assert.Equal(SevkiyatRaporKurali.EskiKayitEtiketi, seferler.Cell(8, 1).GetString());
        Assert.Equal(8, seferler.LastRowUsed()!.RowNumber());

        Assert.Equal(7, wb.Worksheet("Araçlar").LastRowUsed()!.RowNumber());
        Assert.Equal(9, wb.Worksheet("Projeler").LastRowUsed()!.RowNumber());
        var sandiklar = wb.Worksheet("Sandıklar");
        Assert.Equal(10, sandiklar.LastRowUsed()!.RowNumber());
        Assert.Equal(("PA727-03", "1", "Kanat kökü", "Root", "Kontrplak Sandık"),
            (sandiklar.Cell(6, 4).GetString(), sandiklar.Cell(6, 5).GetString(), sandiklar.Cell(6, 6).GetString(),
             sandiklar.Cell(6, 7).GetString(), sandiklar.Cell(6, 8).GetString()));
        Assert.Equal((1200m, 100m, "Evet", 2), (sandiklar.Cell(6, 9).GetValue<decimal>(), sandiklar.Cell(6, 13).GetValue<decimal>(),
            sandiklar.Cell(6, 15).GetString(), sandiklar.Cell(6, 16).GetValue<int>()));
        // Ağırlığı girilmemiş sandıkta hücre boş kalır.
        Assert.True(sandiklar.Cell(10, 13).IsEmpty());

        var urunler = wb.Worksheet("Ürünler");
        Assert.Equal(8, urunler.LastRowUsed()!.RowNumber());
        Assert.Equal(("1", "P-2", "MLZ-002", "Bushing", 6m, 5m, 1m, "Adet"),
            (urunler.Cell(7, 5).GetString(), urunler.Cell(7, 6).GetString(), urunler.Cell(7, 7).GetString(), urunler.Cell(7, 8).GetString(),
             urunler.Cell(7, 9).GetValue<decimal>(), urunler.Cell(7, 10).GetValue<decimal>(), urunler.Cell(7, 11).GetValue<decimal>(),
             urunler.Cell(7, 12).GetString()));
        Assert.True(urunler.AutoFilter.IsEnabled);
    }

    [Fact]
    public void Pdf_DonemVeSeferRaporuUretilir_BosAlanlarHataVermez()
    {
        var servis = new SevkiyatRaporDosyaService();
        var rapor = OrnekRapor();

        var donem = servis.DonemPdfOlustur(rapor);
        var sefer = servis.SeferPdfOlustur(rapor.Seferler[0], new DateTime(2026, 10, 13, 10, 0, 0));
        // Plakası, şoförü, açıklaması ve sandık ağırlığı olmayan eski kayıt.
        var eski = servis.SeferPdfOlustur(rapor.Seferler[2], new DateTime(2026, 10, 13, 10, 0, 0));

        Assert.All([donem, sefer, eski], pdf =>
        {
            Assert.True(pdf.Length > 1000);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        });
    }

    /// <summary>
    /// Müşteri gerçekleşen çeki listesi: yalnız istenen projenin sevk edilen sandıkları, doğal sırada; ürünlerden yalnız
    /// konulanlar konulan miktarla; aynı sandık iki sevkiyattaysa son sevk satırı kalır; toplamlar bilinen ağırlıklardır.
    /// </summary>
    [Fact]
    public void MusteriCeki_YalnizProjeninSevkEdilenSandiklariVeKonulanUrunleri()
    {
        var bos = new SevkiyatRaporUrunu("P-9", "MLZ-009", "Konulmadı", 2, 0, 2, "Adet");
        var bushing = new SevkiyatRaporUrunu("P-2", "MLZ-002", "Bushing", 6, 5, 1, "Adet");
        SevkiyatRaporSandigi S(string no, decimal? brut, params SevkiyatRaporUrunu[] urunler) =>
            new(no, "Sandık " + no, no == "10" ? "Case 10" : null, "Ahşap", 1200, 800, 900, brut - 10, brut, "3K", false, urunler);
        var seferler = new[]
        {
            new SevkiyatRaporSeferi("2026-000007", false, "34 ABC 123", null, null, new DateTime(2026, 10, 9, 8, 0, 0), null, null,
                [new SevkiyatRaporProjesi("PA727-03", "GCA", "Normal", 2, null, [S("2", 120, bushing)]),
                 new SevkiyatRaporProjesi("PA698-08", "GE", "Normal", 1, null, [S("1", 999)])]),
            new SevkiyatRaporSeferi("2026-000005", false, "16 DEF 45", null, null, new DateTime(2026, 10, 7, 14, 30, 0), null, null,
                [new SevkiyatRaporProjesi("PA727-03", "GCA", "Normal", 1, null, [S("10", null, bos, bushing), S("2", 100), S("1", 50)])]),
        };

        var liste = MusteriCekiListesi.Kur("PA727-03", "GCA", "NIGERIA", new DateTime(2026, 10, 24), seferler);

        Assert.Equal(["1", "2", "10"], liste.Sandiklar.Select(s => s.SandikNo));
        // Sandık 2 iki sevkiyatta: son sevk (09.10, 34 ABC 123) kalır.
        Assert.Equal((new DateTime(2026, 10, 9, 8, 0, 0), "34 ABC 123", 120m), (liste.Sandiklar[1].SevkTarihi, liste.Sandiklar[1].AracPlaka, liste.Sandiklar[1].BrutKg));
        Assert.Equal([("MLZ-002", 5m)], liste.Sandiklar[2].Urunler.Select(u => (u.MalzemeKodu, u.Miktar)));
        Assert.Equal((3, 170m, 2), (liste.Sandiklar.Count, liste.BrutKg, liste.UrunSatiri));
    }

    [Fact]
    public void MusteriCeki_PdfVeExcelAyniSatirlariIkiDilliBasliklaYazar()
    {
        var bushing = new MusteriCekiUrunu("P-2", "MLZ-002", "Bushing", 5, "Adet");
        var liste = new MusteriCekiListesi("PA727-03", "GCA", "NIGERIA", new DateTime(2026, 10, 24, 9, 0, 0),
        [
            new MusteriCekiSandigi("1", "Kanat kökü", "Root", 1200, 800, 900, 90, 100, new DateTime(2026, 10, 7, 14, 30, 0), "34 ABC 123",
                [bushing, new MusteriCekiUrunu(null, "MLZ-001", "Conta", 4, null)]),
            new MusteriCekiSandigi("2", "Sandık 2", null, null, null, null, null, null, new DateTime(2026, 10, 9), null, []),
        ]);
        var servis = new SevkiyatRaporDosyaService();

        var pdf = servis.MusteriCekiPdfOlustur(liste);
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

        using var wb = new XLWorkbook(new MemoryStream(servis.MusteriCekiExcelOlustur(liste)));
        Assert.Equal(["Sandıklar - Cases", "Ürünler - Items"], wb.Worksheets.Select(w => w.Name));
        var sandiklar = wb.Worksheet("Sandıklar - Cases");
        Assert.Equal("Gerçekleşen Çeki Listesi / Actual Packing List – PA727-03", sandiklar.Cell(1, 1).GetString());
        Assert.Contains("Müşteri / Customer: GCA", sandiklar.Cell(2, 1).GetString());
        // Sevk tarihi müşteri belgesinde yok (bilgi satırında ve sütunlarda).
        Assert.DoesNotContain("Sevk", sandiklar.Cell(2, 1).GetString());
        Assert.DoesNotContain("Shipment", sandiklar.Cell(2, 1).GetString());
        Assert.DoesNotContain(sandiklar.Row(5).CellsUsed(), c => c.GetString().Contains("Sevk") || c.GetString().Contains("Shipment"));
        Assert.Contains("Koli / Cases: 2", sandiklar.Cell(3, 1).GetString());
        Assert.Equal("Koli No / Case No", sandiklar.Cell(5, 2).GetString());
        Assert.Equal(("1", "Kanat kökü", "Root", 1200m, 100m, 2, "Kalem / Items"),
            (sandiklar.Cell(6, 2).GetString(), sandiklar.Cell(6, 3).GetString(), sandiklar.Cell(6, 4).GetString(),
             sandiklar.Cell(6, 5).GetValue<decimal>(), sandiklar.Cell(6, 9).GetValue<decimal>(), sandiklar.Cell(6, 10).GetValue<int>(),
             sandiklar.Cell(5, 10).GetString()));
        Assert.True(sandiklar.Cell(5, 11).IsEmpty());
        Assert.True(sandiklar.Cell(7, 9).IsEmpty());
        Assert.Equal(7, sandiklar.LastRowUsed()!.RowNumber());

        var urunler = wb.Worksheet("Ürünler - Items");
        Assert.Equal("Miktar / Qty", urunler.Cell(5, 7).GetString());
        Assert.Equal(("1", "Kanat kökü / Root", "P-2", "MLZ-002", "Bushing", 5m, "Adet"),
            (urunler.Cell(6, 1).GetString(), urunler.Cell(6, 2).GetString(), urunler.Cell(6, 4).GetString(), urunler.Cell(6, 5).GetString(),
             urunler.Cell(6, 6).GetString(), urunler.Cell(6, 7).GetValue<decimal>(), urunler.Cell(6, 8).GetString()));
        Assert.Equal(7, urunler.LastRowUsed()!.RowNumber());
    }

    /// <summary>İzin, projenin gerçek tipinin gerçekleşen çeki raporu okumasıdır (proje tipi veritabanından okunur).</summary>
    [Theory]
    [InlineData((int)ProjeTipi.Normal, YetkiKodlari.Sandik.GerceklesenCekiRaporu)]
    [InlineData((int)ProjeTipi.Saha, YetkiKodlari.Saha.GerceklesenCekiRaporu)]
    public async Task MusteriCeki_YetkisiProjeTipininGerceklesenCekiRaporuOkumasidir(int projeTipiId, string beklenen)
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 5, ProjeNo = "PA727-03", ProjeTipiId = projeTipiId });
        var istek = new _3K.Application.Features.PdfIslemleri.Queries.GetMusteriCekiListesiQuery { ProjeId = 5 };
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
        Assert.Equal(SevkiyatRaporBicimleri.Pdf, istek.Format);

        var izin = await new RequestMenuPermissionResolver(uow, new EntityFrameworkReadQueryExecutor()).ResolveAsync(istek, default);
        Assert.Equal([MenuPermissionRequirement.Read(beklenen)], izin.Groups.SelectMany(g => g.Requirements));
    }

    [Theory]
    [InlineData("pdf", "pdf")]
    [InlineData(" PDF ", "pdf")]
    [InlineData("excel", "excel")]
    [InlineData("xlsx", "excel")]
    [InlineData("doc", null)]
    [InlineData(null, null)]
    public void Bicim_PdfVeExcelKabulEdilir(string? girdi, string? beklenen) => Assert.Equal(beklenen, SevkiyatRaporBicimleri.Coz(girdi));

    [Fact]
    public void Dogrulayici_GecersizBicimTersTarihVeSefersizIstegiReddeder()
    {
        var donem = new SevkiyatDonemRaporuQueryValidator();
        Assert.True(donem.Validate(new SevkiyatDonemRaporuQuery { Format = "excel", Baslangic = new DateTime(2026, 10, 1), Bitis = new DateTime(2026, 10, 1) }).IsValid);
        Assert.False(donem.Validate(new SevkiyatDonemRaporuQuery { Format = "doc" }).IsValid);
        Assert.False(donem.Validate(new SevkiyatDonemRaporuQuery { Format = "pdf", Baslangic = new DateTime(2026, 10, 2), Bitis = new DateTime(2026, 10, 1) }).IsValid);

        var sefer = new SevkSeferiRaporuQueryValidator();
        Assert.True(sefer.Validate(new SevkSeferiRaporuQuery { SeferId = 5 }).IsValid);
        Assert.True(sefer.Validate(new SevkSeferiRaporuQuery { EskiTarih = new DateTime(2026, 10, 1) }).IsValid);
        Assert.False(sefer.Validate(new SevkSeferiRaporuQuery()).IsValid);
        Assert.False(sefer.Validate(new SevkSeferiRaporuQuery { SeferId = 0 }).IsValid);
    }

    [Fact]
    public async Task Yetki_RaporlarSevkiyatlarOkumaIzniIster()
    {
        var resolver = new RequestMenuPermissionResolver(new OrtakMemoryUow(), new EntityFrameworkReadQueryExecutor());
        foreach (object istek in new object[] { new SevkiyatDonemRaporuQuery(), new SevkSeferiRaporuQuery { SeferId = 1 } })
        {
            Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
            var izin = await resolver.ResolveAsync(istek, default);
            Assert.Equal([MenuPermissionRequirement.Read(YetkiKodlari.Proje.Sevkiyatlar)], izin.Groups.SelectMany(g => g.Requirements));
        }
    }

    /// <summary>
    /// Gerçek SQL: plaka süzgeci yalnız o aracın seferini, proje no süzgeci projeyi içeren seferleri (eski kayıt dahil)
    /// bütün projeleriyle seçer; sandıklar doğal sırada, içerikler tahsis miktarıyla (Grid kapanmışsa tamamı konulmuş),
    /// saha elle eklenen malzeme girilen miktarla gelir; gönderen seferi kaydeden kullanıcıdır; tek sefer ve eski kayıt
    /// PDF'i bulunur; uymayan süzgeç 404, geçersiz biçim 400. Üretilen Excel gerçek veriyle açılır.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Postgres_DonemVeSeferRaporu_SuzgecSandikVeIcerikleriKurar()
    {
        await FinansPostgresTests.InDatabase(async options =>
        {
            var gun = new DateTime(2026, 10, 7);
            int seferId;
            await using (var db = new AppDbContext(options))
            {
                db.Kullanicilar.AddRange(
                    new Kullanici { Id = 7, AdSoyad = "Depo Sorumlusu", Email = "depo@example.invalid", RolId = SistemRolleri.AdminId },
                    new Kullanici { Id = 8, AdSoyad = "Sevk Planlama", Email = "plan@example.invalid", RolId = SistemRolleri.AdminId });
                var normal = new Proje { ProjeNo = "PA727-03", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Normal, Lokasyon = "NIGERIA" };
                var saha = new Proje { ProjeNo = "PA727-03-SAHA", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Saha };
                var diger = new Proje { ProjeNo = "PA698-08", Musteri = "GE", ProjeTipiId = (int)ProjeTipi.Normal };
                Sandik S(Proje p, string no, decimal? brut, bool duzeltme = false) => new()
                {
                    Proje = p, SandikNo = no, Ad = "Sandık " + no, AdIngilizce = "Case " + no, DurumId = (int)SandikDurum.Sevkedildi,
                    TipId = 3, DepoLokasyonId = 2, Boy = 1200, En = 800, Yukseklik = 900, NetKg = brut - 20, GrossKg = brut,
                    SevkiyatDuzeltmeAcikMi = duzeltme
                };
                var n10 = S(normal, "10", 300); var n2 = S(normal, "2", 200); var n1 = S(normal, "1", 100, duzeltme: true);
                var s1 = S(saha, "1", 50); var d1 = S(diger, "1", 80); var d2 = S(diger, "2", null);
                db.Sandiklar.AddRange(n10, n2, n1, s1, d1, d2);

                var ceki = new Ceki { Proje = normal, OrijinalDosyaYolu = "ceki.xlsx" };
                var bushing = new CekiSatiri { Ceki = ceki, SiraNo = 2, OlcuResmiPozNo = "P-2", BarkodNo = "MLZ-002", Aciklama = "Bushing", IstenenAdet = 10 };
                var conta = new CekiSatiri { Ceki = ceki, SiraNo = 1, BarkodNo = "MLZ-001", Aciklama = "Conta", IstenenAdet = 4, GridDurumuId = (int)GridDurum.GridKapandi };
                db.SandikIcerikleri.AddRange(
                    new SandikIcerik { Sandik = n1, CekiSatiri = bushing, TahsisMiktari = 6, KonulanAdet = 5 },
                    new SandikIcerik { Sandik = n2, CekiSatiri = bushing, TahsisMiktari = 4, KonulanAdet = 4 },
                    new SandikIcerik { Sandik = n1, CekiSatiri = conta },
                    new SandikIcerik { Sandik = s1, BarkodNo = "SAHA-01", Isim = "Saha yedeği", Miktar = 3, BirimId = 1 });

                var anaSefer = new SevkSeferi { SeferNo = "2026-000005", AracPlaka = "34ABC123", DorsePlaka = "34XYZ99", Sofor = "Ahmet",
                    Aciklama = "Gebze çıkışlı", SevkTarihi = gun.AddHours(9), KullaniciId = 8 };
                var sefer2 = new SevkSeferi { SeferNo = "2026-000006", AracPlaka = "16DEF45", SevkTarihi = gun.AddDays(1).AddHours(8), KullaniciId = 8 };
                Sevkiyat V(Proje p, int no, DateTime tarih, SevkSeferi? sf, string? plaka, string? aciklama, params Sandik[] sandiklar)
                {
                    var v = new Sevkiyat { Proje = p, SevkiyatNo = no, SevkTarihi = tarih, SevkSeferi = sf, AracPlaka = plaka, Aciklama = aciklama, KullaniciId = 7 };
                    foreach (var sandik in sandiklar) v.Sandiklar.Add(new SevkiyatSandik { Sandik = sandik });
                    return v;
                }
                db.Sevkiyatlar.AddRange(
                    V(normal, 2, gun.AddHours(9), anaSefer, "34 ABC 123", null, n10, n2, n1),
                    V(saha, 1, gun.AddHours(9), anaSefer, "34 ABC 123", null, s1),
                    V(diger, 1, gun.AddDays(-20), null, "06 a 12", "Eski sevk", d1),
                    V(diger, 2, gun.AddDays(1).AddHours(8), sefer2, "16 DEF 45", null, d2));
                // Sevkiyatı olmayan proje: depoda sandığı var, müşteri çeki listesi verilmez.
                var sevksiz = new Proje { ProjeNo = "PA-SEVKSIZ", Musteri = "GCA", ProjeTipiId = (int)ProjeTipi.Normal };
                db.Sandiklar.Add(new Sandik { Proje = sevksiz, SandikNo = "1", Ad = "Depoda", DurumId = (int)SandikDurum.Kapandi, TipId = 3, DepoLokasyonId = 2 });
                await db.SaveChangesAsync();
                seferId = anaSefer.Id;
            }

            async Task<(Result<DosyaIcerigi> Sonuc, YakalayanDosyaServisi Dosya)> Calistir(Func<SevkiyatRaporQueryHandlers, Task<Result<DosyaIcerigi>>> calistir)
            {
                await using var db = new AppDbContext(options);
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var dosya = new YakalayanDosyaServisi();
                return (await calistir(new SevkiyatRaporQueryHandlers(uow, new EntityFrameworkReadQueryExecutor(), new AdliLookup(), dosya)), dosya);
            }

            // Plaka süzgeci: yalnız 34 ABC 123 seferi; projeler proje no, sandıklar doğal sırayla.
            var (sonuc, dosya) = await Calistir(h => h.Handle(new SevkiyatDonemRaporuQuery
            {
                Format = "pdf", Plaka = "34 abc", Baslangic = gun.AddDays(-30), Bitis = gun
            }, default));
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            Assert.Matches(@"^Sevkiyat_Raporu_\d{8}_\d{6}\.pdf$", sonuc.Value!.DosyaAdi);
            var rapor = dosya.Rapor!;
            Assert.Equal("07.09.2026 – 07.10.2026 · Plaka: 34 ABC", rapor.SuzgecAciklamasi);
            var sefer = Assert.Single(rapor.Seferler);
            Assert.Equal(("2026-000005", false, "34 ABC 123", "34 XYZ 99", "Ahmet", "Sevk Planlama", "Gebze çıkışlı"),
                (sefer.SeferNo, sefer.EskiKayit, sefer.AracPlaka, sefer.DorsePlaka, sefer.Sofor, sefer.Gonderen, sefer.Aciklama));
            Assert.Equal(["PA727-03", "PA727-03-SAHA"], sefer.Projeler.Select(p => p.ProjeNo));
            var normalProje = sefer.Projeler[0];
            Assert.Equal((2, "NIGERIA", "LookupProjeTipi:1", "GCA"), (normalProje.SevkiyatNo, normalProje.Lokasyon, normalProje.ProjeTipi, normalProje.Musteri));
            Assert.Equal(["1", "2", "10"], normalProje.Sandiklar.Select(s => s.SandikNo));
            var bir = normalProje.Sandiklar[0];
            Assert.Equal((true, "Case 1", "LookupSandikTipi:3", "LookupDepoLokasyon:2", 1200m, 80m, 100m),
                (bir.DuzeltmeAcik, bir.AdIngilizce, bir.Tip, bir.DepoLokasyonu, bir.Boy, bir.NetKg, bir.BrutKg));
            // Sıra no'ya göre; Grid kapanan conta tamamı konulmuş sayılır, bushing sandığa düşen tahsisle.
            Assert.Equal([("MLZ-001", 4m, 4m, 0m), ("MLZ-002", 6m, 5m, 1m)], bir.Urunler.Select(u => (u.MalzemeKodu, u.Miktar, u.Konulan, u.Eksik)));
            Assert.Equal(("P-2", "LookupBirim:1"), (bir.Urunler[1].PozNo, bir.Urunler[1].Birim));
            Assert.Equal([("MLZ-002", 4m, 4m)], normalProje.Sandiklar[1].Urunler.Select(u => (u.MalzemeKodu, u.Miktar, u.Konulan)));
            Assert.Equal([("SAHA-01", "Saha yedeği", 3m, 3m, 0m)],
                sefer.Projeler[1].Sandiklar.Single().Urunler.Select(u => (u.MalzemeKodu, u.Aciklama, u.Miktar, u.Konulan, u.Eksik)));
            Assert.Equal((4, 650m, true), (sefer.SandikSayisi, sefer.BrutKg, sefer.DuzeltmeAcikSandikVar));

            // Gerçek dosya servisi aynı modelden açılabilen Excel üretir.
            using (var wb = new XLWorkbook(new MemoryStream(new SevkiyatRaporDosyaService().DonemExcelOlustur(rapor))))
                Assert.Equal(9, wb.Worksheet("Ürünler").LastRowUsed()!.RowNumber());

            // Proje no süzgeci: projeyi içeren seferler (eski kayıt dahil), en yeni önce.
            (sonuc, dosya) = await Calistir(h => h.Handle(new SevkiyatDonemRaporuQuery { Format = "excel", ProjeNo = "698" }, default));
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            Assert.EndsWith(".xlsx", sonuc.Value!.DosyaAdi);
            Assert.Equal("Tüm tarihler · Proje no: 698", dosya.Rapor!.SuzgecAciklamasi);
            Assert.Equal(["2026-000006", SevkiyatRaporKurali.EskiKayitEtiketi], dosya.Rapor.Seferler.Select(s => s.Etiket));
            var eski = dosya.Rapor.Seferler[1];
            Assert.Equal(("06 A 12", "Depo Sorumlusu", "Eski sevk", (string?)null), (eski.AracPlaka, eski.Gonderen, eski.Aciklama, eski.Sofor));
            Assert.Null(dosya.Rapor.Seferler[0].Projeler.Single().Sandiklar.Single().BrutKg);

            (sonuc, _) = await Calistir(h => h.Handle(new SevkiyatDonemRaporuQuery { Format = "pdf", Plaka = "99 ZZ" }, default));
            Assert.Equal(404, sonuc.StatusCode);
            (sonuc, _) = await Calistir(h => h.Handle(new SevkiyatDonemRaporuQuery { Format = "doc" }, default));
            Assert.Equal(400, sonuc.StatusCode);

            // Tek sefer ve eski kayıt PDF'i.
            (sonuc, dosya) = await Calistir(h => h.Handle(new SevkSeferiRaporuQuery { SeferId = seferId }, default));
            Assert.Equal(("Sefer_2026-000005.pdf", 2), (sonuc.Value!.DosyaAdi, dosya.Sefer!.Projeler.Count));
            (sonuc, dosya) = await Calistir(h => h.Handle(new SevkSeferiRaporuQuery { EskiPlaka = "06-a-12", EskiTarih = gun.AddDays(-20) }, default));
            Assert.Equal(("Sevkiyat_06A12_20260917.pdf", "PA698-08"), (sonuc.Value!.DosyaAdi, dosya.Sefer!.Projeler.Single().ProjeNo));
            (sonuc, _) = await Calistir(h => h.Handle(new SevkSeferiRaporuQuery { EskiPlaka = "06-a-12", EskiTarih = gun }, default));
            Assert.Equal(404, sonuc.StatusCode);
            (sonuc, _) = await Calistir(h => h.Handle(new SevkSeferiRaporuQuery { SeferId = 999_999 }, default));
            Assert.Equal(404, sonuc.StatusCode);

            // Müşteri gerçekleşen çeki listesi: yalnız projenin sevkiyatındaki sandıklar, konulan ürünlerle.
            async Task<(Result<DosyaIcerigi> Sonuc, YakalayanDosyaServisi Dosya)> Musteri(string projeNo, string format)
            {
                await using var db = new AppDbContext(options);
                using var uow = new UnitOfWork(db, NullLogger<UnitOfWork>.Instance);
                var projeId = await db.Projeler.Where(p => p.ProjeNo == projeNo).Select(p => p.Id).SingleOrDefaultAsync();
                var dosya = new YakalayanDosyaServisi();
                var handler = new _3K.Application.Features.PdfIslemleri.Queries.GetMusteriCekiListesiQueryHandler(
                    uow, new EntityFrameworkReadQueryExecutor(), new AdliLookup(), dosya);
                return (await handler.Handle(new() { ProjeId = projeId == 0 ? 999_999 : projeId, Format = format }, default), dosya);
            }
            (sonuc, dosya) = await Musteri("PA727-03", "excel");
            Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
            Assert.Equal("PA727-03_GerceklesenCekiListesi_Musteri.xlsx", sonuc.Value!.DosyaAdi);
            var musteri = dosya.Musteri!;
            Assert.Equal(("PA727-03", "GCA", "NIGERIA"), (musteri.ProjeNo, musteri.Musteri, musteri.Lokasyon));
            Assert.Equal(["1", "2", "10"], musteri.Sandiklar.Select(s => s.SandikNo));
            Assert.Equal([("MLZ-001", 4m), ("MLZ-002", 5m)], musteri.Sandiklar[0].Urunler.Select(u => (u.MalzemeKodu, u.Miktar)));
            Assert.Empty(musteri.Sandiklar[2].Urunler);
            Assert.Equal((gun.AddHours(9), "34 ABC 123"), (musteri.Sandiklar[0].SevkTarihi, musteri.Sandiklar[0].AracPlaka));
            using (var wb = new XLWorkbook(new MemoryStream(new SevkiyatRaporDosyaService().MusteriCekiExcelOlustur(musteri))))
                Assert.Equal(8, wb.Worksheet("Ürünler - Items").LastRowUsed()!.RowNumber());
            (sonuc, dosya) = await Musteri("PA727-03-SAHA", "pdf");
            Assert.Equal(("PA727-03-SAHA_GerceklesenCekiListesi_Musteri.pdf", "SAHA-01"),
                (sonuc.Value!.DosyaAdi, dosya.Musteri!.Sandiklar.Single().Urunler.Single().MalzemeKodu));
            Assert.Equal(400, (await Musteri("PA-SEVKSIZ", "pdf")).Sonuc.StatusCode);
            Assert.Equal(404, (await Musteri("YOK", "pdf")).Sonuc.StatusCode);
            Assert.Equal(400, (await Musteri("PA727-03", "doc")).Sonuc.StatusCode);
        });
    }

    /// <summary>Üç sefer: iki gerçek (aynı plaka), bir eski kayıt (plakasız, ağırlıksız sandık).</summary>
    private static SevkiyatRaporu OrnekRapor()
    {
        static SevkiyatRaporSandigi Sandik(string no, decimal? brut, bool duzeltme = false, params SevkiyatRaporUrunu[] urunler) =>
            new(no, no == "1" ? "Kanat kökü" : "Sandık " + no, no == "1" ? "Root" : null, "Kontrplak Sandık", 1200, 800, 900,
                brut is null ? null : brut - 10, brut, "3K", duzeltme, urunler);
        var bushing = new SevkiyatRaporUrunu("P-2", "MLZ-002", "Bushing", 6, 5, 1, "Adet");
        var conta = new SevkiyatRaporUrunu(null, "MLZ-001", "Conta", 4, 4, 0, "Adet");
        return new SevkiyatRaporu("Sevkiyat Raporu", "01.10.2026 – 13.10.2026 · Plaka: 34 ABC", new DateTime(2026, 10, 13, 10, 0, 0),
        [
            new SevkiyatRaporSeferi("2026-000005", false, "34 ABC 123", "34 XYZ 99", "Ahmet", new DateTime(2026, 10, 7, 14, 30, 0),
                "Sevk Planlama", "Gebze çıkışlı",
                [
                    new SevkiyatRaporProjesi("PA727-03", "GCA", "Normal", 2, "NIGERIA",
                        [Sandik("1", 100, duzeltme: true, conta, bushing), Sandik("2", 100, false, bushing)]),
                    new SevkiyatRaporProjesi("PA727-03-SAHA", "GCA", "Saha", 1, null, [Sandik("1", 50)]),
                ]),
            new SevkiyatRaporSeferi("2026-000002", false, "34 ABC 123", null, null, new DateTime(2026, 10, 1, 9, 0, 0), "Depo", null,
                [new SevkiyatRaporProjesi("PA698-08", "GE", "Normal", 1, null, [Sandik("4", 80)])]),
            new SevkiyatRaporSeferi("", true, null, null, null, new DateTime(2026, 9, 20), null, null,
                [new SevkiyatRaporProjesi("PA600-35-YDK", "GE", "Yedek", 1, null, [Sandik("Y-1", null)])]),
        ]);
    }

    private sealed class YakalayanDosyaServisi : ISevkiyatRaporDosyaService
    {
        public SevkiyatRaporu? Rapor { get; private set; }
        public SevkiyatRaporSeferi? Sefer { get; private set; }
        public byte[] DonemPdfOlustur(SevkiyatRaporu rapor) { Rapor = rapor; return [1]; }
        public byte[] DonemExcelOlustur(SevkiyatRaporu rapor) { Rapor = rapor; return [2]; }
        public byte[] SeferPdfOlustur(SevkiyatRaporSeferi sefer, DateTime olusturmaTarihi) { Sefer = sefer; return [3]; }
        public MusteriCekiListesi? Musteri { get; private set; }
        public byte[] MusteriCekiPdfOlustur(MusteriCekiListesi liste) { Musteri = liste; return [4]; }
        public byte[] MusteriCekiExcelOlustur(MusteriCekiListesi liste) { Musteri = liste; return [5]; }
    }

    /// <summary>Lookup tipini ve kimliği metne yazar; doğru lookup'ın sorulduğu görülür.</summary>
    private sealed class AdliLookup : ILookupCacheService
    {
        public string GetDeger<T>(int id) where T : LookupBase => $"{typeof(T).Name}:{id}";
        public Task WarmupAsync(CancellationToken token = default) => Task.CompletedTask;
        public Task RefreshAsync<T>(CancellationToken token = default) where T : LookupBase => Task.CompletedTask;
    }
}
