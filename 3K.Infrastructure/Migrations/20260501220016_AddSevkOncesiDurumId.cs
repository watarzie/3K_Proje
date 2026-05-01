using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSevkOncesiDurumId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SevkOncesiDurumId",
                table: "Sandiklar",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6520));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6815));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6815));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6816));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6817));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6817));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6818));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6819));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(6819));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4916));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4917));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4918));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4918));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4919));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4920));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4920));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4921));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4922));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4922));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3136));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3138));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3138));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3139));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4085));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4086));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4087));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4087));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3550));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3552));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3552));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3553));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3554));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3554));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3555));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3556));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3556));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3557));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3557));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3558));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3559));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3559));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3732));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3733));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3743));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4543));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4553));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4554));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4554));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4555));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4556));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4561));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4562));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4563));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4563));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4564));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4565));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4565));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4566));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4566));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4567));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4568));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4568));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4569));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4570));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4570));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4571));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4571));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4572));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4573));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4573));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4574));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4575));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4575));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4576));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4576));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4577));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4578));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8044));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8535));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8536));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8537));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 613, DateTimeKind.Utc).AddTicks(8539));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4748));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4749));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4750));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2652));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2655));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2655));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2656));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2925));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(2927));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4382));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4384));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4384));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3911));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3912));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3913));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3914));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3914));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3915));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3916));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3916));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3917));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3918));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3918));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3919));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3919));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3328));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3329));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3330));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3331));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3331));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3332));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3333));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3333));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3334));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3335));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3347));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3355));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3356));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3357));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3358));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3358));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3359));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3359));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3360));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3361));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(3361));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4239));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4240));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 614, DateTimeKind.Utc).AddTicks(4241));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(2869));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3659));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3749));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3751));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3661));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3662));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3663));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3664));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3665));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3666));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3752));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3753));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3754));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3756));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3757));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(3758));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4093));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4456));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4457));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4457));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4458));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4459));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4460));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4460));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4460));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4461));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4462));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4462));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4462));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4463));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4463));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(4463));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(2165));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(2381));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(2382));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 22, 0, 15, 615, DateTimeKind.Utc).AddTicks(2383));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SevkOncesiDurumId",
                table: "Sandiklar");

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3008));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3345));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3346));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3347));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3348));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3348));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3349));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3349));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(3350));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1413));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1414));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1415));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1416));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1416));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1417));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1418));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1418));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1426));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9692));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9694));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9694));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9695));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(599));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(600));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(600));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(601));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(83));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(85));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(85));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(86));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(87));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(87));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(88));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(89));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(89));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(90));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(91));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(91));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(92));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(93));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(258));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(259));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(259));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1046));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1056));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1057));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1057));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1058));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1059));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1059));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1060));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1061));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1061));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1062));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1062));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1063));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1064));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1064));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1065));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1066));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1066));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1067));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1068));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1068));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1069));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1069));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1070));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1071));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1072));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1073));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1073));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1074));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1075));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1076));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5015));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5517));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5519));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5531));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(5532));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1260));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1261));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(1262));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9230));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9234));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9235));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9236));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9494));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9495));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(900));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(901));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(901));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(423));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(424));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(424));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(425));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(426));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(426));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(427));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(428));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(428));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(429));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(436));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(437));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(438));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9875));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9877));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9877));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9878));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9879));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9879));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9880));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9881));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9881));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9882));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9893));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9894));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9894));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9895));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9896));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9896));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9897));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9898));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9898));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9899));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 184, DateTimeKind.Utc).AddTicks(9900));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(750));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(751));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(752));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(1096));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2098));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2213));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2215));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2100));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2101));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2102));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2103));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2104));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2105));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2216));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2217));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2218));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2220));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2221));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2222));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2584));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2955));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2956));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2957));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2957));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2958));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2959));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2959));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2969));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2970));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2970));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(2972));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 185, DateTimeKind.Utc).AddTicks(9577));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(152));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(154));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 21, 27, 43, 186, DateTimeKind.Utc).AddTicks(155));
        }
    }
}
