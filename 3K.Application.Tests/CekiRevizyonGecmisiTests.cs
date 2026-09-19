using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using _3K.Application.Features.CekiIslemleri.Queries;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Core.Helpers;
using _3K.Core.Models;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Services;

namespace _3K.Application.Tests;

public class CekiRevizyonGecmisiTests
{
    [Theory]
    [InlineData(true, false, null, null, "Uygulandi")]
    [InlineData(false, true, null, null, "EskiKayit")]
    [InlineData(false, false, OnayDurumu.Bekliyor, OnayCalistirmaDurumu.Bekliyor, "OnayBekliyor")]
    [InlineData(false, false, OnayDurumu.Reddedildi, OnayCalistirmaDurumu.Atlandi, "Reddedildi")]
    [InlineData(false, false, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Basarisiz, "Basarisiz")]
    [InlineData(false, false, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Calisiyor, "Uygulaniyor")]
    [InlineData(false, false, OnayDurumu.Onaylandi, OnayCalistirmaDurumu.Bekliyor, "Onaylandi")]
    [InlineData(false, false, null, null, "Uygulanmadi")]
    public void Durum_UygulamaKararVeOnizlemeAyridir(bool applied, bool legacy, OnayDurumu? decision, OnayCalistirmaDurumu? execution, string expected)
        => Assert.Equal(expected, CekiRevizyonGecmisiService.DurumBelirle(applied, legacy, decision, execution));

