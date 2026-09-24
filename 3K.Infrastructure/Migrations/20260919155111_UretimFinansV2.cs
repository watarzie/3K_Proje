using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <summary>Manuel temel şemaya artımlı geçiş; yalnız bu sürümün SQL'i uygulanır.</summary>
    public partial class UretimFinansV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $baseline$
                BEGIN
                    IF to_regclass('public."AmbalajUretimKayitlari"') IS NULL
                       OR to_regclass('public."FinansIsKayitlari"') IS NULL
                       OR to_regclass('public."FinansSiparisKalemleri"') IS NULL
                       OR to_regclass('public."FinansFaturaKalemleri"') IS NULL
                       OR to_regclass('public."FinansGiderleri"') IS NULL
                       OR to_regclass('public."MenuTanimlari"') IS NULL THEN
                        RAISE EXCEPTION 'Önce mevcut temel Ambalaj/Finans şema kurulumu tamamlanmalı. InitialCreate tek başına yeterli değildir. scripts/database/README.md belgesini inceleyin.';
                    END IF;
                END
                $baseline$;
                SET LOCAL lock_timeout = '10s';
                """);
            foreach (var name in new[]
            {
                "20260919_01_Sandik_Toplu_Tasima.sql",
                "20260919_02_Uretim_YasamDongusu.sql",
                "20260919_03_Finans_V2.sql",
                "20260919_04_Granular_Yetkiler.sql"
            })
            {
                using var stream = typeof(UretimFinansV2).Assembly
                    .GetManifestResourceStream($"_3K.Infrastructure.Migrations.Sql.{name}")
                    ?? throw new InvalidOperationException($"Migration kaynağı bulunamadı: {name}");
                using var reader = new StreamReader(stream);
                // DBA dosyaları kendi transaction'ını açar; EF dört adımı tek
                // transaction'da yönettiği için yalnız dış BEGIN/COMMIT kaldırılır.
                var lines = reader.ReadToEnd().Split('\n')
                    .Where(line => line.Trim() is not "BEGIN;" and not "COMMIT;");
                migrationBuilder.Sql(string.Join('\n', lines));
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Üretim formu, belge ve audit geçmişi silinemez. Geri dönüşü doğrulanmış yedekle ve scripts/database/README.md planıyla yapın.");
        }
    }
}
