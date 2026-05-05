using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class KaliteSurecKolonlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KaliteDurumId",
                table: "CekiSatirlari",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SurecDurumId",
                table: "CekiSatirlari",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LookupKaliteDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupKaliteDurumlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupSurecDurumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Anahtar = table.Column<int>(type: "integer", nullable: false),
                    Deger = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupSurecDurumlari", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(9977));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(317));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(318));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(319));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(319));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(320));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(321));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(321));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(322));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8077));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8078));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8079));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8080));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8080));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8081));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8082));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8082));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8083));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8084));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6168));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6170));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6171));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6171));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7107));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7108));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7109));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7109));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6565));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6567));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6568));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6568));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6569));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6569));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6570));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6571));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6580));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6581));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6582));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6582));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6583));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6583));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6769));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6770));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6771));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7675));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7677));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7678));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7679));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7679));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7683));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7684));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7684));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7685));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7686));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7686));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7687));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7687));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7688));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7689));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7698));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7699));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7700));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7700));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7701));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7702));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7702));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7703));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7703));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7704));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7705));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7705));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7706));

            migrationBuilder.InsertData(
                table: "LookupKaliteDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8240), "Onaylandı", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8241), "Tadilatta", null, null }
                });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1250));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1746));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1747));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1748));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1749));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(1749));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7920));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7921));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7922));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5716));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5718));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5719));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5719));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5959));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(5961));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7503));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7504));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7505));

            migrationBuilder.InsertData(
                table: "LookupSurecDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 1, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8396), "Ambar", null, null },
                    { 2, 2, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8397), "İmalat", null, null },
                    { 3, 3, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8398), "Tedarik", null, null },
                    { 4, 4, null, new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(8398), "Tedarik 3K Teslim", null, null }
                });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6936));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6937));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6938));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6939));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6940));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6941));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6941));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6942));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6943));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6943));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6944));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6944));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6352));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6369));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6370));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6370));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6371));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6372));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6372));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6373));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6374));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6374));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6375));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6376));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6376));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6377));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6377));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6378));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6379));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6379));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6380));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6380));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(6381));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7279));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7281));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 170, DateTimeKind.Utc).AddTicks(7282));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8935));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9830));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9955));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9957));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9833));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9834));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9836));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9837));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9838));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9839));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9958));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9959));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9960));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9962));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9963));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9964));

            migrationBuilder.InsertData(
                table: "MenuTanimlari",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Icon", "Kod", "LabelKey", "ParentId", "Route", "Sira", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 20, null, new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9965), "", "kalite-modulu", "MENU.KALITE_MODULU", 2, null, 6, null, null },
                    { 21, null, new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(9966), "", "surec-modulu", "MENU.SUREC_MODULU", 2, null, 7, null, null }
                });

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(363));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(779));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(780));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(781));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(781));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(783));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(783));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(784));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(784));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(785));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(785));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(786));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(786));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(786));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(788));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(7931));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8213));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8214));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8215));

            migrationBuilder.InsertData(
                table: "Roller",
                columns: new[] { "Id", "Ad", "CreatedBy", "CreatedDate", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 5, "Kalite", null, new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8216), null, null },
                    { 6, "Surec", null, new DateTime(2026, 5, 5, 21, 38, 5, 171, DateTimeKind.Utc).AddTicks(8216), null, null }
                });

            migrationBuilder.InsertData(
                table: "RolYetkileri",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "MenuTanimiId", "RolId", "UpdatedBy", "UpdatedDate", "YetkiTipiId" },
                values: new object[,]
                {
                    { 16, null, new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(787), 20, 1, null, null, 3 },
                    { 17, null, new DateTime(2026, 5, 5, 21, 38, 5, 172, DateTimeKind.Utc).AddTicks(787), 21, 1, null, null, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_KaliteDurumId",
                table: "CekiSatirlari",
                column: "KaliteDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_CekiSatirlari_SurecDurumId",
                table: "CekiSatirlari",
                column: "SurecDurumId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupKaliteDurumlari_Deger",
                table: "LookupKaliteDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupSurecDurumlari_Deger",
                table: "LookupSurecDurumlari",
                column: "Deger",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CekiSatirlari_LookupKaliteDurumlari_KaliteDurumId",
                table: "CekiSatirlari",
                column: "KaliteDurumId",
                principalTable: "LookupKaliteDurumlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CekiSatirlari_LookupSurecDurumlari_SurecDurumId",
                table: "CekiSatirlari",
                column: "SurecDurumId",
                principalTable: "LookupSurecDurumlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CekiSatirlari_LookupKaliteDurumlari_KaliteDurumId",
                table: "CekiSatirlari");

            migrationBuilder.DropForeignKey(
                name: "FK_CekiSatirlari_LookupSurecDurumlari_SurecDurumId",
                table: "CekiSatirlari");

            migrationBuilder.DropTable(
                name: "LookupKaliteDurumlari");

            migrationBuilder.DropTable(
                name: "LookupSurecDurumlari");

            migrationBuilder.DropIndex(
                name: "IX_CekiSatirlari_KaliteDurumId",
                table: "CekiSatirlari");

            migrationBuilder.DropIndex(
                name: "IX_CekiSatirlari_SurecDurumId",
                table: "CekiSatirlari");

            migrationBuilder.DeleteData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DropColumn(
                name: "KaliteDurumId",
                table: "CekiSatirlari");

            migrationBuilder.DropColumn(
                name: "SurecDurumId",
                table: "CekiSatirlari");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3149));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3426));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3427));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3428));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3428));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3429));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3430));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3430));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(3431));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1502));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1504));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1504));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1505));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1505));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1512));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1512));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1513));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1514));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1514));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9863));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9864));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9865));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9866));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(706));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(708));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(708));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(709));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(210));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(212));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(212));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(213));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(214));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(214));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(216));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(217));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(217));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(218));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(219));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(219));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(373));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(374));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(375));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1157));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1158));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1159));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1159));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1169));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1170));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1171));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1171));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1172));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1172));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1173));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1174));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1174));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1175));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1176));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1176));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1177));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1178));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1178));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1179));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1179));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1180));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1181));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1181));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1182));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1183));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1183));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1184));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1184));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1186));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1186));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5213));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5818));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5819));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5830));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5831));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(5832));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1355));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1356));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1356));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9457));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9459));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9460));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9461));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9678));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 199, DateTimeKind.Utc).AddTicks(9680));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1010));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1011));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(1012));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(529));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(530));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(540));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(541));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(542));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(543));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(549));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(551));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(551));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(552));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(553));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(553));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(554));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(26));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(27));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(28));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(28));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(29));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(30));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(30));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(31));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(32));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(32));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(33));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(34));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(34));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(46));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(47));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(48));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(48));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(49));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(49));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(50));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(51));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(864));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(865));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(866));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(9368));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(99));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(208));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(210));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(111));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(112));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(113));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(114));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(116));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(117));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(211));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(212));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(213));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(214));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(217));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(509));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(834));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(835));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(835));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(835));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(837));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(844));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(849));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(849));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(849));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 201, DateTimeKind.Utc).AddTicks(850));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(8689));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(8891));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(8892));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 5, 13, 52, 26, 200, DateTimeKind.Utc).AddTicks(8893));
        }
    }
}
