using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public sealed class FinansV2PostgresTests
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
            var service = Service(db);
            var po = await service.SiparisOlusturAsync(Po("PO-6", work.Id, 6000), Ct);
            Assert.Equal(0m, Assert.Single(po.Kalemler).Adet);
            Assert.True(po.Kalemler[0].TutarBazli);
            Assert.Equal(4000, (await service.IsKaydiGetirAsync(work.Id, Ct))!.KalanSiparisNetTutar);
            var beforeAudits = await db.Set<FinansDegisiklikGecmisi>().CountAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.FaturaOlusturAsync(Invoice("INV-BAD", po.Id, po.Kalemler[0].Id, 6001), Ct));
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());
            Assert.Equal(beforeAudits, await db.Set<FinansDegisiklikGecmisi>().CountAsync());
            await service.FaturaOlusturAsync(Invoice("INV-6", po.Id, po.Kalemler[0].Id, 6000), Ct);
            var partial = (await service.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(FinansIsDurumu.KismiFaturalandi, partial.Durum);
            var second = await service.SiparisOlusturAsync(Po("PO-4", work.Id, 4000), Ct);
            await service.FaturaOlusturAsync(Invoice("INV-4", second.Id, second.Kalemler[0].Id, 4000), Ct);
            Assert.Equal(FinansIsDurumu.Faturalandi, (await service.IsKaydiGetirAsync(work.Id, Ct))!.Durum);
            var category = new FinansGiderKategori { Ad = "Ücret", Aktif = true }; db.Add(category); await db.SaveChangesAsync();
            var advance = await service.GiderOlusturAsync(Expense(category.Id, 1000, true), Ct);
            var expense = await service.GiderOlusturAsync(Expense(category.Id, 2500, false, advance.Id), Ct);
            Assert.Equal(Date, expense.FinansTarihi);
            Assert.Equal("BELGE-QA", expense.BelgeNo);
            var panel = await service.PanelAsync(new(2026, 9, 15), new(2026, 9, 30), Ct);
            var euro = Assert.Single(panel.Tutarlar, x => x.ParaBirimi == "EUR");
            Assert.Equal(10000m, euro.Gelir); Assert.Equal(2500m, euro.Gider); Assert.Equal(7500m, euro.TahminiKar);
            var listed = await service.IsKayitlariAsync(new() { PageSize = 1 }, Ct);
            Assert.Equal(10000, Assert.Single(listed.Toplamlar).NetTutar);
            Assert.Equal(10000, Assert.Single((await service.SiparislerAsync(new() { PageSize = 1 }, Ct)).Toplamlar).NetTutar);
            Assert.Equal(10000, Assert.Single((await service.FaturalarAsync(new() { PageSize = 1 }, Ct)).Toplamlar).NetTutar);
            var dashboard = await service.DashboardAsync(new(2026, 9, 15), new(2026, 9, 30), Ct);
            Assert.Equal(10000, Assert.Single(dashboard.Gelirler).NetTutar);
            var reports = new FinansRaporService(service);
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
            var movement = await service.HareketlerAsync(new() { Baslangic = new(2026, 9, 15), Bitis = new(2026, 9, 30), PageSize = 1 }, "IsKaydi", Ct);
            Assert.Equal(1, movement.TotalCount); Assert.Equal("IsKaydi", Assert.Single(movement.Items).Tur);
            Assert.False((await service.KaliciSilOnizlemeAsync("IsKaydi", work.Id, Ct))!.Silinebilir);
            var audit = await service.DegisiklikGecmisiAsync("IsKaydi", work.Id, 1, 250, Ct);
            Assert.NotEmpty(audit.Items);
            await service.FinansTarihiDegistirAsync(work.Id, new(new(2026, 10, 3), "Dönem düzeltmesi"), Ct);
            Assert.Equal(10000m, (await service.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
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
                try { await Service(concurrent).SiparisOlusturAsync(Po(number, id, 6000), Ct); return true; }
                catch (InvalidOperationException) { return false; }
            }
            var results = await Task.WhenAll(Attempt("RACE-A"), Attempt("RACE-B"));
            Assert.Single(results, x => x);
            await using var verify = new AppDbContext(options);
            Assert.Equal(6000m, await verify.Set<FinansSiparisKalemi>().SumAsync(x => x.NetTutarSnapshot));
            var service = Service(verify);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.BelgeYukleAsync(new("Siparis", 99999, "test.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF")), Ct));
            Assert.Empty(await verify.Set<FinansBelge>().ToArrayAsync());
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task KaynakNetSarf_ManuelFinansSahipligi_SilmeBastirmasi_Duzenli31Catchup()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var service = Service(db);
            var source = new FinansUretimAktarimModel("AMBALAJURETIM", "qa-source", true, null, "QA", "Sentetik", FinansIsTuru.AnaAmbalaj, "Kasa", 2, 1.25m, Date, Date, SarfM3: .6m);
            await service.UretimKayitlariniAktarAsync([source], Ct);
            await service.UretimKayitlariniAktarAsync([source], Ct);
            var jobs = await db.Set<FinansIsKaydi>().AsNoTracking().OrderBy(x => x.KaynakBileseni).ToArrayAsync();
            Assert.Equal(2, jobs.Length); Assert.Equal(2.5m, jobs[0].ToplamM3); Assert.Equal(.6m, jobs[1].ToplamM3);
            await service.FiyatlandirAsync(jobs[0].Id, new(FinansFiyatlandirmaBirimi.Metrekup, 2, 2, 120, "EUR", 20, "Manuel finans ölçüsü"), Ct);
            await service.FinansTarihiDegistirAsync(jobs[0].Id, new(new(2026, 10, 3), "Manuel finans tarihi"), Ct);
            await service.UretimKayitlariniAktarAsync([source with { Adet = 5, BirimM3 = 9, UretimM3Hesaplanabilir = false, SarfM3 = 0 }], Ct);
            var preserved = (await service.IsKaydiGetirAsync(jobs[0].Id, Ct))!;
            Assert.Equal(4m, preserved.ToplamM3); Assert.Equal(480m, preserved.NetTutar); Assert.Equal(new DateTime(2026, 10, 3), preserved.FinansTarihi);
            var preview = (await service.KaliciSilOnizlemeAsync("IsKaydi", jobs[0].Id, Ct))!;
            Assert.True(preview.Silinebilir);
            await service.FinansTarihiDegistirAsync(jobs[0].Id, new(new(2026, 10, 4), "İkinci tarih düzenlemesi"), Ct);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, true, "Eski sürüm ile silme"), Ct));
            preview = (await service.KaliciSilOnizlemeAsync("IsKaydi", jobs[0].Id, Ct))!;
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, false, "Deneme silme gerekçesi"), Ct));
            await service.KaliciSilAsync(new("IsKaydi", jobs[0].Id, preview.Surum, true, "Deneme silme gerekçesi"), Ct);
            db.ChangeTracker.Clear();
            await service.UretimKayitlariniAktarAsync([source], Ct);
            Assert.Equal(1, await db.Set<FinansIsKaydi>().CountAsync());
            Assert.Single(await db.Set<FinansKaynakBastirma>().ToArrayAsync());
            db.Add(new FinansDuzenliIs { IsAdi = "Aylık", IsTuru = FinansIsTuru.OzelIs, OzelIsTuru = "Aylık", Musteri = "QA", BaslangicTarihi = new(2026, 1, 1), OlusturmaGunu = 31, Miktar = 1, BirimFiyat = 500, ParaBirimi = "EUR", KdvOrani = 20, Aktif = true, HesaplamaYontemi = FinansHesaplamaYontemi.SabitAylik });
            await db.SaveChangesAsync();
            Assert.Equal(3, (await service.DuzenliIsDonemiOlusturAsync(new(2026, 3, 31), Ct)).Olusturulan);
            Assert.Equal(0, (await service.DuzenliIsDonemiOlusturAsync(new(2026, 3, 31), Ct)).Olusturulan);
            var dates = await db.Set<FinansIsKaydi>().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).Select(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new[] { new DateTime(2026, 1, 31), new(2026, 2, 28), new(2026, 3, 31) }, dates);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task SablonSurumu_Bilesenler_BelgeSurumu_ve_MigrationIdempotency()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var service = Service(db);
            var work = Work("Şablon", 10000); db.Add(work); await db.SaveChangesAsync();
            var template = await service.SablonKaydetAsync(null, new("QA", "Test", true, [new("uzunluk", "Uzunluk", "sayi", true)]), Ct);
            var price = new FinansFiyatlandirmaModel(FinansFiyatlandirmaBirimi.ManuelToplam, 1, 0, 0, "EUR", 20, "Bileşenli hesaplama", SablonSurumId: template.SurumId, AlanDegerleri: new Dictionary<string, string?> { ["uzunluk"] = "2.5" },
                Bilesenler: [new("Ahşap hacmi", FinansFiyatlandirmaBirimi.Metrekup, 1.2m, 500), new("Sarf hacmi", FinansFiyatlandirmaBirimi.Metrekup, .5m, 800), new("Sabit", FinansFiyatlandirmaBirimi.SabitTutar, 1, 200)]);
            var beforeAudit = await db.Set<FinansDegisiklikGecmisi>().CountAsync();
            var missingField = await Assert.ThrowsAsync<InvalidOperationException>(() => service.FiyatlandirAsync(work.Id,
                price with { AlanDegerleri = new Dictionary<string, string?>() }, Ct));
            Assert.Contains("zorunludur", missingField.Message);
            db.ChangeTracker.Clear();
            Assert.Equal(10000m, (await service.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
            Assert.Null((await service.IsKaydiGetirAsync(work.Id, Ct))!.SablonSurumId);
            Assert.Equal(beforeAudit, await db.Set<FinansDegisiklikGecmisi>().CountAsync());
            Assert.Equal(1200m, (await service.FiyatlandirAsync(work.Id, price, Ct))!.NetTutar);
            var updated = await service.SablonKaydetAsync(template.Id, new("QA", "Test v2", true, [new("yeni", "Yeni alan", "metin", false)]), Ct);
            Assert.Equal(2, updated.Surum);
            var detail = (await service.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(template.SurumId, detail.SablonSurumId);
            Assert.Equal(1200m, detail.ManuelNetTutar);
            Assert.Equal("2.5", detail.AlanDegerleri!["uzunluk"]);
            Assert.Equal(price.Bilesenler, detail.Bilesenler);
            Assert.NotNull(detail.Sablon);
            Assert.Equal(template.SurumId, detail.Sablon.SurumId);
            Assert.Equal(1, detail.Sablon.Surum);
            Assert.Equal("uzunluk", Assert.Single(detail.Sablon.Alanlar).Kod);
            // Detaydan yeniden açılan aynı form eski sürüm/bileşenleri kaybetmeden kaydedilir.
            await service.FiyatlandirAsync(work.Id, price with { SablonSurumId = detail.SablonSurumId,
                AlanDegerleri = detail.AlanDegerleri, Bilesenler = detail.Bilesenler, ManuelNetTutar = detail.ManuelNetTutar }, Ct);
            Assert.Equal(1200m, (await service.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
            var po = await service.SiparisOlusturAsync(Po("DOC", work.Id, 1200), Ct);
            var pdf = await new FinansRaporService(service).OzetRaporAsync("genel", false, new(), Ct);
            var first = await service.BelgeYukleAsync(new("Siparis", po.Id, "../../test.pdf", pdf), Ct);
            var second = await service.BelgeYukleAsync(new("Siparis", po.Id, "test.pdf", pdf), Ct);
            Assert.Equal(1, first.Surum); Assert.Equal(2, second.Surum); Assert.DoesNotContain("/", first.OrijinalAd);
            Assert.Equal(pdf, (await service.BelgeIndirAsync(first.Id, Ct))!.Icerik);
            await service.FaturaOlusturAsync(Invoice("MIGRATE", po.Id, po.Kalemler[0].Id, 1200), Ct);
            var root = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(root, "3K_Proje.slnx"))) root = Directory.GetParent(root)!.FullName;
            var sql = await File.ReadAllTextAsync(Path.Combine(root, "scripts/database/20260919_03_Finans_V2.sql"));
            await db.Database.ExecuteSqlRawAsync(sql); await db.Database.ExecuteSqlRawAsync(sql);
            Assert.Equal(1200, (await service.IsKaydiGetirAsync(work.Id, Ct))!.NetTutar);
        });
    }

    [Theory]
    [InlineData(14, "0–14")][InlineData(15, "15–29")][InlineData(29, "15–29")][InlineData(30, "30–59")][InlineData(60, "60–89")][InlineData(90, "90+")]
    public void YaslandirmaAraliklari_Cakismiyor(int day, string bucket) => Assert.Equal(bucket, FinansService.YasGrubu(day));

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task CokProjePo_MixedCurrencyRollback_PdfAuditFailOrphanYok()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var service = Service(db);
            var a = Work("Proje A", 10000); a.ProjeNo = "A";
            var b = Work("Proje B", 1500); b.ProjeNo = "B";
            var usd = Work("USD", 2000); usd.ParaBirimiSnapshot = "USD";
            db.AddRange(a, b, usd); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SiparisOlusturAsync(new("MIXED", Date, "QA",
                [new(a.Id, 0, 0, NetTutar: 500), new(usd.Id, 0, 0, NetTutar: 500)]), Ct));
            db.ChangeTracker.Clear(); Assert.Empty(await db.Set<FinansSiparis>().ToArrayAsync());
            var po = await service.SiparisOlusturAsync(new("MULTI", Date, "QA", [new(a.Id, 0, 0, NetTutar: 1000), new(b.Id, 0, 0, NetTutar: 500)], "EUR", 1500), Ct);
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
            var deletePreview = (await service.KaliciSilOnizlemeAsync("IsKaydi", a.Id, Ct))!;
            Assert.False(deletePreview.Silinebilir);
            Assert.NotEmpty(deletePreview.Bagimliliklar);
            var blockedDelete = await Assert.ThrowsAsync<InvalidOperationException>(() => service.KaliciSilAsync(
                new("IsKaydi", a.Id, deletePreview.Surum, true, "Bağlı iş için zorlanmış silme denemesi"), Ct));
            Assert.Contains("Bağlı kayıtlar bulundu", blockedDelete.Message);
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
            Assert.Equal(1500m, (await service.SiparisGetirAsync(po.Id, Ct))!.Tutarlar.Single().NetTutar);
            var pdf = await new FinansRaporService(service).OzetRaporAsync("genel", false, new(), Ct);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION qa_reject_pdf_audit() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN IF NEW."Islem" = 'PDF Yükleme' THEN RAISE EXCEPTION 'QA sentetik audit hatası'; END IF; RETURN NEW; END $$;
                CREATE TRIGGER qa_reject_pdf BEFORE INSERT ON "FinansDegisiklikGecmisleri" FOR EACH ROW EXECUTE FUNCTION qa_reject_pdf_audit();
                """);
            await Assert.ThrowsAsync<DbUpdateException>(() => service.BelgeYukleAsync(new("Siparis", po.Id, "gercek.pdf", pdf), Ct));
            db.ChangeTracker.Clear(); Assert.Empty(await db.Set<FinansBelge>().ToArrayAsync());
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task TutarRevizyonu_GerceklestirilenTutarinAltinaInemez_FaturaAsimiRollback()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var service = Service(db);
            var work = Work("Düzeltme", 10000); db.Add(work); await db.SaveChangesAsync();
            var po = await service.SiparisOlusturAsync(Po("REV-PO", work.Id, 6000), Ct);
            var invoice = await service.FaturaOlusturAsync(Invoice("REV-INV", po.Id, po.Kalemler[0].Id, 3000), Ct);
            Assert.Single(invoice.Kalemler);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SiparisGuncelleAsync(po.Id,
                new(po.PoNumarasi, Date, "QA", [new(work.Id, 0, 0, NetTutar: 2999)], "Geçersiz azaltma"), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal(6000, (await service.SiparisGetirAsync(po.Id, Ct))!.Tutarlar[0].NetTutar);
            await service.SiparisGuncelleAsync(po.Id, new(po.PoNumarasi, Date, "QA", [new(work.Id, 0, 0, NetTutar: 7000)], "PO tutar düzeltmesi"), Ct);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.FaturaGuncelleAsync(invoice.Id,
                new(invoice.FaturaNumarasi, Date, "QA", "EUR", 7001, 1400.2m, 8401.2m, Kalemler: [new(po.Kalemler[0].Id, 0, 0, 7001)], Gerekce: "Geçersiz artış"), Ct));
            db.ChangeTracker.Clear();
            Assert.Equal(3000, (await service.FaturaGetirAsync(invoice.Id, Ct))!.Kalemler[0].NetTutar);
            await service.FaturaGuncelleAsync(invoice.Id,
                new(invoice.FaturaNumarasi, Date, "QA", "EUR", 7000, 1400, 8400, Kalemler: [new(po.Kalemler[0].Id, 0, 0, 7000)], Gerekce: "Fatura tutar düzeltmesi"), Ct);
            var result = (await service.IsKaydiGetirAsync(work.Id, Ct))!;
            Assert.Equal(7000, result.FaturalananNetTutar); Assert.Equal(3000, result.KalanSiparisNetTutar); Assert.Equal(FinansIsDurumu.KismiFaturalandi, result.Durum);
            const string cancellationReason = "K34 yanlış fatura tutarı nedeniyle iptal";
            var beforeCancel = _3K.Core.Helpers.TurkeyTime.Now.AddSeconds(-1);
            Assert.True(await service.FaturaIptalAsync(invoice.Id, cancellationReason, Ct));
            var afterCancel = _3K.Core.Helpers.TurkeyTime.Now.AddSeconds(1);
            db.ChangeTracker.Clear();
            var restored = (await service.IsKaydiGetirAsync(work.Id, Ct))!;
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
            await using var db = new AppDbContext(options); var service = Service(db);
            var old = Work("Geçmiş dönem işi", 10000); old.ProjeNo = "K15-PROJE-GECMIS";
            old.UretimTarihi = new(2025, 12, 19); old.FinansTarihi = old.UretimTarihi;
            old.FinansDonemi = new(2025, 12, 1); old.KayitTarihi = old.UretimTarihi;
            var current = Work("Seçili ayın işi", 4000); current.ProjeNo = "K15-SIMDI";
            db.AddRange(old, current); await db.SaveChangesAsync();
            var po = await service.SiparisOlusturAsync(Po("K15-PO-ESKI", old.Id, 6000) with { SiparisTarihi = new(2025, 12, 20) }, Ct);
            await service.FaturaOlusturAsync(Invoice("K15-FATURA-ESKI", po.Id, po.Kalemler[0].Id, 3000) with { FaturaTarihi = new(2025, 12, 21) }, Ct);
            var selectedMonth = new FinansListeFiltre(Baslangic: new(2026, 9, 1), Bitis: new(2026, 9, 30), PageSize: 1);
            foreach (var term in new[] { old.ProjeNo, "K15-PO-ESKI", "K15-FATURA-ESKI", "k15-proje-gecmis", "k15-po-eski", "k15-fatura-eski" })
            {
                var filter = selectedMonth with { Arama = term };
                Assert.Empty((await service.IsKayitlariAsync(filter, Ct)).Items);
                var global = await service.GenelAramaAsync(filter, Ct);
                Assert.True(global.TotalCount == 1, $"Genel arama terimi '{term}' seçili ay dışında da bulunmalıdır; kültür={System.Globalization.CultureInfo.CurrentCulture.Name}.");
                var found = Assert.Single(global.Items);
                Assert.Equal(old.Id, found.Id);
                Assert.Equal(new DateTime(2025, 12, 19), found.FinansTarihi);
                Assert.Contains("K15-PO-ESKI", found.PoNumaralari);
                Assert.Contains("K15-FATURA-ESKI", found.FaturaNumaralari);
                Assert.Equal(10000m, Assert.Single(global.Toplamlar).NetTutar);
                Assert.Empty((await service.GenelAramaAsync(filter with { ParaBirimi = "USD" }, Ct)).Items);
            }
            var specific = await service.GenelAramaAsync(selectedMonth with { ProjeNo = old.ProjeNo, PoNumarasi = "K15-PO-ESKI", FaturaNumarasi = "K15-FATURA-ESKI" }, Ct);
            Assert.Equal(old.Id, Assert.Single(specific.Items).Id);

            var literal = Work("K15-LITERAL %_\\ ISI", 1000);
            literal.Musteri = "K15-FIRMA %_\\"; literal.TalepEdenKisi = "K15-KISI %_\\";
            var category = new FinansGiderKategori { Ad = "K15 gider", Aktif = true };
            db.AddRange(literal, category); await db.SaveChangesAsync();
            var literalPo = await service.SiparisOlusturAsync(Po("K15-PO-%_\\", literal.Id, 100), Ct);
            await service.FaturaOlusturAsync(Invoice("K15-FATURA-%_\\", literalPo.Id, literalPo.Kalemler[0].Id, 50), Ct);
            var expense = await service.GiderOlusturAsync(Expense(category.Id, 100, false) with
                { Aciklama = "K15-ESKI %_\\", FirmaVeyaKisi = literal.Musteri }, Ct);
            // Kullanıcı metnindeki LIKE jokerleri yalnız gerçek karakterleri eşleştirir.
            foreach (var term in new[] { "%", "_", "\\" })
            {
                foreach (var filter in new[] { new FinansListeFiltre(Arama: term), new(Firma: term),
                    new(TalepEden: term), new(PoNumarasi: term), new(FaturaNumarasi: term) })
                    Assert.Equal(literal.Id, Assert.Single((await service.GenelAramaAsync(filter, Ct)).Items).Id);
                foreach (var filter in new[] { new FinansListeFiltre(Arama: term), new(Firma: term) })
                    Assert.Equal(expense.Id, Assert.Single((await service.HareketlerAsync(filter, "Gider", Ct)).Items).Id);
            }
            Assert.Equal(expense.Id, Assert.Single((await service.HareketlerAsync(new(Arama: "K15-ESKI"), "Gider", Ct)).Items).Id);
            Assert.Equal(expense.Id, Assert.Single((await service.HareketlerAsync(new(Firma: "K15-FIRMA"), "Gider", Ct)).Items).Id);
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task CokProjePo_7000_3000Fatura_SarfAcikTamamlanmaz_PoZorunlu_TarihYaslandirmayiDegistirmez()
    {
        await InDatabase(async options =>
        {
            await using var db = new AppDbContext(options); var service = Service(db);
            var main = Work("Ana iş", 10000); main.ProjeNo = "K12-A"; main.IsTuru = FinansIsTuru.AnaAmbalaj;
            var sarf = Work("Ayrı sarf", 500); sarf.ProjeNo = main.ProjeNo; sarf.IsTuru = FinansIsTuru.SarfKereste;
            sarf.KaynakBileseni = "SARF"; sarf.KayitTarihi = new(2026, 8, 1);
            // Liste dönemi dışındaki sarf da proje tamamlanmasının parçasıdır.
            sarf.FinansTarihi = new(2026, 10, 1); sarf.FinansDonemi = new(2026, 10, 1);
            var other = Work("Diğer proje", 2000); other.ProjeNo = "K22-B";
            db.AddRange(main, sarf, other); await db.SaveChangesAsync();

            var missing = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.FaturaOlusturAsync(Invoice("NO-PO", 999999, 999999, 1), Ct));
            Assert.Contains("Sipariş bulunamadı", missing.Message);
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());
            var cancelled = await service.SiparisOlusturAsync(Po("CANCEL-PO", sarf.Id, 500), Ct);
            await service.SiparisIptalAsync(cancelled.Id, "İptal edilmiş PO kontrolü", Ct);
            var inactive = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.FaturaOlusturAsync(Invoice("CANCEL-INV", cancelled.Id, cancelled.Kalemler[0].Id, 500), Ct));
            Assert.Contains("İptal edilmiş sipariş", inactive.Message);
            db.ChangeTracker.Clear();
            Assert.Empty(await db.Set<FinansFatura>().ToArrayAsync());

            var po = await service.SiparisOlusturAsync(new("MULTI-12000", Date, "K22",
                [new(main.Id, 0, 0, NetTutar: 10000), new(other.Id, 0, 0, NetTutar: 2000)], "EUR", 12000), Ct);
            var mainLine = Assert.Single(po.Kalemler, x => x.IsKaydiId == main.Id);
            var otherLine = Assert.Single(po.Kalemler, x => x.IsKaydiId == other.Id);
            var first = await service.FaturaOlusturAsync(new(po.Id, "MULTI-INV-1", Date, "K21/K22",
                [new(mainLine.Id, 0, 0, 7000), new(otherLine.Id, 0, 0, 1000)], "EUR", 8000, 1600, 9600), Ct);
            Assert.Equal(2, first.Kalemler.Count);
            var partial = (await service.IsKaydiGetirAsync(main.Id, Ct))!;
            Assert.Equal(7000m, partial.FaturalananNetTutar);
            Assert.Equal(3000m, partial.KalanFaturaNetTutar);
            Assert.Equal(FinansIsDurumu.KismiFaturalandi, partial.Durum);

            var filter = new FinansListeFiltre(ProjeNo: main.ProjeNo);
            var beforeAge = await service.YaslandirmaAsync(new(2026, 10, 19), 0, filter, Ct);
            Assert.Equal(30, Assert.Single(beforeAge.Items, x => x.IsKaydiId == main.Id).Gun);
            Assert.Equal(79, Assert.Single(beforeAge.Items, x => x.IsKaydiId == sarf.Id).Gun);
            await service.FinansTarihiDegistirAsync(main.Id, new(new(2026, 11, 10), "Finans dönemi revizyonu"), Ct);
            await service.FinansTarihiDegistirAsync(sarf.Id, new(new(2026, 11, 11), "Sarf dönemi revizyonu"), Ct);
            var afterAge = await service.YaslandirmaAsync(new(2026, 10, 19), 0, filter, Ct);
            Assert.Equal(beforeAge.Items, afterAge.Items);
            var moved = (await service.IsKaydiGetirAsync(main.Id, Ct))!;
            Assert.Equal(Date, moved.UretimTarihi);
            Assert.Equal(partial.BirimFiyat, moved.BirimFiyat);
            Assert.Equal(partial.NetTutar, moved.NetTutar);

            var second = await service.FaturaOlusturAsync(new(po.Id, "MULTI-INV-2", Date, "K22 ikinci fatura",
                [new(mainLine.Id, 0, 0, 3000), new(otherLine.Id, 0, 0, 1000)], "EUR", 4000, 800, 4800), Ct);
            Assert.Equal(2, second.Kalemler.Count);
            Assert.Equal(12000m, await db.Set<FinansFaturaKalemi>().SumAsync(x => x.NetTutarSnapshot));
            Assert.Equal(FinansSiparisDurumu.Faturalandi, (await service.SiparisGetirAsync(po.Id, Ct))!.Durum);
            Assert.Equal(FinansIsDurumu.Faturalandi, (await service.IsKaydiGetirAsync(main.Id, Ct))!.Durum);
            Assert.Equal(FinansIsDurumu.SiparisBekliyor, (await service.IsKaydiGetirAsync(sarf.Id, Ct))!.Durum);
            // Ana iş seçen filtre, açık sarfı hesap dışına çıkarıp projeyi tamamlandı gösteremez.
            var project = Assert.Single((await service.ProjelerAsync(filter with { IsTuru = FinansIsTuru.AnaAmbalaj }, Ct)).Items);
            Assert.Equal(2, project.ToplamIsAdedi);
            Assert.Equal("Devam Ediyor", project.GenelDurum);
            Assert.Equal(10500m, Assert.Single(project.Tutarlar).NetTutar);
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
                try { return (await Service(db).DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct)).Olusturulan; }
                catch (InvalidOperationException exception)
                {
                    // Serializable/unique yarışındaki kaybeden worker sonraki turda yakalar.
                    var cause = exception.InnerException;
                    while (cause is not null && cause is not PostgresException) cause = cause.InnerException;
                    Assert.True(cause is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.UniqueViolation });
                    db.ChangeTracker.Clear();
                    return (await Service(db).DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct)).Olusturulan;
                }
            }
            Assert.Equal(5, (await Task.WhenAll(Worker(), Worker())).Sum());
            await using var verify = new AppDbContext(options); var service = Service(verify);
            var generated = await verify.Set<FinansIsKaydi>().AsNoTracking().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new[] { new DateTime(2026, 1, 31), new(2026, 2, 28), new(2026, 3, 31), new(2026, 4, 30), new(2026, 5, 31) }, generated.Select(x => x.FinansTarihi));
            Assert.Equal(5, generated.Select(x => x.KaynakKayitId).Distinct().Count());
            Assert.Equal(new decimal[] { 100, 100, 100, 200, 200 }, generated.Select(x => x.BirimFiyatSnapshot));
            Assert.All(generated, item => Assert.NotNull(item.TarifeIdSnapshot));
            var tariff = await verify.Set<FinansFiyatTarifesi>().SingleAsync(x => x.FinansUrunId == productId && x.BirimFiyat == 200);
            tariff.BirimFiyat = 300; await verify.SaveChangesAsync();
            Assert.Equal(0, (await service.DuzenliIsDonemiOlusturAsync(new(2026, 5, 31), Ct)).Olusturulan);
            Assert.Equal(1, (await service.DuzenliIsDonemiOlusturAsync(new(2026, 6, 30), Ct)).Olusturulan);
            var after = await verify.Set<FinansIsKaydi>().AsNoTracking().Where(x => x.DuzenliIsId != null).OrderBy(x => x.FinansTarihi).ToArrayAsync();
            Assert.Equal(new decimal[] { 100, 100, 100, 200, 200, 300 }, after.Select(x => x.BirimFiyatSnapshot));
            Assert.Equal(600m, (await service.IsKaydiGetirAsync(after[^1].Id, Ct))!.NetTutar);

            foreach (var (method, expected) in new[] { (FinansFiyatlandirmaBirimi.Adet, 200m), (FinansFiyatlandirmaBirimi.Metrekup, 250m),
                (FinansFiyatlandirmaBirimi.SabitTutar, 100m), (FinansFiyatlandirmaBirimi.ManuelToplam, 1200m) })
            {
                var work = Work($"Yöntem {method}", 1); verify.Add(work); await verify.SaveChangesAsync();
                var priced = (await service.FiyatlandirAsync(work.Id, new(method, 2, 1.25m, 100, "EUR", 20, "Yöntem doğrulaması",
                    ManuelNetTutar: method == FinansFiyatlandirmaBirimi.ManuelToplam ? expected : null), Ct))!;
                Assert.Equal(expected, priced.NetTutar);
                Assert.Equal(expected * 1.2m, priced.ToplamTutar);
                var po = await service.SiparisOlusturAsync(Po($"METHOD-{method}", work.Id, expected / 2), Ct);
                Assert.Equal(expected / 2, Assert.Single(po.Kalemler).NetTutar);
                Assert.Equal(0m, po.Kalemler[0].Adet); Assert.Equal(0m, po.Kalemler[0].M3);
                Assert.Equal(expected / 2, (await service.IsKaydiGetirAsync(work.Id, Ct))!.KalanSiparisNetTutar);
            }
        });
    }

    private static FinansGiderKaydetModel Expense(int category, decimal amount, bool advance, int? linked = null) => new(Date, new(2026, 9, 1), category, null, null, "QA Kişi", "Sentetik gider", 1, "Adet", amount, "EUR", false, 0, null, "QA", null, "BELGE-QA", advance, linked);
    private static FinansSiparisOlusturModel Po(string number, int work, decimal amount) => new(number, Date, "QA", [new(work, 0, 0, NetTutar: amount)], "EUR", amount);
    private static FinansFaturaOlusturModel Invoice(string number, int po, int line, decimal amount) => new(po, number, Date, "QA", [new(line, 0, 0, amount)], "EUR", amount, amount * .2m, amount * 1.2m);
    private static FinansService Service(AppDbContext db) => new(db, new User());
    private sealed class User : ICurrentUserService { public int? UserId => 7; public bool IsAuthenticated => true; public string? MenuKod => null; }
    private static FinansIsKaydi Work(string name, decimal amount) => new() { IsAdi = name, ProjeNo = "QA", Musteri = "QA", IsTuru = FinansIsTuru.OzelIs, KaynakTuru = "Manuel", KaynakAktif = true, Adet = 1, Birim = "Adet", FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.SabitTutar, BirimFiyatSnapshot = amount, ParaBirimiSnapshot = "EUR", KdvOraniSnapshot = 20, UretimTarihi = Date, FinansTarihi = Date, FinansDonemi = new(2026, 9, 1), KayitTarihi = Date };

    internal static async Task InDatabase(Func<DbContextOptions<AppDbContext>, Task> test)
    {
        var cs = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(cs.Host is "127.0.0.1" or "localhost"); Assert.Equal(55439, cs.Port); Assert.Equal("postgres", cs.Database);
        var name = $"finance_v2_test_{Guid.NewGuid():N}";
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
