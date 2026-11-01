using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Common;
using _3K.Application.Features.PdfIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services.Raporlar;

namespace _3K.Application.Tests;

/// <summary>
/// Saha/Yedek sevke hazır sandık raporu (beyanname): yalnız kapalı ve ölçü (Boy/En/Yükseklik) ile ağırlığı (Net/Brüt
/// kg) girilmiş sandıklar listelenir; başta listelenmeyen sandıkların sayısı ve nedeni yazılır. Mevcut sandık raporu
/// değişmez; izin mevcut saha-raporu / yedek-raporu okumasıdır.
/// </summary>
public sealed class SevkeHazirSandikRaporuTests
{
    private static Sandik Sandik(string no, SandikDurum durum, bool olculu = true, int id = 0) => new()
    {
        Id = id, SandikNo = no, DurumId = (int)durum,
        Boy = olculu ? 2000 : null, En = olculu ? 900 : null, Yukseklik = olculu ? 900 : null,
        NetKg = olculu ? 300 : null, GrossKg = olculu ? 400 : null
    };

    [Fact]
    public void KapaliAmaOlcusuzSandik_ListelenmezNedeniOlcuAgirlikEksik()
    {
        var sandik = Sandik("1", SandikDurum.Kapandi, olculu: false);

        Assert.Equal(SevkeHazirSandikDurumu.OlcuAgirlikEksik, SevkeHazirSandikKurali.Durum(sandik));
        Assert.Equal(["Boy", "En", "Yükseklik", "Net kg", "Brüt kg"], SevkeHazirSandikKurali.EksikAlanlar(sandik));
    }

    [Fact]
    public void OlculuAmaAcikSandik_ListelenmezNedeniKapanmadi()
    {
        Assert.Equal(SevkeHazirSandikDurumu.Kapanmadi, SevkeHazirSandikKurali.Durum(Sandik("1", SandikDurum.Hazirlaniyor)));
        Assert.Equal(SevkeHazirSandikDurumu.Kapanmadi, SevkeHazirSandikKurali.Durum(Sandik("1", SandikDurum.Bos)));
    }

    [Fact]
    public void HemKapaliHemOlcuVeAgirligiGirilmisSandik_SevkeHazirdir()
    {
        var sandik = Sandik("1", SandikDurum.Kapandi);

        Assert.Equal(SevkeHazirSandikDurumu.Hazir, SevkeHazirSandikKurali.Durum(sandik));
        Assert.Empty(SevkeHazirSandikKurali.EksikAlanlar(sandik));
    }

    /// <summary>Beyannamede ölçü ve iki ağırlık birlikte gerekir: biri boş ya da sıfırsa sandık hazır sayılmaz.</summary>
    [Theory]
    [InlineData("Boy")]
    [InlineData("En")]
    [InlineData("Yükseklik")]
    [InlineData("Net kg")]
    [InlineData("Brüt kg")]
    public void KapaliSandikta_TekBirOlcuVeyaAgirlikEksikse_HazirSayilmaz(string alan)
    {
        foreach (decimal? deger in new decimal?[] { null, 0m })
        {
            var sandik = Sandik("1", SandikDurum.Kapandi);
            switch (alan)
            {
                case "Boy": sandik.Boy = deger; break;
                case "En": sandik.En = deger; break;
                case "Yükseklik": sandik.Yukseklik = deger; break;
                case "Net kg": sandik.NetKg = deger; break;
                default: sandik.GrossKg = deger; break;
            }

            Assert.Equal(SevkeHazirSandikDurumu.OlcuAgirlikEksik, SevkeHazirSandikKurali.Durum(sandik));
            Assert.Equal([alan], SevkeHazirSandikKurali.EksikAlanlar(sandik));
        }
    }

    [Fact]
    public void SevkEdilmisSandik_YeniBeyannameyeGirmez()
    {
        Assert.Equal(SevkeHazirSandikDurumu.SevkEdildi, SevkeHazirSandikKurali.Durum(Sandik("1", SandikDurum.Sevkedildi)));
    }

