using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RbacEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_LookupKullaniciRolleri_Rol",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_Rol",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Kullanicilar",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "MenuTanimlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kod = table.Column<string>(type: "text", nullable: false),
                    LabelKey = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<string>(type: "text", nullable: true),
                    Sira = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuTanimlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuTanimlari_MenuTanimlari_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ad = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolYetkileri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    MenuTanimiId = table.Column<int>(type: "integer", nullable: false),
                    YetkiTipi = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolYetkileri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_MenuTanimlari_MenuTanimiId",
                        column: x => x.MenuTanimiId,
                        principalTable: "MenuTanimlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolYetkileri_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7045), "ri-dashboard-line", "dashboard", "MENU.DASHBOARD", null, "/dashboard", 1, null, null },
                    { 2, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7836), "ri-folder-line", "projeler", "MENU.PROJELER", null, null, 2, null, null },
                    { 5, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7837), "ri-archive-line", "sandik-yonetimi", "MENU.SANDIK_YONETIMI", null, "/sandik-yonetimi", 3, null, null },
                    { 6, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7847), "ri-error-warning-line", "eksik-listesi", "MENU.EKSIK_LISTESI", null, "/eksik-listesi", 4, null, null },
                    { 7, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7848), "ri-building-2-line", "depo-durumu", "MENU.DEPO_DURUMU", null, "/depo-durumu", 5, null, null },
                    { 8, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7849), "ri-arrow-left-right-line", "fb-transfer", "MENU.FB_TRANSFER", null, "/fb-transfer", 6, null, null },
                    { 9, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7850), "ri-stack-line", "stok", "MENU.STOK_MODULU", null, "/stok", 7, null, null },
                    { 10, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7851), "ri-tools-line", "saha-malzeme", "MENU.SAHA_MALZEMESI", null, "/saha-malzeme", 8, null, null },
                    { 11, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7852), "ri-history-line", "hareket-gecmisi", "MENU.HAREKET_GECMISI", null, "/hareket-gecmisi", 9, null, null },
                    { 12, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7853), "ri-user-settings-line", "kullanicilar", "MENU.KULLANICI_YETKI", null, "/kullanicilar", 10, null, null },
                    { 13, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7855), "ri-shield-user-line", "rol-yonetimi", "MENU.ROL_YONETIMI", null, "/rol-yonetimi", 11, null, null }
                });

            migrationBuilder.InsertData(
                table: "Roller",
                columns: new[] { "Id", "Ad", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "Admin", null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6236), null, null },
                    { 2, "Personel3K", null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6571), null, null },
                    { 3, "PersonelGrid", null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6573), null, null },
                    { 4, "Yonetici", null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(6573), null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 3, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7937), "", "aktif-projeler", "MENU.AKTIF_PROJELER", 2, "/projeler", 1, null, null },
                    { 4, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(7939), "", "sevk-edilen", "MENU.SEVK_EDILEN", 2, "/projeler/sevk-edilen", 2, null, null }
                });

            migrationBuilder.InsertData(
                table: "RolYetkileri",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "MenuTanimiId", "RolId", "UpdatedBy", "UpdatedDate", "YetkiTipi" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8178), 1, 1, null, null, "W" },
                    { 2, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8535), 2, 1, null, null, "W" },
                    { 5, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8536), 5, 1, null, null, "W" },
                    { 6, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8538), 6, 1, null, null, "W" },
                    { 7, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8538), 7, 1, null, null, "W" },
                    { 8, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8539), 8, 1, null, null, "W" },
                    { 9, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8539), 9, 1, null, null, "W" },
                    { 10, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540), 10, 1, null, null, "W" },
                    { 11, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540), 11, 1, null, null, "W" },
                    { 12, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8540), 12, 1, null, null, "W" },
                    { 13, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8541), 13, 1, null, null, "W" },
                    { 3, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8535), 3, 1, null, null, "W" },
                    { 4, null, new DateTime(2026, 4, 15, 16, 23, 12, 175, DateTimeKind.Utc).AddTicks(8536), 4, 1, null, null, "W" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuTanimlari_Kod",
                table: "MenuTanimlari",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuTanimlari_ParentId",
                table: "MenuTanimlari",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_MenuTanimiId",
                table: "RolYetkileri",
                column: "MenuTanimiId");

            migrationBuilder.CreateIndex(
                name: "IX_RolYetkileri_RolId_MenuTanimiId",
                table: "RolYetkileri",
                columns: new[] { "RolId", "MenuTanimiId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Roller_RolId",
                table: "Kullanicilar",
                column: "RolId",
                principalTable: "Roller",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Roller_RolId",
                table: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "RolYetkileri");

            migrationBuilder.DropTable(
                name: "MenuTanimlari");

            migrationBuilder.DropTable(
                name: "Roller");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Kullanicilar",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Rol",
                table: "Kullanicilar",
                column: "Rol");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_LookupKullaniciRolleri_Rol",
                table: "Kullanicilar",
                column: "Rol",
                principalTable: "LookupKullaniciRolleri",
                principalColumn: "Deger",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
