using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;
using _3K.Infrastructure.Data;

namespace _3K.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL uzerinde proje bazinda birlesen kalici ambalaj kaynak kuyrugu.
/// Claim islemi kisa bir transaction icinde SKIP LOCKED kullanir; asil
/// senkronizasyon bu transaction kapandiktan sonra gerceklestirilir.
/// </summary>
public sealed class PostgreSqlAmbalajKaynakSenkronizasyonKuyrugu
    : IAmbalajKaynakSenkronizasyonKuyrugu
{
    internal const int SonHataAzamiUzunlugu = 2000;

    // Dogrudan kuyruklama da kaynak degisikligiyle ayni transaction'a katilir.
    // Yeni bir Surum her zaman temiz deneme sayisiyla baslar; devam eden lease
    // yalniz gecikmis worker'in sahipligini Surum kontroluyle sonlandirmak icin
    // korunur. NOTIFY teslimat degil, sadece dusuk gecikmeli uyandirma sinyalidir.
    private const string KuyrugaEkleSql = """
        WITH zaman AS
        (
            SELECT clock_timestamp() AS "Utc"
        )
        INSERT INTO "AmbalajKaynakSenkronizasyonKuyrugu" AS kuyruk
        (
            "ProjeId",
            "Surum",
            "Durum",
            "TalepTarihiUtc",
            "UygunTarihUtc",
            "DenemeSayisi"
        )
        SELECT
            @projeId,
            1,
            @bekliyor,
            zaman."Utc",
            zaman."Utc",
            0
        FROM zaman
        ON CONFLICT ("ProjeId") DO UPDATE SET
            "Surum" = kuyruk."Surum" + 1,
            "Durum" = CASE
                WHEN kuyruk."Durum" = @isleniyor
                    AND kuyruk."KilitKimligi" IS NOT NULL
                    AND kuyruk."KilitBitisTarihiUtc" > EXCLUDED."TalepTarihiUtc"
                    THEN @isleniyor
                ELSE @bekliyor
            END,
            "TalepTarihiUtc" = EXCLUDED."TalepTarihiUtc",
            "UygunTarihUtc" = EXCLUDED."UygunTarihUtc",
            "DenemeSayisi" = 0,
            "KilitKimligi" = CASE
                WHEN kuyruk."Durum" = @isleniyor
                    AND kuyruk."KilitKimligi" IS NOT NULL
                    AND kuyruk."KilitBitisTarihiUtc" > EXCLUDED."TalepTarihiUtc"
                    THEN kuyruk."KilitKimligi"
                ELSE NULL
            END,
            "KilitBitisTarihiUtc" = CASE
                WHEN kuyruk."Durum" = @isleniyor
                    AND kuyruk."KilitKimligi" IS NOT NULL
                    AND kuyruk."KilitBitisTarihiUtc" > EXCLUDED."TalepTarihiUtc"
                    THEN kuyruk."KilitBitisTarihiUtc"
                ELSE NULL
            END,
            "SonHata" = NULL,
            "HataKuyrugunaAlindiTarihiUtc" = NULL;

        SELECT pg_notify('ambalaj_kaynak_degisti', '');
        """;

    private const string IsleriSahiplenSql = """
        WITH secilen AS
        (
            SELECT kuyruk."ProjeId"
            FROM "AmbalajKaynakSenkronizasyonKuyrugu" AS kuyruk
            WHERE
                kuyruk."UygunTarihUtc" <= CURRENT_TIMESTAMP
                AND
                (
                    kuyruk."Durum" = @bekliyor
                    OR
                    (
                        kuyruk."Durum" = @isleniyor
                        AND kuyruk."KilitBitisTarihiUtc" <= CURRENT_TIMESTAMP
                    )
                )
                AND
                (
                    kuyruk."KilitKimligi" IS NULL
                    OR kuyruk."KilitBitisTarihiUtc" <= CURRENT_TIMESTAMP
                )
            ORDER BY kuyruk."TalepTarihiUtc", kuyruk."ProjeId"
            FOR UPDATE OF kuyruk SKIP LOCKED
            LIMIT @azamiIsSayisi
        )
        UPDATE "AmbalajKaynakSenkronizasyonKuyrugu" AS kuyruk
        SET
            "Durum" = @isleniyor,
            "KilitKimligi" = @kilitKimligi,
            "KilitBitisTarihiUtc" = CURRENT_TIMESTAMP + @kilitSuresi,
            "SonDenemeTarihiUtc" = CURRENT_TIMESTAMP
        FROM secilen
        WHERE kuyruk."ProjeId" = secilen."ProjeId"
        RETURNING
            kuyruk."ProjeId",
            kuyruk."Surum",
            kuyruk."KilitKimligi",
            kuyruk."DenemeSayisi"
        """;

    // Surum, claim sonrasinda degismisse worker'in okudugu snapshot artik en
    // guncel kabul edilmez. Bu durumda kayit tamamlanmaz ve hemen tekrar bekler.
    private const string BasariliTamamlaSql = """
        UPDATE "AmbalajKaynakSenkronizasyonKuyrugu" AS kuyruk
        SET
            "Durum" = CASE
                WHEN kuyruk."Surum" = @surum THEN @tamamlandi
                ELSE @bekliyor
            END,
            "UygunTarihUtc" = CURRENT_TIMESTAMP,
            "DenemeSayisi" = 0,
            "KilitKimligi" = NULL,
            "KilitBitisTarihiUtc" = NULL,
            "SonBasariliTarihUtc" = CASE
                WHEN kuyruk."Surum" = @surum THEN CURRENT_TIMESTAMP
                ELSE kuyruk."SonBasariliTarihUtc"
            END,
            "SonHata" = NULL,
            "HataKuyrugunaAlindiTarihiUtc" = NULL
        WHERE
            kuyruk."ProjeId" = @projeId
            AND kuyruk."KilitKimligi" = @kilitKimligi
        RETURNING kuyruk."Durum", kuyruk."DenemeSayisi"
        """;

    // Yeni bir Surum gelmisse eski isin hatasi yeni isi backoff/dead-letter ile
    // cezalandiramaz. Yeni surum sifir denemeyle hemen tekrar islenebilir olur.
    private const string BasarisizTamamlaSql = """
        UPDATE "AmbalajKaynakSenkronizasyonKuyrugu" AS kuyruk
        SET
            "Durum" = CASE
                WHEN kuyruk."Surum" <> @surum THEN @bekliyor
                WHEN kuyruk."DenemeSayisi" + 1 >= @azamiDenemeSayisi THEN @hataKuyrugunda
                ELSE @bekliyor
            END,
            "UygunTarihUtc" = CASE
                WHEN kuyruk."Surum" <> @surum THEN CURRENT_TIMESTAMP
                WHEN kuyruk."DenemeSayisi" + 1 >= @azamiDenemeSayisi THEN CURRENT_TIMESTAMP
                ELSE @yenidenDenemeTarihiUtc
            END,
            "DenemeSayisi" = CASE
                WHEN kuyruk."Surum" <> @surum THEN 0
                ELSE kuyruk."DenemeSayisi" + 1
            END,
            "KilitKimligi" = NULL,
            "KilitBitisTarihiUtc" = NULL,
            "SonHata" = CASE
                WHEN kuyruk."Surum" <> @surum THEN NULL
                ELSE @hata
            END,
            "HataKuyrugunaAlindiTarihiUtc" = CASE
                WHEN kuyruk."Surum" <> @surum THEN NULL
                WHEN kuyruk."DenemeSayisi" + 1 >= @azamiDenemeSayisi THEN CURRENT_TIMESTAMP
                ELSE NULL
            END
        WHERE
            kuyruk."ProjeId" = @projeId
            AND kuyruk."KilitKimligi" = @kilitKimligi
        RETURNING kuyruk."Durum", kuyruk."DenemeSayisi"
        """;

    private const string IstatistikSql = """
        SELECT
            COUNT(*) FILTER (WHERE "Durum" = @bekliyor) AS "Bekleyen",
            COUNT(*) FILTER (WHERE "Durum" = @isleniyor) AS "Isleniyor",
            COUNT(*) FILTER (WHERE "Durum" = @tamamlandi) AS "Tamamlanan",
            COUNT(*) FILTER (WHERE "Durum" = @hataKuyrugunda) AS "HataKuyrugunda",
            COUNT(*) FILTER (WHERE "Durum" = @bekliyor AND "DenemeSayisi" > 0) AS "YenidenDenenecek",
            MIN("TalepTarihiUtc") FILTER (WHERE "Durum" IN (@bekliyor, @isleniyor)) AS "EnEskiBekleyen",
            MAX("SonBasariliTarihUtc") AS "SonBasarili"
        FROM "AmbalajKaynakSenkronizasyonKuyrugu"
        """;

    private readonly AppDbContext _context;

    public PostgreSqlAmbalajKaynakSenkronizasyonKuyrugu(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task KuyrugaEkleAsync(
        int projeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(projeId);

        await BaglantiIleCalisAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = KuyrugaEkleSql;
            AktifTransactioniBagla(command);
            ParametreEkle(command, "projeId", NpgsqlDbType.Integer, projeId);
            DurumParametresiEkle(command, "bekliyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            DurumParametresiEkle(command, "isleniyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Isleniyor);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<AmbalajKaynakSenkronizasyonKuyrukIsi>> IsleriSahiplenAsync(
        int azamiIsSayisi,
        TimeSpan kilitSuresi,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(azamiIsSayisi);
        if (kilitSuresi <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(kilitSuresi), "Kilit suresi sifirdan buyuk olmalidir.");
        if (_context.Database.CurrentTransaction != null)
            throw new InvalidOperationException("Kuyruk claim islemi mevcut bir transaction icinde baslatilamaz.");

        return await BaglantiIleCalisAsync(async connection =>
        {
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = IsleriSahiplenSql;

            var kilitKimligi = Guid.NewGuid();
            ParametreEkle(command, "azamiIsSayisi", NpgsqlDbType.Integer, azamiIsSayisi);
            ParametreEkle(command, "kilitKimligi", NpgsqlDbType.Uuid, kilitKimligi);
            ParametreEkle(command, "kilitSuresi", NpgsqlDbType.Interval, kilitSuresi);
            DurumParametresiEkle(command, "bekliyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            DurumParametresiEkle(command, "isleniyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Isleniyor);

            var isler = new List<AmbalajKaynakSenkronizasyonKuyrukIsi>(azamiIsSayisi);
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    isler.Add(new AmbalajKaynakSenkronizasyonKuyrukIsi(
                        reader.GetInt32(0),
                        reader.GetInt64(1),
                        reader.GetGuid(2),
                        reader.GetInt32(3)));
                }
            }

            // Claim transaction'i yalniz satir secme ve lease yazma suresince acik
            // kalir. Uzun senkronizasyon bu commit'ten sonra worker tarafinda yapilir.
            await transaction.CommitAsync(cancellationToken);
            return (IReadOnlyList<AmbalajKaynakSenkronizasyonKuyrukIsi>)isler;
        }, cancellationToken);
    }

    public async Task<AmbalajKaynakSenkronizasyonSonlandirmaSonucu> BasariliTamamlaAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(isKaydi);

        return await BaglantiIleCalisAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = BasariliTamamlaSql;
            AktifTransactioniBagla(command);
            IsKimligiParametreleriniEkle(command, isKaydi);
            DurumParametresiEkle(command, "bekliyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            DurumParametresiEkle(command, "tamamlandi", AmbalajKaynakSenkronizasyonKuyrukDurumu.Tamamlandi);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SahiplikKaybi();

            var durum = DurumOku(reader, 0);
            var denemeSayisi = reader.GetInt32(1);
            return new AmbalajKaynakSenkronizasyonSonlandirmaSonucu(
                durum == AmbalajKaynakSenkronizasyonKuyrukDurumu.Tamamlandi
                    ? AmbalajKaynakSenkronizasyonSonlandirmaDurumu.Tamamlandi
                    : AmbalajKaynakSenkronizasyonSonlandirmaDurumu.YenidenKuyrugaAlindi,
                denemeSayisi);
        }, cancellationToken);
    }

    public async Task<AmbalajKaynakSenkronizasyonSonlandirmaSonucu> BasarisizTamamlaAsync(
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi,
        string hata,
        DateTime yenidenDenemeTarihiUtc,
        int azamiDenemeSayisi,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(isKaydi);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(azamiDenemeSayisi);

        var guvenliHata = HataMetniniSinirla(hata);
        var guvenliYenidenDenemeTarihi = UtcTarihineCevir(yenidenDenemeTarihiUtc);
        if (guvenliYenidenDenemeTarihi < DateTime.UtcNow)
            guvenliYenidenDenemeTarihi = DateTime.UtcNow;

        return await BaglantiIleCalisAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = BasarisizTamamlaSql;
            AktifTransactioniBagla(command);
            IsKimligiParametreleriniEkle(command, isKaydi);
            ParametreEkle(command, "hata", NpgsqlDbType.Varchar, guvenliHata);
            ParametreEkle(
                command,
                "yenidenDenemeTarihiUtc",
                NpgsqlDbType.TimestampTz,
                guvenliYenidenDenemeTarihi);
            ParametreEkle(command, "azamiDenemeSayisi", NpgsqlDbType.Integer, azamiDenemeSayisi);
            DurumParametresiEkle(command, "bekliyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            DurumParametresiEkle(command, "hataKuyrugunda", AmbalajKaynakSenkronizasyonKuyrukDurumu.HataKuyrugunda);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SahiplikKaybi();

            var durum = DurumOku(reader, 0);
            var denemeSayisi = reader.GetInt32(1);
            return new AmbalajKaynakSenkronizasyonSonlandirmaSonucu(
                durum == AmbalajKaynakSenkronizasyonKuyrukDurumu.HataKuyrugunda
                    ? AmbalajKaynakSenkronizasyonSonlandirmaDurumu.HataKuyrugunaAlindi
                    : AmbalajKaynakSenkronizasyonSonlandirmaDurumu.YenidenKuyrugaAlindi,
                denemeSayisi);
        }, cancellationToken);
    }

    public async Task<AmbalajKaynakSenkronizasyonKuyrukIstatistigi> IstatistikleriGetirAsync(
        CancellationToken cancellationToken = default)
    {
        return await BaglantiIleCalisAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = IstatistikSql;
            AktifTransactioniBagla(command);
            DurumParametresiEkle(command, "bekliyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Bekliyor);
            DurumParametresiEkle(command, "isleniyor", AmbalajKaynakSenkronizasyonKuyrukDurumu.Isleniyor);
            DurumParametresiEkle(command, "tamamlandi", AmbalajKaynakSenkronizasyonKuyrukDurumu.Tamamlandi);
            DurumParametresiEkle(command, "hataKuyrugunda", AmbalajKaynakSenkronizasyonKuyrukDurumu.HataKuyrugunda);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return new AmbalajKaynakSenkronizasyonKuyrukIstatistigi(0, 0, 0, 0, 0, null, null);

            return new AmbalajKaynakSenkronizasyonKuyrukIstatistigi(
                SayacOku(reader, 0),
                SayacOku(reader, 1),
                SayacOku(reader, 2),
                SayacOku(reader, 3),
                SayacOku(reader, 4),
                NullableUtcTarihOku(reader, 5),
                NullableUtcTarihOku(reader, 6));
        }, cancellationToken);
    }

    private async Task<T> BaglantiIleCalisAsync<T>(
        Func<NpgsqlConnection, Task<T>> islem,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Ambalaj senkronizasyon kuyrugu PostgreSQL baglantisi gerektirir.");
        var baglantiyiBizActik = connection.State != ConnectionState.Open;
        if (baglantiyiBizActik)
            await connection.OpenAsync(cancellationToken);

        try
        {
            return await islem(connection);
        }
        finally
        {
            if (baglantiyiBizActik)
                await connection.CloseAsync();
        }
    }

    private void AktifTransactioniBagla(DbCommand command)
    {
        var transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        if (transaction != null)
            command.Transaction = transaction;
    }

    private static void IsKimligiParametreleriniEkle(
        DbCommand command,
        AmbalajKaynakSenkronizasyonKuyrukIsi isKaydi)
    {
        ParametreEkle(command, "projeId", NpgsqlDbType.Integer, isKaydi.ProjeId);
        ParametreEkle(command, "surum", NpgsqlDbType.Bigint, isKaydi.Surum);
        ParametreEkle(command, "kilitKimligi", NpgsqlDbType.Uuid, isKaydi.KilitKimligi);
    }

    private static void DurumParametresiEkle(
        DbCommand command,
        string ad,
        AmbalajKaynakSenkronizasyonKuyrukDurumu durum) =>
        ParametreEkle(command, ad, NpgsqlDbType.Smallint, (short)durum);

    private static void ParametreEkle(
        DbCommand command,
        string ad,
        NpgsqlDbType tip,
        object deger)
    {
        command.Parameters.Add(new NpgsqlParameter(ad, tip) { Value = deger });
    }

    private static AmbalajKaynakSenkronizasyonKuyrukDurumu DurumOku(DbDataReader reader, int ordinal) =>
        (AmbalajKaynakSenkronizasyonKuyrukDurumu)reader.GetInt16(ordinal);

    private static int SayacOku(DbDataReader reader, int ordinal) =>
        checked((int)reader.GetInt64(ordinal));

    private static DateTime? NullableUtcTarihOku(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : UtcTarihineCevir(reader.GetDateTime(ordinal));

    private static DateTime UtcTarihineCevir(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string HataMetniniSinirla(string? hata)
    {
        var value = string.IsNullOrWhiteSpace(hata)
            ? "Bilinmeyen senkronizasyon hatasi."
            : hata.Trim();
        return value.Length <= SonHataAzamiUzunlugu
            ? value
            : value[..SonHataAzamiUzunlugu];
    }

    private static AmbalajKaynakSenkronizasyonSonlandirmaSonucu SahiplikKaybi() =>
        new(AmbalajKaynakSenkronizasyonSonlandirmaDurumu.SahiplikKaybedildi, 0);
}