    /// <summary>PA707-5010 örneği: gönderilecek 2 kapalı sandık listelenir, gelmeyen ürünlerin toplandığı açık sandık çıkmaz.</summary>
    [Fact]
    public void Ayrim_DogalSiradaHazirlariVeNedenleriyleListelenmeyenleriVerir_NotTekMetindir()
    {
        var ayrim = SevkeHazirSandikKurali.Ayir(
        [
            Sandik("10", SandikDurum.Kapandi, id: 1),
            Sandik("3", SandikDurum.Hazirlaniyor, olculu: false, id: 2),
            Sandik("2", SandikDurum.Kapandi, id: 3)
        ]);

        Assert.Equal(["2", "10"], ayrim.Hazir.Select(s => s.SandikNo));
        var disarida = Assert.Single(ayrim.Disarida);
        Assert.Equal(("3", SevkeHazirSandikDurumu.Kapanmadi), (disarida.Sandik.SandikNo, disarida.Neden));
        Assert.Equal("3 sandıktan 2 sandık sevke hazır. 1 sandık kapanmadığı için listelenmedi (No: 3).", ayrim.Not);
    }

    [Fact]
    public void Not_HerNedeniAyriSayarVeTamamiHazirsaBunuSoyler()
    {
        var karisik = SevkeHazirSandikKurali.Ayir(
        [
            Sandik("1", SandikDurum.Kapandi),
            Sandik("2", SandikDurum.Kapandi, olculu: false),
            Sandik("3", SandikDurum.Sevkedildi),
            Sandik("4", SandikDurum.Bos, olculu: false),
            Sandik("5", SandikDurum.Kapandi, olculu: false)
        ]);
        Assert.Equal(
            "5 sandıktan 1 sandık sevke hazır. 1 sandık kapanmadığı için listelenmedi (No: 4). " +
            "2 sandık ölçü/ağırlık girilmediği için listelenmedi (No: 2, 5). 1 sandık zaten sevk edildiği için listelenmedi (No: 3).",
            karisik.Not);

        var tamami = SevkeHazirSandikKurali.Ayir([Sandik("1", SandikDurum.Kapandi), Sandik("2", SandikDurum.Kapandi)]);
        Assert.Equal("Projedeki 2 sandığın tamamı sevke hazır.", tamami.Not);
    }

