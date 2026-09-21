using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using _3K.Core.Constants;
using _3K.Core.Entities;
using _3K.Core.Enums;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Migrations;

namespace _3K.Application.Tests;

/// <summary>
/// Gerçek migration Up SQL'i, yalnız GUID adlı sentetik localhost DB'sinde çalışır.
/// V1-benzeri temel şema güncel modelden V2 kolon/tablo çıkarılarak kurulur; canlı dump değildir.
/// </summary>
public sealed class UretimFinansMigrationPostgresTests
{
    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task K39_GercekMigration01den04e_GecmisSnapshotiKorur_TekrarYetkiIptaliniGeriAlmaz()
    {
        await InDatabase(async (connection, options) =>
        {
            await LegacyBaseline(options, connection, invalidInvoice: false);
            var operations = new UretimFinansV2().UpOperations.OfType<SqlOperation>().ToArray();
            Assert.Equal(5, operations.Length);
            Assert.All(operations, x => Assert.False(x.SuppressTransaction));
            await ApplyMigration(connection);
            Assert.Equal(1000m, await Scalar<decimal>(connection, "SELECT \"NetTutarSnapshot\" FROM \"FinansFaturaKalemleri\" WHERE \"Id\"=1"));
            Assert.Equal(900m, await Scalar<decimal>(connection, "SELECT (\"Snapshot\"->>'NetTutarSnapshot')::numeric FROM \"FinansV2GecisYedegi\" WHERE \"VarlikTuru\"='FinansFaturaKalemi' AND \"VarlikId\"=1"));
            Assert.True(await Scalar<bool>(connection, "SELECT \"FinansMiktariManuel\" FROM \"FinansIsKayitlari\" WHERE \"Id\"=1"));
            Assert.Equal(new DateTime(2026, 8, 1), await Scalar<DateTime>(connection, "SELECT \"FinansTarihi\" FROM \"FinansIsKayitlari\" WHERE \"Id\"=1"));
            Assert.Equal(new DateTime(2026, 8, 19), await Scalar<DateTime>(connection, "SELECT \"UretimTarihi\" FROM \"FinansIsKayitlari\" WHERE \"Id\"=1"));
            Assert.Equal(1L, await Scalar<long>(connection, "SELECT count(*) FROM \"FinansDegisiklikGecmisleri\" WHERE \"Islem\"='V2 Mutabakat Geçişi'"));
            foreach (var table in new[] { "SandikTopluTasimaIslemleri", "AmbalajUretimFormuSurumleri", "AmbalajUretimGerceklesmeleri", "FinansBelgeleri", "KullaniciYetkileri", "YetkiDegisiklikleri" })
                Assert.True(await Exists(connection, table));
            Assert.Equal("Kontrplak Sandık", await Scalar<string>(connection, "SELECT \"Deger\" FROM \"LookupSandikTipleri\" WHERE \"Id\"=3"));
            Assert.NotEqual(default, await Scalar<DateTime>(connection, "SELECT \"CreatedDate\" FROM \"LookupSandikTipleri\" WHERE \"Id\"=3"));

            var criticalId = YetkiKatalogu.Bul(YetkiKodlari.Finans.KaliciSil)!.Id;
            await Execute(connection, $"DELETE FROM \"RolYetkileri\" WHERE \"RolId\"=1 AND \"MenuTanimiId\"={criticalId}");
            await ApplyMigration(connection);
            Assert.Equal(0L, await Scalar<long>(connection, $"SELECT count(*) FROM \"RolYetkileri\" WHERE \"RolId\"=1 AND \"MenuTanimiId\"={criticalId}"));
            Assert.Equal(1L, await Scalar<long>(connection, "SELECT count(*) FROM \"FinansDegisiklikGecmisleri\" WHERE \"Islem\"='V2 Mutabakat Geçişi'"));
            Assert.Equal(1000m, await Scalar<decimal>(connection, "SELECT \"NetTutarSnapshot\" FROM \"FinansFaturaKalemleri\" WHERE \"Id\"=1"));
            Assert.Equal(900m, await Scalar<decimal>(connection, "SELECT (\"Snapshot\"->>'NetTutarSnapshot')::numeric FROM \"FinansV2GecisYedegi\" WHERE \"VarlikTuru\"='FinansFaturaKalemi' AND \"VarlikId\"=1"));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task K39_FinansKapasiteHatasinda_OncekiSqlAdimlariVeMutabakatYazimiBirlikteRollbackOlur()
    {
        await InDatabase(async (connection, options) =>
        {
            await LegacyBaseline(options, connection, invalidInvoice: true);
            var error = await Assert.ThrowsAsync<PostgresException>(() => ApplyMigration(connection));
            Assert.Contains("PO kapasitesini", error.MessageText);
            Assert.False(await Exists(connection, "SandikTopluTasimaIslemleri"));
            Assert.False(await Exists(connection, "AmbalajUretimFormuSurumleri"));
            Assert.False(await Exists(connection, "FinansV2GecisYedegi"));
            Assert.False(await Exists(connection, "KullaniciYetkileri"));
            Assert.Equal(900m, await Scalar<decimal>(connection, "SELECT \"NetTutarSnapshot\" FROM \"FinansFaturaKalemleri\" WHERE \"Id\"=1"));
            Assert.Equal(0L, await Scalar<long>(connection, "SELECT count(*) FROM \"FinansDegisiklikGecmisleri\""));
            Assert.Equal(0L, await Scalar<long>(connection, "SELECT count(*) FROM information_schema.columns WHERE table_name='FinansIsKayitlari' AND column_name='FinansTarihi'"));
        });
    }

    [PostgresRaporFact, Trait("Category", "Postgres")]
    public async Task K39_TemelSemaYoksa_MigrationHicbirV2TablosuOlusturmadanDurur()
    {
        await InDatabase(async (connection, _) =>
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => ApplyMigration(connection));
            Assert.Contains("temel Ambalaj/Finans", error.MessageText);
            Assert.False(await Exists(connection, "SandikTopluTasimaIslemleri"));
            Assert.False(await Exists(connection, "FinansV2GecisYedegi"));
            Assert.False(await Exists(connection, "KullaniciYetkileri"));
        });
    }

    private static async Task ApplyMigration(NpgsqlConnection connection)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var operation in new UretimFinansV2().UpOperations.Cast<SqlOperation>())
        {
            await using var command = new NpgsqlCommand(operation.Sql, connection, transaction);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    private static async Task LegacyBaseline(DbContextOptions<AppDbContext> options, NpgsqlConnection connection, bool invalidInvoice)
    {
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var work = new FinansIsKaydi
            {
                Id = 1, ProjeNo = "MIGRATION-QA", Musteri = "Sentetik", IsAdi = "Geçiş fixture",
                IsTuru = FinansIsTuru.OzelIs, Adet = 1, Birim = "Adet", BirimFiyatSnapshot = 10000,
                FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet,
                UretimTarihi = new(2026, 8, 19), FinansDonemi = new(2026, 8, 1), FinansTarihi = new(2026, 8, 1),
                KayitTarihi = new(2026, 8, 19)
            };
            var po = new FinansSiparis { Id = 1, KayitNo = "PO-QA", PoNumarasi = "PO-QA", SiparisTarihi = new(2026, 8, 20) };
            var poLine = new FinansSiparisKalemi
            {
                Id = 1, FinansSiparis = po, FinansIsKaydi = work, Adet = 1,
                FiyatlandirmaBirimiSnapshot = FinansFiyatlandirmaBirimi.Adet, BirimFiyatSnapshot = 1000,
                NetTutarSnapshot = 1000, KdvTutariSnapshot = 200, ToplamTutarSnapshot = 1200
            };
            var invoice = new FinansFatura
            {
                Id = 1, FinansSiparis = po, KayitNo = "INV-QA", FaturaNumarasi = "INV-QA", FaturaTarihi = new(2026, 8, 21),
                BelgeNetTutarSnapshot = invalidInvoice ? 1001 : 1000, BelgeKdvTutariSnapshot = 200,
                BelgeToplamTutarSnapshot = invalidInvoice ? 1201 : 1200, BelgeParaBirimiSnapshot = "EUR"
            };
            db.Add(new FinansFaturaKalemi
            {
                Id = 1, FinansFatura = invoice, FinansSiparisKalemi = poLine, Adet = 1,
                NetTutarSnapshot = 900, KdvTutariSnapshot = 180, ToplamTutarSnapshot = 1080
            });
            await db.SaveChangesAsync();
        }

        // Sadece bu testin oluşturduğu GUID veritabanını V1-benzeri hale getirir.
        await Execute(connection, """
            DELETE FROM "RolYetkileri" WHERE "MenuTanimiId">=5000;
            DELETE FROM "MenuTanimlari" WHERE "Id">=5000;
            DELETE FROM "LookupSandikTipleri" WHERE "Id"=3;
            DROP TABLE "KullaniciYetkileri", "YetkiDegisiklikleri", "SandikTopluTasimaIslemleri";
            DROP TABLE "AmbalajUretimFormuKayitlari", "AmbalajUretimFormuSurumleri", "AmbalajUretimGerceklesmeleri";
            DROP TABLE "FinansBelgeleri", "FinansKaynakBastirmalari", "FinansIsSablonSurumleri", "FinansIsSablonlari" CASCADE;
            ALTER TABLE "FinansIsKayitlari"
              DROP COLUMN "FinansTarihi", DROP COLUMN "FinansTarihiManuel", DROP COLUMN "FinansMiktariManuel",
              DROP COLUMN "ManuelNetTutar", DROP COLUMN "TarifeIdSnapshot", DROP COLUMN "SandikCinsi",
              DROP COLUMN "SablonSurumId", DROP COLUMN "AlanDegerleriJson", DROP COLUMN "FiyatBilesenleriJson", DROP COLUMN "KaynakBileseni";
            ALTER TABLE "FinansSiparisleri" DROP COLUMN "ParaBirimi";
            ALTER TABLE "FinansSiparisKalemleri" DROP COLUMN "TutarBazli";
            ALTER TABLE "FinansFaturaKalemleri" DROP COLUMN "TutarBazli";
            ALTER TABLE "FinansGiderleri" DROP COLUMN "FinansTarihi", DROP COLUMN "BelgeNo", DROP COLUMN "AvansMi", DROP COLUMN "MahsupEdilenAvansId";
            ALTER TABLE "FinansDegisiklikGecmisleri" DROP COLUMN "IslemGrubu", DROP COLUMN "Referans";
            """);
    }

    private static async Task<bool> Exists(NpgsqlConnection connection, string table)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", "public.\"" + table + "\"");
        return (bool)(await command.ExecuteScalarAsync())!;
    }
    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }
    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
    private static async Task InDatabase(Func<NpgsqlConnection, DbContextOptions<AppDbContext>, Task> action)
    {
        var config = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("THREEK_TEST_POSTGRES"));
        Assert.True(config.Host is "127.0.0.1" or "localhost");
        Assert.Equal(55439, config.Port);
        Assert.Equal("postgres", config.Database);
        var database = "migration_v2_tests_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(config.ConnectionString);
        await admin.OpenAsync();
        await Execute(admin, $"CREATE DATABASE \"{database}\"");
        try
        {
            config.Database = database;
            await using var connection = new NpgsqlConnection(config.ConnectionString);
            await connection.OpenAsync();
            await action(connection, new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(config.ConnectionString).Options);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await Execute(admin, $"DROP DATABASE \"{database}\" WITH (FORCE)");
        }
    }
}
