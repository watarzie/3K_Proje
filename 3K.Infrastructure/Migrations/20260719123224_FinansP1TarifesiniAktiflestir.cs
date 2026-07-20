using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinansP1TarifesiniAktiflestir : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "FinansUrunleri" AS urun
                SET "Aktif" = TRUE
                FROM "FinansUrunEslesmeleri" AS eslesme
                INNER JOIN "AmbalajIcSandikSablonlari" AS sablon
                    ON sablon."Id" = eslesme."IcSandikSablonId"
                WHERE eslesme."UrunId" = urun."Id"
                    AND eslesme."IsTuru" = 3
                    AND eslesme."Aktif" = TRUE
                    AND UPPER(TRIM(sablon."Ad")) = UPPER('Nem Alıcı Sandık P1');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
