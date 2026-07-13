using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using _3K.Infrastructure.Data;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260713142500_AddAmbalajUretimMenu")]
    public partial class AddAmbalajUretimMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "MenuTanimlari" ("Id", "CreatedDate", "Icon", "Kod", "LabelKey", "Route", "Sira")
                VALUES (46, TIMESTAMP '2026-07-13 14:25:00', 'ri-ruler-2-line', 'ambalaj-uretim-listesi', 'MENU.AMBALAJ_URETIM_LISTESI', '/ambalaj-uretim-listesi', 8);

                INSERT INTO "RolYetkileri" ("Id", "CreatedDate", "MenuTanimiId", "RolId", "YetkiTipiId")
                VALUES (42, TIMESTAMP '2026-07-13 14:25:00', 46, 1, 3);

                UPDATE "MenuTanimlari"
                SET "Sira" = CASE "Id" WHEN 10 THEN 9 WHEN 11 THEN 10 WHEN 12 THEN 11 WHEN 99 THEN 12 END
                WHERE "Id" IN (10, 11, 12, 99);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "RolYetkileri" WHERE "Id" = 42;
                DELETE FROM "MenuTanimlari" WHERE "Id" = 46;
                UPDATE "MenuTanimlari"
                SET "Sira" = CASE "Id" WHEN 10 THEN 8 WHEN 11 THEN 9 WHEN 12 THEN 10 WHEN 99 THEN 11 END
                WHERE "Id" IN (10, 11, 12, 99);
                """);
        }
    }
}