using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _3K.Infrastructure.Data;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260719111000_AddYuklemeSimulasyonuMenu")]
    public partial class AddYuklemeSimulasyonuMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "MenuTanimlari"
                SET "Sira" = "Sira" + 1
                WHERE "ParentId" IS NULL AND "Sira" >= 9;

                INSERT INTO "MenuTanimlari" ("Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "CreatedDate")
                SELECT 'ri-truck-line', 'yukleme-simulasyonu', 'MENU.YUKLEME_SIMULASYONU', NULL, '/yukleme-simulasyonu', 9, CURRENT_TIMESTAMP
                WHERE NOT EXISTS (
                    SELECT 1 FROM "MenuTanimlari" WHERE "Kod" = 'yukleme-simulasyonu'
                );

                INSERT INTO "RolYetkileri" ("MenuTanimiId", "RolId", "YetkiTipiId", "CreatedDate")
                SELECT menu."Id", 1, 3, CURRENT_TIMESTAMP
                FROM "MenuTanimlari" AS menu
                WHERE menu."Kod" = 'yukleme-simulasyonu'
                  AND NOT EXISTS (
                      SELECT 1 FROM "RolYetkileri" AS yetki
                      WHERE yetki."RolId" = 1 AND yetki."MenuTanimiId" = menu."Id"
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "RolYetkileri"
                WHERE "MenuTanimiId" IN (
                    SELECT "Id" FROM "MenuTanimlari" WHERE "Kod" = 'yukleme-simulasyonu'
                );
                DELETE FROM "MenuTanimlari" WHERE "Kod" = 'yukleme-simulasyonu';
                UPDATE "MenuTanimlari"
                SET "Sira" = "Sira" - 1
                WHERE "ParentId" IS NULL AND "Sira" > 9;
                """);
        }
    }
}