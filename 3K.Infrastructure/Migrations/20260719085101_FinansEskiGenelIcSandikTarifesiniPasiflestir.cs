using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansEskiGenelIcSandikTarifesiniPasiflestir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("""
                                UPDATE "FinansUrunleri" AS urun
                                SET "Aktif" = FALSE
                                WHERE EXISTS (
                                        SELECT 1
                                        FROM "FinansUrunEslesmeleri" AS eslesme
                                        WHERE eslesme."UrunId" = urun."Id"
                                            AND eslesme."IsTuru" = 3
                                            AND eslesme."IcSandikSablonId" IS NULL
                                );

                                UPDATE "FinansUrunEslesmeleri"
                                SET "Aktif" = FALSE
                                WHERE "IsTuru" = 3
                                    AND "IcSandikSablonId" IS NULL;
                                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
                        migrationBuilder.Sql("""
                                UPDATE "FinansUrunEslesmeleri"
                                SET "Aktif" = TRUE
                                WHERE "IsTuru" = 3
                                    AND "IcSandikSablonId" IS NULL;

                                UPDATE "FinansUrunleri" AS urun
                                SET "Aktif" = TRUE
                                WHERE EXISTS (
                                        SELECT 1
                                        FROM "FinansUrunEslesmeleri" AS eslesme
                                        WHERE eslesme."UrunId" = urun."Id"
                                            AND eslesme."IsTuru" = 3
                                            AND eslesme."IcSandikSablonId" IS NULL
                                );
                                """);
        }
    }
}