    /// <summary>İzin mevcut Saha/Yedek sandık raporuyla aynıdır; proje tipi veritabanından okunur.</summary>
    [Theory]
    [InlineData((int)ProjeTipi.Saha, YetkiKodlari.Saha.Raporu)]
    [InlineData((int)ProjeTipi.Yedek, YetkiKodlari.Yedek.Raporu)]
    public async Task Yetki_MevcutSahaYedekRaporOkumasidir(int projeTipiId, string beklenen)
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 5, ProjeNo = "PA707-5010", ProjeTipiId = projeTipiId });
        var resolver = new RequestMenuPermissionResolver(uow, new EntityFrameworkReadQueryExecutor());

        var istek = new GetSevkeHazirSandikRaporuQuery { ProjeId = 5, Format = "excel" };
        Assert.True(RequestMenuPermissionResolver.HasServerDefinition(istek));
        var izin = await resolver.ResolveAsync(istek, default);
        var mevcut = await resolver.ResolveAsync(new GetSahaProjePdfQuery { ProjeId = 5 }, default);

        Assert.Equal([MenuPermissionRequirement.Read(beklenen)], izin.Groups.SelectMany(g => g.Requirements));
        Assert.Equal(mevcut.Groups.SelectMany(g => g.Requirements), izin.Groups.SelectMany(g => g.Requirements));
    }

    [Theory]
    [InlineData("pdf", "PA707-5010_YedekRaporu_SevkeHazir.pdf", "pdf")]
    [InlineData("excel", "PA707-5010_YedekRaporu_SevkeHazir.xlsx", "excel")]
    public async Task Isleyici_BicimeGoreServisiCagirirVeDosyaAdiniVerir(string format, string dosyaAdi, string beklenenCagri)
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 5, ProjeNo = "PA707-5010", ProjeTipiId = (int)ProjeTipi.Yedek });
        var servis = new KaydedenRaporServisi();

        var sonuc = await new GetSevkeHazirSandikRaporuQueryHandler(uow, servis).Handle(new() { ProjeId = 5, Format = format }, default);

        Assert.True(sonuc.IsSuccess, sonuc.Error?.Message);
        Assert.Equal(dosyaAdi, sonuc.Value!.DosyaAdi);
        Assert.Equal([beklenenCagri], servis.Cagrilar);
    }

    [Fact]
    public async Task Isleyici_NormalProjeyiVeGecersizBicimiServiseGitmedenReddeder()
    {
        var uow = new OrtakMemoryUow();
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 5, ProjeNo = "PA707-01", ProjeTipiId = (int)ProjeTipi.Normal });
        uow.Repo<Proje>().Rows.Add(new Proje { Id = 6, ProjeNo = "PA707-5010", ProjeTipiId = (int)ProjeTipi.Saha });
        var servis = new KaydedenRaporServisi();
        var handler = new GetSevkeHazirSandikRaporuQueryHandler(uow, servis);

        var normal = await handler.Handle(new() { ProjeId = 5 }, default);
        var bicim = await handler.Handle(new() { ProjeId = 6, Format = "doc" }, default);
        var yok = await handler.Handle(new() { ProjeId = 99 }, default);

        Assert.Equal((400, "Sevke hazır sandık raporu yalnız Saha ve Yedek projelerinde alınır."), (normal.StatusCode, normal.Error!.Message));
        Assert.Equal(400, bicim.StatusCode);
        Assert.Equal(404, yok.StatusCode);
        Assert.Empty(servis.Cagrilar);
    }

    /// <summary>
    /// Gerçek servis: PDF ve Excel aynı sandıkları ve notu taşır; açık ve ölçüsüz sandık içerikleriyle birlikte dışarıda
    /// kalır; mevcut sandık raporu bütün sandıkları üretmeye devam eder; hiç hazır sandık yoksa neden mesajı döner.
    /// </summary>
    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Postgres_PdfVeExcelYalnizSevkeHazirSandiklariVeNotuIcerir()
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(connection.Host is "127.0.0.1" or "localhost", "Yalnız yerel izole test sunucusu kullanılabilir.");
        Assert.Equal(55439, connection.Port);
        Assert.Equal("postgres", connection.Database);
        var databaseName = $"sevke_hazir_tests_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await create.ExecuteNonQueryAsync();

        try
        {
            connection.Database = databaseName;
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var proje = new Proje { ProjeNo = "PA707-5010", Musteri = "Sentetik Beyanname", ProjeTipiId = (int)ProjeTipi.Yedek };
            void Ekle(string no, SandikDurum durum, bool olculu, params string[] urunler)
            {
                var sandik = Sandik(no, durum, olculu);
                sandik.Proje = proje;
                sandik.Ad = $"Sandık {no}";
                foreach (var urun in urunler)
                    sandik.SandikIcerikleri.Add(new SandikIcerik { Sandik = sandik, Isim = urun, BarkodNo = "B-" + urun, Miktar = 2, TahsisMiktari = 2 });
                proje.Sandiklar.Add(sandik);
            }
            Ekle("10", SandikDurum.Kapandi, true, "Pompa", "Vana");
            Ekle("2", SandikDurum.Kapandi, true, "Motor");
            Ekle("3", SandikDurum.Hazirlaniyor, true, "Gelmeyen-1", "Gelmeyen-2");
            Ekle("4", SandikDurum.Kapandi, false, "Olcusuz");
            context.Projeler.Add(proje);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var servis = new SahaSandikRaporService(context);
            var pdf = await servis.SevkeHazirSandiklarPdfOlusturAsync(proje.Id);
            var excel = await servis.SevkeHazirSandiklarExcelOlusturAsync(proje.Id);
            var tumu = await servis.SahaProjeSandiklariPdfOlusturAsync(proje.Id);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(tumu, 0, 4));
            await SaveSampleAsync("Yedek-sevke-hazir.pdf", pdf);
            await SaveSampleAsync("Yedek-sevke-hazir.xlsx", excel);
            await SaveSampleAsync("Yedek-tum-sandiklar.pdf", tumu);

            const string not = "4 sandıktan 2 sandık sevke hazır. 1 sandık kapanmadığı için listelenmedi (No: 3). " +
                               "1 sandık ölçü/ağırlık girilmediği için listelenmedi (No: 4).";
            using var workbook = new XLWorkbook(new MemoryStream(excel));
            Assert.Equal(["Sevke Hazır Sandıklar", "Listelenmeyen Sandıklar", "Sandık İçerikleri"], workbook.Worksheets.Select(w => w.Name));
            foreach (var sheet in workbook.Worksheets)
            {
                Assert.Equal("Sevke Hazır Sandık Raporu", sheet.Cell(1, 1).GetString());
                Assert.Equal(XLColor.FromHtml("#1565C0"), sheet.Cell(6, 1).Style.Fill.BackgroundColor);
                Assert.Equal($"{SevkeHazirSandikKurali.KapsamMetni} {not}", sheet.Cell(4, 1).GetString());
                Assert.Equal("2 / 4", sheet.Cell(3, 2).GetString());
            }

            var ozet = workbook.Worksheet("Sevke Hazır Sandıklar");
            Assert.Equal(("2", "10", "Toplam"), (ozet.Cell(7, 2).GetString(), ozet.Cell(8, 2).GetString(), ozet.Cell(9, 2).GetString()));
            Assert.Equal((2000m, 900m, 900m, 300m, 400m), (ozet.Cell(7, 4).GetValue<decimal>(), ozet.Cell(7, 5).GetValue<decimal>(),
                ozet.Cell(7, 6).GetValue<decimal>(), ozet.Cell(7, 7).GetValue<decimal>(), ozet.Cell(7, 8).GetValue<decimal>()));
            Assert.Equal((600m, 800m, 3m), (ozet.Cell(9, 7).GetValue<decimal>(), ozet.Cell(9, 8).GetValue<decimal>(), ozet.Cell(9, 9).GetValue<decimal>()));
            Assert.Equal(9, ozet.LastRowUsed()!.RowNumber());

            var disarida = workbook.Worksheet("Listelenmeyen Sandıklar");
            Assert.Equal(("3", "Hazırlanıyor", "Kapanmadı", "-"),
                (disarida.Cell(7, 2).GetString(), disarida.Cell(7, 4).GetString(), disarida.Cell(7, 5).GetString(), disarida.Cell(7, 6).GetString()));
            Assert.Equal(("4", "Kapandı", "Ölçü/ağırlık eksik", "Boy, En, Yükseklik, Net kg, Brüt kg"),
                (disarida.Cell(8, 2).GetString(), disarida.Cell(8, 4).GetString(), disarida.Cell(8, 5).GetString(), disarida.Cell(8, 6).GetString()));

            var icerik = workbook.Worksheet("Sandık İçerikleri");
            var satirlar = icerik.RowsUsed().Where(r => r.RowNumber() > 6).ToList();
            Assert.Equal([("2", "Motor"), ("10", "Pompa"), ("10", "Vana")],
                satirlar.Select(r => (r.Cell(1).GetString(), r.Cell(5).GetString())).OrderBy(x => x.Item1 == "2" ? 0 : 1).ThenBy(x => x.Item2));
            Assert.DoesNotContain(satirlar, r => r.Cell(5).GetString().StartsWith("Gelmeyen") || r.Cell(5).GetString() == "Olcusuz");
            Assert.All(satirlar, r => Assert.Equal(2m, r.Cell(6).GetValue<decimal>()));

            // Hiç hazır sandık yoksa boş rapor yerine neden döner.
            var acik = new Proje { ProjeNo = "PA708-5010", Musteri = "Sentetik", ProjeTipiId = (int)ProjeTipi.Saha };
            acik.Sandiklar.Add(new Sandik { Proje = acik, SandikNo = "1", DurumId = (int)SandikDurum.Hazirlaniyor });
            context.Projeler.Add(acik);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            var hata = await Assert.ThrowsAsync<IsKuraliException>(() => servis.SevkeHazirSandiklarExcelOlusturAsync(acik.Id));
            Assert.Equal("Sevke hazır sandık bulunamadı. 1 sandıktan 0 sandık sevke hazır. 1 sandık kapanmadığı için listelenmedi (No: 1).", hata.Message);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task SaveSampleAsync(string name, byte[] data)
    {
        var output = Environment.GetEnvironmentVariable("THREEK_REPORT_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await File.WriteAllBytesAsync(Path.Combine(output, name), data);
    }

    private sealed class KaydedenRaporServisi : ISahaSandikRaporService
    {
        public List<string> Cagrilar { get; } = [];

        public Task<byte[]> SahaProjeSandiklariPdfOlusturAsync(int projeId)
        {
            Cagrilar.Add("tumu");
            return Task.FromResult(new byte[] { 1 });
        }

        public Task<byte[]> SevkeHazirSandiklarPdfOlusturAsync(int projeId)
        {
            Cagrilar.Add("pdf");
            return Task.FromResult(new byte[] { 1 });
        }

        public Task<byte[]> SevkeHazirSandiklarExcelOlusturAsync(int projeId)
        {
            Cagrilar.Add("excel");
            return Task.FromResult(new byte[] { 1 });
        }
    }
}