    [Fact]
    public void Snapshot_HashVeProjeIliskisiDogrulanir_GuncelVeridenFarkUretilmez()
    {
        var preview = Preview(31, 41);
        var json = JsonSerializer.Serialize(preview, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var hash = CekiRevizyonOnizlemeButunlugu.HashOlustur(preview);
        var result = CekiRevizyonGecmisiService.SnapshotOku(json, hash, 1, 31, 41);
        Assert.Equal(2m, Assert.Single(result!.Satirlar).EskiIstenenAdet);
        Assert.Equal(4m, result.Satirlar[0].YeniIstenenAdet);
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku(json, hash, 1, 32, 41));
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku(json, new string('0', 64), 1, 31, 41));
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku("broken", hash, 1, 31, 41));
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku(json, hash, 99, 31, 41));
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku("{\"projeId\":31,\"anaCekiId\":41,\"sandikEtkileri\":[null]}", hash, 1, 31, 41));
        Assert.Null(CekiRevizyonGecmisiService.SnapshotOku("{\"projeId\":31,\"anaCekiId\":41,\"satirlar\":[null]}", hash, 1, 31, 41));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(1000001, 10)]
    public void Liste_SinirsizVeyaGecersizSayfaReddedilir(int page, int size)
        => Assert.False(new GetCekiRevizyonGecmisiQueryValidator().Validate(new GetCekiRevizyonGecmisiQuery { ProjeId = 1, PageNumber = page, PageSize = size }).IsValid);

    [Theory]
    [InlineData("../ceki")]
    [InlineData("dosya")]
    [InlineData("")]
    public void Dosya_IstemciDosyaYoluKaynakOlamaz(string source)
        => Assert.False(new GetCekiRevizyonDosyaQueryValidator().Validate(new GetCekiRevizyonDosyaQuery { ProjeId = 1, KayitId = 1, Kaynak = source }).IsValid);

    [PostgresRaporFact]
    [Trait("Category", "Postgres")]
    public async Task Gecmis_GercekPostgresSayfalamaSnapshotTemizlemeSonrasiDosyaVeLegacy()
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.Equal("127.0.0.1", builder.Host);
        Assert.Equal(55439, builder.Port);
        Assert.Equal("postgres", builder.Database);
        var database = $"revision_tests_{Guid.NewGuid():N}";
        var temp = Path.Combine(Path.GetTempPath(), "3k-revision-tests-" + Guid.NewGuid().ToString("N"));
        await using var admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            builder.Database = database;
            await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(builder.ConnectionString).Options);
            await context.Database.EnsureCreatedAsync();
            var project = new Proje { ProjeNo = "REV-QA", Musteri = "Sentetik", ProjeTipiId = 1 };
            var main = new Ceki { Proje = project, OrijinalDosyaYolu = "initial.xlsx" };
            var user = new Kullanici { AdSoyad = "Test Yükleyen", Email = "revision@example.invalid", SifreHash = "synthetic", RolId = 1 };
            context.AddRange(main, user);
            await context.SaveChangesAsync();
            var bytes = new byte[] { 80, 75, 3, 4, 1, 2, 3 };
            var preview = Preview(project.Id, main.Id);
            var talep = new CekiRevizyonTalebi
            {
                ProjeId = project.Id, AnaCekiId = main.Id, TalepEdenKullaniciId = user.Id,
                DosyaAdi = "orijinal-revizyon.xlsx", DosyaIcerigi = bytes,
                DosyaSha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                OnizlemeJson = JsonSerializer.Serialize(preview, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                OnizlemeHash = CekiRevizyonOnizlemeButunlugu.HashOlustur(preview), GuncellenenSatirSayisi = 1
            };
            context.Add(talep);
            await context.SaveChangesAsync();
            var approval = new OnayBekleyenIslem
            {
                ProjeId = project.Id, TalepEdenKullaniciId = user.Id, ReferansId = talep.Id,
                ReferansTipi = OnayReferansTipleri.CekiRevizyonTalebi, IslemKodu = OnayIslemKodlari.CekiRevizyonuUygula,
                Durum = OnayDurumu.Bekliyor, PayloadJson = "{}"
            };
            context.Add(approval);
            await context.SaveChangesAsync();
            var service = new CekiRevizyonGecmisiService(context, temp);
            var pending = await service.ListeleAsync(project.Id, 1, 1, default);
            Assert.Equal("OnayBekliyor", Assert.Single(pending.Items).Durum);
            Assert.True(pending.Items[0].DosyaMevcut);
            Assert.Equal(bytes, (await service.DosyaAsync(project.Id, "talep", talep.Id, default))!.Icerik);
            Assert.Null(await service.DosyaAsync(project.Id + 1, "talep", talep.Id, default));

            // Mevcut uygulama ve temizlik sözleşmesini temsil eder; geçmiş servisi yazmaz.
            var dir = Path.Combine(temp, project.Id.ToString(), "Revizyonlar");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".xlsx");
            await File.WriteAllBytesAsync(path, bytes);
            var revision = new Ceki { ProjeId = project.Id, KaynakCekiId = main.Id, CekiTipiId = 3, OrijinalDosyaYolu = path };
            context.Add(revision);
            await context.SaveChangesAsync();
            talep.UygulananRevizyonCekiId = revision.Id;
            talep.UygulamaTarihi = TurkeyTime.Now;
            talep.DosyaIcerigi = null;
            approval.Durum = OnayDurumu.Onaylandi;
            approval.CalistirmaDurumu = OnayCalistirmaDurumu.Basarili;
            approval.OnaylayanKullaniciId = user.Id;
            approval.KararTarihi = TurkeyTime.Now;
            var legacy = new Ceki { ProjeId = project.Id, CekiTipiId = 3, OrijinalDosyaYolu = "missing.xlsx", Aciklama = "Revizyon dosyası: legacy.xlsx", YuklemeTarihi = TurkeyTime.Now.AddDays(-1) };
            context.Add(legacy);
            await context.SaveChangesAsync();
            var first = await service.ListeleAsync(project.Id, 1, 1, default);
            Assert.Equal(2, first.TotalCount); // Uygulanan çeki + talep mükerrer değildir.
            Assert.Equal("Uygulandi", Assert.Single(first.Items).Durum);
            Assert.Equal("Test Yükleyen", first.Items[0].Onaylayan);
            Assert.Equal(bytes, (await service.DosyaAsync(project.Id, "talep", talep.Id, default))!.Icerik);
            var detail = await service.DetayAsync(project.Id, "talep", talep.Id, default);
            Assert.Equal(2m, detail!.Onizleme!.Satirlar[0].EskiIstenenAdet);
            var second = await service.ListeleAsync(project.Id, 2, 1, default);
            var old = Assert.Single(second.Items);
            Assert.Equal("legacy.xlsx", old.DosyaAdi);
            Assert.Equal("EskiKayit", old.Durum);
            Assert.Null(old.EklenenSatirSayisi);
            Assert.Null((await service.DetayAsync(project.Id, "ceki", legacy.Id, default))!.Onizleme);
            Assert.False(old.DosyaMevcut);
            Assert.Null(await service.DosyaAsync(project.Id, "ceki", legacy.Id, default));
            Assert.Null(await service.DetayAsync(project.Id + 1, "talep", talep.Id, default));
            Assert.Null(RevizyonDosyaDeposu.GuvenliYol(temp, project.Id + 1, path));
            var outside = Path.Combine(temp, "outside.xlsx");
            await File.WriteAllBytesAsync(outside, bytes);
            Assert.Null(RevizyonDosyaDeposu.GuvenliYol(temp, project.Id, outside));
            await File.WriteAllBytesAsync(path, [0]);
            Assert.Null(await service.DosyaAsync(project.Id, "talep", talep.Id, default));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    private static CekiRevizyonOnizlemeSonuc Preview(int project, int main) => new()
    {
        ProjeId = project, AnaCekiId = main, GuncellenenSatirSayisi = 1,
        Satirlar = [new() { ExcelSatirNo = 23, MevcutCekiSatiriId = 43626, BarkodNo = "SYNTHETIC", CheckKodu = "U", EskiIstenenAdet = 2m, YeniIstenenAdet = 4m, EskiKoliNo = "2", YeniKoliNo = "10" }]
    };
}
