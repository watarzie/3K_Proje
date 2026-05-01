using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeriGonderilenMiktarToCekiSatiri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeriGonderilenMiktar",
                table: "CekiSatirlari",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1243));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1554));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1555));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1556));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1556));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1557));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1558));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1559));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(1559));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9680));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9681));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9682));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9683));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9684));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9684));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9685));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9686));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9687));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9687));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7995));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7997));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7998));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7999));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8879));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8880));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8881));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8882));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8381));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8382));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8383));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8384));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8384));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8385));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8386));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8386));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8387));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8388));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8388));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8389));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8390));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8546));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8547));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8548));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9329));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9330));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9339));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9340));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9341));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9342));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9342));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9343));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9344));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9345));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9345));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9346));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9347));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9347));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9348));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9349));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9350));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9350));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9351));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9352));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9352));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9359));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9361));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9362));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9363));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9363));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9364));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9365));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9366));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9366));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9367));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9368));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9368));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3393));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3835));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3837));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3838));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3839));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(3840));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9533));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9534));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9535));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7571));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7574));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7575));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7575));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7799));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(7801));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9180));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9181));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9182));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8712));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8713));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8714));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8714));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8715));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8716));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8717));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8717));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8718));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8719));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8719));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8720));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8721));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8170));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8171));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8172));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8173));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8174));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8174));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8175));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8176));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8177));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8177));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8189));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8189));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8190));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8191));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8192));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8192));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8193));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8194));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8195));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8195));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8196));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9034));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9036));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(9036));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(7554));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8326));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8439));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8440));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8328));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8329));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8330));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8331));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8333));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8342));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8442));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8443));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8445));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8446));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8447));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8449));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(8765));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9100));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9102));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9104));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9105));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9106));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9107));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9108));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9108));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(9109));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(6734));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(6947));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(6949));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 10, 48, 54, 721, DateTimeKind.Utc).AddTicks(6950));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeriGonderilenMiktar",
                table: "CekiSatirlari");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2360));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2634));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2635));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2636));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2637));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2638));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2638));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2639));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(2640));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(842));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(843));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(844));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(845));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(847));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(848));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(849));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9209));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9211));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9212));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9213));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(62));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(63));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(64));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(65));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9567));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9570));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9571));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9571));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9579));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9580));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9581));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9581));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9582));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9583));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9583));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9744));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9746));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9746));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(503));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(504));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(513));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(514));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(514));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(515));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(516));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(516));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(517));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(518));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(518));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(519));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(520));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(520));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(521));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(522));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(522));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(523));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(524));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(524));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(525));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(526));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(526));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(527));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(528));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(528));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(529));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(530));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(530));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(531));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(532));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(532));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(533));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(4806));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(5222));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(5224));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(5225));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(5225));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(5226));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(695));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(696));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(697));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(8809));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(8812));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(8812));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(8813));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9027));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9029));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(357));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(358));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(359));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9905));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9907));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9907));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9908));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9909));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9909));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9910));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9911));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9912));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9912));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9913));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9914));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9914));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9375));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9377));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9378));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9378));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9379));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9380));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9381));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9381));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9382));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9383));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9394));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9394));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9395));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9396));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9396));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9397));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9398));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9398));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9399));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9400));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 136, DateTimeKind.Utc).AddTicks(9401));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(214));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 137, DateTimeKind.Utc).AddTicks(216));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(1303));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2120));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2222));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2224));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2122));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2123));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2124));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2126));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2127));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2129));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2234));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2235));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2236));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2238));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2239));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2240));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2570));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2908));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2909));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2910));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2910));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2912));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2912));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2913));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2913));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2914));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2914));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2915));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2915));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2916));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2916));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(2916));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(497));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(747));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(748));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 1, 55, 17, 138, DateTimeKind.Utc).AddTicks(750));
        }
    }
}
