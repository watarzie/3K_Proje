using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKaynakProjeNoToSandikIcerik : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KaynakProjeNo",
                table: "SandikIcerikleri",
                type: "text",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KaynakProjeNo",
                table: "SandikIcerikleri");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(147));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(425));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(426));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(427));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(428));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(428));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(429));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(430));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(430));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8563));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8564));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8565));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8565));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8566));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8567));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8567));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8568));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8569));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8570));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6772));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6773));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6774));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6774));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7681));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7683));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7148));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7150));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7150));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7151));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7152));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7152));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7153));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7153));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7154));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7155));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7156));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7156));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7157));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7343));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7344));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7345));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8198));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8205));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8216));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8217));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8218));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8218));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8219));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8220));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8220));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8221));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8221));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8222));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8223));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8223));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8224));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8225));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8225));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8226));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8226));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8227));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8228));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8228));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8229));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8230));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8230));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8231));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8231));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8232));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8233));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8233));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8234));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8234));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8235));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2137));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2575));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2577));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2578));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2579));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(2579));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8408));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8410));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6332));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6334));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6335));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6335));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6581));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6583));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8018));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8019));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(8020));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7512));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7513));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7514));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7515));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7515));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7516));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7517));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7517));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7518));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7519));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7519));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7520));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6943));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6945));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6945));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6946));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6947));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6955));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6957));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6957));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6958));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6959));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6969));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6970));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6971));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6971));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6972));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6973));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6973));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6974));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6975));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6975));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(6976));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7855));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7857));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 714, DateTimeKind.Utc).AddTicks(7857));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(6398));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7166));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7262));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7263));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7167));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7169));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7170));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7171));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7172));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7173));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7264));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7265));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7266));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7267));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7269));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7270));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7598));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7935));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7935));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7936));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7936));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7937));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7938));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7938));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7938));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7939));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7940));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7941));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7941));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(7941));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(5666));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(5878));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(5880));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 21, 12, 22, 715, DateTimeKind.Utc).AddTicks(5880));
        }
    }
}
