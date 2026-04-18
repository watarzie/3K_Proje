using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class YetkiTipiLookupVeKullaniciRolDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LookupKullaniciRolleri");

            migrationBuilder.CreateTable(
                name: "LookupYetkiTipleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupYetkiTipleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupYetkiTipleri_Deger", x => x.Deger);
                });

            migrationBuilder.InsertData(
                table: "LookupYetkiTipleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "N" },
                    { 2, 1, "R" },
                    { 3, 2, "W" }
                });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2704));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3458));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3544));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3545));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3459));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3460));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3462));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3463));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3464));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3465));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3466));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3467));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3468));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(3754));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4103));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4103));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4106));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(4108));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(1859));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2238));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2239));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 37, 4, 445, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_YetkiTipi",
                table: "RolYetkileri",
                column: "YetkiTipi");

            migrationBuilder.AddForeignKey(
                name: "FK_RolYetkileri_LookupYetkiTipleri_YetkiTipi",
                table: "RolYetkileri",
                column: "YetkiTipi",
                principalTable: "LookupYetkiTipleri",
                principalColumn: "Deger",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolYetkileri_LookupYetkiTipleri_YetkiTipi",
                table: "RolYetkileri");

            migrationBuilder.DropTable(
                name: "LookupYetkiTipleri");

            migrationBuilder.DropIndex(
                name: "IX_RolYetkileri_YetkiTipi",
                table: "RolYetkileri");

            migrationBuilder.CreateTable(
                name: "LookupKullaniciRolleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupKullaniciRolleri", x => x.Id);
                    table.UniqueConstraint("AK_LookupKullaniciRolleri_Deger", x => x.Deger);
                });

            migrationBuilder.InsertData(
                table: "LookupKullaniciRolleri",
                columns: new[] { "Id", "Anahtar", "Deger" },
                values: new object[,]
                {
                    { 1, 0, "Admin" },
                    { 2, 1, "Personel3K" },
                    { 3, 2, "PersonelGrid" },
                    { 4, 3, "Yonetici" }
                });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7045));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7836));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7939));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7837));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7847));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7848));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7849));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7850));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7851));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7852));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7853));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7855));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8178));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8535));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8535));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8536));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8536));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8539));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8539));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8541));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6236));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6571));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6573));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6573));
        }
    }
}
