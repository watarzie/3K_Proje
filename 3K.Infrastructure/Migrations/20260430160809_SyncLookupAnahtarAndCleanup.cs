using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _3K.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncLookupAnahtarAndCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 5);

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
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3882) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3884) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3885) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3885) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5009) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5011) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5011) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4319) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4321) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4322) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4322) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4323) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4324) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4324) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4325) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4325) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4326) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4327) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4327) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 13, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4328) });

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
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9263) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9696) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9697) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9698) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9699) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 16, 8, 9, 364, DateTimeKind.Utc).AddTicks(9700) });

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
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3449) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3452) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3452) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(3453) });

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
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5309) });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5310) });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5311) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4819) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4821) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4829) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4831) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4832) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4833) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4833) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4834) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4835) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4835) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4836) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4836) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 13, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4837) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4055) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4056) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4057) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4057) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4058) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4059) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4059) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4060) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4061) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4061) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4074) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4074) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 13, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4075) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 14, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4076) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 15, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4076) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 16, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4077) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 17, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4078) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 18, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4078) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 19, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4079) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 20, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4080) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 21, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(4080) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5162) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5164) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 16, 8, 9, 365, DateTimeKind.Utc).AddTicks(5164) });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(4838));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5141));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5142));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5143));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5143));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5144));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5145));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5145));

            migrationBuilder.UpdateData(
                table: "IslemOnayKurallari",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(5146));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3273));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3274));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3274));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3275));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3276));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3276));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3277));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3277));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3278));

            migrationBuilder.UpdateData(
                table: "LookupBirimler",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3279));

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1544) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1545) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1546) });

            migrationBuilder.UpdateData(
                table: "LookupDepoLokasyonlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1546) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2450) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2451) });

            migrationBuilder.UpdateData(
                table: "LookupGeriGonderilmeSebepleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2452) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1941) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1942) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1943) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1944) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1945) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1945) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1946) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1946) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1947) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1948) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1949) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1949) });

            migrationBuilder.UpdateData(
                table: "LookupGridDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1950) });

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2120));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2121));

            migrationBuilder.UpdateData(
                table: "LookupGridSevkDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2122));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2898));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2899));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2909));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2915));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2916));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2917));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2917));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2918));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2919));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2919));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2920));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2921));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2921));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2922));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2922));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2923));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2924));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2924));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2925));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2925));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2926));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2927));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2927));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2928));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 25,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2929));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 26,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2929));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 27,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2930));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 28,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2930));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 29,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2931));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 30,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2932));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 31,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2932));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 32,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2933));

            migrationBuilder.UpdateData(
                table: "LookupIslemTipleri",
                keyColumn: "Id",
                keyValue: 33,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2934));

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(6813) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(7308) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(7309) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(7310) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(7312) });

            migrationBuilder.UpdateData(
                table: "LookupProjeDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 15, 42, 59, 424, DateTimeKind.Utc).AddTicks(7313) });

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3115));

            migrationBuilder.UpdateData(
                table: "LookupProjeTipleri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(3116));

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1096) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1098) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1099) });

            migrationBuilder.UpdateData(
                table: "LookupSandikDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1099) });

            migrationBuilder.InsertData(
                table: "LookupSandikDurumlari",
                columns: new[] { "Id", "Anahtar", "CreatedBy", "CreatedDate", "Deger", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 5, 4, null, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1100), "Kapandı", null, null });

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1348));

            migrationBuilder.UpdateData(
                table: "LookupSandikTipleri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1349));

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2748) });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2749) });

            migrationBuilder.UpdateData(
                table: "LookupStokDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2750) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2286) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2288) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2288) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2289) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2289) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2290) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2291) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2291) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2292) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2293) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2293) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2294) });

            migrationBuilder.UpdateData(
                table: "LookupUcKDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2294) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1721) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1722) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1723) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 3, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1724) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 4, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1724) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 5, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1733) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 6, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1736) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 7, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1737) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 8, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1737) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 9, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1749) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 10, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1750) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 11, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1751) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 12, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1751) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 13, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1752) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 14, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1753) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 15, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1753) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 16, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1754) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 17, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1754) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 18, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1755) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 19, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1756) });

            migrationBuilder.UpdateData(
                table: "LookupUrunDurumlari",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 20, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(1756) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 0, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2602) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 1, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2603) });

            migrationBuilder.UpdateData(
                table: "LookupYetkiTipleri",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Anahtar", "CreatedDate" },
                values: new object[] { 2, new DateTime(2026, 4, 30, 15, 42, 59, 425, DateTimeKind.Utc).AddTicks(2604) });

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(1502));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2322));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2420));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2421));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2323));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2324));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2326));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2327));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2328));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2329));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2422));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2423));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2425));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2426));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2427));

            migrationBuilder.UpdateData(
                table: "MenuTanimlari",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2428));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(2776));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3128));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3129));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3130));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3130));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3132));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3132));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3133));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3133));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3134));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3134));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3135));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3135));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3135));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3136));

            migrationBuilder.UpdateData(
                table: "RolYetkileri",
                keyColumn: "Id",
                keyValue: 99,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(3136));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(690));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(940));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(941));

            migrationBuilder.UpdateData(
                table: "Roller",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 30, 15, 42, 59, 426, DateTimeKind.Utc).AddTicks(942));
        }
    }
}
