using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansAylikIsAkisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "FinansOzelIsleri"
                        WHERE LENGTH(TRIM("ParaBirimi")) > 3
                           OR ABS("BirimFiyat") >= 100000000000000::numeric
                           OR ABS("KdvOrani") >= 1000::numeric
                                    OR "BirimFiyat" <> ROUND("BirimFiyat", 4)
                                    OR "KdvOrani" <> ROUND("KdvOrani", 2)
                    ) OR EXISTS (
                        SELECT 1
                        FROM "FinansDuzenliIsleri"
                        WHERE LENGTH(TRIM("ParaBirimi")) > 3
                           OR ABS("BirimFiyat") >= 100000000000000::numeric
                           OR ABS("KdvOrani") >= 1000::numeric
                                    OR "BirimFiyat" <> ROUND("BirimFiyat", 4)
                                    OR "KdvOrani" <> ROUND("KdvOrani", 2)
                    ) THEN
                        RAISE EXCEPTION 'Finans tarife verileri yeni para birimi veya sayısal alan sınırlarına uymuyor.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ParaBirimi",
                table: "FinansOzelIsleri",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "KdvOrani",
                table: "FinansOzelIsleri",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansOzelIsleri",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "HesaplamaYontemi",
                table: "FinansOzelIsleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RaporGrubu",
                table: "FinansOzelIsleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ParaBirimi",
                table: "FinansDuzenliIsleri",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "KdvOrani",
                table: "FinansDuzenliIsleri",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansDuzenliIsleri",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "HesaplamaYontemi",
                table: "FinansDuzenliIsleri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RaporGrubu",
                table: "FinansDuzenliIsleri",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "FinansDuzenliIsleri"
                SET "HesaplamaYontemi" = CASE
                        WHEN "IsTuru" ILIKE '%kira%' OR "IsAdi" ILIKE '%kira%' THEN 1
                        WHEN "IsTuru" ILIKE '%sevkiyat%' OR "IsAdi" ILIKE '%sevkiyat%' THEN 2
                        WHEN "IsTuru" ILIKE '%skit%' OR "IsAdi" ILIKE '%skit%'
                          OR "IsTuru" ILIKE '%haliade-x%' OR "IsAdi" ILIKE '%haliade-x%' THEN 3
                        ELSE 3
                    END,
                    "RaporGrubu" = CASE
                        WHEN "IsTuru" ILIKE '%kira%' OR "IsAdi" ILIKE '%kira%' THEN 'Kira'
                        WHEN "IsTuru" ILIKE '%sevkiyat%' OR "IsAdi" ILIKE '%sevkiyat%' THEN 'Sevkiyat'
                        WHEN "IsTuru" ILIKE '%skit%' OR "IsAdi" ILIKE '%skit%' THEN 'SKIT'
                        WHEN "IsTuru" ILIKE '%haliade-x%' OR "IsAdi" ILIKE '%haliade-x%' THEN 'Haliade-X'
                        ELSE COALESCE(NULLIF(TRIM("IsTuru"), ''), 'Özel İş')
                    END
                WHERE "HesaplamaYontemi" = 0 OR TRIM("RaporGrubu") = '';

                UPDATE "FinansOzelIsleri" AS o
                SET "HesaplamaYontemi" = CASE
                        WHEN o."HesaplamaYontemi" = 0 THEN d."HesaplamaYontemi"
                        ELSE o."HesaplamaYontemi"
                    END,
                    "RaporGrubu" = CASE
                        WHEN TRIM(o."RaporGrubu") = '' THEN d."RaporGrubu"
                        ELSE o."RaporGrubu"
                    END
                FROM "FinansDuzenliIsleri" AS d
                WHERE o."DuzenliIsId" = d."Id"
                  AND (o."HesaplamaYontemi" = 0 OR TRIM(o."RaporGrubu") = '');

                UPDATE "FinansOzelIsleri"
                SET "HesaplamaYontemi" = CASE WHEN "HesaplamaYontemi" = 0 THEN 3 ELSE "HesaplamaYontemi" END,
                    "RaporGrubu" = CASE WHEN TRIM("RaporGrubu") = '' THEN 'Özel İş' ELSE "RaporGrubu" END
                WHERE "HesaplamaYontemi" = 0 OR TRIM("RaporGrubu") = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HesaplamaYontemi",
                table: "FinansOzelIsleri");

            migrationBuilder.DropColumn(
                name: "RaporGrubu",
                table: "FinansOzelIsleri");

            migrationBuilder.DropColumn(
                name: "HesaplamaYontemi",
                table: "FinansDuzenliIsleri");

            migrationBuilder.DropColumn(
                name: "RaporGrubu",
                table: "FinansDuzenliIsleri");

            migrationBuilder.AlterColumn<string>(
                name: "ParaBirimi",
                table: "FinansOzelIsleri",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "KdvOrani",
                table: "FinansOzelIsleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansOzelIsleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<string>(
                name: "ParaBirimi",
                table: "FinansDuzenliIsleri",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<decimal>(
                name: "KdvOrani",
                table: "FinansDuzenliIsleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BirimFiyat",
                table: "FinansDuzenliIsleri",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4);
        }
    }
}
