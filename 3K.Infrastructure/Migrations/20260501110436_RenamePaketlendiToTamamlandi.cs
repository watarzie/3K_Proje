using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaketlendiToTamamlandi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(6720));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7110));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7111));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7112));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7112));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7113));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7114));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7115));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(7115));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4694));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4695));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4696));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4697));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4697));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4698));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4699));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4700));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4700));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4701));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(793));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(795));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(796));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3824));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3825));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3826));

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3826));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3108));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3109));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3110));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3111));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3111));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3112));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3115));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3115));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3116));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3127));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3128));

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3129));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3399));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3401));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3402));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4348));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4349));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4350));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4351));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4351));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4352));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4353));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4354));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4354));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4355));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4356));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4356));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4357));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4358));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4358));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4359));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4360));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4360));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4361));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4362));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4363));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4363));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4364));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4365));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4365));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4366));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4367));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4367));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4368));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4369));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4369));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4370));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4371));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6476));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6899));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6901));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6902));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6903));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 102, DateTimeKind.Utc).AddTicks(6903));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4543));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4544));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4545));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(376));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(378));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(379));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(380));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(605));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(606));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4186));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4187));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4188));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3624));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3625));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3626));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3627));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3627), "Tamamlandı" });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3628));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3629));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3629));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3630));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3631));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3631));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3632));

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(3633));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2661));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2665));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2666));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2685));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2686));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2687));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2688));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2688));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2689));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2690));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2691));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2691));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2692));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2693));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2694));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2695));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2695));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2696));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2697));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2698));

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(2698));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4012));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4013));

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 103, DateTimeKind.Utc).AddTicks(4014));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3101));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3839));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3932));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3933));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3841));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3843));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3845));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3846));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3847));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3848));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3934));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3936));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3937));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3938));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3940));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(3941));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4238));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4556));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4557));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4558));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4558));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4561));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4561));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4562));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4562));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4563));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4563));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4563));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4564));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4564));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4564));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(4565));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(2422));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(2627));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(2628));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 5, 1, 11, 4, 36, 104, DateTimeKind.Utc).AddTicks(2629));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "CreatedDate", "Deger" },
                values: new object[] { new DateTime(2026, 5, 1, 10, 48, 54, 720, DateTimeKind.Utc).AddTicks(8715), "Paketlendi" });

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
    }
}
