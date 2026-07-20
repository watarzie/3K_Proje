using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansSahaYedekSandikTipiTarifeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Boy",
                table: "FinansUrunEslesmeleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "En",
                table: "FinansUrunEslesmeleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SandikTipi",
                table: "FinansUrunEslesmeleri",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Yukseklik",
                table: "FinansUrunEslesmeleri",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Boy",
                table: "FinansIsKayitlari",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "En",
                table: "FinansIsKayitlari",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Yukseklik",
                table: "FinansIsKayitlari",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "FinansIsKayitlari" AS finans
                SET "Boy" = kaynak."Boy",
                    "En" = kaynak."En",
                    "Yukseklik" = kaynak."Yukseklik",
                    "SandikTipi" = kaynak."SandikTipi"
                FROM "AmbalajBagimsizSandiklar" AS kaynak
                WHERE finans."KaynakModul" = 'AmbalajBagimsizSandik'
                    AND finans."KaynakKayitId" = kaynak."Id";

                UPDATE "FinansIsKayitlari" AS finans
                SET "Boy" = kaynak."Boy",
                    "En" = kaynak."En",
                    "Yukseklik" = kaynak."Yukseklik",
                    "SandikTipi" = kaynak."SandikTipi"
                FROM "AmbalajUretimKalemleri" AS kaynak
                WHERE finans."KaynakModul" = 'AmbalajUretimKalemi'
                    AND finans."KaynakKayitId" = kaynak."Id";

                UPDATE "FinansUrunEslesmeleri"
                SET "Aktif" = FALSE
                WHERE "IsTuru" IN (4, 5)
                    AND "SandikTipi" IS NULL;

                WITH tarifeler("Kod", "Ad", "IsTuru", "SandikTipi", "BirimFiyat", "Sira") AS (
                    VALUES
                        ('SAHA-AHSAP-M3', 'Saha Sandığı · Ahşap Kapalı', 4, 'Ahşap Kapalı', 561.00::numeric, 40),
                        ('SAHA-KAFES-M3', 'Saha Sandığı · Kafes Sandık', 4, 'Kafes Sandık', 561.00::numeric, 41),
                        ('SAHA-KONTRPLAK-M3', 'Saha Sandığı · Kontrplak Sandık', 4, 'Kontrplak Sandık', 1000.00::numeric, 42),
                        ('YEDEK-AHSAP-M3', 'Yedek Sandık · Ahşap Kapalı', 5, 'Ahşap Kapalı', 561.00::numeric, 50),
                        ('YEDEK-KAFES-M3', 'Yedek Sandık · Kafes Sandık', 5, 'Kafes Sandık', 561.00::numeric, 51),
                        ('YEDEK-KONTRPLAK-M3', 'Yedek Sandık · Kontrplak Sandık', 5, 'Kontrplak Sandık', 1000.00::numeric, 52)
                )
                INSERT INTO "FinansUrunleri"
                    ("Kod", "Ad", "FiyatlandirmaBirimi", "BirimFiyat", "ParaBirimi", "KdvOrani", "Aktif", "Sira", "CreatedDate")
                SELECT t."Kod", t."Ad", 2, t."BirimFiyat", 'EUR', 20.00, TRUE, t."Sira", NOW()
                FROM tarifeler t
                WHERE NOT EXISTS (SELECT 1 FROM "FinansUrunleri" u WHERE u."Kod" = t."Kod");

                WITH tarifeler("Kod", "IsTuru", "SandikTipi") AS (
                    VALUES
                        ('SAHA-AHSAP-M3', 4, 'Ahşap Kapalı'),
                        ('SAHA-KAFES-M3', 4, 'Kafes Sandık'),
                        ('SAHA-KONTRPLAK-M3', 4, 'Kontrplak Sandık'),
                        ('YEDEK-AHSAP-M3', 5, 'Ahşap Kapalı'),
                        ('YEDEK-KAFES-M3', 5, 'Kafes Sandık'),
                        ('YEDEK-KONTRPLAK-M3', 5, 'Kontrplak Sandık')
                )
                INSERT INTO "FinansUrunEslesmeleri"
                    ("UrunId", "IsTuru", "SandikAdi", "SandikTipi", "Aktif", "CreatedDate")
                SELECT u."Id", t."IsTuru", NULL, t."SandikTipi", TRUE, NOW()
                FROM tarifeler t
                INNER JOIN "FinansUrunleri" u ON u."Kod" = t."Kod"
                WHERE NOT EXISTS (
                    SELECT 1 FROM "FinansUrunEslesmeleri" e
                    WHERE e."IsTuru" = t."IsTuru" AND e."SandikTipi" = t."SandikTipi"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "FinansUrunEslesmeleri"
                WHERE "UrunId" IN (
                    SELECT "Id" FROM "FinansUrunleri"
                    WHERE "Kod" IN ('SAHA-AHSAP-M3', 'SAHA-KAFES-M3', 'SAHA-KONTRPLAK-M3',
                        'YEDEK-AHSAP-M3', 'YEDEK-KAFES-M3', 'YEDEK-KONTRPLAK-M3')
                );

                DELETE FROM "FinansUrunleri"
                WHERE "Kod" IN ('SAHA-AHSAP-M3', 'SAHA-KAFES-M3', 'SAHA-KONTRPLAK-M3',
                    'YEDEK-AHSAP-M3', 'YEDEK-KAFES-M3', 'YEDEK-KONTRPLAK-M3')
                    AND NOT EXISTS (SELECT 1 FROM "FinansSiparisKalemleri" WHERE "UrunId" = "FinansUrunleri"."Id");

                UPDATE "FinansUrunEslesmeleri"
                SET "Aktif" = TRUE
                WHERE "IsTuru" IN (4, 5)
                    AND "SandikTipi" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Boy",
                table: "FinansUrunEslesmeleri");

            migrationBuilder.DropColumn(
                name: "En",
                table: "FinansUrunEslesmeleri");

            migrationBuilder.DropColumn(
                name: "SandikTipi",
                table: "FinansUrunEslesmeleri");

            migrationBuilder.DropColumn(
                name: "Yukseklik",
                table: "FinansUrunEslesmeleri");

            migrationBuilder.DropColumn(
                name: "Boy",
                table: "FinansIsKayitlari");

            migrationBuilder.DropColumn(
                name: "En",
                table: "FinansIsKayitlari");

            migrationBuilder.DropColumn(
                name: "Yukseklik",
                table: "FinansIsKayitlari");
        }
    }
}
