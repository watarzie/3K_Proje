using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUcumuliveFields_GeriGonderilme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeriGonderilmeSebebi",
                table: "CekiSatirlari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HataliMiktar",
                table: "CekiSatirlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KarsilananMiktar",
                table: "CekiSatirlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KaynakProjeId",
                table: "CekiSatirlari",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LookupGeriGonderilmeSebepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupGeriGonderilmeSebepleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupGeriGonderilmeSebepleri_Deger", x => x.Deger);
                });

            migrationBuilder.InsertData(
                table: "LookupGeriGonderilmeSebepleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Tadilat" },
                    { 2, 1, "Iptal" }
                });

            migrationBuilder.InsertData(
                table: "LookupUcKDurumlari",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[] { 13, 12, "GeriGonderildi" });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(7811));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8547));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8642));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8644));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8549));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8551));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8552));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8553));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8554));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8555));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8556));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8557));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8558));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8645));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8646));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(8849));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9189));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9190));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9190));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9191));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9192));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9193));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9193));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9193));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9194));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9195));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9196));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(9196));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(7330));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(7331));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 16, 20, 36, 25, 231, DateTimeKind.Utc).AddTicks(7332));

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_GeriGonderilmeSebebi",
                table: "CekiSatirlari",
                column: "GeriGonderilmeSebebi");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KaynakProjeId",
                table: "CekiSatirlari",
                column: "KaynakProjeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CekiSatirlari_LookupGeriGonderilmeSebepleri_GeriGonderilmeS~",
                table: "CekiSatirlari",
                column: "GeriGonderilmeSebebi",
                principalTable: "LookupGeriGonderilmeSebepleri",
                principalColumn: "Deger",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CekiSatirlari_Projeler_KaynakProjeId",
                table: "CekiSatirlari",
                column: "KaynakProjeId",
                principalTable: "Projeler",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CekiSatirlari_LookupGeriGonderilmeSebepleri_GeriGonderilmeS~",
                table: "CekiSatirlari");

            migrationBuilder.DropForeignKey(
                name: "FK_CekiSatirlari_Projeler_KaynakProjeId",
                table: "CekiSatirlari");

            migrationBuilder.DropTable(
                name: "LookupGeriGonderilmeSebepleri");

            migrationBuilder.DropIndex(
                name: "IX_CekiSatirlari_GeriGonderilmeSebebi",
                table: "CekiSatirlari");

            migrationBuilder.DropIndex(
                name: "IX_CekiSatirlari_KaynakProjeId",
                table: "CekiSatirlari");

            migrationBuilder.DeleteData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "GeriGonderilmeSebebi",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "HataliMiktar",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "KarsilananMiktar",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "KaynakProjeId",
                table: "CekiSatirlari");

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3080));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3843));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3935));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3844));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3846));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3847));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3848));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3849));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3850));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3851));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3852));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3853));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3938));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(3939));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4148));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4499));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4500));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4501));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4501));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4503));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4511));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4512));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4513));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4514));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(4515));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2258));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2590));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2591));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 22, 29, 50, 258, DateTimeKind.Utc).AddTicks(2592));
        }
    }
}
