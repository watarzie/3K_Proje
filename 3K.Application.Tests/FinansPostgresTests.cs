using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services.Finans;

namespace _3K.Application.Tests;

public sealed class FinansPostgresTests
{
    private static readonly DateTime Date = new(2026, 9, 19);
    private static readonly CancellationToken Ct = CancellationToken.None;

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task TutarDagitimi_KismiPoFatura_AsimRollback_Tamamlama_ve_GunHassasPanel()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options);
            var work = Work("Sabit", 10000m); db.Add(work); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var finans = Finans(db);
            var po = await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-6", work.Id, 6000), Ct);
            Assert.Equal(0m, Assert.Single(po.Kalemler).Adet);
            Assert.True(po.Kalemler[0].TutarBazli);
            Assert.Equal(4000, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.KalanSiparisNetTutar);
            var beforeAudits = await db.Set<FinansDegisiklikGecmisi>().CountAsync();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaOlusturAsync(Invoice("INV-BAD", po.Id, po.Kalemler[0].Id, 6001), Ct));
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());
            Assert.Equal(beforeAudits, await db.Set<FinansDegisiklikGecmisi>().CountAsync());
            await finans.SiparisFatura.FaturaOlusturAsync(Invoice("INV-6", po.Id, po.Kalemler[0].Id, 6000), Ct);
            var partial = (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(FinansIsDurumu.KismiFaturalandi, partial.Durum);
            var second = await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-4", work.Id, 4000), Ct);
            await finans.SiparisFatura.FaturaOlusturAsync(Invoice("INV-4", second.Id, second.Kalemler[0].Id, 4000), Ct);
            Assert.Equal(FinansIsDurumu.Faturalandi, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.Durum);
            var category = new FinansGiderKategori { Ad = "Ücret", Aktif = true }; db.Add(category); await db.SaveChangesAsync();
            var advance = await finans.Gider.GiderOlusturAsync(Expense(category.Id, 1000, true), Ct);
            var expense = await finans.Gider.GiderOlusturAsync(Expense(category.Id, 2500, false, advance.Id), Ct);
            Assert.Equal(Date, expense.FinansTarihi);
            Assert.Equal("BELGE-QA", expense.BelgeNo);
            var panel = await finans.Panel.PanelAsync(new(2026, 9, 15), new(2026, 9, 30), Ct);
            var euro = Assert.Single(panel.Tutarlar, x => x.ParaBirimi == "EUR");
            Assert.Equal(10000m, euro.Gelir); Assert.Equal(2500m, euro.Gider); Assert.Equal(7500m, euro.TahminiKar);
            var listed = await finans.Panel.GenelAramaAsync(new() { PageSize = 1 }, Ct);
            Assert.Equal(10000, Assert.Single(listed.Toplamlar).NetTutar);
            Assert.Equal(10000, Assert.Single((await finans.SiparisFatura.SiparislerAsync(new() { PageSize = 1 }, Ct)).Toplamlar).NetTutar);
            Assert.Equal(10000, Assert.Single((await finans.SiparisFatura.FaturalarAsync(new() { PageSize = 1 }, Ct)).Toplamlar).NetTutar);
            var reports = finans.Raporlar;
            var filter = new FinansListeFiltre(Baslangic: new(2026, 9, 15), Bitis: new(2026, 9, 30));
            foreach (var reportType in new[] { "genel", "gider-kategori", "proje-maliyet", "siparis-bekleyen", "fatura-bekleyen" })
            {
                var excel = await reports.OzetRaporAsync(reportType, true, filter, Ct);
                using var workbook = new XLWorkbook(new MemoryStream(excel));
                Assert.Equal(2, workbook.Worksheets.Count);
                if (reportType is "genel" or "gider-kategori")
                {
                    var headers = reportType == "genel"
                        ? new[] { "Para Birimi", "İş Net Bedeli", "Faturalanan Net Gelir", "Net Gider", "Fark", "Tahmini Kâr" }
                        : new[] { "Kategori", "Para Birimi", "Kayıt", "Net Gider", "KDV", "Brüt" };
                    var sheet = workbook.Worksheet("Finans Özeti");
                    for (var index = 0; index < headers.Length; index++)
                    {
                        var header = sheet.Cell(1, index + 1);
                        Assert.Equal(headers[index], header.GetString());
                        Assert.True(header.Style.Font.Bold);
                        Assert.Equal(XLColor.White.Color.ToArgb(), header.Style.Font.FontColor.Color.ToArgb());
                        Assert.Equal(XLFillPatternValues.Solid, header.Style.Fill.PatternType);
                        Assert.Equal(XLColor.FromHtml("#3F51B5").Color.ToArgb(), header.Style.Fill.BackgroundColor.Color.ToArgb());
                    }
                    Assert.Contains("15.09.2026", workbook.Worksheet("Kapsam").Cell(2, 1).GetString());
                    Assert.Contains("30.09.2026", workbook.Worksheet("Kapsam").Cell(2, 1).GetString());
                }
                var pdf = await reports.OzetRaporAsync(reportType, false, filter, Ct);
                Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
                var qaOutput = Environment.GetEnvironmentVariable("THREEK_REPORT_QA_OUTPUT");
                if (!string.IsNullOrWhiteSpace(qaOutput) && reportType is "genel" or "gider-kategori")
                {
                    // Yalnız açıkça istenen sentetik görsel QA; normal test koşumu dosya üretmez.
                    var output = Path.GetFullPath(qaOutput);
                    Directory.CreateDirectory(output);
                    await File.WriteAllBytesAsync(Path.Combine(output, $"finans-{reportType}.xlsx"), excel);
                    await File.WriteAllBytesAsync(Path.Combine(output, $"finans-{reportType}.pdf"), pdf);
                }
            }
            var movement = await finans.Panel.HareketlerAsync(new() { Baslangic = new(2026, 9, 15), Bitis = new(2026, 9, 30), PageSize = 1 }, "IsKaydi", Ct);
            Assert.Equal(1, movement.TotalCount); Assert.Equal("IsKaydi", Assert.Single(movement.Items).Tur);
            Assert.False((await finans.KaliciSilme.KaliciSilOnizlemeAsync("IsKaydi", work.Id, Ct))!.Silinebilir);
            var audit = await finans.Panel.DegisiklikGecmisiAsync("IsKaydi", work.Id, 1, 250, Ct);
            Assert.NotEmpty(audit.Items);
            await finans.IsKaydi.FinansTarihiDegistirAsync(work.Id, new(new(2026, 10, 3), "Dönem düzeltmesi"), Ct);
            Assert.Equal(10000m, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task EszamanliPo_KapasiteyiAsamaz_ve_HataliBelgeHicKayitBirakmaz()
    {
        await InDatabase(async options =>
        {
            int id;
            await using (var db = new AppDbContext(options)) { var work = Work("Yarış", 10000); db.Add(work); await db.SaveChangesAsync(); id = work.Id; }
            async Task<bool> Attempt(string number)
            {
                await using var concurrent = new AppDbContext(options);
                try { await Finans(concurrent).SiparisFatura.SiparisOlusturAsync(Po(number, id, 6000), Ct); return true; }
                catch (IsKuraliException) { return false; }
            }
            var results = await Task.WhenAll(Attempt("RACE-A"), Attempt("RACE-B"));
            Assert.Single(results, x => x);
            await using var verify = new AppDbContext(options);
            Assert.Equal(6000m, await verify.Set<FinansSiparisKalemi>().SumAsync(x => x.NetTutarSnapshot));
            var finans = Finans(verify);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Belge.BelgeYukleAsync(new("Siparis", 99999, "test.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF")), Ct));
            Assert.Empty(await verify.Set<FinansBelge>().ToArrayAsync());
        });
    }

    /// <summary>
    /// Panel detay dökümü gruplu ve sıralıdır: Normal → Saha → Yedek projeler → Bağımsız işler; grup içinde iş bedeli
    /// büyükten küçüğe. Projesiz işler müşteriye göre ayrı satırlardır (eskiden tek "BAĞIMSIZ" satırında birleşip ilk
    /// müşteriyi gösteriyordu).
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task PanelDokumu_ProjeTipineGoreGruplanir_GrupIcindeTutaraGoreSiralanir_BagimsizMusteriyeBolunur()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            db.Projeler.AddRange(new Proje { Id = 101, ProjeNo = "PA-N", ProjeTipiId = (int)ProjeTipi.Normal },
                new Proje { Id = 102, ProjeNo = "PA-N2", ProjeTipiId = (int)ProjeTipi.Normal },
                new Proje { Id = 103, ProjeNo = "PA-S", ProjeTipiId = (int)ProjeTipi.Saha },
                new Proje { Id = 104, ProjeNo = "PA-Y", ProjeTipiId = (int)ProjeTipi.Yedek });
            FinansIsKaydi Is(int? projeId, string projeNo, string musteri, decimal tutar)
            {
                var w = Work($"{projeNo} {musteri}", tutar);
                w.ProjeId = projeId; w.ProjeNo = projeNo; w.Musteri = musteri;
                return w;
            }
            db.Set<FinansIsKaydi>().AddRange(Is(101, "PA-N", "GE", 100m), Is(102, "PA-N2", "GE", 300m), Is(103, "PA-S", "GE", 900m),
                Is(104, "PA-Y", "GE", 500m), Is(null, "BAĞIMSIZ", "GE Vernova", 50m), Is(null, "BAĞIMSIZ", "ACME", 70m),
                Is(null, "BAĞIMSIZ", "ACME", 5m));
            await db.SaveChangesAsync();

            var panel = await finans.Panel.PanelAsync(new(2026, 9, 1), new(2026, 9, 30), Ct);

            Assert.Equal(
                [("PA-N2", "GE", FinansPanelProjeGrubu.Normal, 1), ("PA-N", "GE", FinansPanelProjeGrubu.Normal, 1),
                 ("PA-S", "GE", FinansPanelProjeGrubu.Saha, 1), ("PA-Y", "GE", FinansPanelProjeGrubu.Yedek, 1),
                 ("BAĞIMSIZ", "ACME", FinansPanelProjeGrubu.Bagimsiz, 2), ("BAĞIMSIZ", "GE Vernova", FinansPanelProjeGrubu.Bagimsiz, 1)],
                panel.ProjeOzetleri!.Select(x => (x.ProjeNo, x.Musteri, x.ProjeGrubu, x.IsKaydi)));
            Assert.Equal(75m, panel.ProjeOzetleri!.Single(x => x.Musteri == "ACME").IsBedeli);
        });
    }

    /// <summary>
    /// Kontra sandıkta Ambalaj m³ hesaplamaz: ahşap ve kontra m³ elle girilir, bileşik toplam m³ tarifesiyle fiyatlanır,
    /// kayıt otomatik fiyatlamada kalır (FinansMiktariManuel değişmez), üretim aktarımı hacmi ezmez ve PO'ya bağlı
    /// kaydın hacmi değiştirilemez.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task KontraHacim_ElleGirilir_BilesikToplamTarifeyleFiyatlanir_AktarimEzmez()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            await FinansPostgresTests.PoOnayiniKapatAsync(db);
            var kaynak = new FinansUretimAktarimModel("AMBALAJURETIM", "kontra-1", true, null, "QA", "Sentetik", FinansIsTuru.AnaAmbalaj,
                "Kontra kasa", 2, 0m, Date, Date, SandikTipi: "Kontrplak Sandık", SandikCinsi: AmbalajSandikCinsi.Kontrplak,
                UretimM3Hesaplanabilir: false);
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([kaynak], Ct);
            var kayit = await db.Set<FinansIsKaydi>().SingleAsync();
            Assert.Equal(0m, kayit.ToplamM3);
            kayit.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Metrekup;
            kayit.BirimFiyatSnapshot = 100m;
            kayit.TarifeYiliSnapshot = 2026;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.HacimGirAsync(kayit.Id, new(0m, 0m, "Boş hacim"), Ct));
            var girilen = (await finans.IsKaydi.HacimGirAsync(kayit.Id, new(1.2m, 0.8m, "Kontra ölçüsü girildi"), Ct))!;

            Assert.Equal((1.2m, 0.8m, 2m, 1m, true), (girilen.AhsapM3, girilen.KontraM3, girilen.ToplamM3, girilen.BirimM3, girilen.HacimManuel));
            Assert.Equal(200m, girilen.NetTutar);
            Assert.False(girilen.FinansMiktariManuel);

            db.ChangeTracker.Clear();
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([kaynak with { Adet = 4 }], Ct);
            var aktarimSonrasi = (await finans.IsKaydi.IsKaydiGetirAsync(kayit.Id, Ct))!;
            Assert.Equal((4m, 2m, 0.5m, 1.2m, 0.8m, 200m),
                (aktarimSonrasi.Adet, aktarimSonrasi.ToplamM3, aktarimSonrasi.BirimM3, aktarimSonrasi.AhsapM3, aktarimSonrasi.KontraM3, aktarimSonrasi.NetTutar));

            db.ChangeTracker.Clear();
            await finans.SiparisFatura.SiparisOlusturAsync(Po("PO-HACIM", kayit.Id, 200m), Ct);
            db.ChangeTracker.Clear();
            var hata = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.HacimGirAsync(kayit.Id, new(2m, 1m, "PO sonrası deneme"), Ct));
            Assert.Contains("PO'ya bağlı", hata.Message);

            // Ahşap kapalı sandığın m³'ü üretimden gelir; elle hacim reddedilir.
            db.ChangeTracker.Clear();
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([new FinansUretimAktarimModel("AMBALAJURETIM", "ahsap-1", true, null, "QA",
                "Sentetik", FinansIsTuru.AnaAmbalaj, "Ahşap kasa", 1, 1.5m, Date, Date, SandikTipi: "Ahşap Kapalı",
                SandikCinsi: AmbalajSandikCinsi.AhsapKapali)], Ct);
            var ahsap = await db.Set<FinansIsKaydi>().SingleAsync(x => x.KaynakKayitId == "ahsap-1");
            var ahsapHata = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.HacimGirAsync(ahsap.Id, new(0m, 0.5m, "Kontra kapak"), Ct));
            Assert.Contains("kontrplak veya katlanır", ahsapHata.Message);
        });
    }

    /// <summary>
    /// Malzemeli tarife: kontrplak ve katlanır sandıkta (boyutsuz) ahşap m³ ahşap, kontra m³ kontra tarifesiyle fiyatlanır.
    /// Ahşap tarifesi sonradan tanımlanınca fiyatsız kalan ahşap tarafı tamamlanır; ahşap kapalı sandık ahşap tarifesini
    /// tek tarife olarak kullanır; m³ ile kısmi PO işin netinden m³ oranıyla pay alır ve kalan m³ kalan neti kapatır.
    /// </summary>
    /// <summary>
    /// Yalnız ahşap malzemeli tarife tanımlıysa kontrplak sandığın kontra tarafı malzemesiz genel m³ tarifesiyle fiyatlanır;
    /// adetle fiyatlanan genel tarife (sırası önde olsa da) kontra m³'ü fiyatlamak için kullanılmaz.
    /// </summary>
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task MalzemeliTarife_KontraTarifesiYoksa_GenelM3TarifesiKullanilir_AdetTarifesiKullanilmaz()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var baslangic = new DateTime(2026, 1, 1);
            FinansUrunEslesmesiModel Eslesme(FinansSandikMalzemesi? malzeme) => new(null, FinansIsTuru.AnaAmbalaj, null, null, null, malzeme);
            await finans.Urun.UrunOlusturAsync(new("ADET", "Adet kasa", FinansFiyatlandirmaBirimi.Adet, true, 0, [Eslesme(null)], 999m, "EUR", 20, baslangic), Ct);
            await finans.Urun.UrunOlusturAsync(new("GENEL", "Genel m³", FinansFiyatlandirmaBirimi.Metrekup, true, 1, [Eslesme(null)], 280m, "EUR", 20, baslangic), Ct);
            await finans.Urun.UrunOlusturAsync(new("AHSAP", "Ahşap m³", FinansFiyatlandirmaBirimi.Metrekup, true, 2,
                [Eslesme(FinansSandikMalzemesi.Ahsap)], 150m, "EUR", 20, baslangic), Ct);
            db.ChangeTracker.Clear();
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([new FinansUretimAktarimModel("AMBALAJURETIM", "kontra-genel", true, null, "QA",
                "Sentetik", FinansIsTuru.AnaAmbalaj, "Kontra kasa", 1, 0m, Date, Date, SandikTipi: "Kontrplak Sandık",
                SandikCinsi: AmbalajSandikCinsi.Kontrplak, UretimM3Hesaplanabilir: false)], Ct);
            var id = (await db.Set<FinansIsKaydi>().AsNoTracking().SingleAsync()).Id;
            db.ChangeTracker.Clear();

            var kayit = (await finans.IsKaydi.HacimGirAsync(id, new(1.2m, 0.8m), Ct))!;

            Assert.Equal((280m, 150m, 0.8m * 280m + 1.2m * 150m), (kayit.BirimFiyat, kayit.AhsapBirimFiyat, kayit.NetTutar));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task MalzemeliTarife_AhsapVeKontraM3KendiTarifesiyle_KatlanirBoyutsuz_KismiPoOranli()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            await FinansPostgresTests.PoOnayiniKapatAsync(db);
            var baslangic = new DateTime(2026, 1, 1);
            FinansUretimAktarimModel Sandik(string id, string tipi, AmbalajSandikCinsi cinsi, decimal birimM3) =>
                new("AMBALAJURETIM", id, true, null, "QA", "Sentetik", FinansIsTuru.AnaAmbalaj, tipi, 1, birimM3, Date, Date,
                    SandikTipi: tipi, SandikCinsi: cinsi, UretimM3Hesaplanabilir: birimM3 > 0);
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([
                Sandik("kontra-m", "Kontrplak Sandık", AmbalajSandikCinsi.Kontrplak, 0m),
                Sandik("katlanir-m", "Katlanır Sandık", AmbalajSandikCinsi.Katlanir, 0m),
                Sandik("ahsap-m", "Ahşap Kapalı", AmbalajSandikCinsi.AhsapKapali, 1.5m)], Ct);
            var kayitlar = await db.Set<FinansIsKaydi>().AsNoTracking().ToDictionaryAsync(x => x.KaynakKayitId!, x => x.Id);
            db.ChangeTracker.Clear();

            await finans.Urun.UrunOlusturAsync(new("KONTRA", "Kontra m³", FinansFiyatlandirmaBirimi.Metrekup, true, 0,
                [new FinansUrunEslesmesiModel(null, FinansIsTuru.AnaAmbalaj, null, null, null, FinansSandikMalzemesi.Kontra)],
                300m, "EUR", 20, baslangic), Ct);
            db.ChangeTracker.Clear();
            await finans.IsKaydi.HacimGirAsync(kayitlar["kontra-m"], new(1.2m, 0.8m), Ct);
            await finans.IsKaydi.HacimGirAsync(kayitlar["katlanir-m"], new(0.5m, 1m), Ct);
            db.ChangeTracker.Clear();
            var ahsapsiz = (await finans.IsKaydi.IsKaydiGetirAsync(kayitlar["kontra-m"], Ct))!;
            Assert.Equal((300m, 0m, 240m), (ahsapsiz.BirimFiyat, ahsapsiz.AhsapBirimFiyat, ahsapsiz.NetTutar));

            await finans.Urun.UrunOlusturAsync(new("AHSAP", "Ahşap m³", FinansFiyatlandirmaBirimi.Metrekup, true, 1,
                [new FinansUrunEslesmesiModel(null, FinansIsTuru.AnaAmbalaj, null, null, null, FinansSandikMalzemesi.Ahsap)],
                150m, "EUR", 20, baslangic), Ct);
            db.ChangeTracker.Clear();
            var kontra = (await finans.IsKaydi.IsKaydiGetirAsync(kayitlar["kontra-m"], Ct))!;
            var katlanir = (await finans.IsKaydi.IsKaydiGetirAsync(kayitlar["katlanir-m"], Ct))!;
            var ahsap = (await finans.IsKaydi.IsKaydiGetirAsync(kayitlar["ahsap-m"], Ct))!;
            Assert.Equal((300m, 150m, 0.8m * 300m + 1.2m * 150m), (kontra.BirimFiyat, kontra.AhsapBirimFiyat, kontra.NetTutar));
            Assert.Equal((300m, 150m, 375m), (katlanir.BirimFiyat, katlanir.AhsapBirimFiyat, katlanir.NetTutar));
            Assert.Equal((150m, (decimal?)null, 225m), (ahsap.BirimFiyat, ahsap.AhsapBirimFiyat, ahsap.NetTutar));

            // SQL tutar ifadesi (panel/iş takibi) domain kuralıyla aynı neti verir.
            var sqlNetler = await db.Set<FinansIsKaydi>().AsNoTracking().Select(FinansSorgulari.IsNetIfadesi).ToListAsync();
            Assert.Equal(420m + 375m + 225m, sqlNetler.Sum());

            db.ChangeTracker.Clear();
            await finans.SiparisFatura.SiparisOlusturAsync(new("PO-MALZ-1", Date, "QA", [new(katlanir.Id, 0, 0.75m)], "EUR"), Ct);
            db.ChangeTracker.Clear();
            Assert.Equal(187.5m, (await finans.IsKaydi.IsKaydiGetirAsync(katlanir.Id, Ct))!.SiparisNetTutar);
            await finans.SiparisFatura.SiparisOlusturAsync(new("PO-MALZ-2", Date, "QA", [new(katlanir.Id, 0, 0.75m)], "EUR"), Ct);
            db.ChangeTracker.Clear();
            Assert.Equal(375m, (await finans.IsKaydi.IsKaydiGetirAsync(katlanir.Id, Ct))!.SiparisNetTutar);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var source = new FinansUretimAktarimModel("AMBALAJURETIM", "qa-source", true, null, "QA", "Sentetik", FinansIsTuru.AnaAmbalaj, "Kasa", 2, 1.25m, Date, Date, SarfM3: .6m);
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([source], Ct);
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([source], Ct);
            var jobs = await db.Set<FinansIsKaydi>().AsNoTracking().OrderBy(x => x.KaynakBileseni).ToArrayAsync();
            Assert.Equal(2, jobs.Length); Assert.Equal(2.5m, jobs[0].ToplamM3); Assert.Equal(.6m, jobs[1].ToplamM3);
            await finans.IsKaydi.FiyatlandirAsync(jobs[0].Id, new(FinansFiyatlandirmaBirimi.Metrekup, 2, 2, 120, "EUR", 20, "Manuel finans ölçüsü"), Ct);
            await finans.IsKaydi.FinansTarihiDegistirAsync(jobs[0].Id, new(new(2026, 10, 3), "Manuel finans tarihi"), Ct);
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([source with { Adet = 5, BirimM3 = 9, UretimM3Hesaplanabilir = false, SarfM3 = 0 }], Ct);
            var preserved = (await finans.IsKaydi.IsKaydiGetirAsync(jobs[0].Id, Ct))!;
            Assert.Equal(4m, preserved.ToplamM3); Assert.Equal(480m, preserved.NetTutar); Assert.Equal(new DateTime(2026, 10, 3), preserved.FinansTarihi);
            var preview = (await finans.KaliciSilme.KaliciSilOnizlemeAsync("IsKaydi", jobs[0].Id, Ct))!;
            Assert.True(preview.Silinebilir);
            await finans.IsKaydi.FinansTarihiDegistirAsync(jobs[0].Id, new(new(2026, 10, 4), "İkinci tarih düzenlemesi"), Ct);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.KaliciSilme.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, true, "Eski sürüm ile silme"), Ct));
            preview = (await finans.KaliciSilme.KaliciSilOnizlemeAsync("IsKaydi", jobs[0].Id, Ct))!;
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.KaliciSilme.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, false, "Deneme silme gerekçesi"), Ct));
            await finans.KaliciSilme.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, true, "Deneme silme gerekçesi"), Ct);
            db.ChangeTracker.Clear();
            await finans.UretimAktarim.UretimKayitlariniAktarAsync([source], Ct);
            Assert.Equal(1, await db.Set<FinansIsKaydi>().CountAsync());
            Assert.Single(await db.Set<FinansKaynakBastirma>().ToArrayAsync());
            db.Add(new FinansDuzenliIs { IsAdi = "Aylık", IsTuru = FinansIsTuru.OzelIs, OzelIsTuru = "Aylık", Musteri = "QA", BaslangicTarihi = new(2026, 1, 1), OlusturmaGunu = 31, Miktar = 1, BirimFiyat = 500, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true, HesaplamaYontemi = FinansHesaplamaYontemi.SabitAylik });
            await db.SaveChangesAsync();
            Assert.Equal(3, (await finans.DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 3, 31), Ct)).Olusturulan);
            Assert.Equal(0, (await finans.DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 3, 31), Ct)).Olusturulan);
            var dates = await db.Set<FinansIsKaydi>().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).Select(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new[] { new DateTime(2026, 1, 31), new(2026, 2, 28), new(2026, 3, 31) }, dates);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task RaporOnizleme_DosyaylaAyniSutunlarVeFiltrelenmisSatirlariGosterir()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var a = Work("Montaj A", 1000); a.ProjeNo = "PA-1";
            var b = Work("Montaj B", 2500); b.ProjeNo = "PA-2";
            db.AddRange(a, b); await db.SaveChangesAsync();

            var tumu = await finans.Raporlar.OnizlemeAsync("is", new(), Ct);
            Assert.Equal("Finans İşleri Raporu", tumu.Baslik);
            Assert.Equal(25, tumu.Sutunlar.Count);
            Assert.Contains("Ahşap m³", tumu.Sutunlar);
            Assert.Contains("Kontra m³", tumu.Sutunlar);
            Assert.Equal(2, tumu.ToplamSatir);
            Assert.All(tumu.Satirlar, satir => Assert.Equal(tumu.Sutunlar.Count, satir.Count));

            var filtreli = await finans.Raporlar.OnizlemeAsync("is", new() { ProjeNo = "PA-2" }, Ct);
            var satir = Assert.Single(filtreli.Satirlar);
            Assert.Equal("PA-2", satir[1]);
            Assert.Equal("2.500,00", satir[tumu.Sutunlar.ToList().IndexOf("Net")]);
            // Enum değerleri raporda Türkçe adlarıyla yazılır ("OzelIs" değil "Özel İş").
            Assert.Equal(("Özel İş", "Sipariş Bekliyor", "Sabit Tutar"), (satir[tumu.Sutunlar.ToList().IndexOf("İş Türü")],
                satir[tumu.Sutunlar.ToList().IndexOf("Durum")], satir[tumu.Sutunlar.ToList().IndexOf("Fiyatlandırma")]));

            var genel = await finans.Raporlar.OnizlemeAsync("genel", new(), Ct);
            Assert.Equal("Genel Finans Özeti", genel.Baslik);
            Assert.Equal("EUR", Assert.Single(genel.Satirlar)[0]);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.Raporlar.OnizlemeAsync("yok", new(), Ct));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task AylikDeger_DuzenliIsMiktarVeTarifesiBirlikteGuncellenir_BilesenliVePoluDegismez()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var kira = Work("Seymen Kira", 360000); kira.FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet;
            kira.HesaplamaYontemi = FinansHesaplamaYontemi.SabitAylik; kira.Birim = "Hizmet"; kira.ManuelNetTutar = 360000;
            var bilesenli = Work("Bileşenli", 100); bilesenli.HesaplamaYontemi = FinansHesaplamaYontemi.Bilesenli;
            var sabit = Work("Sabit tutar", 500);
            db.AddRange(kira, bilesenli, sabit); await db.SaveChangesAsync();

            Assert.True(await finans.IsKaydi.OzelIsAylikDegerGuncelleAsync(kira.Id, new(Miktar: 2, NetBirimFiyat: 380000), Ct));
            db.ChangeTracker.Clear();
            var guncel = (await finans.IsKaydi.IsKaydiGetirAsync(kira.Id, Ct))!;
            Assert.Equal(2m, guncel.Adet); Assert.Equal(760000m, guncel.NetTutar); Assert.Null(guncel.ManuelNetTutar);

            // Sabit tutarlı satırda gösterilen miktar adetten gelmez; yalnız tarife düzenlenir.
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.OzelIsAylikDegerGuncelleAsync(sabit.Id, new(Miktar: 3), Ct));
            Assert.True(await finans.IsKaydi.OzelIsAylikDegerGuncelleAsync(sabit.Id, new(NetBirimFiyat: 650), Ct));
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.OzelIsAylikDegerGuncelleAsync(bilesenli.Id, new(NetBirimFiyat: 1), Ct));

            await finans.SiparisFatura.SiparisOlusturAsync(Po("AYLIK-PO", kira.Id, 760000), Ct);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.OzelIsAylikDegerGuncelleAsync(kira.Id, new(NetBirimFiyat: 1), Ct));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task Bilesenler_Malzemeli_ve_BelgeSurumuKalici()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var work = Work("Bileşen", 10000); db.Add(work); await db.SaveChangesAsync();
            // F11: kontra sandıkta birden çok m³ satırı; her satır m³, birim fiyat ve malzemesini taşır.
            var price = new FinansFiyatlandirmaModel(FinansFiyatlandirmaBirimi.ManuelToplam, 1, 0, 0, "EUR", 20, "Bileşenli hesaplama",
                Bilesenler: [new("Ahşap hacmi", FinansFiyatlandirmaBirimi.Metrekup, 1.2m, 500, FinansSandikMalzemesi.Ahsap),
                    new("Kontra hacmi", FinansFiyatlandirmaBirimi.Metrekup, .5m, 800, FinansSandikMalzemesi.Kontra),
                    new("Sabit", FinansFiyatlandirmaBirimi.SabitTutar, 1, 200)]);
            var beforeAudit = await db.Set<FinansDegisiklikGecmisi>().CountAsync();
            var invalid = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.FiyatlandirAsync(work.Id,
                price with { Bilesenler = [new("Sabit", FinansFiyatlandirmaBirimi.SabitTutar, 1, 200, FinansSandikMalzemesi.Kontra)] }, Ct));
            Assert.Contains("m³", invalid.Message);
            var undefinedCurrency = await Assert.ThrowsAsync<IsKuraliException>(() => finans.IsKaydi.FiyatlandirAsync(work.Id,
                price with { ParaBirimi = "GBP" }, Ct));
            Assert.Contains("GBP", undefinedCurrency.Message);
            db.ChangeTracker.Clear();
            Assert.Equal(10000m, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
            Assert.Equal(beforeAudit, await db.Set<FinansDegisiklikGecmisi>().CountAsync());
            Assert.Equal(1200m, (await finans.IsKaydi.FiyatlandirAsync(work.Id, price, Ct))!.NetTutar);
            var detail = (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(1200m, detail.ManuelNetTutar);
            Assert.Equal(price.Bilesenler, detail.Bilesenler);
            // Detaydan yeniden açılan aynı form bileşenleri ve malzemeleri kaybetmeden kaydedilir.
            await finans.IsKaydi.FiyatlandirAsync(work.Id, price with { Bilesenler = detail.Bilesenler, ManuelNetTutar = detail.ManuelNetTutar }, Ct);
            Assert.Equal(1200m, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
            var po = await finans.SiparisFatura.SiparisOlusturAsync(Po("DOC", work.Id, 1200), Ct);
            var pdf = await finans.Raporlar.OzetRaporAsync("genel", false, new(), Ct);
            var first = await finans.Belge.BelgeYukleAsync(new("Siparis", po.Id, "../../test.pdf", pdf), Ct);
            var second = await finans.Belge.BelgeYukleAsync(new("Siparis", po.Id, "test.pdf", pdf), Ct);
            Assert.Equal(1, first.Surum); Assert.Equal(2, second.Surum); Assert.DoesNotContain("/", first.OrijinalAd);
            Assert.Equal(pdf, (await finans.Belge.BelgeIndirAsync(first.Id, Ct))!.Icerik);
            await finans.SiparisFatura.FaturaOlusturAsync(Invoice("MIGRATE", po.Id, po.Kalemler[0].Id, 1200), Ct);
            var auditCount = await db.Set<FinansDegisiklikGecmisi>().CountAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(1200, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
            Assert.Equal(pdf, (await finans.Belge.BelgeIndirAsync(first.Id, Ct))!.Icerik);
            Assert.Equal(2, await db.Set<FinansBelge>().CountAsync());
            Assert.Equal(auditCount, await db.Set<FinansDegisiklikGecmisi>().CountAsync());
        });
    }

    [Theory]
    [InlineData(14, "0–14")][InlineData(15, "15–29")][InlineData(29, "15–29")][InlineData(30, "30–59")][InlineData(60, "60–89")][InlineData(90, "90+")]
    public void YaslandirmaAraliklari_Cakismiyor(int day, string bucket) => Assert.Equal(bucket, FinansPanelService.YasGrubu(day));

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var a = Work("Proje A", 10000); a.ProjeNo = "A";
            var b = Work("Proje B", 1500); b.ProjeNo = "B";
            var usd = Work("USD", 2000); usd.ParaBirimiSnapshot = "USD";
            db.AddRange(a, b, usd); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.SiparisOlusturAsync(new("MIXED", Date, "QA",
                [new(a.Id, 0, 0, NetTutar: 500), new(usd.Id, 0, 0, NetTutar: 500)]), Ct));
            db.ChangeTracker.Clear(); Assert.Empty(await db.Set<FinansSiparis>().ToArrayAsync());
            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("MULTI", Date, "QA", [new(a.Id, 0, 0, NetTutar: 1000), new(b.Id, 0, 0, NetTutar: 500)], "EUR", 1500), Ct);
            Assert.Equal(2, po.Kalemler.Count); Assert.Contains("A", po.ProjeNo); Assert.Contains("B", po.ProjeNo);
            // K36: geçerli önizleme sürümü ve açık ikinci onay, bağımlılık korumasını aşamaz.
            var beforeLines = await db.Set<FinansSiparisKalemi>().AsNoTracking()
                .Where(x => x.FinansSiparisId == po.Id).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.FinansSiparisId, x.FinansIsKaydiId, x.NetTutarSnapshot,
                    x.KdvTutariSnapshot, x.ToplamTutarSnapshot, x.ParaBirimiSnapshot }).ToArrayAsync();
            var workSnapshotSql = "SELECT to_jsonb(w)::text AS \"Value\" FROM \"FinansIsKayitlari\" w ORDER BY w.\"Id\"";
            var auditSnapshotSql = "SELECT to_jsonb(a)::text AS \"Value\" FROM \"FinansDegisiklikGecmisleri\" a ORDER BY a.\"Id\"";
            var beforeWorks = await db.Database.SqlQueryRaw<string>(workSnapshotSql).ToArrayAsync();
            var beforeAudits = await db.Database.SqlQueryRaw<string>(auditSnapshotSql).ToArrayAsync();
            var deletePreview = (await finans.KaliciSilme.KaliciSilOnizlemeAsync("IsKaydi", a.Id, Ct))!;
            Assert.False(deletePreview.Silinebilir);
            Assert.NotEmpty(deletePreview.Bagimliliklar);
            var blockedDelete = await Assert.ThrowsAsync<IsKuraliException>(() => finans.KaliciSilme.KaliciSilAsync(
                new("IsKaydi", a.Id, deletePreview.Surum, true, "Bağlı iş için zorlanmış silme denemesi"), Ct));
            // RR Q7/§3.18: engel, işi kullanan PO'yu adıyla belirtir.
            Assert.Contains("Bu kayıt PO MULTI içerisinde kullanılmaktadır.", blockedDelete.Message);
            db.ChangeTracker.Clear();
            var afterLines = await db.Set<FinansSiparisKalemi>().AsNoTracking()
                .Where(x => x.FinansSiparisId == po.Id).OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.FinansSiparisId, x.FinansIsKaydiId, x.NetTutarSnapshot,
                    x.KdvTutariSnapshot, x.ToplamTutarSnapshot, x.ParaBirimiSnapshot }).ToArrayAsync();
            Assert.Equal(beforeLines, afterLines);
            Assert.Equal(1000m, Assert.Single(afterLines, x => x.FinansIsKaydiId == a.Id).NetTutarSnapshot);
            Assert.Equal(500m, Assert.Single(afterLines, x => x.FinansIsKaydiId == b.Id).NetTutarSnapshot);
            Assert.Equal(beforeWorks, await db.Database.SqlQueryRaw<string>(workSnapshotSql).ToArrayAsync());
            Assert.Equal(beforeAudits, await db.Database.SqlQueryRaw<string>(auditSnapshotSql).ToArrayAsync());
            Assert.Equal(1500m, (await finans.SiparisFatura.SiparisGetirAsync(po.Id, Ct))!.Tutarlar.Single().NetTutar);
            var pdf = await finans.Raporlar.OzetRaporAsync("genel", false, new(), Ct);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION qa_reject_pdf_audit() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF NEW."Islem" = 'PDF Yükleme' THEN RAISE EXCEPTION 'QA sentetik audit hatası'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER qa_reject_pdf BEFORE INSERT ON "FinansDegisiklikGecmisleri" FOR EACH ROW EXECUTE FUNCTION qa_reject_pdf_audit();
                """);
            await Assert.ThrowsAsync<DbUpdateException>(() => finans.Belge.BelgeYukleAsync(new("Siparis", po.Id, "gercek.pdf", pdf), Ct));
            db.ChangeTracker.Clear(); Assert.Empty(await db.Set<FinansBelge>().ToArrayAsync());
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var work = Work("Düzeltme", 10000); db.Add(work); await db.SaveChangesAsync();
            var po = await finans.SiparisFatura.SiparisOlusturAsync(Po("REV-PO", work.Id, 6000), Ct);
            var invoice = await finans.SiparisFatura.FaturaOlusturAsync(Invoice("REV-INV", po.Id, po.Kalemler[0].Id, 3000), Ct);
            Assert.Single(invoice.Kalemler);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.SiparisGuncelleAsync(po.Id,
                new(po.PoNumarasi, Date, "QA", [new(work.Id, 0, 0, NetTutar: 2999)], "Geçersiz azaltma"), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal(6000, (await finans.SiparisFatura.SiparisGetirAsync(po.Id, Ct))!.Tutarlar[0].NetTutar);
            await finans.SiparisFatura.SiparisGuncelleAsync(po.Id, new(po.PoNumarasi, Date, "QA", [new(work.Id, 0, 0, NetTutar: 7000)], "PO tutar düzeltmesi"), Ct);
            await Assert.ThrowsAsync<IsKuraliException>(() => finans.SiparisFatura.FaturaGuncelleAsync(invoice.Id,
                new(invoice.FaturaNumarasi, Date, "QA", "EUR", 7001, 1400.2m, 8401.2m, Kalemler: [new(po.Kalemler[0].Id, 0, 0, 7001)], Gerekce: "Geçersiz artış"), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal(3000, (await finans.SiparisFatura.FaturaGetirAsync(invoice.Id, Ct))!.Kalemler[0].NetTutar);
            await finans.SiparisFatura.FaturaGuncelleAsync(invoice.Id,
                new(invoice.FaturaNumarasi, Date, "QA", "EUR", 7000, 1400, 8400, Kalemler: [new(po.Kalemler[0].Id, 0, 0, 7000)], Gerekce: "Fatura tutar düzeltmesi"), Ct);
            var result = (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(7000, result.FaturalananNetTutar); Assert.Equal(3000, result.KalanSiparisNetTutar); Assert.Equal(FinansIsDurumu.KismiFaturalandi, result.Durum);
            const string cancellationReason = "K34 yanlış fatura tutarı nedeniyle iptal";
            var beforeCancel = _3K.Core.Helpers.TurkeyTime.Now.AddSeconds(-1);
            Assert.True(await finans.SiparisFatura.FaturaIptalAsync(invoice.Id, cancellationReason, Ct));
            var afterCancel = _3K.Core.Helpers.TurkeyTime.Now.AddSeconds(1);
            db.ChangeTracker.Clear();
            var restored = (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(0m, restored.FaturalananNetTutar);
            Assert.Equal(7000m, restored.KalanFaturaNetTutar);
            Assert.Equal(3000m, restored.KalanSiparisNetTutar);
            Assert.Equal(FinansIsDurumu.KismiSiparis, restored.Durum);
            var cancelled = await db.Set<FinansFatura>().AsNoTracking().SingleAsync(x => x.Id == invoice.Id);
            Assert.True(cancelled.IptalEdildi);
            Assert.Equal(FinansFaturaDurumu.IptalEdildi, cancelled.Durum);
            Assert.Equal(cancellationReason, cancelled.IptalAciklamasi);
            Assert.InRange(cancelled.IptalTarihi!.Value, beforeCancel, afterCancel);
            Assert.Equal(7000m, await db.Set<FinansFaturaKalemi>().Where(x => x.FinansFaturaId == invoice.Id).SumAsync(x => x.NetTutarSnapshot));
            var cancellationAudit = await db.Set<FinansDegisiklikGecmisi>().AsNoTracking().SingleAsync(x =>
                x.VarlikTuru == nameof(FinansFatura) && x.VarlikId == invoice.Id && x.Islem == "İptal");
            Assert.Equal("7", cancellationAudit.IslemYapan);
            Assert.Equal("7", cancellationAudit.CreatedBy);
            Assert.Equal(cancellationReason, cancellationAudit.Aciklama);
            Assert.Equal("false", cancellationAudit.EskiDeger);
            Assert.Equal("true", cancellationAudit.YeniDeger);
            Assert.InRange(cancellationAudit.CreatedDate, beforeCancel, afterCancel);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task K15_GenelArama_SeciliAyDisindakiProjePoVeFaturayiBulur_DigerFiltreleriKorur()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            var old = Work("Geçmiş dönem işi", 10000); old.ProjeNo = "K15-PROJE-GECMIS";
            old.UretimTarihi = new(2025, 12, 19); old.FinansTarihi = old.UretimTarihi;
            old.FinansDonemi = new(2025, 12, 1); old.KayitTarihi = old.UretimTarihi;
            var current = Work("Seçili ayın işi", 4000); current.ProjeNo = "K15-SIMDI";
            db.AddRange(old, current); await db.SaveChangesAsync();
            var po = await finans.SiparisFatura.SiparisOlusturAsync(Po("K15-PO-ESKI", old.Id, 6000) with { SiparisTarihi = new(2025, 12, 20) }, Ct);
            await finans.SiparisFatura.FaturaOlusturAsync(Invoice("K15-FATURA-ESKI", po.Id, po.Kalemler[0].Id, 3000) with { FaturaTarihi = new(2025, 12, 21) }, Ct);
            var selectedMonth = new FinansListeFiltre(Baslangic: new(2026, 9, 1), Bitis: new(2026, 9, 30), PageSize: 1);
            foreach (var term in new[] { old.ProjeNo, "K15-PO-ESKI", "K15-FATURA-ESKI", "k15-proje-gecmis", "k15-po-eski", "k15-fatura-eski" })
            {
                var filter = selectedMonth with { Arama = term };
                var global = await finans.Panel.GenelAramaAsync(filter, Ct);
                Assert.True(global.TotalCount == 1, $"Genel arama terimi '{term}' seçili ay dışında da bulunmalıdır; kültür={System.Globalization.CultureInfo.CurrentCulture.Name}.");
                var found = Assert.Single(global.Items);
                Assert.Equal(old.Id, found.Id);
                Assert.Equal(new DateTime(2025, 12, 19), found.FinansTarihi);
                Assert.Contains("K15-PO-ESKI", found.PoNumaralari);
                Assert.Contains("K15-FATURA-ESKI", found.FaturaNumaralari);
                Assert.Equal(10000m, Assert.Single(global.Toplamlar).NetTutar);
                Assert.Empty((await finans.Panel.GenelAramaAsync(filter with { ParaBirimi = "USD" }, Ct)).Items);
            }
            var specific = await finans.Panel.GenelAramaAsync(selectedMonth with { ProjeNo = old.ProjeNo, PoNumarasi = "K15-PO-ESKI", FaturaNumarasi = "K15-FATURA-ESKI" }, Ct);
            Assert.Equal(old.Id, Assert.Single(specific.Items).Id);

            var literal = Work("K15-LITERAL %_\\ ISI", 1000);
            literal.Musteri = "K15-FIRMA %_\\"; literal.TalepEdenKisi = "K15-KISI %_\\";
            var category = new FinansGiderKategori { Ad = "K15 gider", Aktif = true };
            db.AddRange(literal, category); await db.SaveChangesAsync();
            var literalPo = await finans.SiparisFatura.SiparisOlusturAsync(Po("K15-PO-%_\\", literal.Id, 100), Ct);
            await finans.SiparisFatura.FaturaOlusturAsync(Invoice("K15-FATURA-%_\\", literalPo.Id, literalPo.Kalemler[0].Id, 50), Ct);
            var expense = await finans.Gider.GiderOlusturAsync(Expense(category.Id, 100, false) with
                { Aciklama = "K15-ESKI %_\\", FirmaVeyaKisi = literal.Musteri }, Ct);
            // Kullanıcı metnindeki LIKE jokerleri yalnız gerçek karakterleri eşleştirir.
            foreach (var term in new[] { "%", "_", "\\" })
            {
                foreach (var filter in new[] { new FinansListeFiltre(Arama: term), new(Firma: term),
                    new(TalepEden: term), new(PoNumarasi: term), new(FaturaNumarasi: term) })
                    Assert.Equal(literal.Id, Assert.Single((await finans.Panel.GenelAramaAsync(filter, Ct)).Items).Id);
                foreach (var filter in new[] { new FinansListeFiltre(Arama: term), new(Firma: term) })
                    Assert.Equal(expense.Id, Assert.Single((await finans.Panel.HareketlerAsync(filter, "Gider", Ct)).Items).Id);
            }
            Assert.Equal(expense.Id, Assert.Single((await finans.Panel.HareketlerAsync(new(Arama: "K15-ESKI"), "Gider", Ct)).Items).Id);
            Assert.Equal(expense.Id, Assert.Single((await finans.Panel.HareketlerAsync(new(Firma: "K15-FIRMA"), "Gider", Ct)).Items).Id);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task CokProjePo_7000_3000Fatura_SarfAcikTamamlanmaz_PoZorunlu_TarihYaslandirmayiDegistirmez()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var finans = Finans(db);
            await FinansPostgresTests.PoOnayiniKapatAsync(db);
            var main = Work("Ana iş", 10000); main.ProjeNo = "K12-A"; main.IsTuru = FinansIsTuru.AnaAmbalaj;
            var sarf = Work("Ayrı sarf", 500); sarf.ProjeNo = main.ProjeNo; sarf.IsTuru = FinansIsTuru.SarfKereste;
            sarf.KaynakBileseni = "SARF"; sarf.KayitTarihi = new(2026, 8, 1);
            // Liste dönemi dışındaki sarf da proje tamamlanmasının parçasıdır.
            sarf.FinansTarihi = new(2026, 10, 1); sarf.FinansDonemi = new(2026, 10, 1);
            var other = Work("Diğer proje", 2000); other.ProjeNo = "K22-B";
            db.AddRange(main, sarf, other); await db.SaveChangesAsync();

            var missing = await Assert.ThrowsAsync<IsKuraliException>(() =>
                finans.SiparisFatura.FaturaOlusturAsync(Invoice("NO-PO", 999999, 999999, 1), Ct));
            Assert.Contains("Sipariş bulunamadı", missing.Message);
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());
            var cancelled = await finans.SiparisFatura.SiparisOlusturAsync(Po("CANCEL-PO", sarf.Id, 500), Ct);
            await finans.SiparisFatura.SiparisIptalAsync(cancelled.Id, "İptal edilmiş PO kontrolü", Ct);
            var inactive = await Assert.ThrowsAsync<IsKuraliException>(() =>
                finans.SiparisFatura.FaturaOlusturAsync(Invoice("CANCEL-INV", cancelled.Id, cancelled.Kalemler[0].Id, 500), Ct));
            Assert.Contains("İptal edilmiş sipariş", inactive.Message);
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());

            var po = await finans.SiparisFatura.SiparisOlusturAsync(new("MULTI-12000", Date, "K22",
                [new(main.Id, 0, 0, NetTutar: 10000), new(other.Id, 0, 0, NetTutar: 2000)], "EUR", 12000), Ct);
            var mainLine = Assert.Single(po.Kalemler, x => x.IsKaydiId == main.Id);
            var otherLine = Assert.Single(po.Kalemler, x => x.IsKaydiId == other.Id);
            var first = await finans.SiparisFatura.FaturaOlusturAsync(new(po.Id, "MULTI-INV-1", Date, "K21/K22",
                [new(mainLine.Id, 0, 0, 7000), new(otherLine.Id, 0, 0, 1000)], "EUR", 8000, 1600, 9600), Ct);
            Assert.Equal(2, first.Kalemler.Count);
            var partial = (await finans.IsKaydi.IsKaydiGetirAsync(main.Id, Ct))!;
            Assert.Equal(7000m, partial.FaturalananNetTutar);
            Assert.Equal(3000m, partial.KalanFaturaNetTutar);
            Assert.Equal(FinansIsDurumu.KismiFaturalandi, partial.Durum);

            var filter = new FinansListeFiltre(ProjeNo: main.ProjeNo);
            var beforeAge = await finans.Panel.YaslandirmaAsync(new(2026, 10, 19), 0, null, filter, Ct);
            Assert.Equal(30, Assert.Single(beforeAge.Items, x => x.IsKaydiId == main.Id).Gun);
            Assert.Equal(79, Assert.Single(beforeAge.Items, x => x.IsKaydiId == sarf.Id).Gun);
            await finans.IsKaydi.FinansTarihiDegistirAsync(main.Id, new(new(2026, 11, 10), "Finans dönemi revizyonu"), Ct);
            await finans.IsKaydi.FinansTarihiDegistirAsync(sarf.Id, new(new(2026, 11, 11), "Sarf dönemi revizyonu"), Ct);
            var afterAge = await finans.Panel.YaslandirmaAsync(new(2026, 10, 19), 0, null, filter, Ct);
            Assert.Equal(beforeAge.Items, afterAge.Items);
            var moved = (await finans.IsKaydi.IsKaydiGetirAsync(main.Id, Ct))!;
            Assert.Equal(Date, moved.UretimTarihi);
            Assert.Equal(partial.BirimFiyat, moved.BirimFiyat);
            Assert.Equal(partial.NetTutar, moved.NetTutar);

            var second = await finans.SiparisFatura.FaturaOlusturAsync(new(po.Id, "MULTI-INV-2", Date, "K22 ikinci fatura",
                [new(mainLine.Id, 0, 0, 3000), new(otherLine.Id, 0, 0, 1000)], "EUR", 4000, 800, 4800), Ct);
            Assert.Equal(2, second.Kalemler.Count);
            Assert.Equal(12000m, await db.Set<FinansFaturaKalemi>().SumAsync(x => x.NetTutarSnapshot));
            Assert.Equal(FinansSiparisDurumu.Faturalandi, (await finans.SiparisFatura.SiparisGetirAsync(po.Id, Ct))!.Durum);
            Assert.Equal(FinansIsDurumu.Faturalandi, (await finans.IsKaydi.IsKaydiGetirAsync(main.Id, Ct))!.Durum);
            Assert.Equal(FinansIsDurumu.SiparisBekliyor, (await finans.IsKaydi.IsKaydiGetirAsync(sarf.Id, Ct))!.Durum);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task IkiWorkerEszamanliCatchup_TarifeTarihSnapshotlariVeDortFiyatYontemi()
    {
        await InDatabase(async options =>
        {
            int productId;
            await using (var seed = new AppDbContext(options))
            {
                var product = new FinansUrun { Kod = "K26-TARIFE", Ad = "Dönem fiyatı", Aktif = true, FiyatlandirmaBirimi = FinansFiyatlandirmaBirimi.Adet };
                seed.Add(product); await seed.SaveChangesAsync(); productId = product.Id;
                seed.AddRange(
                    new FinansFiyatTarifesi { FinansUrunId = productId, Yil = 2026, GecerlilikBaslangici = new(2026, 1, 1), GecerlilikBitisi = new(2026, 3, 31), BirimFiyat = 100, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true },
                    new FinansFiyatTarifesi { FinansUrunId = productId, Yil = 2026, GecerlilikBaslangici = new(2026, 4, 1), GecerlilikBitisi = new(2026, 12, 31), BirimFiyat = 200, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true },
                    new FinansDuzenliIs { IsAdi = "İki worker", IsTuru = FinansIsTuru.OzelIs, OzelIsTuru = "Düzenli", Musteri = "QA",
                        BaslangicTarihi = new(2026, 1, 1), OlusturmaGunu = 31, Miktar = 2, Birim = "Adet", FinansUrunId = productId,
                        BirimFiyat = 9999, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true, HesaplamaYontemi = FinansHesaplamaYontemi.DegiskenAdet });
                await seed.SaveChangesAsync();
            }
            var ready = 0;
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<int> Worker()
            {
                await using var db = new AppDbContext(options);
                await db.Database.OpenConnectionAsync(Ct);
                if (Interlocked.Increment(ref ready) == 2) gate.SetResult();
                await gate.Task;
                var sonuc = await Finans(db).DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct);
                if (sonuc.Hatalar.Count == 0) return sonuc.Olusturulan;
                // Serializable/unique yarışını kaybeden şablon hata olarak raporlanır; sonraki tur tamamlar.
                Assert.All(sonuc.Hatalar, hata => Assert.True(hata.Contains("değiştirildi") || hata.Contains("benzersiz"), hata));
                return sonuc.Olusturulan + (await Finans(db).DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct)).Olusturulan;
            }
            Assert.Equal(5, (await Task.WhenAll(Worker(), Worker())).Sum());
            await using var verify = new AppDbContext(options); var finans = Finans(verify);
            var generated = await verify.Set<FinansIsKaydi>().AsNoTracking().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new[] { new DateTime(2026, 1, 31), new(2026, 2, 28), new(2026, 3, 31), new(2026, 4, 30), new(2026, 5, 31) }, generated.Select(x => x.FinansTarihi));
            Assert.Equal(5, generated.Select(x => x.KaynakKayitId).Distinct().Count());
            Assert.Equal(new decimal[] { 100, 100, 100, 200, 200 }, generated.Select(x => x.BirimFiyatSnapshot));
            Assert.All(generated, item => Assert.NotNull(item.TarifeIdSnapshot));
            var tariff = await verify.Set<FinansFiyatTarifesi>().SingleAsync(x => x.FinansUrunId == productId && x.BirimFiyat == 200);
            tariff.BirimFiyat = 300; await verify.SaveChangesAsync();
            Assert.Equal(0, (await finans.DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct)).Olusturulan);
            Assert.Equal(1, (await finans.DuzenliIs.DuzenliIsDonemiOlusturAsync(new(2026, 6, 30), Ct)).Olusturulan);
            var after = await verify.Set<FinansIsKaydi>().AsNoTracking().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new decimal[] { 100, 100, 100, 200, 200, 300 }, after.Select(x => x.BirimFiyatSnapshot));
            Assert.Equal(600m, (await finans.IsKaydi.IsKaydiGetirAsync(after[^1].Id, Ct))!.NetTutar);

            foreach (var (method, expected) in new[] { (FinansFiyatlandirmaBirimi.Adet, 200m), (FinansFiyatlandirmaBirimi.Metrekup, 250m),
                (FinansFiyatlandirmaBirimi.SabitTutar, 100m), (FinansFiyatlandirmaBirimi.ManuelToplam, 1200m) })
            {
                var work = Work($"Yöntem {method}", 1); verify.Add(work); await verify.SaveChangesAsync();
                var priced = (await finans.IsKaydi.FiyatlandirAsync(work.Id, new(method, 2, 1.25m, 100, "EUR", 20, "Yöntem doğrulaması",
                    ManuelNetTutar: method == FinansFiyatlandirmaBirimi.ManuelToplam ? expected : null), Ct))!;
                Assert.Equal(expected, priced.NetTutar);
                Assert.Equal(expected * 1.2m, priced.ToplamTutar);
                var po = await finans.SiparisFatura.SiparisOlusturAsync(Po($"METHOD-{method}", work.Id, expected / 2), Ct);
                Assert.Equal(expected / 2, Assert.Single(po.Kalemler).NetTutar);
                Assert.Equal(0m, po.Kalemler[0].Adet); Assert.Equal(0m, po.Kalemler[0].M3);
                Assert.Equal(expected / 2, (await finans.IsKaydi.IsKaydiGetirAsync(work.Id, Ct))!.KalanSiparisNetTutar);
            }
        });
    }

    private static FinansGiderKaydetModel Expense(int category, decimal amount, bool advance, int? linked = null) => new(Date, new(2026, 9, 1), category, null, null, "QA Kişi", "Sentetik gider", 1, "Adet", amount, "EUR", false, 0, null, "QA", null, "BELGE-QA", advance, linked);
    private static FinansSiparisOlusturModel Po(string number, int work, decimal amount) => new(number, Date, "QA", [new(work, 0, 0, NetTutar: amount)], "EUR", amount);
    private static FinansFaturaOlusturModel Invoice(string number, int po, int line, decimal amount) => new(po, number, Date, "QA", [new(line, 0, 0, amount)], "EUR", amount, amount * .2m, amount * 1.2m);
    private static FinansTestServisleri Finans(AppDbContext db) => new(db, new User());
    private sealed class User : ICurrentUserService { public int? UserId => 7; public bool IsAuthenticated => true; }
    private static FinansIsKaydi Work(string name, decimal amount) => new() { IsAdi = name, ProjeNo = "QA", Musteri = "QA", IsTuru = FinansIsTuru.OzelIs, KaynakTuru = "Manuel", KaynakAktif = true, Adet = 1, Birim = "Adet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar, BirimFiyatSnapshot = amount, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, UretimTarihi = Date, FinansTarihi = Date, FinansDonemi = new(2026, 9, 1), KayitTarihi = Date };

    /// <summary>
    /// Onay Merkezi'nde PO öncesi iş onayını kapatır. PO dağıtımını, faturayı veya fiyatlamayı sınayan testler onayı konu
    /// almaz; kural kaydı yoksa onay açık sayılır. Onay akışı <see cref="FinansPoOnayPostgresTests"/> sınıfındadır.
    /// </summary>
    internal static async Task PoOnayiniKapatAsync(AppDbContext db)
    {
        db.Set<OnayOperasyonKurali>().Add(new OnayOperasyonKurali { IslemKodu = _3K.Core.Constants.OnayIslemKodlari.FinansPoIsOnayi, OnayGerektirirMi = false });
        await db.SaveChangesAsync();
    }

    internal static async Task InDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var cs = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(cs.Host is "127.0.0.1" or "localhost"); Assert.Equal(55439, cs.Port); Assert.Equal("postgres", cs.Database);
        var name = $"finans_test_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(cs.ConnectionString); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            cs.Database = name;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(cs.ConnectionString).Options;
            await using (var schema = new AppDbContext(options)) await schema.Database.EnsureCreatedAsync();
            await test(options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", admin); await drop.ExecuteNonQueryAsync();
        }
    }
}
