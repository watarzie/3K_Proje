using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameUcKDurumTamGeldiEksikGeldi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4455));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4767));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4768));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4769));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4770));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4770));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4771));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4771));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(4772));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2670));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2671));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2672));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2672));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2673));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2673));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2674));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2675));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2675));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2676));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(763));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(764));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(764));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(765));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1758));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1760));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1760));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1181));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1183));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1183));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1184));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1186));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1188));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1189));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1189));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1190));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1401));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1403));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1403));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2274));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2275));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2276));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2291));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2292));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2293));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2293));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2294));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2295));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2295));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2296));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2296));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2297));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2298));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2298));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2299));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2300));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2300));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2301));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2301));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2302));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2303));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2303));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2304));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2304));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2305));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2306));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2306));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2307));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2308));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2308));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2309));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2310));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5421));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5882));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5883));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5884));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5885));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 409, DateTimeKind.Utc).AddTicks(5885));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2499));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2500));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2501));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(269));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(272));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(273));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(273));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(548));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(549));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2105));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2106));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(2107));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1575));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1576), "Sevk Adeti Tam Geldi" });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1577), "Sevk Adeti Eksik Geldi" });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1578));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1578));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1579));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1580));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1580));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1581));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1582));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1582));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1583));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1583));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(962));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(963));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(964));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(965));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(965));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(966));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(966));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(967));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(968));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(968));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(980));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(981));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(982));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(982));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(983));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(984));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(984));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(985));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(986));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(986));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(987));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1925));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1926));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 410, DateTimeKind.Utc).AddTicks(1927));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(2973));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4521));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4761));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4763));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4546));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4548));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4549));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4550));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4551));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4552));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4764));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4765));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4766));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4767));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4768));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(4769));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5317));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5936));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5937));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5938));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5938));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5941));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5941));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5942));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5943));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5944));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5944));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5959));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(5959));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(1032));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(1706));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(1707));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 42, 26, 411, DateTimeKind.Utc).AddTicks(1708));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7351));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7640));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7640));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7641));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7642));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7642));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7643));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7644));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(7644));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5801));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5802));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5808));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5810));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5811));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5812));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5812));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5813));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5813));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5814));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3882));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3884));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3885));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3885));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5009));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5011));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5011));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4319));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4321));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4322));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4322));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4323));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4324));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4324));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4325));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4325));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4326));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4327));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4327));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4328));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4583));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4585));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4586));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5455));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5456));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5457));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5468));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5469));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5469));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5470));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5471));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5471));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5472));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5473));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5473));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5474));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5474));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5475));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5476));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5476));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5477));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5478));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5478));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5479));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5479));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5480));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5481));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5481));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5482));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5483));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5483));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5484));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5484));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5485));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5486));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5486));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9263));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9696));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9697));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9698));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9699));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9700));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5652));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5653));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5654));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3449));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3452));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3452));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3453));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3679));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3681));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5309));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5310));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5311));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4819));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4821), "Tam Geldi" });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4829), "Eksik Geldi" });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4831));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4832));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4833));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4833));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4834));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4835));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4835));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4836));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4836));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4837));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4055));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4056));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4057));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4057));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4058));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4059));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4059));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4060));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4061));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4061));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4074));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4074));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4075));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4076));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4076));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4077));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4078));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4078));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4079));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4080));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4080));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5162));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5164));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5164));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4116));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4887));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4985));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4986));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4899));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4900));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4902));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4903));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4904));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4987));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4989));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4990));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4991));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4992));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(4993));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5291));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5617));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5618));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5619));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5627));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5629));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5630));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5630));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5631));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5632));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5632));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5632));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5633));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5633));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5633));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(5634));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(3201));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(3537));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(3538));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 16, 8, 9, 366, DateTimeKind.Utc).AddTicks(3539));
        }
    }
}
